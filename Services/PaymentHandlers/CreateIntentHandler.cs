// <copyright file="CreateIntentHandler.cs" company="Global Mobile Software LLC">
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

namespace GMS.TifoXRCoreWebAPI.Services.PaymentHandlers
{
    public sealed class CreateIntentHandler(
        IPaymentGatewayResolver resolver, 
        IOrderRepository orders,
        IApprovalLinkService approvalLinks)
    {
        private readonly IPaymentGatewayResolver _resolver = resolver;
        private readonly IOrderRepository _orders = orders;

        public async Task<CreatePaymentIntentResponse> ExecuteAsync(
            int spaceId, string orderId, int gatewayId, CreatePaymentIntentRequest req)
        {
            if (string.IsNullOrWhiteSpace(orderId))
                throw new ArgumentException("orderId is required.", nameof(orderId));
            if (req is null)
                throw new ArgumentNullException(nameof(req));
            if (string.IsNullOrWhiteSpace(req.IdempotencyKey))
                throw new ArgumentException("IdempotencyKey is required.", nameof(req.IdempotencyKey));

            // header: (SpaceId, CurrencyId, TotalNetMinor, GatewayPreferredId)
            var header = await _orders.GetOrderHeaderAsync(orderId);
            if (header is not { } h || h.SpaceId != spaceId)
                throw new InvalidOperationException("Order not found in this space.");

            var gateway = _resolver.GetById(gatewayId);

            // Providers expect decimal major units; your DB stores minor units.
            var amountMajor = h.TotalNetMinor / 100m;

            // -------- Idempotency: existing intent for the same key --------
            var existing = await _orders.FindPaymentIntentByIdempotencyAsync(orderId, req.IdempotencyKey);
            if (existing is not null)
            {
                string? approveLink = null;

                if (!string.IsNullOrWhiteSpace(existing.Value.ProviderIntentId))
                {
                    // Stripe/PayPal: provider returns a human approve link.
                    // Crypto: provider returns an API link; we convert it to /pay/crypto.html?... below.
                    var providerStatus = await gateway.GetIntentAsync(existing.Value.ProviderIntentId);
                    var providerApprove = providerStatus?.ApproveLink;

                    if (string.Equals(gateway.Name, "crypto", StringComparison.OrdinalIgnoreCase))
                    {
                        approveLink = BuildCryptoApproveLinkFromProviderLink(
                            providerApprove,
                            spaceId,
                            orderId,
                            existing.Value.Id,
                            existing.Value.ProviderIntentId!);
                    }
                    else
                    {
                        approveLink = providerApprove;
                    }
                }

                return new CreatePaymentIntentResponse
                {
                    PaymentIntentId = existing.Value.Id,
                    PaymentGatewayId = gatewayId,
                    StatusId = (int)PaymentIntentStatus.RequiresAction,
                    ClientSecret = null,
                    ProviderIntentId = existing.Value.ProviderIntentId,
                    ApproveLink = approveLink
                };
            }

            // -------- Fresh provider intent --------
            var created = await gateway.CreateIntentAsync(
                new CreateGatewayIntentRequest(
                    IdempotencyKey: req.IdempotencyKey,
                    Amount: amountMajor,
                    CurrencyId: h.CurrencyId,
                    ReturnUrl: $"https://localhost:7017/api/space/{spaceId}/orders/{orderId}/payments/{gateway.Name}/return",
                    CancelUrl: $"https://localhost:7017/pay/cancel")
            );

            if (string.IsNullOrWhiteSpace(created.ProviderIntentId))
                throw new InvalidOperationException("ProviderIntentId was not returned by gateway.");

            // Persist our intent (no approve link stored)
            var piId = await _orders.InsertPaymentIntentAsync(
                orderId: orderId,
                gatewayId: gatewayId,
                statusId: (int)PaymentIntentStatus.RequiresAction,
                amountMinor: h.TotalNetMinor,
                currencyId: h.CurrencyId,
                providerIntentId: created.ProviderIntentId!,
                idempotencyKey: req.IdempotencyKey
            );

            // Build human-facing approve link in real time
            string? freshApprove;
            if (string.Equals(gateway.Name, "crypto", StringComparison.OrdinalIgnoreCase))
            {
                freshApprove = BuildCryptoApproveLinkFromProviderLink(
                    created.ApproveLink,
                    spaceId,
                    orderId,
                    piId,
                    created.ProviderIntentId!);
            }
            else
            {
                freshApprove = created.ApproveLink;
            }

            return new CreatePaymentIntentResponse
            {
                PaymentIntentId = piId,
                PaymentGatewayId = gatewayId,
                StatusId = (int)PaymentIntentStatus.RequiresAction,
                ClientSecret = null,       // Stripe may set this
                ProviderIntentId = created.ProviderIntentId,
                ApproveLink = freshApprove
            };
        }

        // providerLink (crypto) is usually "https://host/api/crypto/intents/{pid}" or ".../pay/crypto.html".
        // We always emit "https://host/pay/crypto.html?spaceId=...&orderId=...&intentId=...&pid=...".
        private static string BuildCryptoApproveLinkFromProviderLink(
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

            return $"{origin}/pay/crypto.html" +
                   $"?spaceId={spaceId}" +
                   $"&orderId={Uri.EscapeDataString(orderId)}" +
                   $"&intentId={Uri.EscapeDataString(intentId)}" +
                   $"&pid={Uri.EscapeDataString(providerIntentId)}";
        }
    }
}
