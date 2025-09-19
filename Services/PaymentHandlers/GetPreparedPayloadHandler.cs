// <copyright file="GetPreparedPayloadHandler.cs" company="Global Mobile Software LLC">
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
    /// Returns a wallet-ready prepared payload (if the crypto gateway supports it).
    /// </summary>
    public sealed class GetPreparedPayloadHandler(IPaymentGatewayResolver resolver, IOrderRepository orders)
    {
        private readonly IPaymentGatewayResolver _resolver = resolver;
        private readonly IOrderRepository _orders = orders;

        public async Task<string> ExecuteAsync(
            int spaceId,
            string orderId,
            string intentId,
            string sender)
        {
            if (string.IsNullOrWhiteSpace(sender))
                throw new ArgumentException("sender is required.", nameof(sender));

            // 1) Validate & load context (same pattern as other handlers)
            var ctx = await _orders.GetIntentContextAsync(intentId);

            if (ctx is not { } c || c.OrderId != orderId || c.SpaceId != spaceId)
                throw new InvalidOperationException("payment_intent not found for this order/space.");

            if (string.IsNullOrWhiteSpace(c.ProviderIntentId))
                throw new InvalidOperationException("provider_intent_id missing.");

            // 2) Resolve gateway and delegate
            var gateway = _resolver.GetById(c.GatewayId);

            if (gateway is not ICryptoGateway crypto)
                throw new NotSupportedException($"Gateway {c.GatewayId} does not support prepared payloads.");

            return await crypto.GetPreparedJsonAsync(c.ProviderIntentId!, sender);
        }
    }
}
