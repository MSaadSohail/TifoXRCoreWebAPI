using GMS.TifoXRCoreWebAPI.Models;

namespace GMS.TifoXRCoreWebAPI.Services
{
    public interface IOrderService
    {
        Task<CreateOrderResponse> CreateOrderAsync(int spaceId, CreateOrderRequest req);
        Task<OrderDto?> GetOrderAsync(int spaceId, string orderId);
        Task<InvoiceListResponse?> GetInvoicesAsync(int spaceId, string orderId);
    }
}
