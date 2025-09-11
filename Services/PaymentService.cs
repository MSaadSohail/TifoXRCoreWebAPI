// <copyright file="PaymentService.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Saad Sohail</author>
// <date>09/02/2025</date>
// <summary></summary>

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
        private const int StatusProcessing     = 2;  // e.g., "in_processing"
        private const int StatusSucceeded      = 3;  // e.g., "succeeded"
        private const int OrderStatusPaid      = 3;  // e.g., "Paid"
        private const int RefundStatusSucceeded = 3;

        private readonly IPaymentGatewayResolver _resolver = resolver;
        private readonly IOrderRepository _orders = orders;

        private static readonly HashSet<string> StripeAllowedReasons = new(StringComparer.OrdinalIgnoreCase) 
            { "duplicate", "fraudulent", "requested_by_customer" };

        public async Task<CreatePaymentIntentResponse> CreateIntentAsync(
            int spaceId, 
            string orderId,
            int gatewayId,
            CreatePaymentIntentRequest req)
        {
            if (req is null) throw new ArgumentNullException(nameof(req));
            if (string.IsNullOrWhiteSpace(req.IdempotencyKey))
                throw new ArgumentException("IdempotencyKey is required.", nameof(req.IdempotencyKey));

            // 1) Load minimal header & validate ownership
            var header = await _orders.GetOrderHeaderAsync(orderId);
            if (header is not { } h || h.SpaceId != spaceId)
                throw new InvalidOperationException("Order not found in this space.");

            var gateway = _resolver.GetById(gatewayId);
            var amountMinor = h.TotalNetMinor;
            var amountMajor = amountMinor / 100m; // TODO: replace 100 with currency minor-unit resolver

            // 2) Idempotency: short-circuit if we’ve already created an intent
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

            // 3) Create provider intent
            var created = await gateway.CreateIntentAsync(new CreateGatewayIntentRequest(
                IdempotencyKey: req.IdempotencyKey,
                CurrencyId:     h.CurrencyId,
                Amount:         amountMajor,
                ReturnUrl:      $"https://localhost:7017/api/space/{spaceId}/orders/{orderId}/payments/{gateway.Name}/return",
                CancelUrl:      $"https://your.app/cancel"));

            if (string.IsNullOrWhiteSpace(created.ProviderIntentId))
                throw new InvalidOperationException("Gateway did not return a provider intent id.");

            // 4) Persist our intent with the chosen gateway
            var piId = await _orders.InsertPaymentIntentAsync(
                orderId:          orderId,
                gatewayId:        gatewayId,
                statusId:         StatusRequiresAction,
                amountMinor:      amountMinor,
                currencyId:       h.CurrencyId,
                providerIntentId: created.ProviderIntentId!,
                idempotencyKey:   req.IdempotencyKey);

            var approveLink = created.ApproveLink;

            // If this is the crypto gateway, build our unified approve URL with all IDs we now have
            if (string.Equals(gateway.Name, "crypto", StringComparison.OrdinalIgnoreCase))
            {
                var baseUrl = "https://localhost:7017"; // or your public base
                approveLink = $"{baseUrl}/pay/crypto.html" +
                              $"?spaceId={spaceId}&" +
                              $"orderId={orderId}&" +
                              $"intentId={piId}&" +
                              $"pid={created.ProviderIntentId}";
            }

            return new CreatePaymentIntentResponse
            {
                PaymentIntentId = piId,
                PaymentGatewayId = gatewayId,
                StatusId = StatusRequiresAction,
                ProviderIntentId = created.ProviderIntentId,
                ApproveLink = approveLink
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

            await _orders.UpdatePaymentIntentStatusAsync(intentId, StatusProcessing);

            CaptureGatewayResult cap;
            try
            {
                cap = await gateway.CaptureAsync(new CaptureGatewayRequest(
                    IdempotencyKey: req.IdempotencyKey,
                    ProviderIntentId: providerIntentId));
            }
            catch
            {
                // Roll back to requires_action
                await _orders.UpdatePaymentIntentStatusAsync(intentId, StatusRequiresAction);

                // Re-throw so global middleware catches and formats it
                throw;
            }

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

            await _orders.UpdatePaymentIntentStatusAsync(intentId, StatusSucceeded);
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

        public async Task<RefundResponse> RefundAsync(
            int spaceId, string orderId, string chargeId, RefundRequest req)
        {
            if (req is null) throw new ArgumentNullException(nameof(req));
            if (string.IsNullOrWhiteSpace(req.IdempotencyKey))
                throw new ArgumentException("IdempotencyKey is required.", nameof(req.IdempotencyKey));

            // 1) Resolve charge context (gateway, provider charge id, currency, space/order ownership)
            var ctx = await _orders.GetChargeContextAsync(chargeId);
            if (ctx is not { } c || c.OrderId != orderId || c.SpaceId != spaceId)
                throw new InvalidOperationException("payment_charge not found for this order/space.");

            var gateway = _resolver.GetById(c.GatewayId);

            if (string.Equals(gateway.Name, "stripe", StringComparison.OrdinalIgnoreCase))
            {
                if (!string.IsNullOrWhiteSpace(req.Reason) && !StripeAllowedReasons.Contains(req.Reason))
                {
                    throw new ArgumentException(
                        "Invalid reason for Stripe refund. Allowed: duplicate, fraudulent, requested_by_customer",
                        nameof(req.Reason));
                }
            }

            // 2) Amount: default to full refund if not provided
            // If req.AmountMinor is null => full refund => pass req.Amount = 0 so body is omitted
            var isFull = !req.AmountMinor.HasValue;
            var amountMinor = isFull ? c.AmountCapturedMinor : req.AmountMinor.Value;
            if (amountMinor <= 0) throw new ArgumentException("Refund amount must be positive.", nameof(req.AmountMinor));

            // NEW: remaining guardrails
            var remainingMinor = c.AmountCapturedMinor - c.TotalRefundedSoFarMinor;
            if (remainingMinor <= 0)
            {
                // Everything was already refunded
                throw new InvalidOperationException("Charge has already been fully refunded.");
            }

            if(amountMinor > remainingMinor)
{
                // Too much requested
                throw new ArgumentException(
                    $"Refund amount exceeds remaining refundable amount. Remaining={remainingMinor} minor units.",
                    nameof(req.AmountMinor));
            }

            var amountMajor = amountMinor / 100m; // TODO: replace 100 with currency minor-unit resolver

            // 3) Call provider refund
            var prov = await gateway.RefundAsync(new RefundGatewayRequest(
                IdempotencyKey: req.IdempotencyKey,
                ProviderChargeId: c.ProviderChargeId ?? throw new InvalidOperationException("provider_charge_id missing."),
                Amount: amountMajor,
                CurrencyId: c.CurrencyId,
                Reason: req.Reason
            ));

            // 4) Persist refund row
            var refundId = await _orders.InsertRefundAsync(
                chargeId: chargeId,
                statusId: RefundStatusSucceeded,
                amountMinor: (long)decimal.Round(prov.RefundedAmount * 100m, 0, MidpointRounding.AwayFromZero),
                currencyId: c.CurrencyId,
                providerRefundId: prov.ProviderRefundId,
                reason: req.Reason
            );

            return new RefundResponse
            {
                OrderId = orderId,
                ChargeId = chargeId,
                RefundId = refundId,
                ProviderRefundId = prov.ProviderRefundId,
                StatusId = RefundStatusSucceeded,
                RefundedAmountMinor = (long)decimal.Round(prov.RefundedAmount * 100m, 0, MidpointRounding.AwayFromZero),
                CurrencyId = c.CurrencyId
            };
        }

        public IPaymentGateway GetGatewayByName(string name) => _resolver.GetByName(name);

        public IPaymentGateway GetGatewayById(int gatewayId) => _resolver.GetById(gatewayId);
    }
}
