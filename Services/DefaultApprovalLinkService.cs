// <copyright file="DefaultApprovalLinkService.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Saad Sohail</author>
// <date>9/19/2025</date>
// <summary></summary>

using GMS.TifoXRCoreWebAPI.Application.PaymentGateways.Crypto.Chiliz;
using Microsoft.Extensions.Options;
using TifoXRCoreWebAPI.Services.Interfaces;

namespace TifoXRCoreWebAPI.Services
{
    public sealed class DefaultApprovalLinkService : IApprovalLinkService
    {
        private readonly CryptoChilizOptions _crypto;

        public DefaultApprovalLinkService(IOptions<CryptoChilizOptions> crypto)
        {
            _crypto = crypto?.Value ?? new CryptoChilizOptions();
        }

        public string BuildApproveLink(
            string gatewayName,
            string? gatewayApproveLinkFromProvider,
            int spaceId,
            string orderId,
            string intentId,
            string providerIntentId)
        {
            // Stripe/PayPal (and most redirect-style gateways) → just use provider link if present
            if (!string.Equals(gatewayName, "crypto", StringComparison.OrdinalIgnoreCase))
                return gatewayApproveLinkFromProvider ?? string.Empty;

            // Crypto → always return the human page with required query params
            var baseUrl = _crypto.PublicBaseUrl?.TrimEnd('/') ?? "https://localhost:7017";

            return $"{baseUrl}/pay/crypto.html" +
                   $"?spaceId={spaceId}" +
                   $"&orderId={Uri.EscapeDataString(orderId)}" +
                   $"&intentId={Uri.EscapeDataString(intentId)}" +
                   $"&pid={Uri.EscapeDataString(providerIntentId)}";
        }
    }
}
