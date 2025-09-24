// <copyright file="PaymentQueryService.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Saad Sohail</author>
// <date>9/12/2025</date>
// <summary></summary>

using GMS.TifoXRCoreWebAPI.Application.PaymentGateways;
using GMS.TifoXRCoreWebAPI.Models;
using GMS.TifoXRCoreWebAPI.Repositories;
using GMS.TifoXRCoreWebAPI.Utilities;
using GMS.TifoXRCoreWebAPI.Utilities.Domain.Enums;

namespace GMS.TifoXRCoreWebAPI.Services
{
    public sealed class PaymentQueryService(
        IOrderRepository repo,
        IPaymentGatewayResolver resolver) : IPaymentQueryService
    {
        private readonly IOrderRepository _repo = repo;
        private readonly IPaymentGatewayResolver _resolver = resolver;

        public async Task<PendingIntentResponse> GetPendingForOrderAsync(int spaceId, string orderId)
        {
            if (string.IsNullOrWhiteSpace(orderId))
                throw new ArgumentException("orderId is required.", nameof(orderId));

            var order = await _repo.GetOrderAsync(spaceId, orderId);
            if (order is null) return new PendingIntentResponse { Found = false };

            var pi = await _repo.GetPendingIntentForOrderAsync(orderId);
            if (pi is null) return new PendingIntentResponse { Found = false };

            // Unpack
            var intentId = pi.Value.IntentId;
            var providerIntentId = pi.Value.ProviderIntentId;
            var gatewayId = pi.Value.PaymentGatewayId;
            var statusId = pi.Value.StatusId;
            var idemKey = pi.Value.IdempotencyKey;
            var amtMinor = pi.Value.AmountMinor;
            var currencyId = pi.Value.CurrencyId;

            // Only build approve link for pending
            string? approveLink = null;
            string providerPidToUse = providerIntentId ?? string.Empty;

            if (statusId == (int)PaymentIntentStatus.RequiresAction || statusId == 1)
            {
                var gateway = _resolver.GetById(gatewayId);
                string? providerApprove = null;

                if (!string.IsNullOrWhiteSpace(providerPidToUse))
                {
                    var providerStatus = await gateway.GetIntentAsync(providerPidToUse);

                    if (providerStatus is null && string.Equals(gateway.Name, "crypto", StringComparison.OrdinalIgnoreCase))
                    {
                        // auto-refresh in place
                        var amountMajor = MoneyConverter.ToMajor(amtMinor);
                        var (newPid, newApproveLink) = await RefreshCryptoProviderIntentAsync(
                            spaceId, orderId, intentId, gatewayId, amountMajor, currencyId);

                        providerPidToUse = newPid;
                        providerApprove = newApproveLink;
                    }
                    else
                    {
                        providerApprove = providerStatus?.ApproveLink;
                    }
                }

                approveLink = string.Equals(gateway.Name, "crypto", StringComparison.OrdinalIgnoreCase)
                    ? BuildCryptoApproveLinkFromProviderLink(providerApprove, spaceId, orderId, intentId, providerPidToUse)
                    : providerApprove;

                return new PendingIntentResponse
                {
                    Found = true,                    
                    OrderId = orderId,
                    PaymentIntentId = intentId,
                    StatusId = statusId,
                    IdempotencyKey = idemKey,
                    ProviderIntentId = providerPidToUse,       // may be refreshed
                    PaymentGatewayId = gatewayId,
                    AmountMinor = amtMinor,
                    CurrencyId = currencyId,
                    ApproveLink = approveLink
                };
            }

            // Not pending; we return the record but without link
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
                CurrencyId = currencyId,
                ApproveLink = null
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

                if (string.Equals(gateway.Name, "crypto", StringComparison.OrdinalIgnoreCase))
                {
                    approveLink = BuildCryptoApproveLinkFromProviderLink(
                        providerApprove,
                        spaceId,
                        orderId,
                        intentId,
                        providerIntentId ?? string.Empty);
                }
                else
                {
                    approveLink = providerApprove;
                }
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
                CurrencyId = currencyId,
                ApproveLink = approveLink
            };
        }

        private static string? BuildCryptoApproveLinkFromProviderLink(
            string? providerLink,
            int spaceId,
            string orderId,
            string intentId,
            string providerIntentId)
        {
            string origin;
            if (!string.IsNullOrWhiteSpace(providerLink) &&
                Uri.TryCreate(providerLink, UriKind.Absolute, out var uri))
            {
                origin = $"{uri.Scheme}://{uri.Authority}";
            }
            else
            {
                origin = "https://localhost:7017"; // fallback
            }

            return $"{origin}/pay/crypto.html"
                 + $"?spaceId={spaceId}"
                 + $"&orderId={Uri.EscapeDataString(orderId)}"
                 + $"&intentId={Uri.EscapeDataString(intentId)}"
                 + $"&pid={Uri.EscapeDataString(providerIntentId)}";
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

            // Create a fresh provider intent (provider idempotency can be a transient value; not persisted)
            var created = await gateway.CreateIntentAsync(new CreateGatewayIntentRequest(
                IdempotencyKey: $"refresh::{intentId}::{DateTime.UtcNow.Ticks}",
                Amount: amountMajor,
                CurrencyId: currencyId,
                ReturnUrl: $"https://localhost:7017/api/space/{spaceId}/orders/{orderId}/payments/{gateway.Name}/return",
                CancelUrl: $"https://localhost:7017/pay/cancel"
            ));

            if (string.IsNullOrWhiteSpace(created.ProviderIntentId))
                throw new InvalidOperationException("Crypto refresh failed: no ProviderIntentId from gateway.");

            // Persist the new PID on the SAME payment_intent id
            var rows = await _repo.UpdatePaymentIntentProviderIdAsync(intentId, created.ProviderIntentId!);
            if (rows != 1)
                throw new InvalidOperationException("Crypto refresh failed: could not update provider_intent_id.");

            return (created.ProviderIntentId!, created.ApproveLink);
        }
    }
}
