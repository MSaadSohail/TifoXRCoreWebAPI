using GMS.TifoXRCoreWebAPI.Models;
namespace GMS.TifoXRCoreWebAPI.Repositories.Interfaces
{


    public interface IOrderRepository
    {
        Task<OrderDto?> GetOrderAsync(int spaceId, string orderId);
        /// <summary>
        /// Returns null if order doesn't exist in the given space; otherwise returns the entitlements (possibly empty).
        /// </summary>
        Task<List<EntitlementDto>?> GetEntitlementsByOrderAsync(int spaceId, string orderId);

        /// <summary>
        /// Creates an order idempotently. If an order already exists for the given idempotency key (and space),
        /// returns that existing order summary.
        /// </summary>
        Task<CreateOrderResponse> CreateOrderAsync(int pathSpaceId, CreateOrderRequest req);

        Task<CreatePaymentIntentResponse> CreatePaymentIntentAsync(int spaceId, string orderId, CreatePaymentIntentRequest req);
        Task<ConfirmPaymentIntentResponse> ConfirmPaymentIntentAsync(int spaceId, string orderId, string intentId, ConfirmPaymentIntentRequest req);
        Task<InvoiceListResponse?> GetInvoicesByOrderAsync(int spaceId, string orderId);

    }

}
