// <copyright file="IOrderRepository.Payments.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Saad Sohail</author>
// <date>9/16/2025</date>
// <summary></summary>

namespace GMS.TifoXRCoreWebAPI.Repositories
{
    public partial interface IOrderRepository
    {
        Task<(int SpaceId, int CurrencyId, long TotalNetMinor, int? GatewayPreferredId)?>
            GetOrderHeaderAsync(string orderId);

        Task<(string IntentId, int StatusId, string IdempotencyKey, string? ProviderIntentId,
              int PaymentGatewayId, long AmountMinor, int CurrencyId)?>
            GetPendingIntentForOrderAsync(string orderId);

        Task<string?> FindLatestOrderIdWithPendingIntentAsync(int spaceId, string userId,
            int itemTypeId, int itemRefId);

        Task<string> ResolveCurrencyIsoAsync(int currencyId);

        Task<(string Id, string? ProviderIntentId)?> FindPaymentIntentByIdempotencyAsync(
            string orderId, string idempotencyKey);

        Task<string> InsertPaymentIntentAsync(string orderId, int gatewayId, int statusId,
            long amountMinor, int currencyId, string providerIntentId, string idempotencyKey);

        Task<(string OrderId, int SpaceId, int CurrencyId, long TotalNetMinor, string UserId,
              string? ProviderIntentId, int GatewayId)?>
            GetIntentContextAsync(string intentId);

        Task UpdatePaymentIntentStatusAsync(string intentId, int statusId);

        // Set a new provider intent id on an existing (pending) payment_intent row.
        Task<int> UpdatePaymentIntentProviderIdAsync(string intentId, string newProviderIntentId);

        Task<string> InsertChargeAsync(string intentId, int statusId, long amountCapturedMinor,
            int currencyId, string providerChargeId, DateTime paidAtUtc, string userId);

        Task MarkPaidIfCoveredAsync(string orderId, long amountJustCapturedMinor, int paidStatusId);

        Task<(string OrderId, int SpaceId, int CurrencyId, int GatewayId,
               string? ProviderChargeId, long AmountCapturedMinor, long TotalRefundedSoFarMinor)?>
            GetChargeContextAsync(string chargeId);

        Task<string> InsertRefundAsync(string chargeId, int statusId, long amountMinor,
            int currencyId, string providerRefundId, string? reason);
    }
}

