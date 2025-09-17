// <copyright file="OrderRepository.Payments.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Saad Sohail</author>
// <date>9/16/2025</date>
// <summary></summary>

// OrderRepository.Payments.cs
using static GMS.TifoXRCoreWebAPI.Repositories.SQL.OrderSql;
using static GMS.TifoXRCoreWebAPI.Repositories.SQL.PaymentIntentSql;

namespace GMS.TifoXRCoreWebAPI.Repositories
{
    /// <summary>Payment persistence helpers (used by your PaymentService).</summary>
    public sealed partial class OrderRepository : IOrderRepository
    {
        public async Task<(int SpaceId, int CurrencyId, long TotalNetMinor, int? GatewayPreferredId)?>
            GetOrderHeaderAsync(string orderId)
        {
            await using var conn = await _db.OpenConnectionAsync();
            await using var cmd = _db.CreateCommand(conn, Order_GetHeader);

            cmd.Parameters.Add(_db.CreateParameter("@OrderId", orderId));

            await using var r = await cmd.ExecuteReaderAsync();
            if (!await r.ReadAsync()) return null;

            return (
                r.GetInt32(r.GetOrdinal("space_id")),
                r.GetInt32(r.GetOrdinal("currency_id")),
                r.GetInt64(r.GetOrdinal("total_net_amount")),
                r.IsDBNull(r.GetOrdinal("gateway_preferred_id")) 
                    ? null 
                    : r.GetInt32(r.GetOrdinal("gateway_preferred_id"))
            );
        }

        public async Task<string> ResolveCurrencyIsoAsync(int currencyId)
        {
            
            await using var conn = await _db.OpenConnectionAsync();
            await using var cmd = _db.CreateCommand(conn, Currency_SelectIsoById);

            cmd.Parameters.Add(_db.CreateParameter("@Id", currencyId));

            var o = await cmd.ExecuteScalarAsync();

            if (o is null || o == DBNull.Value) 
                throw new InvalidOperationException($"Unknown currency id '{currencyId}'.");
            
            return Convert.ToString(o)!;
        }

        public async Task<(string Id, string? ProviderIntentId)?> FindPaymentIntentByIdempotencyAsync(string orderId, string idemKey)
        {
            await using var conn = await _db.OpenConnectionAsync();
            await using var cmd = _db.CreateCommand(conn, Intent_FindByIdem);

            cmd.Parameters.Add(_db.CreateParameter("@OrderId", orderId));
            cmd.Parameters.Add(_db.CreateParameter("@Key", idemKey));

            await using var r = await cmd.ExecuteReaderAsync();

            if (!await r.ReadAsync()) return null;

            return (
                r.GetString(r.GetOrdinal("id")),
                r.IsDBNull(r.GetOrdinal("provider_intent_id")) 
                    ? null 
                    : r.GetString(r.GetOrdinal("provider_intent_id"))
            );
        }

        public async Task<string> InsertPaymentIntentAsync(string orderId, int gatewayId, int statusId, long amountMinor, int currencyId, string providerIntentId, string idempotencyKey)
        {
            var id = Guid.NewGuid().ToString();

            await using var conn = await _db.OpenConnectionAsync();
            await using var cmd = _db.CreateCommand(conn, Intent_Insert);

            cmd.Parameters.Add(_db.CreateParameter("@Id", id));
            cmd.Parameters.Add(_db.CreateParameter("@OrderId", orderId));
            cmd.Parameters.Add(_db.CreateParameter("@Gw", gatewayId));
            cmd.Parameters.Add(_db.CreateParameter("@Status", statusId));
            cmd.Parameters.Add(_db.CreateParameter("@Amt", amountMinor));
            cmd.Parameters.Add(_db.CreateParameter("@Ccy", currencyId));
            cmd.Parameters.Add(_db.CreateParameter("@ProvId", providerIntentId));
            cmd.Parameters.Add(_db.CreateParameter("@Key", idempotencyKey));
            cmd.Parameters.Add(_db.CreateParameter("@ModBy", "system"));
            
            await cmd.ExecuteNonQueryAsync();
            
            return id;
        }

        public async Task UpdatePaymentIntentStatusAsync(string intentId, int statusId)
        {
            await using var conn = await _db.OpenConnectionAsync();
            await using var cmd = _db.CreateCommand(conn, Intent_UpdateStatus);

            cmd.Parameters.Add(_db.CreateParameter("@IntentId", intentId));
            cmd.Parameters.Add(_db.CreateParameter("@Status", statusId));
            cmd.Parameters.Add(_db.CreateParameter("@ModBy", "system"));

            await cmd.ExecuteNonQueryAsync();
        }

        public async Task<(string OrderId, int SpaceId, int CurrencyId, long TotalNetMinor, string UserId, string? ProviderIntentId, int GatewayId)?>
            GetIntentContextAsync(string intentId)
        {
            await using var conn = await _db.OpenConnectionAsync();
            await using var cmd = _db.CreateCommand(conn, Intent_Context);

            cmd.Parameters.Add(_db.CreateParameter("@IntentId", intentId));

            await using var r = await cmd.ExecuteReaderAsync();

            if (!await r.ReadAsync()) return null;

            return (
                r.GetString(r.GetOrdinal("order_id")),
                r.GetInt32(r.GetOrdinal("space_id")),
                r.GetInt32(r.GetOrdinal("currency_id")),
                r.GetInt64(r.GetOrdinal("total_net_amount")),
                r.GetString(r.GetOrdinal("user_id")),
                r.IsDBNull(r.GetOrdinal("provider_intent_id")) 
                    ? null 
                    : r.GetString(r.GetOrdinal("provider_intent_id")),
                r.GetInt32(r.GetOrdinal("payment_gateway_id"))
            );
        }

        public async Task<string> InsertChargeAsync(string intentId, int statusId, long amountCapturedMinor, int currencyId, string providerChargeId, DateTime paidAtUtc, string userId)
        {
            var id = Guid.NewGuid().ToString();

            await using var conn = await _db.OpenConnectionAsync();
            await using var cmd = _db.CreateCommand(conn, Charge_Insert);

            cmd.Parameters.Add(_db.CreateParameter("@Id", id));
            cmd.Parameters.Add(_db.CreateParameter("@IntentId", intentId));
            cmd.Parameters.Add(_db.CreateParameter("@Status", statusId));
            cmd.Parameters.Add(_db.CreateParameter("@Amt", amountCapturedMinor));
            cmd.Parameters.Add(_db.CreateParameter("@Ccy", currencyId));
            cmd.Parameters.Add(_db.CreateParameter("@ProvCharge", providerChargeId));
            cmd.Parameters.Add(_db.CreateParameter("@PaidAt", paidAtUtc));
            cmd.Parameters.Add(_db.CreateParameter("@ModBy", "system"));

            await cmd.ExecuteNonQueryAsync();

            return id;
        }

        public async Task MarkPaidIfCoveredAsync(string orderId, long amountJustCapturedMinor, int paidStatusId)
        {
            await using var conn = await _db.OpenConnectionAsync();

            long totalNet;

            await using (var cmd = _db.CreateCommand(conn, Order_GetTotalNet))
            {
                cmd.Parameters.Add(_db.CreateParameter("@OrderId", orderId));

                var o = await cmd.ExecuteScalarAsync();

                if (o is null || o == DBNull.Value) return;

                totalNet = Convert.ToInt64(o);
            }

            long captured;

            await using (var cmd = _db.CreateCommand(conn, Order_SumCaptured))
            {
                cmd.Parameters.Add(_db.CreateParameter("@OrderId", orderId));
                captured = Convert.ToInt64(await cmd.ExecuteScalarAsync() ?? 0);
            }

            if (captured >= totalNet)
            {
                await using var upd = _db.CreateCommand(conn, Order_UpdatePaidStatus);

                upd.Parameters.Add(_db.CreateParameter("@PaidStatusId", paidStatusId));
                upd.Parameters.Add(_db.CreateParameter("@OrderId", orderId));
                upd.Parameters.Add(_db.CreateParameter("@ModBy", "system"));

                await upd.ExecuteNonQueryAsync();
            }
        }

        public async Task<(string IntentId, int StatusId, string IdempotencyKey, string? ProviderIntentId,
                          int PaymentGatewayId, long AmountMinor, int CurrencyId)?>
            GetPendingIntentForOrderAsync(string orderId)
        {
            await using var conn = await _db.OpenConnectionAsync();
            await using var cmd = _db.CreateCommand(conn, Intent_FindPendingForOrder);

            cmd.Parameters.Add(_db.CreateParameter("@OrderId", orderId));

            await using var r = await cmd.ExecuteReaderAsync();

            if (!await r.ReadAsync()) return null;

            return (
                r.GetString(r.GetOrdinal("id")),
                r.GetInt32(r.GetOrdinal("status_id")),
                r.GetString(r.GetOrdinal("idempotency_key")),
                r.IsDBNull(r.GetOrdinal("provider_intent_id")) ? null : r.GetString(r.GetOrdinal("provider_intent_id")),
                r.GetInt32(r.GetOrdinal("payment_gateway_id")),
                r.GetInt64(r.GetOrdinal("amount")),
                r.GetInt32(r.GetOrdinal("currency_id"))
            );
        }

        // Matches: FindLatestOrderIdWithPendingIntentAsync(int, string, int, int)
        public async Task<string?> FindLatestOrderIdWithPendingIntentAsync(
            int spaceId, string userId, int itemTypeId, int itemRefId)
        {
            await using var conn = await _db.OpenConnectionAsync();
            await using var cmd = _db.CreateCommand(conn, Intent_FindLatestOrderWithPending);

            cmd.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));
            cmd.Parameters.Add(_db.CreateParameter("@UserId", userId));
            cmd.Parameters.Add(_db.CreateParameter("@ItemTypeId", itemTypeId));
            cmd.Parameters.Add(_db.CreateParameter("@ItemRefId", itemRefId));

            var o = await cmd.ExecuteScalarAsync();

            return o is null || o == DBNull.Value ? null : Convert.ToString(o);
        }

        public async Task<(string OrderId, int SpaceId, int CurrencyId, int GatewayId,
                           string? ProviderChargeId, long AmountCapturedMinor, long TotalRefundedSoFarMinor)?>
            GetChargeContextAsync(string chargeId)
        {
            await using var conn = await _db.OpenConnectionAsync();
            await using var cmd = _db.CreateCommand(conn, Charge_GetContext);

            cmd.Parameters.Add(_db.CreateParameter("@ChargeId", chargeId));

            await using var r = await cmd.ExecuteReaderAsync();

            if (!await r.ReadAsync()) return null;

            return (
                OrderId: r.GetString(r.GetOrdinal("order_id")),
                SpaceId: r.GetInt32(r.GetOrdinal("space_id")),
                CurrencyId: r.GetInt32(r.GetOrdinal("currency_id")),
                GatewayId: r.GetInt32(r.GetOrdinal("payment_gateway_id")),
                ProviderChargeId: r.IsDBNull(r.GetOrdinal("provider_charge_id")) 
                    ? null 
                    : r.GetString(r.GetOrdinal("provider_charge_id")),
                AmountCapturedMinor: r.GetInt64(r.GetOrdinal("amount_captured_minor")),
                TotalRefundedSoFarMinor: r.GetInt64(r.GetOrdinal("total_refunded_so_far_minor"))
            );
        }

        public async Task<string> InsertRefundAsync(
            string chargeId, int statusId, long amountMinor, int currencyId,
            string providerRefundId, string? reason)
        {
            var id = Guid.NewGuid().ToString();
            await using var conn = await _db.OpenConnectionAsync();
            await using var cmd = _db.CreateCommand(conn, Refund_Insert);

            cmd.Parameters.Add(_db.CreateParameter("@Id", id));
            cmd.Parameters.Add(_db.CreateParameter("@ChargeId", chargeId));
            cmd.Parameters.Add(_db.CreateParameter("@Status", statusId));
            cmd.Parameters.Add(_db.CreateParameter("@Amt", amountMinor));
            cmd.Parameters.Add(_db.CreateParameter("@Ccy", currencyId));
            cmd.Parameters.Add(_db.CreateParameter("@ProvRefund", providerRefundId));
            cmd.Parameters.Add(_db.CreateParameter("@Reason", (object?)reason ?? DBNull.Value));
            cmd.Parameters.Add(_db.CreateParameter("@ModBy", "system"));

            await cmd.ExecuteNonQueryAsync();

            return id;
        }
    }
}
