// <copyright file="IPaymentQueryService.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Saad Sohail</author>
// <date>9/12/2025</date>
// <summary></summary>

using GMS.TifoXRCoreWebAPI.Models;

namespace GMS.TifoXRCoreWebAPI.Services
{
    public interface IPaymentQueryService
    {
        Task<PendingIntentResponse> GetPendingForOrderAsync(int spaceId, string orderId);
        Task<PendingIntentResponse> FindPendingByItemAsync(int spaceId, string userId, int itemTypeId, int itemRefId);
    }
}
