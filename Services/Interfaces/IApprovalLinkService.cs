// <copyright file="IApprovalLinkService.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Saad Sohail</author>
// <date>9/19/2025</date>
// <summary></summary>

namespace TifoXRCoreWebAPI.Services.Interfaces
{
    public interface IApprovalLinkService
    {
        string BuildApproveLink(
            string gatewayName,
            string? gatewayApproveLinkFromProvider,
            int spaceId,
            string orderId,
            string intentId,
            string providerIntentId);
    }
}
