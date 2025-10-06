// <copyright file="ReportOnChainHandler.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Saad Sohail</author>
// <date>9/19/2025</date>
// <summary></summary>

using GMS.TifoXRCoreWebAPI.Application.PaymentGateways;
using GMS.TifoXRCoreWebAPI.Repositories;

namespace GMS.TifoXRCoreWebAPI.Services.PaymentHandlers
{
    /// <summary>
    /// Reports an on-chain tx hash to the selected crypto gateway (e.g., Chiliz) and lets it verify/approve.
    /// Thin and focused — same spirit as Create/Capture/Refund handlers.
    /// </summary>
    public sealed class ReportOnChainHandler(IPaymentGatewayResolver resolver, IOrderRepository orders)
    {
        private readonly IPaymentGatewayResolver _resolver = resolver;
        private readonly IOrderRepository _orders = orders;

        public async Task ExecuteAsync(
            int spaceId,
            string orderId,
            string intentId,
            string txHash)
        {
            if (string.IsNullOrWhiteSpace(txHash))
                throw new ArgumentException("txHash is required.", nameof(txHash));

            // Resolve from the order + its intents (no repo “intent context” join)
            var order = await _orders.GetOrderAsync(spaceId, orderId)
                ?? throw new InvalidOperationException("order not found.");

            var intent = order.PaymentIntents.FirstOrDefault(i =>
                string.Equals(i.Id, intentId, StringComparison.OrdinalIgnoreCase));

            if (intent is null)
                throw new InvalidOperationException("payment_intent not found for this order/space.");

            if (string.IsNullOrWhiteSpace(intent.ProviderIntentId))
                throw new InvalidOperationException("provider_intent_id missing.");

            var gateway = _resolver.GetById(intent.PaymentGatewayId);
            if (gateway is not ICryptoGateway crypto)
                throw new NotSupportedException($"Gateway {intent.PaymentGatewayId} does not support on-chain reporting.");

            await crypto.ReportTxAsync(intent.ProviderIntentId!, txHash);
        }
    }
}
