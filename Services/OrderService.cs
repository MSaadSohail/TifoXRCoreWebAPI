using GMS.TifoXRCoreWebAPI.Models;
using GMS.TifoXRCoreWebAPI.Repositories.Interfaces;

namespace GMS.TifoXRCoreWebAPI.Services
{
    public sealed class OrderService : IOrderService
    {
        private readonly IOrderRepository _repo;
        public OrderService(IOrderRepository repo) => _repo = repo;

        public Task<CreateOrderResponse> CreateOrderAsync(int spaceId, CreateOrderRequest req)
            => _repo.CreateOrderAsync(spaceId, req); // your current logic is good here; repo already pure persistence

        public Task<OrderDto?> GetOrderAsync(int spaceId, string orderId)
            => _repo.GetOrderAsync(spaceId, orderId);

        public Task<InvoiceListResponse?> GetInvoicesAsync(int spaceId, string orderId)
            => _repo.GetInvoicesByOrderAsync(spaceId, orderId);
    }
}
