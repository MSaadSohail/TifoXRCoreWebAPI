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

            // 1) Validate and load provider/gateway context exactly like your other handlers
            var ctx = await _orders.GetIntentContextAsync(intentId);

            if (ctx is not { } c || c.OrderId != orderId || c.SpaceId != spaceId)
                throw new InvalidOperationException("payment_intent not found for this order/space.");

            if (string.IsNullOrWhiteSpace(c.ProviderIntentId))
                throw new InvalidOperationException("provider_intent_id missing.");

            // 2) Resolve gateway and delegate to crypto-only API
            var gateway = _resolver.GetById(c.GatewayId);

            if (gateway is not ICryptoGateway crypto)
                throw new NotSupportedException($"Gateway {c.GatewayId} does not support on-chain reporting.");

            await crypto.ReportTxAsync(c.ProviderIntentId!, txHash);
            // Approval/verification stays inside the crypto gateway (as you implemented).
        }
    }
}
