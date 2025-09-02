using GMS.TifoXRCoreWebAPI.Models;

namespace GMS.TifoXRCoreWebAPI.Repositories.Interfaces
{
    /// <summary>
    /// Persistence-only contract. All gateway orchestration stays in the service layer.
    /// </summary>
    public interface IOrderRepository
    {
        // Queries
        Task<OrderDto?> GetOrderAsync(int spaceId, string orderId);
        Task<List<EntitlementDto>?> GetEntitlementsByOrderAsync(int spaceId, string orderId);
        Task<InvoiceListResponse?> GetInvoicesByOrderAsync(int spaceId, string orderId);

        // Commands (orders)
        Task<CreateOrderResponse> CreateOrderAsync(int pathSpaceId, CreateOrderRequest req);

        // Commands/Queries (payments) — persistence helpers used by PaymentService
        Task<(int SpaceId, int CurrencyId, long TotalNetMinor)?> GetOrderHeaderAsync(string orderId);
        Task<string> ResolveCurrencyIsoAsync(int currencyId);
        Task<(string Id, string? ProviderIntentId)?> FindPaymentIntentByIdempotencyAsync(string orderId, string idemKey);
        Task<string> InsertPaymentIntentAsync(string orderId, int gatewayId, int statusId, long amountMinor, int currencyId, string providerIntentId, string idempotencyKey);
        Task<(string OrderId, int SpaceId, int CurrencyId, long TotalNetMinor, string? ProviderIntentId)?> GetIntentContextAsync(string intentId);
        Task<string> InsertChargeAsync(string intentId, int statusId, long amountCapturedMinor, int currencyId, string providerChargeId, DateTime paidAtUtc);
        Task MarkPaidIfCoveredAsync(string orderId, long amountJustCapturedMinor, int paidStatusId);
        Task GrantEntitlementsAsync(string orderId);
        Task InsertInvoiceFromOrderAsync(string orderId, string chargeId, int gatewayId);
    }
}
