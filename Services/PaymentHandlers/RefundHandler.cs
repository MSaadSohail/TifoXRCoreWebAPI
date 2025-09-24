// <copyright file="RefundHandler.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Saad Sohail</author>
// <date>9/12/2025</date>
// <summary></summary>

using GMS.TifoXRCoreWebAPI.Application.PaymentGateways;
using GMS.TifoXRCoreWebAPI.Application.Payments.Refunds;
using GMS.TifoXRCoreWebAPI.Models;
using GMS.TifoXRCoreWebAPI.Repositories;
using GMS.TifoXRCoreWebAPI.Utilities;
using GMS.TifoXRCoreWebAPI.Utilities.Domain.Enums;

namespace GMS.TifoXRCoreWebAPI.Services.PaymentHandlers
{
    public sealed class RefundHandler(
        IPaymentGatewayResolver resolver, 
        IOrderRepository orders, 
        IRefundPolicyResolver refundPolicies)
    {
        private readonly IPaymentGatewayResolver _resolver = resolver;
        private readonly IOrderRepository _orders = orders;
        private readonly IRefundPolicyResolver _refundPolicies = refundPolicies;

        public async Task<RefundResponse> ExecuteAsync(int spaceId, string orderId, string chargeId, RefundRequest req)
        {
            if (req is null) throw new ArgumentNullException(nameof(req));
            if (string.IsNullOrWhiteSpace(req.IdempotencyKey))
                throw new ArgumentException("IdempotencyKey is required.", nameof(req.IdempotencyKey));

            var ctx = await _orders.GetChargeContextAsync(chargeId);
            if (ctx is not { } c || c.OrderId != orderId || c.SpaceId != spaceId)
                throw new InvalidOperationException("payment_charge not found for this order/space.");

            var gateway = _resolver.GetById(c.GatewayId);
            var policy = _refundPolicies.Resolve(gateway.Name);

            if (!policy.IsAllowed(req.Reason))
                throw new ArgumentException("Invalid refund reason for this payment gateway.", nameof(req.Reason));

            var normalizedReason = policy.Normalize(req.Reason);

            var isFull = !req.AmountMinor.HasValue;
            var amountMinor = isFull ? c.AmountCapturedMinor : req.AmountMinor.Value;
            if (amountMinor <= 0) throw new ArgumentException("Refund amount must be positive.", nameof(req.AmountMinor));

            var remainingMinor = c.AmountCapturedMinor - c.TotalRefundedSoFarMinor;
            if (remainingMinor <= 0) throw new InvalidOperationException("Charge has already been fully refunded.");
            if (amountMinor > remainingMinor) throw new ArgumentException(
                $"Refund amount exceeds remaining refundable amount. Remaining={remainingMinor} minor units.",
                nameof(req.AmountMinor));

            var amountMajor = MoneyConverter.ToMajor(amountMinor);

            var prov = await gateway.RefundAsync(new RefundGatewayRequest(
                IdempotencyKey: req.IdempotencyKey,
                ProviderChargeId: c.ProviderChargeId ?? throw new InvalidOperationException("provider_charge_id missing."),
                Amount: amountMajor,
                CurrencyId: c.CurrencyId,
                Reason: normalizedReason
            ));

            var refundStatus = RefundStatus.Succeeded;

            var refundId = await _orders.InsertRefundAsync(
                chargeId: chargeId,
                statusId: (int)refundStatus,
                amountMinor: MoneyConverter.ToMinor(prov.RefundedAmount),
                currencyId: c.CurrencyId,
                providerRefundId: prov.ProviderRefundId,
                reason: req.Reason
            );

            var revokeReason = req.Reason ?? "Refund succeeded";

            await _orders.RevokeEntitlementsAsync(orderId, revokeReason);
            await _orders.RevokeOrderAsync(orderId, revokeReason);

            return new RefundResponse
            {
                OrderId = orderId,
                ChargeId = chargeId,
                RefundId = refundId,
                ProviderRefundId = prov.ProviderRefundId,
                StatusId = (int)refundStatus,
                RefundedAmountMinor = MoneyConverter.ToMinor(prov.RefundedAmount),
                CurrencyId = c.CurrencyId
            };
        }
    }
}
