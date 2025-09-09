// <copyright file="OrderRepository.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Saad Sohail</author>
// <date>09/02/2025</date>
// <summary></summary>

using System.Text.Json;
//
using GMS.TifoXRCoreWebAPI.Models;
using GMS.TifoXRCoreWebAPI.Repositories.Interfaces;
using GMS.TifoXRCoreWebAPI.Utilities.Infrastructure;

namespace GMS.TifoXRCoreWebAPI.Repositories
{
    /// <summary>
    /// Data-access only. No payment gateway calls here.
    /// </summary>
    public sealed class OrderRepository(IDbProvider db) : IOrderRepository
    {
        private readonly IDbProvider _db = db;

        // -----------------------
        // Reads/aggregates (kept)
        // -----------------------
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

            // 1) Order header
            OrderDto? order = null;
            await using (var cmd = _db.CreateCommand(conn, sqlOrder))
            {
                cmd.Parameters.Add(_db.CreateParameter("@OrderId", orderId));
                cmd.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));
                
                await using var r = await cmd.ExecuteReaderAsync();

                if (!await r.ReadAsync())
                    return null;

                var o_id = r.GetOrdinal("id");
                var o_user = r.GetOrdinal("user_id");
                var o_space = r.GetOrdinal("space_id");
                var o_trx = r.GetOrdinal("transaction_type_id");
                var o_ccy = r.GetOrdinal("currency_id");
                var o_status = r.GetOrdinal("status_id");
                var o_gross = r.GetOrdinal("total_gross_amount");
                var o_disc = r.GetOrdinal("total_discount_amount");
                var o_tax = r.GetOrdinal("total_tax_amount");
                var o_fee = r.GetOrdinal("total_fee_amount");
                var o_net = r.GetOrdinal("total_net_amount");
                var o_reason = r.GetOrdinal("change_reason");
                var o_orig = r.GetOrdinal("original_order_id");
                var o_session = r.GetOrdinal("session_id");
                var o_gw = r.GetOrdinal("gateway_preferred_id");
                var o_dt = r.GetOrdinal("order_datetime");
                var o_remarks = r.GetOrdinal("remarks");

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
                    LineItems = new(),
                    Adjustments = new(),
                    PaymentIntents = new(),
                    Entitlements = new(),
                    Invoices = new()
                };
            }

            // 2) Lines
            await using (var cmd = _db.CreateCommand(conn, sqlLines))
            {
                cmd.Parameters.Add(_db.CreateParameter("@OrderId", orderId));
                
                await using var r = await cmd.ExecuteReaderAsync();

                var o_id = r.GetOrdinal("id");
                var o_itemType = r.GetOrdinal("item_type_id");
                var o_itemRef = r.GetOrdinal("item_ref_id");
                var o_entity = r.GetOrdinal("entity_id");
                var o_shop = r.GetOrdinal("shop_id");
                var o_qty = r.GetOrdinal("quantity");
                var o_unit = r.GetOrdinal("unit_amount");
                var o_ccy = r.GetOrdinal("currency_id");
                var o_meta = r.GetOrdinal("metadata");

                while (await r.ReadAsync())
                {
                    var unitMajor = r.GetDecimal(o_unit);
                    var unitMinor = (long)decimal.Round(
                        unitMajor * 100m, 0, MidpointRounding.AwayFromZero);
                    
                    order!.LineItems.Add(new OrderLineDto
                    {
                        Id = r.GetString(o_id),
                        ItemTypeId = r.GetInt32(o_itemType),
                        ItemRefId = r.GetString(o_itemRef),
                        EntityId = r.IsDBNull(o_entity) ? null : r.GetInt32(o_entity),
                        ShopId = r.IsDBNull(o_shop) ? null : r.GetInt32(o_shop),
                        Quantity = r.GetInt32(o_qty),
                        UnitAmountMinor = unitMinor,
                        CurrencyId = r.GetInt32(o_ccy),
                        MetadataJson = r.IsDBNull(o_meta) ? null : r.GetString(o_meta)
                    });
                }
            }

            // 3) Adjustments
            await using (var cmd = _db.CreateCommand(conn, sqlAdjust))
            {
                cmd.Parameters.Add(_db.CreateParameter("@OrderId", orderId));
                
                await using var r = await cmd.ExecuteReaderAsync();

                var o_id = r.GetOrdinal("id");
                var o_line = r.GetOrdinal("order_line_id");
                var o_itemType = r.GetOrdinal("item_type_id");
                var o_code = r.GetOrdinal("code");
                var o_amt = r.GetOrdinal("amount");
                var o_desc = r.GetOrdinal("description_key");
                var o_meta = r.GetOrdinal("metadata");

                while (await r.ReadAsync())
                {
                    order!.Adjustments.Add(new OrderAdjustmentDto
                    {
                        Id = r.GetString(o_id),
                        OrderLineId = r.IsDBNull(o_line) ? null : r.GetString(o_line),
                        ItemTypeId = r.GetInt32(o_itemType),
                        Code = r.IsDBNull(o_code) ? null : r.GetString(o_code),
                        AmountMinor = r.GetInt64(o_amt),
                        DescriptionKey = r.IsDBNull(o_desc) ? null : r.GetString(o_desc),
                        MetadataJson = r.IsDBNull(o_meta) ? null : r.GetString(o_meta)
                    });
                }
            }

            // 4) Payment Intents
            var intentsById = new Dictionary<string, PaymentIntentDto>();
            
            await using (var cmd = _db.CreateCommand(conn, sqlIntents))
            {
                cmd.Parameters.Add(_db.CreateParameter("@OrderId", orderId));
                await using var r = await cmd.ExecuteReaderAsync();

                var o_id = r.GetOrdinal("id");
                var o_gw = r.GetOrdinal("payment_gateway_id");
                var o_status = r.GetOrdinal("status_id");
                var o_amt = r.GetOrdinal("amount");
                var o_ccy = r.GetOrdinal("currency_id");
                var o_secret = r.GetOrdinal("client_secret");
                var o_provider = r.GetOrdinal("provider_intent_id");

                while (await r.ReadAsync())
                {
                    var dto = new PaymentIntentDto
                    {
                        Id = r.GetString(o_id),
                        PaymentGatewayId = r.GetInt32(o_gw),
                        StatusId = r.GetInt32(o_status),
                        AmountMinor = r.GetInt64(o_amt),
                        CurrencyId = r.GetInt32(o_ccy),
                        ClientSecret = r.IsDBNull(o_secret) ? null : r.GetString(o_secret),
                        ProviderIntentId = r.IsDBNull(o_provider) ? null : r.GetString(o_provider),
                        Charges = new()
                    };
                    intentsById[dto.Id] = dto;
                    order!.PaymentIntents.Add(dto);
                }
            }

            // 5) Charges
            await using (var cmd = _db.CreateCommand(conn, sqlCharges))
            {
                cmd.Parameters.Add(_db.CreateParameter("@OrderId", orderId));
                
                await using var r = await cmd.ExecuteReaderAsync();

                var o_id = r.GetOrdinal("id");
                var o_intent = r.GetOrdinal("payment_intent_id");
                var o_status = r.GetOrdinal("status_id");
                var o_cap = r.GetOrdinal("amount_captured");
                var o_ccy = r.GetOrdinal("currency_id");
                var o_provider = r.GetOrdinal("provider_charge_id");
                var o_fee = r.GetOrdinal("gateway_fee_amount");
                var o_fx = r.GetOrdinal("exchange_rate");
                var o_dt = r.GetOrdinal("payment_datetime");

                while (await r.ReadAsync())
                {
                    var charge = new PaymentChargeDto
                    {
                        Id = r.GetString(o_id),
                        StatusId = r.GetInt32(o_status),
                        AmountCapturedMinor = r.GetInt64(o_cap),
                        CurrencyId = r.GetInt32(o_ccy),
                        ProviderChargeId = r.IsDBNull(o_provider) ? null : r.GetString(o_provider),
                        GatewayFeeAmountMinor = r.IsDBNull(o_fee) ? null : r.GetInt64(o_fee),
                        ExchangeRate = r.IsDBNull(o_fx) ? null : r.GetDecimal(o_fx),
                        PaymentDateTime = r.GetDateTime(o_dt),
                        Refunds = new()
                    };

                    var intentId = r.GetString(o_intent);
                    if (intentsById.TryGetValue(intentId, out var intent))
                        intent.Charges.Add(charge);
                }
            }

            // 6) Refunds
            await using (var cmd = _db.CreateCommand(conn, sqlRefunds))
            {
                cmd.Parameters.Add(_db.CreateParameter("@OrderId", orderId));
                await using var r = await cmd.ExecuteReaderAsync();

                var o_id = r.GetOrdinal("id");
                var o_charge = r.GetOrdinal("payment_charge_id");
                var o_status = r.GetOrdinal("status_id");
                var o_amt = r.GetOrdinal("amount");
                var o_ccy = r.GetOrdinal("currency_id");
                var o_provider = r.GetOrdinal("provider_refund_id");
                var o_reason = r.GetOrdinal("reason");
                var o_dt = r.GetOrdinal("refund_datetime");

                var chargesById = order!.PaymentIntents.SelectMany(i => i.Charges)
                                                       .ToDictionary(c => c.Id, c => c);

                while (await r.ReadAsync())
                {
                    var refund = new PaymentRefundDto
                    {
                        Id = r.GetString(o_id),
                        StatusId = r.GetInt32(o_status),
                        AmountMinor = r.GetInt64(o_amt),
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

            // 7) Entitlements
            await using (var cmd = _db.CreateCommand(conn, sqlEntitlements))
            {
                cmd.Parameters.Add(_db.CreateParameter("@OrderId", orderId));
                
                await using var r = await cmd.ExecuteReaderAsync();

                var o_id = r.GetOrdinal("id");
                var o_line = r.GetOrdinal("order_line_id");
                var o_user = r.GetOrdinal("user_id");
                var o_status = r.GetOrdinal("status");
                var o_qty = r.GetOrdinal("quantity");
                var o_grant = r.GetOrdinal("granted_datetime");
                var o_rev = r.GetOrdinal("revoked_reason");
                var o_meta = r.GetOrdinal("metadata");

                while (await r.ReadAsync())
                {
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

            // 8) Invoices (summary)
            await using (var cmd = _db.CreateCommand(conn, sqlInvoices))
            {
                cmd.Parameters.Add(_db.CreateParameter("@OrderId", orderId));
                
                await using var r = await cmd.ExecuteReaderAsync();

                var o_id = r.GetOrdinal("id");
                var o_no = r.GetOrdinal("invoice_number");
                var o_status = r.GetOrdinal("status_id");
                var o_ccy = r.GetOrdinal("currency_id");
                var o_total = r.GetOrdinal("total_amount");
                var o_issue = r.GetOrdinal("issue_datetime");
                var o_pdf = r.GetOrdinal("pdf_url");

                while (await r.ReadAsync())
                {
                    var totalMajor = r.GetDecimal(o_total);
                    var totalMinor = (long)decimal.Round(totalMajor * 100m, 0, MidpointRounding.AwayFromZero);

                    order!.Invoices.Add(new InvoiceSummaryDto
                    {
                        Id = r.GetString(o_id),
                        InvoiceNumber = r.GetString(o_no),
                        StatusId = r.GetInt32(o_status),
                        CurrencyId = r.GetInt32(o_ccy),
                        TotalAmountMinor = totalMinor,
                        IssueDateTime = r.GetDateTime(o_issue),
                        PdfUrl = r.IsDBNull(o_pdf) ? null : r.GetString(o_pdf)
                    });
                }
            }

            return order;
        }

        public async Task<List<EntitlementDto>?> GetEntitlementsByOrderAsync(int spaceId, string orderId)
        {
            const string sqlOrderExists = @"
                SELECT 1 
                FROM `order` 
                WHERE id = @OrderId 
                    AND space_id = @SpaceId;
            ";
            
            const string sqlEntitlements = @"
                SELECT e.id, e.order_line_id, e.user_id, e.status, e.quantity, e.granted_datetime, e.revoked_reason, e.metadata
                FROM entitlement e
                INNER JOIN order_line ol ON e.order_line_id = ol.id
                WHERE ol.order_id = @OrderId
                ORDER BY e.creation_time;
            ";

            await using var conn = await _db.OpenConnectionAsync();

            // ensure order belongs to the space
            await using (var check = _db.CreateCommand(conn, sqlOrderExists))
            {
                check.Parameters.Add(_db.CreateParameter("@OrderId", orderId));
                check.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));
                var exists = await check.ExecuteScalarAsync();
                if (exists is null) return null;
            }

            var list = new List<EntitlementDto>();
            await using (var cmd = _db.CreateCommand(conn, sqlEntitlements))
            {
                cmd.Parameters.Add(_db.CreateParameter("@OrderId", orderId));
                await using var r = await cmd.ExecuteReaderAsync();

                var o_id = r.GetOrdinal("id");
                var o_line = r.GetOrdinal("order_line_id");
                var o_user = r.GetOrdinal("user_id");
                var o_status = r.GetOrdinal("status");
                var o_qty = r.GetOrdinal("quantity");
                var o_grant = r.GetOrdinal("granted_datetime");
                var o_rev = r.GetOrdinal("revoked_reason");
                var o_meta = r.GetOrdinal("metadata");

                while (await r.ReadAsync())
                {
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

        public async Task<InvoiceListResponse?> GetInvoicesByOrderAsync(int spaceId, string orderId)
        {
            const string sqlOrderExists = @"
                SELECT 1 
                FROM `order` 
                WHERE id = @OrderId 
                    AND space_id = @SpaceId;
            ";
            
            const string sqlInvoices = @"
                SELECT id, invoice_number, status_id, currency_id, total_amount, issue_datetime, pdf_url
                FROM invoice
                WHERE order_id = @OrderId
                ORDER BY issue_datetime;
            ";

            await using var conn = await _db.OpenConnectionAsync();

            // verify order belongs to this space
            await using (var check = _db.CreateCommand(conn, sqlOrderExists))
            {
                check.Parameters.Add(_db.CreateParameter("@OrderId", orderId));
                check.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));
                var exists = await check.ExecuteScalarAsync();
                if (exists is null) return null;
            }

            var list = new List<InvoiceSummaryDto>();
            
            await using (var cmd = _db.CreateCommand(conn, sqlInvoices))
            {
                cmd.Parameters.Add(_db.CreateParameter("@OrderId", orderId));
                
                await using var r = await cmd.ExecuteReaderAsync();

                var o_id = r.GetOrdinal("id");
                var o_no = r.GetOrdinal("invoice_number");
                var o_status = r.GetOrdinal("status_id");
                var o_ccy = r.GetOrdinal("currency_id");
                var o_total = r.GetOrdinal("total_amount");
                var o_issue = r.GetOrdinal("issue_datetime");
                var o_pdf = r.GetOrdinal("pdf_url");

                while (await r.ReadAsync())
                {
                    var totalMajor = r.GetDecimal(o_total);
                    var totalMinor = (long)decimal.Round(totalMajor * 100m, 0, MidpointRounding.AwayFromZero);

                    list.Add(new InvoiceSummaryDto
                    {
                        Id = r.GetString(o_id),
                        InvoiceNumber = r.GetString(o_no),
                        StatusId = r.GetInt32(o_status),
                        CurrencyId = r.GetInt32(o_ccy),
                        TotalAmountMinor = totalMinor,
                        IssueDateTime = r.GetDateTime(o_issue),
                        PdfUrl = r.IsDBNull(o_pdf) ? null : r.GetString(o_pdf)
                    });
                }
            }

            return new InvoiceListResponse { Invoices = list };
        }

        public async Task<(string IntentId, int StatusId, string IdempotencyKey, 
            string? ProviderIntentId, int PaymentGatewayId, long AmountMinor, 
            int CurrencyId)?>
            GetPendingIntentForOrderAsync(string orderId)
        {
            const string sql = @"
                SELECT pi.id, pi.status_id, pi.idempotency_key, pi.provider_intent_id,
                       pi.payment_gateway_id, pi.amount, pi.currency_id
                FROM payment_intent pi
                WHERE pi.order_id = @OrderId
                  AND pi.status_id IN (1,2)          -- 1 = requires_action, 2 = processing
                ORDER BY pi.creation_time DESC
                LIMIT 1;";

            await using var conn = await _db.OpenConnectionAsync();
            
            await using var cmd = _db.CreateCommand(conn, sql);
            
            cmd.Parameters.Add(_db.CreateParameter("@OrderId", orderId));

            await using var r = await cmd.ExecuteReaderAsync();
            
            if (!await r.ReadAsync()) return null;

            return (
                IntentId: r.GetString(r.GetOrdinal("id")),
                StatusId: r.GetInt32(r.GetOrdinal("status_id")),
                IdempotencyKey: r.GetString(r.GetOrdinal("idempotency_key")),
                ProviderIntentId: r.IsDBNull(r.GetOrdinal("provider_intent_id")) ? null : r.GetString(r.GetOrdinal("provider_intent_id")),
                PaymentGatewayId: r.GetInt32(r.GetOrdinal("payment_gateway_id")),
                AmountMinor: r.GetInt64(r.GetOrdinal("amount")),
                CurrencyId: r.GetInt32(r.GetOrdinal("currency_id"))
            );
        }

        // Finds the newest order for (user,item) that still has a pending intent (1,2)
        public async Task<string?> FindLatestOrderIdWithPendingIntentAsync(int spaceId, string userId, int itemTypeId, int itemRefId)
        {
            const string sql = @"
                SELECT o.id
                FROM `order` o
                JOIN order_line ol ON ol.order_id = o.id
                JOIN payment_intent pi ON pi.order_id = o.id AND pi.status_id IN (1,2)
                WHERE o.space_id = @SpaceId
                  AND o.user_id = @UserId
                  AND ol.item_type_id = @ItemTypeId
                  AND ol.item_ref_id = @ItemRefId
                ORDER BY pi.creation_time DESC
                LIMIT 1;";

            await using var conn = await _db.OpenConnectionAsync();
            
            await using var cmd = _db.CreateCommand(conn, sql);
            
            cmd.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));
            cmd.Parameters.Add(_db.CreateParameter("@UserId", userId));
            cmd.Parameters.Add(_db.CreateParameter("@ItemTypeId", itemTypeId));
            cmd.Parameters.Add(_db.CreateParameter("@ItemRefId", itemRefId));

            var o = await cmd.ExecuteScalarAsync();
            
            return o is null || o == DBNull.Value 
                ? null 
                : Convert.ToString(o);
        }

        // -----------------------
        // Commands (orders)
        // -----------------------
        public async Task<CreateOrderResponse> CreateOrderAsync(
            int pathSpaceId, 
            CreateOrderRequest req,
            int? gatewayPreferredId)
        {
            const int PendingOrderStatusId = 2; // tune as needed
            const string ModifiedBy = "system";

            if (req is null) throw new ArgumentNullException(nameof(req));
            if (string.IsNullOrWhiteSpace(req.IdempotencyKey)) throw new ArgumentException("IdempotencyKey is required.");
            if (string.IsNullOrWhiteSpace(req.UserId)) throw new ArgumentException("UserId is required.");
            if (req.LineItems is null || req.LineItems.Count == 0) throw new ArgumentException("At least one line is required.");

            await using var conn = await _db.OpenConnectionAsync();
            await using var tx = await conn.BeginTransactionAsync();
            
            try
            {
                // 0) idempotency check
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

                    return new CreateOrderResponse 
                    { 
                        OrderId = existingOrderId, 
                        TotalNetMinor = existingNet, 
                        CurrencyId = req.CurrencyId 
                    };
                }

                // 2) totals (minor units)
                long totalGross = 0, totalDiscount = 0, totalTax = 0, totalFees = 0;
                
                foreach (var l in req.LineItems)
                    totalGross += checked(l.UnitAmountMinor * l.Quantity);

                if (req.Adjustments is not null)
                {
                    foreach (var a in req.Adjustments)
                    {
                        if (a.AmountMinor < 0) totalDiscount += checked(-a.AmountMinor);
                        else
                        {
                            var code = a.Code ?? "";
                            if (code.StartsWith("TAX", StringComparison.OrdinalIgnoreCase) ||
                                code.StartsWith("VAT", StringComparison.OrdinalIgnoreCase))
                                totalTax += a.AmountMinor;
                            else
                                totalFees += a.AmountMinor;
                        }
                    }
                }

                var totalNet = checked(totalGross - totalDiscount + totalTax + totalFees);

                // 3) insert order
                var orderId = Guid.NewGuid().ToString();
                
                var sessionId = string.IsNullOrWhiteSpace(req.SessionId) 
                    ? Guid.NewGuid().ToString() 
                    : req.SessionId;

                const string sqlInsertOrder = @"
                    INSERT INTO `order`
                    (id, user_id, space_id, transaction_type_id, currency_id, status_id,
                     total_gross_amount, total_discount_amount, change_reason, total_tax_amount, total_fee_amount, total_net_amount,
                     original_order_id, session_id, gateway_preferred_id, order_datetime, remarks, idempotency_key, modified_by)
                    VALUES
                    (@Id, @UserId, @SpaceId, @Trx, @CcyId, @StatusId,
                     @Gross, @Disc, NULL, @Tax, @Fees, @Net,
                     NULL, @SessionId, @GatewayPreferredId, NOW(6), @Remarks, @IdemKey, @ModBy);
                ";

                await using (var cmd = _db.CreateCommand(conn, sqlInsertOrder))
                {
                    cmd.Transaction = tx;
                    cmd.Parameters.Add(_db.CreateParameter("@Id", orderId));
                    cmd.Parameters.Add(_db.CreateParameter("@UserId", req.UserId));
                    cmd.Parameters.Add(_db.CreateParameter("@SpaceId", pathSpaceId));
                    cmd.Parameters.Add(_db.CreateParameter("@Trx", req.TransactionTypeId));
                    cmd.Parameters.Add(_db.CreateParameter("@CcyId", req.CurrencyId));
                    cmd.Parameters.Add(_db.CreateParameter("@StatusId", PendingOrderStatusId));
                    cmd.Parameters.Add(_db.CreateParameter("@Gross", totalGross));
                    cmd.Parameters.Add(_db.CreateParameter("@Disc", totalDiscount));
                    cmd.Parameters.Add(_db.CreateParameter("@Tax", totalTax));
                    cmd.Parameters.Add(_db.CreateParameter("@Fees", totalFees));
                    cmd.Parameters.Add(_db.CreateParameter("@Net", totalNet));
                    cmd.Parameters.Add(_db.CreateParameter("@SessionId", sessionId));
                    cmd.Parameters.Add(_db.CreateParameter("@GatewayPreferredId", (object?)gatewayPreferredId ?? DBNull.Value));
                    cmd.Parameters.Add(_db.CreateParameter("@Remarks", (object?)req.Remarks ?? DBNull.Value));
                    cmd.Parameters.Add(_db.CreateParameter("@IdemKey", req.IdempotencyKey));
                    cmd.Parameters.Add(_db.CreateParameter("@ModBy", ModifiedBy));
                    await cmd.ExecuteNonQueryAsync();
                }

                // 4) lines
                const string sqlInsertLine = @"
                    INSERT INTO order_line
                    (id, order_id, item_type_id, item_ref_id, entity_id, shop_id,
                     quantity, unit_amount, currency_id, metadata, modified_by)
                    VALUES
                    (@Id, @OrderId, @ItemTypeId, @ItemRefId, @EntityId, @ShopId,
                     @Qty, @UnitAmount, @CcyId, @Meta, @ModBy);
                ";

                foreach (var item in req.LineItems)
                {
                    var lineItemId = Guid.NewGuid().ToString();
                    var unitAmountDecimal = item.UnitAmountMinor / 100m;
                    var metaJson = item.Metadata is null ? null : JsonSerializer.Serialize(item.Metadata);

                    await using var lineCmd = _db.CreateCommand(conn, sqlInsertLine);
                    
                    lineCmd.Transaction = tx;
                    lineCmd.Parameters.Add(_db.CreateParameter("@Id", lineItemId));
                    lineCmd.Parameters.Add(_db.CreateParameter("@OrderId", orderId));
                    lineCmd.Parameters.Add(_db.CreateParameter("@ItemTypeId", item.ItemTypeId));
                    lineCmd.Parameters.Add(_db.CreateParameter("@ItemRefId", item.ItemRefId));
                    lineCmd.Parameters.Add(_db.CreateParameter("@EntityId", (object?)item.EntityId ?? DBNull.Value));
                    lineCmd.Parameters.Add(_db.CreateParameter("@ShopId", (object?)item.ShopId ?? DBNull.Value));
                    lineCmd.Parameters.Add(_db.CreateParameter("@Qty", item.Quantity));
                    lineCmd.Parameters.Add(_db.CreateParameter("@UnitAmount", unitAmountDecimal));
                    lineCmd.Parameters.Add(_db.CreateParameter("@CcyId", req.CurrencyId)); 
                    lineCmd.Parameters.Add(_db.CreateParameter("@Meta", (object?)metaJson ?? DBNull.Value));
                    lineCmd.Parameters.Add(_db.CreateParameter("@ModBy", ModifiedBy));
                    
                    await lineCmd.ExecuteNonQueryAsync();
                }

                // 5) adjustments
                if (req.Adjustments is { Count: > 0 })
                {
                    const string sqlInsertAdj = @"
                        INSERT INTO order_adjustment
                        (id, order_id, order_line_id, item_type_id, code, amount, 
                        description_key, metadata, modified_by)
                        VALUES
                        (@Id, @OrderId, @OrderLineId, @ItemTypeId, @Code, @Amount, @DescKey, @Meta, @ModBy);";

                    foreach (var a in req.Adjustments)
                    {
                        var adjId = Guid.NewGuid().ToString();
                        var metaJson = a.Metadata is null ? null : JsonSerializer.Serialize(a.Metadata);

                        await using var adjCmd = _db.CreateCommand(conn, sqlInsertAdj);
                        
                        adjCmd.Transaction = tx;
                        adjCmd.Parameters.Add(_db.CreateParameter("@Id", adjId));
                        adjCmd.Parameters.Add(_db.CreateParameter("@OrderId", orderId));
                        adjCmd.Parameters.Add(_db.CreateParameter("@OrderLineId", (object?)a.OrderLineItemId ?? DBNull.Value));
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

                return new CreateOrderResponse 
                { 
                    OrderId = orderId, 
                    TotalNetMinor = totalNet, 
                    CurrencyId = req.CurrencyId 
                };
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }


        // --------------------------------------------------
        // Persistence helpers used by PaymentService only
        // --------------------------------------------------

        public async Task<(int SpaceId, int CurrencyId, long TotalNetMinor, 
            int? GatewayPreferredId)?> 
            GetOrderHeaderAsync(string orderId)
        {
            const string sql = @"
                SELECT space_id, currency_id, total_net_amount, gateway_preferred_id
                FROM `order`
                WHERE id = @OrderId
                LIMIT 1;
            ";

            await using var conn = await _db.OpenConnectionAsync();
            
            await using var cmd = _db.CreateCommand(conn, sql);
            
            cmd.Parameters.Add(_db.CreateParameter("@OrderId", orderId));

            await using var r = await cmd.ExecuteReaderAsync();
            
            if (!await r.ReadAsync()) return null;

            return (
                SpaceId: r.GetInt32(r.GetOrdinal("space_id")),
                CurrencyId: r.GetInt32(r.GetOrdinal("currency_id")),
                TotalNetMinor: r.GetInt64(r.GetOrdinal("total_net_amount")),
                GatewayPreferredId: r.IsDBNull(r.GetOrdinal("gateway_preferred_id")) 
                    ? null 
                    : r.GetInt32(r.GetOrdinal("gateway_preferred_id"))
            );
        }

        public async Task<string> ResolveCurrencyIsoAsync(int currencyId)
        {
            const string sql = @"SELECT ISO FROM currency WHERE id = @Id LIMIT 1;";
            await using var conn = await _db.OpenConnectionAsync();
            await using var cmd = _db.CreateCommand(conn, sql);
            cmd.Parameters.Add(_db.CreateParameter("@Id", currencyId));
            var o = await cmd.ExecuteScalarAsync();
            if (o is null || o == DBNull.Value)
                throw new InvalidOperationException($"Unknown currency id '{currencyId}'.");
            return Convert.ToString(o)!;
        }

        public async Task<(string Id, string? ProviderIntentId)?> FindPaymentIntentByIdempotencyAsync(string orderId, string idemKey)
        {
            const string sql = @"
                SELECT id, provider_intent_id
                FROM payment_intent
                WHERE order_id = @OrderId AND idempotency_key = @Key
                LIMIT 1;
            ";

            await using var conn = await _db.OpenConnectionAsync();
            await using var cmd = _db.CreateCommand(conn, sql);

            cmd.Parameters.Add(_db.CreateParameter("@OrderId", orderId));
            cmd.Parameters.Add(_db.CreateParameter("@Key", idemKey));

            await using var r = await cmd.ExecuteReaderAsync();

            if (!await r.ReadAsync()) return null;

            return (
                Id: r.GetString(r.GetOrdinal("id")),
                ProviderIntentId: r.IsDBNull(r.GetOrdinal("provider_intent_id")) 
                ? null 
                : r.GetString(r.GetOrdinal("provider_intent_id"))
            );
        }

        public async Task<string> InsertPaymentIntentAsync(
            string orderId, int gatewayId, int statusId, long amountMinor, int currencyId,
            string providerIntentId, string idempotencyKey)
        {
            const string sql = @"
                INSERT INTO payment_intent
                (id, order_id, payment_gateway_id, status_id, amount, currency_id,
                 client_secret, provider_intent_id, idempotency_key, creation_time, modified_by)
                VALUES
                (@Id, @OrderId, @Gw, @Status, @Amt, @Ccy, NULL, @ProvId, @Key, NOW(6), @ModBy);
            ";
            
            var id = Guid.NewGuid().ToString();
            
            await using var conn = await _db.OpenConnectionAsync();
            await using var cmd = _db.CreateCommand(conn, sql);
            
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

        public async Task<(string OrderId, int SpaceId, int CurrencyId, 
            long TotalNetMinor, string UserId, string? ProviderIntentId, int GatewayId)?> 
            GetIntentContextAsync(string intentId)
        {
            const string sql = @"
                SELECT i.order_id, o.space_id, o.currency_id, o.total_net_amount, 
                    o.user_id, i.provider_intent_id, i.payment_gateway_id
                FROM payment_intent i
                JOIN `order` o ON o.id = i.order_id
                WHERE i.id = @IntentId
                LIMIT 1;";
            
            await using var conn = await _db.OpenConnectionAsync();
            
            await using var cmd = _db.CreateCommand(conn, sql);
            cmd.Parameters.Add(_db.CreateParameter("@IntentId", intentId));
            
            await using var r = await cmd.ExecuteReaderAsync();
            
            if (!await r.ReadAsync()) return null;

            return (
                OrderId: r.GetString(r.GetOrdinal("order_id")),
                SpaceId: r.GetInt32(r.GetOrdinal("space_id")),
                CurrencyId: r.GetInt32(r.GetOrdinal("currency_id")),
                TotalNetMinor: r.GetInt64(r.GetOrdinal("total_net_amount")),
                UserId: r.GetString(r.GetOrdinal("user_id")),
                ProviderIntentId: r.IsDBNull(r.GetOrdinal("provider_intent_id")) 
                ? null 
                : r.GetString(r.GetOrdinal("provider_intent_id")),
                GatewayId: r.GetInt32(r.GetOrdinal("payment_gateway_id"))
            );
        }

        public async Task UpdatePaymentIntentStatusAsync(string intentId, int statusId)
        {
            const string sql = @"
        UPDATE payment_intent
        SET status_id = @Status, modified_by = @ModBy
        WHERE id = @IntentId;
    ";

            await using var conn = await _db.OpenConnectionAsync();
            await using var cmd = _db.CreateCommand(conn, sql);
            cmd.Parameters.Add(_db.CreateParameter("@IntentId", intentId));
            cmd.Parameters.Add(_db.CreateParameter("@Status", statusId));
            cmd.Parameters.Add(_db.CreateParameter("@ModBy", "system"));
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task<string> InsertChargeAsync(
            string intentId, int statusId, long amountCapturedMinor, int currencyId,
            string providerChargeId, DateTime paidAtUtc, string userId)
        {
            const string sql = @"
                INSERT INTO payment_charge
                (id, payment_intent_id, status_id, amount_captured, currency_id, provider_charge_id,
                 payment_datetime, creation_time, modified_by)
                VALUES
                (@Id, @IntentId, @Status, @Amt, @Ccy, @ProvCharge, @PaidAt, NOW(6), @ModBy);";
            
            var id = Guid.NewGuid().ToString();
            
            await using var conn = await _db.OpenConnectionAsync();
            await using var cmd = _db.CreateCommand(conn, sql);
            
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
            const string sqlGet = @"
                SELECT total_net_amount FROM `order` WHERE id = @OrderId LIMIT 1;
            ";

            const string sqlSum = @"
                SELECT COALESCE(SUM(pc.amount_captured),0)
                FROM payment_charge pc
                JOIN payment_intent pi ON pi.id = pc.payment_intent_id
                WHERE pi.order_id = @OrderId;
            ";

            const string sqlUpd = @"
                UPDATE `order` SET status_id = @PaidStatusId, modified_by = @ModBy
                WHERE id = @OrderId AND status_id <> @PaidStatusId;
            ";

            await using var conn = await _db.OpenConnectionAsync();

            long totalNet;
            
            await using (var cmd = _db.CreateCommand(conn, sqlGet))
            {
                cmd.Parameters.Add(_db.CreateParameter("@OrderId", orderId));
                var o = await cmd.ExecuteScalarAsync();
                if (o is null || o == DBNull.Value) return;
                totalNet = Convert.ToInt64(o);
            }

            long captured;
            await using (var cmd = _db.CreateCommand(conn, sqlSum))
            {
                cmd.Parameters.Add(_db.CreateParameter("@OrderId", orderId));
                captured = Convert.ToInt64(await cmd.ExecuteScalarAsync() ?? 0);
            }

            if (captured >= totalNet)
            {
                await using var upd = _db.CreateCommand(conn, sqlUpd);
                upd.Parameters.Add(_db.CreateParameter("@PaidStatusId", paidStatusId));
                upd.Parameters.Add(_db.CreateParameter("@OrderId", orderId));
                upd.Parameters.Add(_db.CreateParameter("@ModBy", "system"));
                await upd.ExecuteNonQueryAsync();
            }
        }

        public async Task GrantEntitlementsAsync(string orderId)
        {
            const int GrantedStatus = 2;  // entitlement.status for "granted"
            const int OrderStatusPaid = 3;

            // Idempotent grant: insert entitlements for all order lines of a PAID order
            // that don't already have an entitlement row.
            const string sql = @"
                INSERT INTO entitlement
                    (id, order_line_id, user_id, status, quantity, granted_datetime, 
                    revoked_reason, metadata, creation_time, modified_by)
                SELECT UUID(), ol.id, o.user_id, @GrantedStatus, ol.quantity, NOW(6), 
                    NULL, ol.metadata, NOW(6), @ModBy
                FROM order_line ol
                JOIN `order` o ON o.id = ol.order_id
                LEFT JOIN entitlement e ON e.order_line_id = ol.id
                WHERE ol.order_id = @OrderId
                    AND o.status_id = @OrderStatusPaid
                    AND e.id IS NULL;
            ";

            await using var conn = await _db.OpenConnectionAsync();
            await using var cmd = _db.CreateCommand(conn, sql);
            cmd.Parameters.Add(_db.CreateParameter("@OrderId", orderId));
            cmd.Parameters.Add(_db.CreateParameter("@OrderStatusPaid", OrderStatusPaid));
            cmd.Parameters.Add(_db.CreateParameter("@GrantedStatus", GrantedStatus));
            cmd.Parameters.Add(_db.CreateParameter("@ModBy", "system"));
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task InsertInvoiceFromOrderAsync(int spaceId, string orderId, string chargeId, int gatewayId)
        {
            const int InvoiceStatusPaid = 3;
            const decimal MinorDivisor = 100m;
            int? TaxItemTypeId = null;
            int? FeeItemTypeId = null;

            const string sqlOrderSnapshot = @"
                SELECT
                  o.user_id, o.space_id, o.currency_id, o.remarks,
                  COALESCE((SELECT SUM(ol.unit_amount * ol.quantity) FROM order_line ol WHERE ol.order_id=o.id),0) AS subtotal_major,
                  COALESCE((SELECT SUM(CASE WHEN oa.amount < 0 THEN (-oa.amount)/@MinorDiv ELSE 0 END) FROM order_adjustment oa WHERE oa.order_id=o.id),0) AS discount_major,
                  COALESCE((SELECT SUM(CASE WHEN (@TaxTypeId IS NOT NULL AND oa.item_type_id=@TaxTypeId) OR (@TaxTypeId IS NULL AND oa.code LIKE 'TAX_%') THEN oa.amount/@MinorDiv ELSE 0 END)
                            FROM order_adjustment oa WHERE oa.order_id=o.id),0) AS tax_major,
                  COALESCE((SELECT SUM(CASE WHEN (@FeeTypeId IS NOT NULL AND oa.item_type_id=@FeeTypeId) OR (@FeeTypeId IS NULL AND oa.code LIKE 'FEE_%') THEN oa.amount/@MinorDiv ELSE 0 END)
                            FROM order_adjustment oa WHERE oa.order_id=o.id),0) AS fee_major
                FROM `order` o
                WHERE o.id=@OrderId AND o.space_id=@SpaceId
                LIMIT 1;";

            const string sqlChargeSnapshot = @"
                SELECT pc.provider_charge_id, pi.payment_gateway_id
                FROM payment_charge pc
                JOIN payment_intent pi ON pi.id = pc.payment_intent_id
                WHERE pc.id=@ChargeId
                LIMIT 1;";

            const string sqlInsertInvoice = @"
                INSERT INTO invoice
                (id, invoice_number, user_id, order_id, subscription_id,
                 status_id, payment_charge_id, payment_gateway_id, provider_charge_id,
                 space_id, issue_datetime, currency_id,
                 subtotal_amount, discount_amount, tax_amount, fee_amount, total_amount,
                 bill_to_name, bill_to_email, notes, pdf_url, metadata,
                 creation_time, modified_by)
                VALUES
                (@Id, @No, @UserId, @OrderId, NULL,
                 @Status, @ChargeId, @GatewayId, @ProvChargeId,
                 @SpaceId, NOW(6), @CurrencyId,
                 @Subtotal, @Discount, @Tax, @Fee, @Total,
                 @BillToName, @BillToEmail, @Notes, NULL, @Metadata,
                 NOW(6), @ModBy);";

            await using var conn = await _db.OpenConnectionAsync();

            string userId; int spaceIdDb; int currencyId; string? notes;
            decimal subtotal, discount, tax, fee;

            await using (var cmd = _db.CreateCommand(conn, sqlOrderSnapshot))
            {
                cmd.Parameters.Add(_db.CreateParameter("@OrderId", orderId));
                cmd.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));
                cmd.Parameters.Add(_db.CreateParameter("@MinorDiv", MinorDivisor));
                cmd.Parameters.Add(_db.CreateParameter("@TaxTypeId", (object?)TaxItemTypeId ?? DBNull.Value));
                cmd.Parameters.Add(_db.CreateParameter("@FeeTypeId", (object?)FeeItemTypeId ?? DBNull.Value));

                await using var r = await cmd.ExecuteReaderAsync();
                
                if (!await r.ReadAsync())
                    throw new InvalidOperationException($"Order '{orderId}' not found in space '{spaceId}'.");

                userId = r.GetString(r.GetOrdinal("user_id"));
                spaceIdDb = r.GetInt32(r.GetOrdinal("space_id"));
                currencyId = r.GetInt32(r.GetOrdinal("currency_id"));
                notes = r.IsDBNull(r.GetOrdinal("remarks")) 
                    ? null 
                    : r.GetString(r.GetOrdinal("remarks"));

                subtotal = r.GetDecimal(r.GetOrdinal("subtotal_major"));
                discount = r.GetDecimal(r.GetOrdinal("discount_major"));
                tax = r.GetDecimal(r.GetOrdinal("tax_major"));
                fee = r.GetDecimal(r.GetOrdinal("fee_major"));
            }

            string? providerChargeId = null;
            int gatewayIdFromDb = gatewayId;

            await using (var cmd = _db.CreateCommand(conn, sqlChargeSnapshot))
            {
                cmd.Parameters.Add(_db.CreateParameter("@ChargeId", chargeId));
                await using var r = await cmd.ExecuteReaderAsync();
                if (await r.ReadAsync())
                {
                    providerChargeId = r.IsDBNull(r.GetOrdinal("provider_charge_id")) ? null : r.GetString(r.GetOrdinal("provider_charge_id"));
                    if (!r.IsDBNull(r.GetOrdinal("payment_gateway_id"))) gatewayIdFromDb = r.GetInt32(r.GetOrdinal("payment_gateway_id"));
                }
            }

            var total = subtotal - discount + tax + fee;
            var invoiceId = Guid.NewGuid().ToString();
            var invoiceNo = $"INV-{DateTime.UtcNow:yyyyMMddHHmmss}-{invoiceId[..8]}";
            var billToName = userId;
            string? billToEmail = null;

            var metadata = System.Text.Json.JsonSerializer.Serialize(new
            {
                source = "auto_from_order",
                orderId,
                chargeId,
                spaceId = spaceIdDb,
                currencyId,
                snapshotAt = DateTime.UtcNow
            });

            await using (var ins = _db.CreateCommand(conn, sqlInsertInvoice))
            {
                ins.Parameters.Add(_db.CreateParameter("@Id", invoiceId));
                ins.Parameters.Add(_db.CreateParameter("@No", invoiceNo));
                ins.Parameters.Add(_db.CreateParameter("@UserId", userId));
                ins.Parameters.Add(_db.CreateParameter("@OrderId", orderId));
                ins.Parameters.Add(_db.CreateParameter("@Status", InvoiceStatusPaid));
                ins.Parameters.Add(_db.CreateParameter("@ChargeId", chargeId));
                ins.Parameters.Add(_db.CreateParameter("@GatewayId", gatewayIdFromDb));
                ins.Parameters.Add(_db.CreateParameter("@ProvChargeId", (object?)providerChargeId ?? DBNull.Value));
                ins.Parameters.Add(_db.CreateParameter("@SpaceId", spaceIdDb));
                ins.Parameters.Add(_db.CreateParameter("@CurrencyId", currencyId));

                ins.Parameters.Add(_db.CreateParameter("@Subtotal", subtotal));
                ins.Parameters.Add(_db.CreateParameter("@Discount", discount));
                ins.Parameters.Add(_db.CreateParameter("@Tax", tax));
                ins.Parameters.Add(_db.CreateParameter("@Fee", fee));
                ins.Parameters.Add(_db.CreateParameter("@Total", total));

                ins.Parameters.Add(_db.CreateParameter("@BillToName", billToName));
                ins.Parameters.Add(_db.CreateParameter("@BillToEmail", (object?)billToEmail ?? DBNull.Value));
                ins.Parameters.Add(_db.CreateParameter("@Notes", (object?)notes ?? DBNull.Value));
                ins.Parameters.Add(_db.CreateParameter("@Metadata", metadata));
                ins.Parameters.Add(_db.CreateParameter("@ModBy", "system"));

                await ins.ExecuteNonQueryAsync();
            }
        }

        public async Task<(string OrderId, int SpaceId, int CurrencyId, int GatewayId,
                  string? ProviderChargeId, long AmountCapturedMinor, long TotalRefundedSoFarMinor)?>
        GetChargeContextAsync(string chargeId)
        {
            const string sql = @"
                SELECT
                  o.id                                   AS order_id,
                  o.space_id,
                  o.currency_id,
                  pi.payment_gateway_id,
                  pc.provider_charge_id,
                  pc.amount_captured                              AS amount_captured_minor,
                  COALESCE(SUM(pr.amount), 0)                     AS total_refunded_so_far_minor
                FROM payment_charge pc
                JOIN payment_intent pi ON pi.id = pc.payment_intent_id
                JOIN `order` o        ON o.id  = pi.order_id
                LEFT JOIN payment_refund pr ON pr.payment_charge_id = pc.id
                                           AND pr.status_id = 3      -- succeeded only
                WHERE pc.id = @ChargeId
                GROUP BY o.id, o.space_id, o.currency_id, pi.payment_gateway_id, pc.provider_charge_id, pc.amount_captured;
                ";
            
            await using var conn = await _db.OpenConnectionAsync();
            await using var cmd = _db.CreateCommand(conn, sql);
            
            cmd.Parameters.Add(_db.CreateParameter("@ChargeId", chargeId));
            
            await using var r = await cmd.ExecuteReaderAsync();
            
            if (!await r.ReadAsync()) return null;

            return (
                OrderId: r.GetString(r.GetOrdinal("order_id")),
                SpaceId: r.GetInt32(r.GetOrdinal("space_id")),
                CurrencyId: r.GetInt32(r.GetOrdinal("currency_id")),
                GatewayId: r.GetInt32(r.GetOrdinal("payment_gateway_id")),
                ProviderChargeId: r.IsDBNull(r.GetOrdinal("provider_charge_id")) ? null : r.GetString(r.GetOrdinal("provider_charge_id")),
                AmountCapturedMinor: r.GetInt64(r.GetOrdinal("amount_captured_minor")),
                TotalRefundedSoFarMinor: r.GetInt64(r.GetOrdinal("total_refunded_so_far_minor"))
            );
        }

        public async Task<string> InsertRefundAsync(
            string chargeId, int statusId, long amountMinor, int currencyId,
            string providerRefundId, string? reason)
        {
            const string sql = @"
                INSERT INTO payment_refund
                (id, payment_charge_id, status_id, amount, currency_id,
                 provider_refund_id, reason, refund_datetime, creation_time, modified_by)
                VALUES
                (@Id, @ChargeId, @Status, @Amt, @Ccy, @ProvRefund, @Reason, NOW(6), NOW(6), @ModBy);";

            var id = Guid.NewGuid().ToString();

            await using var conn = await _db.OpenConnectionAsync();
            await using var cmd = _db.CreateCommand(conn, sql);

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
