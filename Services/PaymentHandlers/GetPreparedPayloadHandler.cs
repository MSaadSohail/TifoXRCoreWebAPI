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

            // Resolve from order graph (avoids brittle joins)
            var order = await _orders.GetOrderAsync(spaceId, orderId)
                ?? throw new InvalidOperationException("order not found.");

            var intent = order.PaymentIntents.FirstOrDefault(i => i.Id == intentId)
                ?? throw new InvalidOperationException("payment_intent not found for this order/space.");

            if (string.IsNullOrWhiteSpace(intent.ProviderIntentId))
                throw new InvalidOperationException("provider_intent_id missing.");

            var gateway = _resolver.GetById(intent.PaymentGatewayId);
            if (gateway is not ICryptoGateway crypto)
                throw new NotSupportedException($"Gateway {intent.PaymentGatewayId} does not support prepared payloads.");

            // Heal stale crypto PID, if needed
            var providerStatus = await gateway.GetIntentAsync(intent.ProviderIntentId);
            if (providerStatus is null)
            {
                var header = await _orders.GetOrderHeaderAsync(orderId)
                    ?? throw new InvalidOperationException("order header not found.");

                var amountMajor = GMS.TifoXRCoreWebAPI.Utilities.MoneyConverter.ToMajor(header.TotalNetMinor);
                var (newPid, _) = await RefreshCryptoProviderIntentAsync(
                    spaceId, orderId, intent.Id, intent.PaymentGatewayId, amountMajor, header.CurrencyId);

                // use the refreshed pid
                return await crypto.GetPreparedJsonAsync(newPid, sender);
            }

            return await crypto.GetPreparedJsonAsync(intent.ProviderIntentId!, sender);
        }

        private async Task<(string NewProviderIntentId, string? ApproveLink)> RefreshCryptoProviderIntentAsync(
            int spaceId,
            string orderId,
            string intentId,
            int gatewayId,
            decimal amountMajor,
            int currencyId)
        {
            var gateway = _resolver.GetById(gatewayId);

            var created = await gateway.CreateIntentAsync(new CreateGatewayIntentRequest(
                IdempotencyKey: $"refresh::{intentId}::{DateTime.UtcNow.Ticks}",
                Amount: amountMajor,
                CurrencyId: currencyId,
                ReturnUrl: $"https://localhost:7017/api/space/{spaceId}/orders/{orderId}/payments/{gateway.Name}/return",
                CancelUrl: $"https://localhost:7017/pay/cancel"
            ));

            if (string.IsNullOrWhiteSpace(created.ProviderIntentId))
                throw new InvalidOperationException("Crypto refresh failed: no ProviderIntentId from gateway.");

            var rows = await _orders.UpdatePaymentIntentProviderIdAsync(intentId, created.ProviderIntentId!);
            if (rows != 1)
                throw new InvalidOperationException("Crypto refresh failed: could not update provider_intent_id.");

            return (created.ProviderIntentId!, created.ApproveLink);
        }
    }
}
