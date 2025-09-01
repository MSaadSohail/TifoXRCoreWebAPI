// Repositories/OrderRepository.cs
using GMS.TifoXRCoreWebAPI.Models;
using GMS.TifoXRCoreWebAPI.Repositories.Interfaces;
using System.Data;
using System.Text.Json;
using TifoXRCoreWebAPI.Utilities.Infrastructure.Interface; // IDbProvider

namespace GMS.TifoXRCoreWebAPI.Repositories
{
    public sealed class OrderRepository : IOrderRepository
    {
        private readonly IDbProvider _db;

        public OrderRepository(IConfiguration configuration, IDbProvider db)
        {
            _db = db;
        }

        public async Task<OrderDto?> GetOrderAsync(int spaceId, string orderId)
        {
            const string sqlOrder = @"
SELECT id, user_id, space_id, transaction_type_id, currency_id, status_id,
       total_gross_amount, total_discount_amount, total_tax_amount, total_fee_amount, total_net_amount,
       change_reason, original_order_id, session_id, gateway_preferred_id, order_datetime, remarks
FROM `order`
WHERE id = @OrderId AND space_id = @SpaceId;";

            const string sqlLines = @"
SELECT id, item_type_id, item_ref_id, entity_id, shop_id, quantity, unit_amount, currency_id, metadata
FROM order_line
WHERE order_id = @OrderId
ORDER BY id;";

            const string sqlAdjust = @"
SELECT id, order_line_id, item_type_id, code, amount, description_key, metadata
FROM order_adjustment
WHERE order_id = @OrderId
ORDER BY id;";

            const string sqlIntents = @"
SELECT id, payment_gateway_id, status_id, amount, currency_id, client_secret, provider_intent_id
FROM payment_intent
WHERE order_id = @OrderId
ORDER BY creation_time;";

            const string sqlCharges = @"
SELECT id, payment_intent_id, status_id, amount_captured, currency_id,
       provider_charge_id, gateway_fee_amount, exchange_rate, payment_datetime
FROM payment_charge
WHERE payment_intent_id IN (SELECT id FROM payment_intent WHERE order_id = @OrderId)
ORDER BY payment_datetime;";

            const string sqlRefunds = @"
SELECT id, payment_charge_id, status_id, amount, currency_id,
       provider_refund_id, reason, refund_datetime
FROM payment_refund
WHERE payment_charge_id IN (
    SELECT pc.id FROM payment_charge pc
    WHERE pc.payment_intent_id IN (SELECT id FROM payment_intent WHERE order_id = @OrderId)
)
ORDER BY refund_datetime;";

            const string sqlEntitlements = @"
SELECT id, order_line_id, user_id, status, quantity, granted_datetime, revoked_reason, metadata
FROM entitlement
WHERE order_line_id IN (SELECT id FROM order_line WHERE order_id = @OrderId)
ORDER BY creation_time;";

            const string sqlInvoices = @"
SELECT id, invoice_number, status_id, currency_id, total_amount, issue_datetime, pdf_url
FROM invoice
WHERE order_id = @OrderId
ORDER BY issue_datetime;";

            await using var conn = await _db.OpenConnectionAsync();

            // ===== 1) Order header =====
            OrderDto? order = null;
            await using (var cmd = _db.CreateCommand(conn, sqlOrder))
            {
                cmd.Parameters.Add(_db.CreateParameter("@OrderId", orderId));
                cmd.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));
                await using var r = await cmd.ExecuteReaderAsync();

                if (!await r.ReadAsync())
                    return null;

                bool ordReady = false;
                int o_id = -1, o_user = -1, o_space = -1, o_trx = -1, o_ccy = -1, o_status = -1,
                    o_gross = -1, o_disc = -1, o_tax = -1, o_fee = -1, o_net = -1,
                    o_reason = -1, o_orig = -1, o_session = -1, o_gw = -1, o_dt = -1, o_remarks = -1;

                if (!ordReady)
                {
                    o_id = r.GetOrdinal("id");
                    o_user = r.GetOrdinal("user_id");
                    o_space = r.GetOrdinal("space_id");
                    o_trx = r.GetOrdinal("transaction_type_id");
                    o_ccy = r.GetOrdinal("currency_id");
                    o_status = r.GetOrdinal("status_id");
                    o_gross = r.GetOrdinal("total_gross_amount");
                    o_disc = r.GetOrdinal("total_discount_amount");
                    o_tax = r.GetOrdinal("total_tax_amount");
                    o_fee = r.GetOrdinal("total_fee_amount");
                    o_net = r.GetOrdinal("total_net_amount");
                    o_reason = r.GetOrdinal("change_reason");
                    o_orig = r.GetOrdinal("original_order_id");
                    o_session = r.GetOrdinal("session_id");
                    o_gw = r.GetOrdinal("gateway_preferred_id");
                    o_dt = r.GetOrdinal("order_datetime");
                    o_remarks = r.GetOrdinal("remarks");
                    ordReady = true;
                }

                order = new OrderDto
                {
                    Id = r.GetString(o_id),
                    UserId = r.GetString(o_user),
                    SpaceId = r.GetInt32(o_space),
                    TransactionTypeId = r.GetInt32(o_trx),
                    CurrencyId = r.GetInt32(o_ccy),
                    StatusId = r.GetInt32(o_status),
                    TotalGrossAmount = r.GetInt64(o_gross),
                    TotalDiscountAmount = r.GetInt64(o_disc),
                    TotalTaxAmount = r.GetInt64(o_tax),
                    TotalFeeAmount = r.GetInt64(o_fee),
                    TotalNetAmount = r.GetInt64(o_net),
                    ChangeReason = r.IsDBNull(o_reason) ? null : r.GetString(o_reason),
                    OriginalOrderId = r.IsDBNull(o_orig) ? null : r.GetString(o_orig),
                    SessionId = r.GetString(o_session),
                    GatewayPreferredId = r.IsDBNull(o_gw) ? null : r.GetInt32(o_gw),
                    OrderDateTime = r.GetDateTime(o_dt),
                    Remarks = r.IsDBNull(o_remarks) ? null : r.GetString(o_remarks),
                    Lines = new(),
                    Adjustments = new(),
                    PaymentIntents = new(),
                    Entitlements = new(),
                    Invoices = new()
                };
            }

            // ===== 2) Lines =====
            await using (var cmd = _db.CreateCommand(conn, sqlLines))
            {
                cmd.Parameters.Add(_db.CreateParameter("@OrderId", orderId));
                await using var r = await cmd.ExecuteReaderAsync();

                bool ordReady = false;
                int o_id = -1, o_itemType = -1, o_itemRef = -1, o_entity = -1, o_shop = -1,
                    o_qty = -1, o_unit = -1, o_ccy = -1, o_meta = -1;

                while (await r.ReadAsync())
                {
                    if (!ordReady)
                    {
                        o_id = r.GetOrdinal("id");
                        o_itemType = r.GetOrdinal("item_type_id");
                        o_itemRef = r.GetOrdinal("item_ref_id");
                        o_entity = r.GetOrdinal("entity_id");
                        o_shop = r.GetOrdinal("shop_id");
                        o_qty = r.GetOrdinal("quantity");
                        o_unit = r.GetOrdinal("unit_amount"); // DECIMAL(9,6)
                        o_ccy = r.GetOrdinal("currency_id");
                        o_meta = r.GetOrdinal("metadata");
                        ordReady = true;
                    }

                    order!.Lines.Add(new OrderLineDto
                    {
                        Id = r.GetString(o_id),
                        ItemTypeId = r.GetInt32(o_itemType),
                        ItemRefId = r.GetString(o_itemRef),
                        EntityId = r.IsDBNull(o_entity) ? null : r.GetInt32(o_entity),
                        ShopId = r.IsDBNull(o_shop) ? null : r.GetInt32(o_shop),
                        Quantity = r.GetInt32(o_qty),
                        UnitAmount = r.GetDecimal(o_unit),
                        CurrencyId = r.GetInt32(o_ccy),
                        MetadataJson = r.IsDBNull(o_meta) ? null : r.GetString(o_meta)
                    });
                }
            }

            // ===== 3) Adjustments =====
            await using (var cmd = _db.CreateCommand(conn, sqlAdjust))
            {
                cmd.Parameters.Add(_db.CreateParameter("@OrderId", orderId));
                await using var r = await cmd.ExecuteReaderAsync();

                bool ordReady = false;
                int o_id = -1, o_line = -1, o_itemType = -1, o_code = -1, o_amt = -1, o_desc = -1, o_meta = -1;

                while (await r.ReadAsync())
                {
                    if (!ordReady)
                    {
                        o_id = r.GetOrdinal("id");
                        o_line = r.GetOrdinal("order_line_id");
                        o_itemType = r.GetOrdinal("item_type_id");
                        o_code = r.GetOrdinal("code");
                        o_amt = r.GetOrdinal("amount");
                        o_desc = r.GetOrdinal("description_key");
                        o_meta = r.GetOrdinal("metadata");
                        ordReady = true;
                    }

                    order!.Adjustments.Add(new OrderAdjustmentDto
                    {
                        Id = r.GetString(o_id),
                        OrderLineId = r.IsDBNull(o_line) ? null : r.GetString(o_line),
                        ItemTypeId = r.GetInt32(o_itemType),
                        Code = r.IsDBNull(o_code) ? null : r.GetString(o_code),
                        Amount = r.GetInt64(o_amt),
                        DescriptionKey = r.IsDBNull(o_desc) ? null : r.GetString(o_desc),
                        MetadataJson = r.IsDBNull(o_meta) ? null : r.GetString(o_meta)
                    });
                }
            }

            // ===== 4) Payment Intents =====
            var intentsById = new Dictionary<string, PaymentIntentDto>();
            await using (var cmd = _db.CreateCommand(conn, sqlIntents))
            {
                cmd.Parameters.Add(_db.CreateParameter("@OrderId", orderId));
                await using var r = await cmd.ExecuteReaderAsync();

                bool ordReady = false;
                int o_id = -1, o_gw = -1, o_status = -1, o_amt = -1, o_ccy = -1, o_secret = -1, o_provider = -1;

                while (await r.ReadAsync())
                {
                    if (!ordReady)
                    {
                        o_id = r.GetOrdinal("id");
                        o_gw = r.GetOrdinal("payment_gateway_id");
                        o_status = r.GetOrdinal("status_id");
                        o_amt = r.GetOrdinal("amount");
                        o_ccy = r.GetOrdinal("currency_id");
                        o_secret = r.GetOrdinal("client_secret");
                        o_provider = r.GetOrdinal("provider_intent_id");
                        ordReady = true;
                    }

                    var dto = new PaymentIntentDto
                    {
                        Id = r.GetString(o_id),
                        PaymentGatewayId = r.GetInt32(o_gw),
                        StatusId = r.GetInt32(o_status),
                        Amount = r.GetInt64(o_amt),
                        CurrencyId = r.GetInt32(o_ccy),
                        ClientSecret = r.IsDBNull(o_secret) ? null : r.GetString(o_secret),
                        ProviderIntentId = r.IsDBNull(o_provider) ? null : r.GetString(o_provider),
                        Charges = new()
                    };
                    intentsById[dto.Id] = dto;
                    order!.PaymentIntents.Add(dto);
                }
            }

            // ===== 5) Charges =====
            var chargesById = new Dictionary<string, PaymentChargeDto>();
            await using (var cmd = _db.CreateCommand(conn, sqlCharges))
            {
                cmd.Parameters.Add(_db.CreateParameter("@OrderId", orderId));
                await using var r = await cmd.ExecuteReaderAsync();

                bool ordReady = false;
                int o_id = -1, o_intent = -1, o_status = -1, o_cap = -1, o_ccy = -1,
                    o_provider = -1, o_fee = -1, o_fx = -1, o_dt = -1;

                while (await r.ReadAsync())
                {
                    if (!ordReady)
                    {
                        o_id = r.GetOrdinal("id");
                        o_intent = r.GetOrdinal("payment_intent_id");
                        o_status = r.GetOrdinal("status_id");
                        o_cap = r.GetOrdinal("amount_captured");
                        o_ccy = r.GetOrdinal("currency_id");
                        o_provider = r.GetOrdinal("provider_charge_id");
                        o_fee = r.GetOrdinal("gateway_fee_amount");
                        o_fx = r.GetOrdinal("exchange_rate");
                        o_dt = r.GetOrdinal("payment_datetime");
                        ordReady = true;
                    }

                    var charge = new PaymentChargeDto
                    {
                        Id = r.GetString(o_id),
                        StatusId = r.GetInt32(o_status),
                        AmountCaptured = r.GetInt64(o_cap),
                        CurrencyId = r.GetInt32(o_ccy),
                        ProviderChargeId = r.IsDBNull(o_provider) ? null : r.GetString(o_provider),
                        GatewayFeeAmount = r.IsDBNull(o_fee) ? null : r.GetInt64(o_fee),
                        ExchangeRate = r.IsDBNull(o_fx) ? null : r.GetDecimal(o_fx),
                        PaymentDateTime = r.GetDateTime(o_dt),
                        Refunds = new()
                    };

                    var intentId = r.GetString(o_intent);
                    if (intentsById.TryGetValue(intentId, out var intent))
                        intent.Charges.Add(charge);

                    chargesById[charge.Id] = charge;
                }
            }

            // ===== 6) Refunds =====
            await using (var cmd = _db.CreateCommand(conn, sqlRefunds))
            {
                cmd.Parameters.Add(_db.CreateParameter("@OrderId", orderId));
                await using var r = await cmd.ExecuteReaderAsync();

                bool ordReady = false;
                int o_id = -1, o_charge = -1, o_status = -1, o_amt = -1, o_ccy = -1, o_provider = -1, o_reason = -1, o_dt = -1;

                while (await r.ReadAsync())
                {
                    if (!ordReady)
                    {
                        o_id = r.GetOrdinal("id");
                        o_charge = r.GetOrdinal("payment_charge_id");
                        o_status = r.GetOrdinal("status_id");
                        o_amt = r.GetOrdinal("amount");
                        o_ccy = r.GetOrdinal("currency_id");
                        o_provider = r.GetOrdinal("provider_refund_id");
                        o_reason = r.GetOrdinal("reason");
                        o_dt = r.GetOrdinal("refund_datetime");
                        ordReady = true;
                    }

                    var refund = new PaymentRefundDto
                    {
                        Id = r.GetString(o_id),
                        StatusId = r.GetInt32(o_status),
                        Amount = r.GetInt64(o_amt),
                        CurrencyId = r.GetInt32(o_ccy),
                        ProviderRefundId = r.IsDBNull(o_provider) ? null : r.GetString(o_provider),
                        Reason = r.IsDBNull(o_reason) ? null : r.GetString(o_reason),
                        RefundDateTime = r.GetDateTime(o_dt)
                    };

                    var chargeId = r.GetString(o_charge);
                    if (chargesById.TryGetValue(chargeId, out var charge))
                        charge.Refunds.Add(refund);
                }
            }

            // ===== 7) Entitlements =====
            await using (var cmd = _db.CreateCommand(conn, sqlEntitlements))
            {
                cmd.Parameters.Add(_db.CreateParameter("@OrderId", orderId));
                await using var r = await cmd.ExecuteReaderAsync();

                bool ordReady = false;
                int o_id = -1, o_line = -1, o_user = -1, o_status = -1, o_qty = -1, o_grant = -1, o_rev = -1, o_meta = -1;

                while (await r.ReadAsync())
                {
                    if (!ordReady)
                    {
                        o_id = r.GetOrdinal("id");
                        o_line = r.GetOrdinal("order_line_id");
                        o_user = r.GetOrdinal("user_id");
                        o_status = r.GetOrdinal("status");
                        o_qty = r.GetOrdinal("quantity");
                        o_grant = r.GetOrdinal("granted_datetime");
                        o_rev = r.GetOrdinal("revoked_reason");
                        o_meta = r.GetOrdinal("metadata");
                        ordReady = true;
                    }

                    order!.Entitlements.Add(new EntitlementDto
                    {
                        Id = r.GetString(o_id),
                        OrderLineId = r.GetString(o_line),
                        UserId = r.GetString(o_user),
                        Status = r.GetInt32(o_status),
                        Quantity = r.GetInt32(o_qty),
                        GrantedDateTime = r.IsDBNull(o_grant) ? null : r.GetDateTime(o_grant),
                        RevokedReason = r.IsDBNull(o_rev) ? null : r.GetString(o_rev),
                        MetadataJson = r.IsDBNull(o_meta) ? null : r.GetString(o_meta)
                    });
                }
            }

            // ===== 8) Invoices (summary) =====
            await using (var cmd = _db.CreateCommand(conn, sqlInvoices))
            {
                cmd.Parameters.Add(_db.CreateParameter("@OrderId", orderId));
                await using var r = await cmd.ExecuteReaderAsync();

                bool ordReady = false;
                int o_id = -1, o_no = -1, o_status = -1, o_ccy = -1, o_total = -1, o_issue = -1, o_pdf = -1;

                while (await r.ReadAsync())
                {
                    if (!ordReady)
                    {
                        o_id = r.GetOrdinal("id");
                        o_no = r.GetOrdinal("invoice_number");
                        o_status = r.GetOrdinal("status_id");
                        o_ccy = r.GetOrdinal("currency_id");
                        o_total = r.GetOrdinal("total_amount");   // DECIMAL(9,6) in schema
                        o_issue = r.GetOrdinal("issue_datetime");
                        o_pdf = r.GetOrdinal("pdf_url");
                        ordReady = true;
                    }

                    order!.Invoices.Add(new InvoiceSummaryDto
                    {
                        Id = r.GetString(o_id),
                        InvoiceNumber = r.GetString(o_no),
                        StatusId = r.GetInt32(o_status),
                        CurrencyId = r.GetInt32(o_ccy),
                        TotalAmount = r.GetDecimal(o_total),
                        IssueDateTime = r.GetDateTime(o_issue),
                        PdfUrl = r.IsDBNull(o_pdf) ? null : r.GetString(o_pdf)
                    });
                }
            }

            return order;
        }


        public async Task<List<EntitlementDto>?> GetEntitlementsByOrderAsync(int spaceId, string orderId)
        {
            const string sqlOrderExists = @"SELECT 1 FROM `order` WHERE id = @OrderId AND space_id = @SpaceId;";
            const string sqlEntitlements = @"
SELECT e.id, e.order_line_id, e.user_id, e.status, e.quantity, e.granted_datetime, e.revoked_reason, e.metadata
FROM entitlement e
INNER JOIN order_line ol ON e.order_line_id = ol.id
WHERE ol.order_id = @OrderId
ORDER BY e.creation_time;";

            await using var conn = await _db.OpenConnectionAsync();

            // 1) Verify the order belongs to this space
            await using (var check = _db.CreateCommand(conn, sqlOrderExists))
            {
                check.Parameters.Add(_db.CreateParameter("@OrderId", orderId));
                check.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));
                var exists = await check.ExecuteScalarAsync();
                if (exists is null)
                    return null; // signal "order not found in this space"
            }

            // 2) Fetch entitlements
            var list = new List<EntitlementDto>();
            await using (var cmd = _db.CreateCommand(conn, sqlEntitlements))
            {
                cmd.Parameters.Add(_db.CreateParameter("@OrderId", orderId));
                await using var r = await cmd.ExecuteReaderAsync();

                bool ordReady = false;
                int o_id = -1, o_line = -1, o_user = -1, o_status = -1, o_qty = -1, o_grant = -1, o_rev = -1, o_meta = -1;

                while (await r.ReadAsync())
                {
                    if (!ordReady)
                    {
                        o_id = r.GetOrdinal("id");
                        o_line = r.GetOrdinal("order_line_id");
                        o_user = r.GetOrdinal("user_id");
                        o_status = r.GetOrdinal("status");
                        o_qty = r.GetOrdinal("quantity");
                        o_grant = r.GetOrdinal("granted_datetime");
                        o_rev = r.GetOrdinal("revoked_reason");
                        o_meta = r.GetOrdinal("metadata");
                        ordReady = true;
                    }

                    list.Add(new EntitlementDto
                    {
                        Id = r.GetString(o_id),
                        OrderLineId = r.GetString(o_line),
                        UserId = r.GetString(o_user),
                        Status = r.GetInt32(o_status),
                        Quantity = r.GetInt32(o_qty),
                        GrantedDateTime = r.IsDBNull(o_grant) ? null : r.GetDateTime(o_grant),
                        RevokedReason = r.IsDBNull(o_rev) ? null : r.GetString(o_rev),
                        MetadataJson = r.IsDBNull(o_meta) ? null : r.GetString(o_meta)
                    });
                }
            }

            return list;
        }


        public async Task<CreateOrderResponse> CreateOrderAsync(int pathSpaceId, CreateOrderRequest req)
        {
            // --- CONFIG/TUNABLES ---
            const int PendingOrderStatusId = 2; // TODO: inject/lookup
            const string ModifiedBy = "system";

            if (req is null) throw new ArgumentNullException(nameof(req));
            if (string.IsNullOrWhiteSpace(req.IdempotencyKey)) throw new ArgumentException("IdempotencyKey is required.");
            if (string.IsNullOrWhiteSpace(req.UserId)) throw new ArgumentException("UserId is required.");
            if (req.Lines.Count == 0) throw new ArgumentException("At least one line is required.");

            await using var conn = await _db.OpenConnectionAsync();
            await using var tx = await conn.BeginTransactionAsync();

            try
            {
                // 0) If an order with the same idempotency key already exists (and belongs to the same space), return it.
                const string sqlFindExisting = @"
SELECT id, total_net_amount, space_id
FROM `order`
WHERE idempotency_key = @IdemKey
LIMIT 1;";
                string? existingOrderId = null;
                long existingNet = 0;
                await using (var findCmd = _db.CreateCommand(conn, sqlFindExisting))
                {
                    findCmd.Transaction = tx;
                    findCmd.Parameters.Add(_db.CreateParameter("@IdemKey", req.IdempotencyKey));
                    await using var r = await findCmd.ExecuteReaderAsync();
                    if (await r.ReadAsync())
                    {
                        var o_id = r.GetOrdinal("id");
                        var o_net = r.GetOrdinal("total_net_amount");
                        var o_space = r.GetOrdinal("space_id");
                        var space = r.GetInt32(o_space);
                        if (space != pathSpaceId)
                            throw new InvalidOperationException("Idempotency key already used for a different space.");

                        existingOrderId = r.GetString(o_id);
                        existingNet = r.GetInt64(o_net);
                    }
                }
                if (existingOrderId is not null)
                {
                    await tx.CommitAsync();
                    return new CreateOrderResponse { OrderId = existingOrderId, TotalNetMinor = existingNet, Currency = req.Currency };
                }

                // 1) Resolve currency_id from currency.ISO (per your table)
                const string sqlCurrency = @"SELECT id FROM currency WHERE `ISO` = @Iso LIMIT 1;";
                int currencyId;
                await using (var curCmd = _db.CreateCommand(conn, sqlCurrency))
                {
                    curCmd.Transaction = tx;
                    var iso = string.IsNullOrWhiteSpace(req.Currency) ? "USD" : req.Currency.Trim().ToUpperInvariant();
                    curCmd.Parameters.Add(_db.CreateParameter("@Iso", iso));

                    var o = await curCmd.ExecuteScalarAsync();
                    if (o is null || o == DBNull.Value)
                        throw new InvalidOperationException($"Unknown currency ISO '{iso}'.");
                    currencyId = Convert.ToInt32(o);
                }


                // 2) Precompute totals (minor units)
                long totalGross = 0, totalDiscount = 0, totalTax = 0, totalFees = 0;
                foreach (var l in req.Lines)
                {
                    checked { totalGross += l.UnitAmountMinor * l.Quantity; }
                }

                if (req.Adjustments is not null)
                {
                    foreach (var a in req.Adjustments)
                    {
                        if (a.AmountMinor < 0)
                        {
                            checked { totalDiscount += -a.AmountMinor; } // store as positive
                        }
                        else
                        {
                            // Heuristic: codes starting with TAX/VAT → tax, else → fees
                            var code = a.Code ?? string.Empty;
                            if (code.StartsWith("TAX", StringComparison.OrdinalIgnoreCase) ||
                                code.StartsWith("VAT", StringComparison.OrdinalIgnoreCase))
                                checked { totalTax += a.AmountMinor; }
                            else
                                checked { totalFees += a.AmountMinor; }
                        }
                    }
                }

                long totalNet = checked(totalGross - totalDiscount + totalTax + totalFees);

                // 3) Insert order
                var orderId = Guid.NewGuid().ToString();
                var sessionId = string.IsNullOrWhiteSpace(req.SessionId) ? Guid.NewGuid().ToString() : req.SessionId;

                const string sqlInsertOrder = @"
INSERT INTO `order`
(id, user_id, space_id, transaction_type_id, currency_id, status_id,
 total_gross_amount, total_discount_amount, change_reason, total_tax_amount, total_fee_amount, total_net_amount,
 original_order_id, session_id, gateway_preferred_id, order_datetime, remarks, idempotency_key,
 creation_time, modified_by)
VALUES
(@Id, @UserId, @SpaceId, @Trx, @CcyId, @StatusId,
 @Gross, @Disc, NULL, @Tax, @Fees, @Net,
 NULL, @SessionId, NULL, NOW(6), @Remarks, @IdemKey,
 NOW(6), @ModBy);";

                await using (var cmd = _db.CreateCommand(conn, sqlInsertOrder))
                {
                    cmd.Transaction = tx;
                    cmd.Parameters.Add(_db.CreateParameter("@Id", orderId));
                    cmd.Parameters.Add(_db.CreateParameter("@UserId", req.UserId));
                    cmd.Parameters.Add(_db.CreateParameter("@SpaceId", pathSpaceId));
                    cmd.Parameters.Add(_db.CreateParameter("@Trx", req.TransactionTypeId));
                    cmd.Parameters.Add(_db.CreateParameter("@CcyId", currencyId));
                    cmd.Parameters.Add(_db.CreateParameter("@StatusId", PendingOrderStatusId));
                    cmd.Parameters.Add(_db.CreateParameter("@Gross", totalGross));
                    cmd.Parameters.Add(_db.CreateParameter("@Disc", totalDiscount));
                    cmd.Parameters.Add(_db.CreateParameter("@Tax", totalTax));
                    cmd.Parameters.Add(_db.CreateParameter("@Fees", totalFees));
                    cmd.Parameters.Add(_db.CreateParameter("@Net", totalNet));
                    cmd.Parameters.Add(_db.CreateParameter("@SessionId", sessionId));
                    cmd.Parameters.Add(_db.CreateParameter("@Remarks", (object?)req.Remarks ?? DBNull.Value));
                    cmd.Parameters.Add(_db.CreateParameter("@IdemKey", req.IdempotencyKey));
                    cmd.Parameters.Add(_db.CreateParameter("@ModBy", ModifiedBy));
                    await cmd.ExecuteNonQueryAsync();
                }

                // 4) Insert order lines
                const string sqlInsertLine = @"
INSERT INTO order_line
(id, order_id, item_type_id, item_ref_id, entity_id, shop_id,
 quantity, unit_amount, currency_id, metadata,
 creation_time, modified_by)
VALUES
(@Id, @OrderId, @ItemTypeId, @ItemRefId, @EntityId, @ShopId,
 @Qty, @UnitAmount, @CcyId, @Meta,
 NOW(6), @ModBy);";

                foreach (var l in req.Lines)
                {
                    var lineId = Guid.NewGuid().ToString();
                    var unitAmountDecimal = l.UnitAmountMinor / 100m; // DECIMAL(9,6) column, cents → decimal
                    var metaJson = l.Metadata is null ? null : JsonSerializer.Serialize(l.Metadata);

                    await using var lineCmd = _db.CreateCommand(conn, sqlInsertLine);
                    lineCmd.Transaction = tx;
                    lineCmd.Parameters.Add(_db.CreateParameter("@Id", lineId));
                    lineCmd.Parameters.Add(_db.CreateParameter("@OrderId", orderId));
                    lineCmd.Parameters.Add(_db.CreateParameter("@ItemTypeId", l.ItemTypeId));
                    lineCmd.Parameters.Add(_db.CreateParameter("@ItemRefId", l.ItemRefId));
                    lineCmd.Parameters.Add(_db.CreateParameter("@EntityId", (object?)l.EntityId ?? DBNull.Value));
                    lineCmd.Parameters.Add(_db.CreateParameter("@ShopId", (object?)l.ShopId ?? DBNull.Value));
                    lineCmd.Parameters.Add(_db.CreateParameter("@Qty", l.Quantity));
                    lineCmd.Parameters.Add(_db.CreateParameter("@UnitAmount", unitAmountDecimal));
                    lineCmd.Parameters.Add(_db.CreateParameter("@CcyId", currencyId));
                    lineCmd.Parameters.Add(_db.CreateParameter("@Meta", (object?)metaJson ?? DBNull.Value));
                    lineCmd.Parameters.Add(_db.CreateParameter("@ModBy", ModifiedBy));
                    await lineCmd.ExecuteNonQueryAsync();
                }

                // 5) Insert order adjustments (optional)
                if (req.Adjustments is not null && req.Adjustments.Count > 0)
                {
                    const string sqlInsertAdj = @"
INSERT INTO order_adjustment
(id, order_id, order_line_id, item_type_id, code, amount, description_key, metadata,
 creation_time, modified_by)
VALUES
(@Id, @OrderId, @OrderLineId, @ItemTypeId, @Code, @Amount, @DescKey, @Meta,
 NOW(6), @ModBy);";

                    foreach (var a in req.Adjustments)
                    {
                        var adjId = Guid.NewGuid().ToString();
                        var metaJson = a.Metadata is null ? null : JsonSerializer.Serialize(a.Metadata);

                        await using var adjCmd = _db.CreateCommand(conn, sqlInsertAdj);
                        adjCmd.Transaction = tx;
                        adjCmd.Parameters.Add(_db.CreateParameter("@Id", adjId));
                        adjCmd.Parameters.Add(_db.CreateParameter("@OrderId", orderId));
                        adjCmd.Parameters.Add(_db.CreateParameter("@OrderLineId", (object?)a.OrderLineId ?? DBNull.Value));
                        adjCmd.Parameters.Add(_db.CreateParameter("@ItemTypeId", a.ItemTypeId));
                        adjCmd.Parameters.Add(_db.CreateParameter("@Code", (object?)a.Code ?? DBNull.Value));
                        adjCmd.Parameters.Add(_db.CreateParameter("@Amount", a.AmountMinor));
                        adjCmd.Parameters.Add(_db.CreateParameter("@DescKey", (object?)a.DescriptionKey ?? DBNull.Value));
                        adjCmd.Parameters.Add(_db.CreateParameter("@Meta", (object?)metaJson ?? DBNull.Value));
                        adjCmd.Parameters.Add(_db.CreateParameter("@ModBy", ModifiedBy));
                        await adjCmd.ExecuteNonQueryAsync();
                    }
                }

                await tx.CommitAsync();
                return new CreateOrderResponse { OrderId = orderId, TotalNetMinor = totalNet, Currency = req.Currency };
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }
    }
}
