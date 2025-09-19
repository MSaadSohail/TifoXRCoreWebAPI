// <copyright file="IOrderRepository.Write.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Saad Sohail</author>
// <date>09/16/2025</date>
// <summary></summary>

using GMS.TifoXRCoreWebAPI.Models;

namespace GMS.TifoXRCoreWebAPI.Repositories
{
    public partial interface IOrderRepository
    {
        Task<CreateOrderResponse> CreateOrderAsync(
            int pathSpaceId, CreateOrderRequest req, int? gatewayPreferredId = null);
    }
}

