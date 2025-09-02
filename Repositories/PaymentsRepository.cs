// <copyright file="PaymentsRepository.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Urvashi Dhingra</author>
// <date>08/29/2025</date>
// <summary>Payments SQL repository</summary>

using System;
using System.Data.Common;
using System.Text.Json;
using System.Threading.Tasks;
using GMS.TifoXRCoreWebAPI.Models;
using GMS.TifoXRCoreWebAPI.Repositories.Interfaces;
using TifoXRCoreWebAPI.Utilities.Infrastructure.Interface;

namespace GMS.TifoXRCoreWebAPI.Repositories
{
    public class PaymentsRepository(IConfiguration configuration, IDbProvider db) : IPaymentsRepository
    {
        private readonly IDbProvider _db = db;

        // Normalize gateway to lowercase for robust comparisons
        private static string SqlGatewayName =>
            "SELECT LOWER(TRIM(payment_gateway)) FROM payment_gateway WHERE id = @Id;";

        private static string SqlPaymentStatusByName =>
            "SELECT id FROM payment_status WHERE status = @Name;";

        private static string SqlRefundStatusByName =>
            "SELECT id FROM refund_status WHERE status = @Name;";

        private static string SqlOrderStatusByName =>
            "SELECT id FROM order_status WHERE status = @Name;";

        #region INTENTS: GET

        public async Task<PaymentIntentData?> GetPaymentIntentAsync(int spaceId, string intentId)
        {
            const string sql = @"
                SELECT
                    pi.id,
                    o.space_id,
                    pi.order_id,
                    pi.payment_gateway_id,
                    pi.status_id,
                    pi.amount,
                    pi.currency_id,
                    pi.provider_intent_id,
                    CAST(pi.metadata AS CHAR) AS metadata_json
                FROM payment_intent pi
                INNER JOIN `order` o ON o.id = pi.order_id
                WHERE o.space_id = @SpaceId AND pi.id = @IntentId;";

            await using var conn = await _db.OpenConnectionAsync();
            await using var cmd = _db.CreateCommand(conn, sql);
            cmd.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));
            cmd.Parameters.Add(_db.CreateParameter("@IntentId", intentId));

            await using var rdr = await cmd.ExecuteReaderAsync();
            if (!await rdr.ReadAsync()) return null;

            return MapIntent(rdr);
        }

        #endregion

        #region INTENTS: POST

        public async Task<PaymentIntentData?> CreatePaymentIntentAsync(int spaceId, CreatePaymentIntentDto dto)
        {
            await using var conn = await _db.OpenConnectionAsync();
            await using var tx = await conn.BeginTransactionAsync();

            try
            {
                // Idempotency by key (intents table HAS idempotency_key)
                const string selExisting = @"
                    SELECT
                        pi.id, o.space_id, pi.order_id, pi.payment_gateway_id, pi.status_id,
                        pi.amount, pi.currency_id, pi.provider_intent_id,
                        CAST(pi.metadata AS CHAR) AS metadata_json
                    FROM payment_intent pi
                    INNER JOIN `order` o ON o.id = pi.order_id
                    WHERE o.space_id = @SpaceId AND pi.idempotency_key = @Idem;";
                await using (var s = _db.CreateCommand(conn, selExisting))
                {
                    s.Transaction = tx;
                    s.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));
                    s.Parameters.Add(_db.CreateParameter("@Idem", dto.IdempotencyKey));
                    await using var er = await s.ExecuteReaderAsync();
                    if (await er.ReadAsync())
                    {
                        await tx.CommitAsync();
                        var existing = MapIntent(er);
                        existing.ApproveLink = TryExtractApproveLink(existing.MetadataJson);
                        return existing;
                    }
                }

                // Load order totals / currency / status, lock row
                long totalMinor = 0;
                int orderCurrencyId = 0;
                int orderStatusId = 0;

                const string getOrderSql = @"
                    SELECT total_net_amount, currency_id, status_id
                    FROM `order`
                    WHERE id = @OrderId AND space_id = @SpaceId
                    FOR UPDATE;";
                await using (var oc = _db.CreateCommand(conn, getOrderSql))
                {
                    oc.Transaction = tx;
                    oc.Parameters.Add(_db.CreateParameter("@OrderId", dto.OrderId));
                    oc.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));
                    await using var r = await oc.ExecuteReaderAsync();
                    if (!await r.ReadAsync())
                        throw new InvalidOperationException($"Order {dto.OrderId} not found in space {spaceId}.");

                    totalMinor = Convert.ToInt64(r.GetValue(0));
                    orderCurrencyId = Convert.ToInt32(r.GetValue(1));
                    orderStatusId = Convert.ToInt32(r.GetValue(2));
                }

                // Disallow new intents for already-paid orders
                int paidStatusId;
                await using (var os = _db.CreateCommand(conn, SqlOrderStatusByName))
                {
                    os.Transaction = tx;
                    os.Parameters.Add(_db.CreateParameter("@Name", "paid"));
                    paidStatusId = Convert.ToInt32(await os.ExecuteScalarAsync());
                }
                if (orderStatusId == paidStatusId)
                    throw new InvalidOperationException("Order is already paid; cannot create a new payment intent.");

                // Gateway name
                string gatewayName;
                await using (var g = _db.CreateCommand(conn, SqlGatewayName))
                {
                    g.Transaction = tx;
                    g.Parameters.Add(_db.CreateParameter("@Id", dto.PaymentGatewayId));
                    gatewayName = (await g.ExecuteScalarAsync())?.ToString()
                                  ?? throw new InvalidOperationException("payment_gateway not found.");
                }

                // Initial status: requires_action
                int requiresActionStatusId;
                await using (var ps = _db.CreateCommand(conn, SqlPaymentStatusByName))
                {
                    ps.Transaction = tx;
                    ps.Parameters.Add(_db.CreateParameter("@Name", "requires_action"));
                    requiresActionStatusId = Convert.ToInt32(await ps.ExecuteScalarAsync());
                }

                // Currency check (must match order currency unless unspecified)
                var currencyId = dto.CurrencyId ?? orderCurrencyId;
                if (dto.CurrencyId.HasValue && dto.CurrencyId.Value != orderCurrencyId)
                    throw new InvalidOperationException("Currency mismatch between order and payment intent.");

                // Compose provider data
                string? providerIntentId = null;
                string? approveLink = null;
                string metadataJson;

                if (gatewayName == "paypal")
                {
                    providerIntentId = Guid.NewGuid().ToString();
                    approveLink = $"https://www.sandbox.paypal.com/checkoutnow?token={providerIntentId}";
                    var meta = new { approve_link = approveLink, idem_key = dto.IdempotencyKey };
                    metadataJson = JsonSerializer.Serialize(meta);
                }
                else if (gatewayName == "crypto_wallet")
                {
                    var toAddr = "0x" + Guid.NewGuid().ToString("N")[..40];
                    var meta = new
                    {
                        network = dto.Network ?? "polygon",
                        token = "USDC",
                        to_address = toAddr,
                        expected_amount_minor = totalMinor,
                        decimals = 6,
                        qr_payload = $"ethereum:{toAddr}?value={totalMinor}",
                        idem_key = dto.IdempotencyKey
                    };
                    metadataJson = JsonSerializer.Serialize(meta);
                }
                else
                {
                    // Generic gateway: external/manual capture path
                    providerIntentId = $"gen_{Guid.NewGuid():N}";
                    var meta = new
                    {
                        gateway = gatewayName,
                        next_action = "external_capture",
                        instructions = "Complete capture via the external provider, then confirm via webhook or ops.",
                        idem_key = dto.IdempotencyKey
                    };
                    metadataJson = JsonSerializer.Serialize(meta);
                }

                // Insert intent
                var intentId = Guid.NewGuid().ToString();

                const string ins = @"
                    INSERT INTO payment_intent
                        (id, order_id, payment_gateway_id, status_id, amount, currency_id,
                         client_secret, provider_intent_id, idempotency_key, metadata,
                         creation_time, modified_time, modified_by)
                    VALUES
                        (@Id, @OrderId, @GatewayId, @StatusId, @Amount, @CurrencyId,
                         NULL, @ProviderIntentId, @Idem, @Metadata,
                         UTC_TIMESTAMP(), UTC_TIMESTAMP(), 'system');";
                await using (var cmd = _db.CreateCommand(conn, ins))
                {
                    cmd.Transaction = tx;
                    cmd.Parameters.Add(_db.CreateParameter("@Id", intentId));
                    cmd.Parameters.Add(_db.CreateParameter("@OrderId", dto.OrderId));
                    cmd.Parameters.Add(_db.CreateParameter("@GatewayId", dto.PaymentGatewayId));
                    cmd.Parameters.Add(_db.CreateParameter("@StatusId", requiresActionStatusId));
                    cmd.Parameters.Add(_db.CreateParameter("@Amount", totalMinor));
                    cmd.Parameters.Add(_db.CreateParameter("@CurrencyId", currencyId));
                    cmd.Parameters.Add(_db.CreateParameter("@ProviderIntentId", (object?)providerIntentId ?? DBNull.Value));
                    cmd.Parameters.Add(_db.CreateParameter("@Idem", dto.IdempotencyKey));
                    cmd.Parameters.Add(_db.CreateParameter("@Metadata", (object?)metadataJson ?? DBNull.Value));
                    await cmd.ExecuteNonQueryAsync();
                }

                await tx.CommitAsync();

                var created = await GetPaymentIntentAsync(spaceId, intentId);
                created!.ApproveLink = TryExtractApproveLink(created.MetadataJson);
                return created;
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        #endregion

        #region CHARGES: GET

        public async Task<PaymentChargeData?> GetChargeAsync(int spaceId, string chargeId)
        {
            const string sql = @"
                SELECT
                    c.id,
                    o.space_id,
                    i.order_id,
                    c.payment_intent_id,
                    c.amount_captured,
                    c.status_id,
                    c.currency_id,
                    c.provider_charge_id,
                    CAST(c.metadata AS CHAR) AS metadata_json,
                    c.payment_datetime
                FROM payment_charge c
                INNER JOIN payment_intent i ON i.id = c.payment_intent_id
                INNER JOIN `order` o ON o.id = i.order_id
                WHERE o.space_id = @SpaceId AND c.id = @ChargeId;";

            await using var conn = await _db.OpenConnectionAsync();
            await using var cmd = _db.CreateCommand(conn, sql);
            cmd.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));
            cmd.Parameters.Add(_db.CreateParameter("@ChargeId", chargeId));

            await using var rdr = await cmd.ExecuteReaderAsync();
            if (!await rdr.ReadAsync()) return null;
            return MapCharge(rdr);
        }

        #endregion

        #region REFUNDS: POST/GET

        public async Task<PaymentRefundData?> CreateRefundAsync(int spaceId, CreateRefundDto dto)
        {
            await using var conn = await _db.OpenConnectionAsync();
            await using var tx = await conn.BeginTransactionAsync();

            try
            {
                // Safe de-dup WITHOUT idempotency_key (Teleport-style simplicity)
                const string selExisting = @"
                    SELECT
                        r.id,
                        o.space_id,
                        r.payment_charge_id,
                        r.amount,
                        r.status_id,
                        r.currency_id,
                        r.provider_refund_id,
                        r.reason
                    FROM payment_refund r
                    INNER JOIN payment_charge c ON c.id = r.payment_charge_id
                    INNER JOIN payment_intent i ON i.id = c.payment_intent_id
                    INNER JOIN `order` o ON o.id = i.order_id
                    WHERE o.space_id = @SpaceId
                      AND r.payment_charge_id = @ChargeId
                      AND r.amount = @Amount
                      AND r.status_id IN (SELECT id FROM refund_status WHERE status IN ('pending','succeeded'))
                    LIMIT 1;";
                await using (var se = _db.CreateCommand(conn, selExisting))
                {
                    se.Transaction = tx;
                    se.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));
                    se.Parameters.Add(_db.CreateParameter("@ChargeId", dto.PaymentChargeId));
                    se.Parameters.Add(_db.CreateParameter("@Amount", dto.Amount));
                    await using var er = await se.ExecuteReaderAsync();
                    if (await er.ReadAsync())
                    {
                        await tx.CommitAsync();
                        return MapRefund(er);
                    }
                }

                // Lock charge, validate it belongs to this space
                long captured = 0;
                int currencyId = 0;
                const string selCharge = @"
                    SELECT c.amount_captured, c.currency_id
                    FROM payment_charge c
                    INNER JOIN payment_intent i ON i.id = c.payment_intent_id
                    INNER JOIN `order` o ON o.id = i.order_id
                    WHERE o.space_id = @SpaceId AND c.id = @ChargeId
                    FOR UPDATE;";
                await using (var sc = _db.CreateCommand(conn, selCharge))
                {
                    sc.Transaction = tx;
                    sc.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));
                    sc.Parameters.Add(_db.CreateParameter("@ChargeId", dto.PaymentChargeId));
                    await using var r = await sc.ExecuteReaderAsync();
                    if (!await r.ReadAsync())
                        throw new InvalidOperationException($"Charge {dto.PaymentChargeId} not found in space {spaceId}.");
                    captured = Convert.ToInt64(r.GetValue(0));
                    currencyId = Convert.ToInt32(r.GetValue(1));
                }

                // Sum existing refunds (pending/succeeded)
                long refunded = 0;
                const string sumRefunds = @"
                    SELECT COALESCE(SUM(r.amount),0)
                    FROM payment_refund r
                    WHERE r.payment_charge_id = @ChargeId
                      AND r.status_id IN (SELECT id FROM refund_status WHERE status IN ('pending','succeeded'));";
                await using (var sr = _db.CreateCommand(conn, sumRefunds))
                {
                    sr.Transaction = tx;
                    sr.Parameters.Add(_db.CreateParameter("@ChargeId", dto.PaymentChargeId));
                    refunded = Convert.ToInt64(await sr.ExecuteScalarAsync());
                }

                if (dto.Amount <= 0 || dto.Amount + refunded > captured)
                    throw new InvalidOperationException("Refund amount exceeds captured total or is invalid.");

                // Pending status id
                int pendingStatusId;
                await using (var rs = _db.CreateCommand(conn, SqlRefundStatusByName))
                {
                    rs.Transaction = tx;
                    rs.Parameters.Add(_db.CreateParameter("@Name", "pending"));
                    pendingStatusId = Convert.ToInt32(await rs.ExecuteScalarAsync());
                }

                // Insert refund
                var refundId = Guid.NewGuid().ToString();
                const string ins = @"
                    INSERT INTO payment_refund
                        (id, payment_charge_id, status_id, amount, currency_id,
                         provider_refund_id, reason, refund_datetime, metadata,
                         creation_time, modified_time, modified_by)
                    VALUES
                        (@Id, @ChargeId, @StatusId, @Amount, @CurrencyId,
                         NULL, @Reason, UTC_TIMESTAMP(), NULL,
                         UTC_TIMESTAMP(), UTC_TIMESTAMP(), 'system');";
                await using (var ir = _db.CreateCommand(conn, ins))
                {
                    ir.Transaction = tx;
                    ir.Parameters.Add(_db.CreateParameter("@Id", refundId));
                    ir.Parameters.Add(_db.CreateParameter("@ChargeId", dto.PaymentChargeId));
                    ir.Parameters.Add(_db.CreateParameter("@StatusId", pendingStatusId));
                    ir.Parameters.Add(_db.CreateParameter("@Amount", dto.Amount));
                    ir.Parameters.Add(_db.CreateParameter("@CurrencyId", currencyId));
                    ir.Parameters.Add(_db.CreateParameter("@Reason", (object?)dto.Reason ?? DBNull.Value));
                    await ir.ExecuteNonQueryAsync();
                }

                await tx.CommitAsync();
                return await GetRefundAsync(spaceId, refundId);
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        public async Task<PaymentRefundData?> GetRefundAsync(int spaceId, string refundId)
        {
            const string sql = @"
                SELECT
                    r.id,
                    o.space_id,
                    r.payment_charge_id,
                    r.amount,
                    r.status_id,
                    r.currency_id,
                    r.provider_refund_id,
                    r.reason
                FROM payment_refund r
                INNER JOIN payment_charge c ON c.id = r.payment_charge_id
                INNER JOIN payment_intent i ON i.id = c.payment_intent_id
                INNER JOIN `order` o ON o.id = i.order_id
                WHERE o.space_id = @SpaceId AND r.id = @RefundId;";

            await using var conn = await _db.OpenConnectionAsync();
            await using var cmd = _db.CreateCommand(conn, sql);
            cmd.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));
            cmd.Parameters.Add(_db.CreateParameter("@RefundId", refundId));

            await using var rdr = await cmd.ExecuteReaderAsync();
            if (!await rdr.ReadAsync()) return null;
            return MapRefund(rdr);
        }

        #endregion

        #region MAP

        private static PaymentIntentData MapIntent(DbDataReader rdr)
        {
            var data = new PaymentIntentData
            {
                Id = rdr.GetString(rdr.GetOrdinal("id")),
                SpaceId = rdr.GetInt32(rdr.GetOrdinal("space_id")),
                OrderId = rdr.GetString(rdr.GetOrdinal("order_id")),
                PaymentGatewayId = rdr.GetInt32(rdr.GetOrdinal("payment_gateway_id")),
                StatusId = rdr.GetInt32(rdr.GetOrdinal("status_id")),
                Amount = rdr.GetInt64(rdr.GetOrdinal("amount")),
                CurrencyId = rdr.GetInt32(rdr.GetOrdinal("currency_id")),
                ProviderIntentId = rdr.IsDBNull(rdr.GetOrdinal("provider_intent_id")) ? null : rdr.GetString(rdr.GetOrdinal("provider_intent_id")),
                MetadataJson = rdr.IsDBNull(rdr.GetOrdinal("metadata_json")) ? null : rdr.GetString(rdr.GetOrdinal("metadata_json")),
            };
            data.ApproveLink = TryExtractApproveLink(data.MetadataJson);
            return data;
        }

        private static PaymentChargeData MapCharge(DbDataReader rdr)
        {
            return new PaymentChargeData
            {
                Id = rdr.GetString(rdr.GetOrdinal("id")),
                SpaceId = rdr.GetInt32(rdr.GetOrdinal("space_id")),
                OrderId = rdr.GetString(rdr.GetOrdinal("order_id")),
                PaymentIntentId = rdr.GetString(rdr.GetOrdinal("payment_intent_id")),
                AmountCaptured = rdr.GetInt64(rdr.GetOrdinal("amount_captured")),
                StatusId = rdr.GetInt32(rdr.GetOrdinal("status_id")),
                CurrencyId = rdr.GetInt32(rdr.GetOrdinal("currency_id")),
                ProviderChargeId = rdr.GetString(rdr.GetOrdinal("provider_charge_id")),
                MetadataJson = rdr.IsDBNull(rdr.GetOrdinal("metadata_json")) ? null : rdr.GetString(rdr.GetOrdinal("metadata_json")),
                PaymentDateTime = rdr.IsDBNull(rdr.GetOrdinal("payment_datetime")) ? null : rdr.GetDateTime(rdr.GetOrdinal("payment_datetime")),
            };
        }

        private static PaymentRefundData MapRefund(DbDataReader rdr)
        {
            return new PaymentRefundData
            {
                Id = rdr.GetString(rdr.GetOrdinal("id")),
                SpaceId = rdr.GetInt32(rdr.GetOrdinal("space_id")),
                PaymentChargeId = rdr.GetString(rdr.GetOrdinal("payment_charge_id")),
                Amount = rdr.GetInt64(rdr.GetOrdinal("amount")),
                StatusId = rdr.GetInt32(rdr.GetOrdinal("status_id")),
                CurrencyId = rdr.GetInt32(rdr.GetOrdinal("currency_id")),
                ProviderRefundId = rdr.IsDBNull(rdr.GetOrdinal("provider_refund_id")) ? null : rdr.GetString(rdr.GetOrdinal("provider_refund_id")),
                Reason = rdr.IsDBNull(rdr.GetOrdinal("reason")) ? null : rdr.GetString(rdr.GetOrdinal("reason")),
            };
        }

        private static string? TryExtractApproveLink(string? metadataJson)
        {
            if (string.IsNullOrWhiteSpace(metadataJson)) return null;
            try
            {
                using var doc = JsonDocument.Parse(metadataJson);
                if (doc.RootElement.TryGetProperty("approve_link", out var link) && link.ValueKind == JsonValueKind.String)
                    return link.GetString();
            }
            catch { /* ignore */ }
            return null;
        }

        #endregion
    }
}
