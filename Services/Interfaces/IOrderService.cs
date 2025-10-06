// <copyright file="IOrderService.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Saad Sohail</author>
// <date>09/08/2025</date>
// <summary>Add File Summary</summary>

using GMS.TifoXRCoreWebAPI.Models;

namespace GMS.TifoXRCoreWebAPI.Services
{
    public interface IOrderService
    {
        Task<CreateOrderResponse> CreateOrderAsync(int spaceId, CreateOrderRequest req);
        Task<OrderDto?> GetOrderAsync(int spaceId, string orderId);
        Task<InvoiceListResponse?> GetInvoicesAsync(int spaceId, string orderId);
        Task<List<EntitlementDto>?> GetEntitlementsByOrderAsync(int spaceId, string orderId);
        Task RevokeOrderAsync(string orderId, string? reason = null);
    }
}
