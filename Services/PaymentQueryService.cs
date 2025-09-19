// <copyright file="PaymentQueryService.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Saad Sohail</author>
// <date>9/12/2025</date>
// <summary></summary>

using GMS.TifoXRCoreWebAPI.Application.PaymentGateways;
using GMS.TifoXRCoreWebAPI.Models;
using GMS.TifoXRCoreWebAPI.Repositories;
using GMS.TifoXRCoreWebAPI.Utilities.Domain.Enums;
using TifoXRCoreWebAPI.Services.Interfaces;

namespace GMS.TifoXRCoreWebAPI.Services
{
    public sealed class PaymentQueryService(
        IOrderRepository repo,
        IPaymentGatewayResolver resolver,
        IApprovalLinkService approvalLinks) : IPaymentQueryService
    {
        private readonly IOrderRepository _repo = repo;
        private readonly IPaymentGatewayResolver _resolver = resolver;
        private readonly IApprovalLinkService _approvalLinks = approvalLinks;

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
                ProviderIntentId = pi.Value.ProviderIntentId,
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

            var (intentId, providerIntentId, gatewayId, statusId, idemKey, amtMinor, currencyId) =
                (pi.Value.IntentId, pi.Value.ProviderIntentId, pi.Value.PaymentGatewayId,
                 pi.Value.StatusId, pi.Value.IdempotencyKey, pi.Value.AmountMinor, pi.Value.CurrencyId);

            string? approveLink = null;

            if (statusId == (int)PaymentIntentStatus.RequiresAction || statusId == 1)
            {
                var gateway = _resolver.GetById(gatewayId);

                string? providerApprove = null;
                if (!string.IsNullOrWhiteSpace(providerIntentId))
                {
                    var providerStatus = await gateway.GetIntentAsync(providerIntentId);
                    providerApprove = providerStatus?.ApproveLink;
                }

                approveLink = _approvalLinks.BuildApproveLink(
                    gateway.Name,
                    providerApprove,
                    spaceId,
                    orderId,
                    intentId,
                    providerIntentId ?? string.Empty);
            }

            return new PendingIntentResponse
            {
                Found = true,
                OrderId = orderId,
                PaymentIntentId = intentId,
                StatusId = statusId,
                IdempotencyKey = idemKey,
                ProviderIntentId = providerIntentId,
                PaymentGatewayId = gatewayId,
                AmountMinor = amtMinor,
                CurrencyId = currencyId
            };
        }
    }
}
