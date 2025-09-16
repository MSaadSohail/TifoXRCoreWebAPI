// <copyright file="CaptureHandler.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Saad Sohail</author>
// <date>9/12/2025</date>
// <summary></summary>

using GMS.TifoXRCoreWebAPI.Models;
using GMS.TifoXRCoreWebAPI.Repositories;
using GMS.TifoXRCoreWebAPI.Utilities;
using GMS.TifoXRCoreWebAPI.Utilities.Domain.Enums;
using GMS.TifoXRCoreWebAPI.Application.PaymentGateways;

namespace GMS.TifoXRCoreWebAPI.Services.PaymentHandlers
{
    public sealed class CaptureHandler(IPaymentGatewayResolver resolver, IOrderRepository orders)
    {
        private readonly IPaymentGatewayResolver _resolver = resolver;
        private readonly IOrderRepository _orders = orders;

        public async Task<ConfirmPaymentIntentResponse> ExecuteAsync(
            int spaceId, string orderId, string intentId, ConfirmPaymentIntentRequest req)
        {
            if (req is null) throw new ArgumentNullException(nameof(req));
            if (string.IsNullOrWhiteSpace(req.IdempotencyKey))
                throw new ArgumentException("IdempotencyKey is required.", nameof(req.IdempotencyKey));

            var ctx = await _orders.GetIntentContextAsync(intentId);
            if (ctx is not { } c || c.OrderId != orderId || c.SpaceId != spaceId)
                throw new InvalidOperationException("payment_intent not found for this order/space.");

            var gateway = _resolver.GetById(c.GatewayId);
            var providerIntentId = c.ProviderIntentId ?? req.ProviderIntentId
                ?? throw new InvalidOperationException("provider_intent_id missing.");

            var status = await gateway.GetIntentAsync(providerIntentId);
            if (status is null || !string.Equals(status.Status, "APPROVED", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("PAYMENT_REQUIRES_APPROVAL");

            await _orders.UpdatePaymentIntentStatusAsync(intentId, (int)PaymentIntentStatus.Processing);

            CaptureGatewayResult cap;
            try
            {
                cap = await gateway.CaptureAsync(new CaptureGatewayRequest(
                    IdempotencyKey: req.IdempotencyKey,
                    ProviderIntentId: providerIntentId));
            }
            catch
            {
                await _orders.UpdatePaymentIntentStatusAsync(intentId, (int)PaymentIntentStatus.RequiresAction);
                throw;
            }

            var amountMinor = MoneyConverter.ToMinor(cap.CapturedAmount);

            var chargeId = await _orders.InsertChargeAsync(
                intentId: intentId,
                statusId: (int)PaymentIntentStatus.Succeeded,
                amountCapturedMinor: amountMinor,
                currencyId: c.CurrencyId,
                providerChargeId: cap.ProviderChargeId,
                paidAtUtc: DateTime.UtcNow,
                userId: c.UserId);

            await _orders.UpdatePaymentIntentStatusAsync(intentId, (int)PaymentIntentStatus.Succeeded);
            await _orders.MarkPaidIfCoveredAsync(orderId, amountMinor, (int)PaymentIntentStatus.Succeeded);
            await _orders.GrantEntitlementsAsync(orderId);
            await _orders.InsertInvoiceFromOrderAsync(spaceId, orderId, chargeId, gatewayId: c.GatewayId);

            return new ConfirmPaymentIntentResponse
            {
                OrderId = orderId,
                PaymentIntentId = intentId,
                ProviderChargeId = cap.ProviderChargeId,
                PaymentStatusId = (int)PaymentIntentStatus.RequiresAction
            };
        }
    }
}
