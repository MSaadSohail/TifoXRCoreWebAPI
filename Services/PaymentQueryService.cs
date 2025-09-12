// <copyright file="PaymentQueryService.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Saad Sohail</author>
// <date>9/12/2025</date>
// <summary></summary>

using GMS.TifoXRCoreWebAPI.Models;
using GMS.TifoXRCoreWebAPI.Repositories;

namespace GMS.TifoXRCoreWebAPI.Services
{
    public sealed class PaymentQueryService : IPaymentQueryService
    {
        private readonly IOrderRepository _repo;
        public PaymentQueryService(IOrderRepository repo) => _repo = repo;

        public async Task<PendingIntentResponse> GetPendingForOrderAsync(int spaceId, string orderId)
        {
            if (string.IsNullOrWhiteSpace(orderId))
                throw new ArgumentException("orderId is required.", nameof(orderId));

            var order = await _repo.GetOrderAsync(spaceId, orderId);
            if (order is null) return new PendingIntentResponse { Found = false };

            var pi = await _repo.GetPendingIntentForOrderAsync(orderId);

            if (pi is null) return new PendingIntentResponse { Found = false };
            
            return new PendingIntentResponse
            {
                Found = true,
                OrderId = orderId,
                PaymentIntentId = pi.Value.IntentId,
                StatusId = pi.Value.StatusId,
                IdempotencyKey = pi.Value.IdempotencyKey,
                ProviderIntentId = pi.Value.IntentId,
                PaymentGatewayId = pi.Value.PaymentGatewayId,
                AmountMinor = pi.Value.AmountMinor,
                CurrencyId = pi.Value.CurrencyId
            };
        }

        public async Task<PendingIntentResponse> FindPendingByItemAsync(int spaceId, string userId, int itemTypeId, int itemRefId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("userId is required.", nameof(userId));

            var orderId = await _repo.FindLatestOrderIdWithPendingIntentAsync(spaceId, userId, itemTypeId, itemRefId);
            if (string.IsNullOrWhiteSpace(orderId)) return new PendingIntentResponse { Found = false };

            var pi = await _repo.GetPendingIntentForOrderAsync(orderId);
            if (pi is null) return new PendingIntentResponse { Found = false };

            return new PendingIntentResponse
            {
                Found = true,
                OrderId = orderId,
                PaymentIntentId = pi.Value.IntentId,
                StatusId = pi.Value.StatusId,
                IdempotencyKey = pi.Value.IdempotencyKey,
                ProviderIntentId = pi.Value.ProviderIntentId,
                PaymentGatewayId = pi.Value.PaymentGatewayId,
                AmountMinor = pi.Value.AmountMinor,
                CurrencyId = pi.Value.CurrencyId
            };
        }
    }
}
