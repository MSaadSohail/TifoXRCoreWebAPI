// <copyright file="IOrderRepository.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Saad Sohail</author>
// <date>09/02/2025</date>
// <summary></summary>

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

        Task<(string IntentId, int StatusId, string IdempotencyKey, string? ProviderIntentId, int PaymentGatewayId, long AmountMinor, int CurrencyId)?>
        GetPendingIntentForOrderAsync(string orderId);

        Task<string?> FindLatestOrderIdWithPendingIntentAsync(int spaceId, string userId, int itemTypeId, int itemRefId);

        // Commands (orders)
        Task<CreateOrderResponse> CreateOrderAsync(int pathSpaceId, CreateOrderRequest req, int? gatewayPreferredId = null);

        // Commands/Queries (payments) — persistence helpers used by PaymentService
        Task<(int SpaceId, int CurrencyId, long TotalNetMinor, int? GatewayPreferredId)?> GetOrderHeaderAsync(string orderId);
        Task<string> ResolveCurrencyIsoAsync(int currencyId);
        Task<(string Id, string? ProviderIntentId)?> FindPaymentIntentByIdempotencyAsync(string orderId, string idemKey);
        Task<string> InsertPaymentIntentAsync(string orderId, int gatewayId, int statusId, long amountMinor, int currencyId, string providerIntentId, string idempotencyKey);
        Task<(string OrderId, int SpaceId, int CurrencyId, long TotalNetMinor, string UserId, string? ProviderIntentId, int GatewayId)?> GetIntentContextAsync(string intentId);
        Task<string> InsertChargeAsync(string intentId, int statusId, long amountCapturedMinor, int currencyId, string providerChargeId, DateTime paidAtUtc, string userId);
        Task MarkPaidIfCoveredAsync(string orderId, long amountJustCapturedMinor, int paidStatusId);
        Task GrantEntitlementsAsync(string orderId);
        Task InsertInvoiceFromOrderAsync(int spaceId, string orderId, string chargeId, int gatewayId);
    }
}
