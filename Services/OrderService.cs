// <copyright file="OrderService.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Saad Sohail</author>
// <date>09/02/2025</date>
// <summary></summary>

using GMS.TifoXRCoreWebAPI.Models;
using GMS.TifoXRCoreWebAPI.Repositories;

namespace GMS.TifoXRCoreWebAPI.Services
{
    public sealed class OrderService : IOrderService
    {
        private readonly IOrderRepository _repo;
        public OrderService(IOrderRepository repo) => _repo = repo;

        public Task<CreateOrderResponse> CreateOrderAsync(int spaceId, CreateOrderRequest req)
            => _repo.CreateOrderAsync(spaceId, req);

        public Task<OrderDto?> GetOrderAsync(int spaceId, string orderId)
            => _repo.GetOrderAsync(spaceId, orderId);

        public Task<InvoiceListResponse?> GetInvoicesAsync(int spaceId, string orderId)
            => _repo.GetInvoicesByOrderAsync(spaceId, orderId);

        public Task<List<EntitlementDto>?> GetEntitlementsByOrderAsync(int spaceId, string orderId)
            => _repo.GetEntitlementsByOrderAsync(spaceId, orderId);
    }
}
