// <copyright file="IOrderRepository.Read.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Saad Sohail</author>
// <date>9/16/2025</date>
// <summary></summary>

using GMS.TifoXRCoreWebAPI.Models;

namespace GMS.TifoXRCoreWebAPI.Repositories
{
    public partial interface IOrderRepository
    {
        Task<OrderDto?> GetOrderAsync(int spaceId, string orderId);
        Task<List<EntitlementDto>?> GetEntitlementsByOrderAsync(int spaceId, string orderId);
        Task<InvoiceListResponse?> GetInvoicesByOrderAsync(int spaceId, string orderId);
        
    }
}
