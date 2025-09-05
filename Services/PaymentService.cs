// Services/PaymentService.cs
using GMS.TifoXRCoreWebAPI.Middleware;
using GMS.TifoXRCoreWebAPI.Models;
using GMS.TifoXRCoreWebAPI.Repositories.Interfaces;
using GMS.TifoXRCoreWebAPI.Application.PaymentGateways;

namespace GMS.TifoXRCoreWebAPI.Services
{
    /// <summary>
    /// Selects the right payment gateway per order/intent and orchestrates persistence.
    /// </summary>
    public sealed class PaymentService(IPaymentGatewayResolver resolver, 
        IOrderRepository orders) : IPaymentService
    {
        // Status constants (align with your lookup tables)
        private const int StatusRequiresAction = 1;  // e.g., "requires_action"
        private const int StatusSucceeded      = 3;  // e.g., "succeeded"
        private const int OrderStatusPaid      = 3;  // e.g., "Paid"

        private readonly IPaymentGatewayResolver _resolver = resolver;
        private readonly IOrderRepository        _orders   = orders;

        public async Task<CreatePaymentIntentResponse> CreateIntentAsync(
            int spaceId, string orderId, CreatePaymentIntentRequest req)
        {
            if (req is null) throw new ArgumentNullException(nameof(req));
            if (string.IsNullOrWhiteSpace(req.IdempotencyKey))
                throw new ArgumentException("IdempotencyKey is required.", nameof(req.IdempotencyKey));

            // 1) Load minimal header & validate ownership
            var header = await _orders.GetOrderHeaderAsync(orderId);
            if (header is not { } h || h.SpaceId != spaceId)
                throw new InvalidOperationException("Order not found in this space.");

            // 2) Decide gateway (priority: explicit request > order's preferred)
            //    If your CreatePaymentIntentRequest.Gateway is nullable, replace comparison with: (req.Gateway ?? 0) > 0
            var gatewayId = req.Gateway > 0
                ? req.Gateway
                : h.GatewayPreferredId ?? throw new InvalidOperationException("No payment gateway specified and no order preference found.");

            var gateway = _resolver.GetById(gatewayId);

            var amountMinor = req.AmountMinor ?? h.TotalNetMinor;
            var amountMajor = amountMinor / 100m; // TODO: replace 100 with currency minor-unit resolver

            // 3) Idempotency: short-circuit if we’ve already created an intent
            var existing = await _orders.FindPaymentIntentByIdempotencyAsync(orderId, req.IdempotencyKey);
            if (existing is not null)
            {
                string? approve = null;
                if (!string.IsNullOrWhiteSpace(existing.Value.ProviderIntentId))
                {
                    var st = await gateway.GetIntentAsync(existing.Value.ProviderIntentId!);
                    approve = st?.ApproveLink;
                }

                return new CreatePaymentIntentResponse
                {
                    PaymentIntentId  = existing.Value.Id,
                    PaymentGatewayId = gatewayId,
                    StatusId         = StatusRequiresAction,
                    ProviderIntentId = existing.Value.ProviderIntentId,
                    ApproveLink      = approve
                };
            }

            // 4) Create provider intent
            var created = await gateway.CreateIntentAsync(new CreateGatewayIntentRequest(
                IdempotencyKey: req.IdempotencyKey,
                CurrencyId:     h.CurrencyId,
                Amount:         amountMajor,
                ReturnUrl:      $"https://localhost:7017/api/space/{spaceId}/orders/{orderId}/payments/{gateway.Name}/return",
                CancelUrl:      $"https://your.app/cancel"));

            if (string.IsNullOrWhiteSpace(created.ProviderIntentId))
                throw new InvalidOperationException("Gateway did not return a provider intent id.");

            // 5) Persist our intent with the chosen gateway
            var piId = await _orders.InsertPaymentIntentAsync(
                orderId:          orderId,
                gatewayId:        gatewayId,
                statusId:         StatusRequiresAction,
                amountMinor:      amountMinor,
                currencyId:       h.CurrencyId,
                providerIntentId: created.ProviderIntentId!,
                idempotencyKey:   req.IdempotencyKey);

            return new CreatePaymentIntentResponse
            {
                PaymentIntentId  = piId,
                PaymentGatewayId = gatewayId,
                StatusId         = StatusRequiresAction,
                ProviderIntentId = created.ProviderIntentId,
                ApproveLink      = created.ApproveLink
            };
        }

        public async Task<ConfirmPaymentIntentResponse> CaptureAsync(
            int spaceId, string orderId, string intentId, ConfirmPaymentIntentRequest req)
        {
            if (req is null) throw new ArgumentNullException(nameof(req));
            if (string.IsNullOrWhiteSpace(req.IdempotencyKey))
                throw new ArgumentException("IdempotencyKey is required.", nameof(req.IdempotencyKey));

            // 1) Load intent context (includes its recorded gateway id) & validate ownership
            var ctx = await _orders.GetIntentContextAsync(intentId);
            if (ctx is not { } c || c.OrderId != orderId || c.SpaceId != spaceId)
                throw new InvalidOperationException("payment_intent not found for this order/space.");

            var gateway = _resolver.GetById(c.GatewayId);

            var providerIntentId = c.ProviderIntentId ?? req.ProviderIntentId
                ?? throw new InvalidOperationException("provider_intent_id missing.");

            // 2) Must be APPROVED before capture (PayPal), other gateways may use different states
            var status = await gateway.GetIntentAsync(providerIntentId);
            if (status is null || !string.Equals(status.Status, "APPROVED", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    GlobalException.FormatExceptionMessage("PAYMENT_REQUIRES_APPROVAL", nameof(CaptureAsync)));
            }

            // 3) Capture on the correct gateway
            var cap = await gateway.CaptureAsync(new CaptureGatewayRequest(
                IdempotencyKey:   req.IdempotencyKey,
                ProviderIntentId: providerIntentId));

            var amountMinor = (long)decimal.Round(cap.CapturedAmount * 100m, 0, MidpointRounding.AwayFromZero);

            // 4) Persist charge + transitions
            var chargeId = await _orders.InsertChargeAsync(
                intentId:            intentId,
                statusId:            StatusSucceeded,
                amountCapturedMinor: amountMinor,
                currencyId:          c.CurrencyId,
                providerChargeId:    cap.ProviderChargeId,
                paidAtUtc:           DateTime.UtcNow,
                userId:              c.UserId);

            await _orders.MarkPaidIfCoveredAsync(orderId, amountMinor, OrderStatusPaid);
            await _orders.GrantEntitlementsAsync(orderId);
            await _orders.InsertInvoiceFromOrderAsync(spaceId, orderId, chargeId, gatewayId: c.GatewayId);

            return new ConfirmPaymentIntentResponse
            {
                OrderId          = orderId,
                PaymentIntentId  = intentId,
                ProviderChargeId = cap.ProviderChargeId,
                PaymentStatusId  = StatusSucceeded
            };
        }
    }
}
