// Services/PaymentService.cs
using GMS.TifoXRCoreWebAPI.Middleware;
using GMS.TifoXRCoreWebAPI.Models;
using GMS.TifoXRCoreWebAPI.Repositories.Interfaces;
using TifoXRCoreWebAPI.Application.PaymentGateways;
using TifoXRCoreWebAPI.Services;

namespace GMS.TifoXRCoreWebAPI.Services
{
    /// <summary>
    /// Orchestrates payment gateway calls and persists results through IOrderRepository helpers.
    /// </summary>
    public sealed class PaymentService(IPaymentGateway gateway, IOrderRepository orders) : IPaymentService
    {
        // Status constants (align with your lookup tables)
        private const int StatusRequiresAction = 1; // e.g., "requires_action"
        private const int StatusSucceeded = 3; // e.g., "succeeded"
        private const int OrderStatusPaid = 3; // e.g., "Paid"

        private readonly IPaymentGateway _gateway = gateway;
        private readonly IOrderRepository _orders = orders;

        public async Task<CreatePaymentIntentResponse> CreateIntentAsync(
            int spaceId, string orderId, CreatePaymentIntentRequest req)
        {
            if (req is null) throw new ArgumentNullException(nameof(req));
            
            if (string.IsNullOrWhiteSpace(req.IdempotencyKey))
                throw new ArgumentException("IdempotencyKey is required.", nameof(req.IdempotencyKey));
            
            if (_gateway == null)
                throw new ArgumentException("Gateway is required.", nameof(req.Gateway));

            // 1) Load order context (minimal header)
            var header = await _orders.GetOrderHeaderAsync(orderId); // space, currency, total net
            
            if (header is null || header.Value.SpaceId != spaceId)
                throw new InvalidOperationException("Order not found in this space.");

            var amountMinor = req.AmountMinor ?? header.Value.TotalNetMinor;
            var amountMajor = amountMinor / 100m;       //FIX ME: Check what is 100m

            // 2) Idempotency — return existing intent if already created
            var existing = await _orders.FindPaymentIntentByIdempotencyAsync(orderId, req.IdempotencyKey);

            if (existing is not null)
            {
                // Optionally refresh approval link via gateway (useful if link expired/rotated)
                string? approve = null;

                if (!string.IsNullOrWhiteSpace(existing.Value.ProviderIntentId))
                {
                    var st = await _gateway.GetIntentAsync(existing.Value.ProviderIntentId!);
                    approve = st?.ApproveLink;
                }

                return new CreatePaymentIntentResponse
                {
                    PaymentIntentId = existing.Value.Id,
                    PaymentGatewayId = req.Gateway,
                    StatusId = StatusRequiresAction,
                    ProviderIntentId = existing.Value.ProviderIntentId,
                    ApproveLink = approve
                };
            }

            // 3) Create intent on gateway
            var created = await _gateway.CreateIntentAsync(new CreateGatewayIntentRequest(
                IdempotencyKey: req.IdempotencyKey,
                CurrencyId: header.Value.CurrencyId,
                Amount: amountMajor, // major units
                ReturnUrl: $"https://your.api/return/{req.Gateway}?orderId={orderId}",  //FIX ME: Add mock return url if required
                CancelUrl: $"https://your.app/cancel"));

            if (string.IsNullOrWhiteSpace(created.ProviderIntentId))
                throw new InvalidOperationException("Gateway did not return a provider intent id.");

            // 4) Persist the intent
            var gatewayId = req.Gateway;

            var piId = await _orders.InsertPaymentIntentAsync(
                orderId: orderId,
                gatewayId: gatewayId,
                statusId: StatusRequiresAction,
                amountMinor: amountMinor,
                currencyId: header.Value.CurrencyId,
                providerIntentId: created.ProviderIntentId!,
                idempotencyKey: req.IdempotencyKey);

            return new CreatePaymentIntentResponse
            {
                PaymentIntentId = piId,
                PaymentGatewayId = gatewayId,
                StatusId = StatusRequiresAction,
                ProviderIntentId = created.ProviderIntentId,
                ApproveLink = created.ApproveLink
            };
        }

        public async Task<ConfirmPaymentIntentResponse> CaptureAsync(
            int spaceId, string orderId, string intentId, ConfirmPaymentIntentRequest req)
        {
            if (req is null) throw new ArgumentNullException(nameof(req));
            
            if (string.IsNullOrWhiteSpace(req.IdempotencyKey))
                throw new ArgumentException("IdempotencyKey is required.", nameof(req.IdempotencyKey));

            // 1) Intent + Order context
            var ctx = await _orders.GetIntentContextAsync(intentId);
            
            if (ctx is null || ctx.Value.OrderId != orderId || ctx.Value.SpaceId != spaceId)
                throw new InvalidOperationException("payment_intent not found for this order/space.");

            var providerIntentId = ctx.Value.ProviderIntentId ?? req.ProviderIntentId
                ?? throw new InvalidOperationException("provider_intent_id missing.");

            // NOTE: If you later store gateway_id on the intent, resolve the gateway from the repo/context.
            // For now, we only support PayPal here.

            var status = await _gateway.GetIntentAsync(providerIntentId);

            if (status is null || !string.Equals(status.Status, "APPROVED", StringComparison.OrdinalIgnoreCase))
            {
                // throw a user-meaningful exception the controller can map to 409/400
                throw new InvalidOperationException(
                    GlobalException.FormatExceptionMessage(
                    "PAYMENT_REQUIRES_APPROVAL",
                    nameof(CaptureAsync)));
            }

            // 2) Capture
            var cap = await _gateway.CaptureAsync(new CaptureGatewayRequest(
                IdempotencyKey: req.IdempotencyKey,
                ProviderIntentId: providerIntentId));

            var amountMinor = (long)decimal.Round(cap.CapturedAmount * 100m, 0, MidpointRounding.AwayFromZero);

            // 3) Persist charge + transitions
            var chargeId = await _orders.InsertChargeAsync(
                intentId: intentId,
                statusId: StatusSucceeded,
                amountCapturedMinor: amountMinor,
                currencyId: ctx.Value.CurrencyId,
                providerChargeId: cap.ProviderChargeId,
                paidAtUtc: DateTime.UtcNow,
                userId: ctx.Value.UserId);

            // Mark order paid if fully covered, grant entitlements, snapshot invoice
            await _orders.MarkPaidIfCoveredAsync(orderId, amountMinor, OrderStatusPaid);
            await _orders.GrantEntitlementsAsync(orderId);
            await _orders.InsertInvoiceFromOrderAsync(spaceId, orderId, chargeId, gatewayId: 2 /* PayPal */);

            return new ConfirmPaymentIntentResponse
            {
                OrderId = orderId,
                PaymentIntentId = intentId,
                ProviderChargeId = cap.ProviderChargeId,
                PaymentStatusId = StatusSucceeded
            };
        }
    }
}
