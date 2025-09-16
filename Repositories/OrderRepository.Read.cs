// <copyright file="OrderRepository.Read.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Saad Sohail</author>
// <date>9/16/2025</date>
// <summary></summary>

using GMS.TifoXRCoreWebAPI.Models;
using System.Data.Common;
using static GMS.TifoXRCoreWebAPI.Repositories.SQL.OrderSql;

namespace GMS.TifoXRCoreWebAPI.Repositories
{
    public sealed partial class OrderRepository : IOrderRepository
    {
        // IOrderRepository.GetOrderAsync
        public async Task<OrderDto?> GetOrderAsync(int spaceId, string orderId)
        {
            await using var conn = await _db.OpenConnectionAsync();

            OrderDto? dto;

            // 1) Header
            await using (var cmd = _db.CreateCommand(conn, Order_SelectHeader))
            {
                cmd.Parameters.Add(_db.CreateParameter("@OrderId", orderId));
                cmd.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));

                await using var r = await cmd.ExecuteReaderAsync();
                if (!await r.ReadAsync()) return null;

                // Your mapper should initialize the lists (LineItems/Adjustments/PaymentIntents/Entitlements/Invoices).
                dto = Utilities.Infrastructure.DtoMappers.MapOrderHeader(r);
            }

            // 2) Load children and ADD to the already-initialized lists
            //    (no reassignments to init-only properties)
            var lines = await LoadLinesAsync(conn, orderId);
            var adjustments = await LoadAdjustmentsAsync(conn, orderId);
            var intentsGraph = await LoadIntentsGraphAsync(conn, orderId);
            var entitlements = await LoadEntitlementsAsync(conn, orderId);
            var invoices = await LoadInvoicesAsync(conn, orderId);

            dto!.LineItems.AddRange(lines);
            dto.Adjustments.AddRange(adjustments);
            dto.PaymentIntents.AddRange(intentsGraph);
            dto.Entitlements.AddRange(entitlements);
            dto.Invoices.AddRange(invoices);

            return dto;
        }

        public async Task<List<EntitlementDto>?> GetEntitlementsByOrderAsync(int spaceId, string orderId)
        {
            await using var conn = await _db.OpenConnectionAsync();

            if (!await ExistsOrderInSpaceAsync(conn, spaceId, orderId)) return null;

            return await LoadEntitlementsAsync(conn, orderId);
        }

        // IOrderRepository.GetInvoicesByOrderAsync
        public async Task<InvoiceListResponse?> GetInvoicesByOrderAsync(int spaceId, string orderId)
        {
            await using var conn = await _db.OpenConnectionAsync();
            if (!await ExistsOrderInSpaceAsync(conn, spaceId, orderId)) return null;

            var invoices = await LoadInvoicesAsync(conn, orderId);
            return new InvoiceListResponse { Invoices = invoices };
        }

        private async Task<bool> ExistsOrderInSpaceAsync(DbConnection conn, int spaceId, string orderId)
        {
            await using var check = _db.CreateCommand(conn, Order_ExistsInSpace);
            check.Parameters.Add(_db.CreateParameter("@OrderId", orderId));
            check.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));
            var exists = await check.ExecuteScalarAsync();
            return exists is not null;
        }

        private async Task<List<OrderLineDto>> LoadLinesAsync(DbConnection conn, string orderId)
        {
            var list = new List<OrderLineDto>();
            await using var cmd = _db.CreateCommand(conn, Order_SelectLines);
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
                var unitMinor = (long)decimal.Round(unitMajor * 100m, 0, MidpointRounding.AwayFromZero);

                list.Add(new OrderLineDto
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
            return list;
        }

        private async Task<List<OrderAdjustmentDto>> LoadAdjustmentsAsync(DbConnection conn, string orderId)
        {
            var list = new List<OrderAdjustmentDto>();
            await using var cmd = _db.CreateCommand(conn, Order_SelectAdjustments);
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
                list.Add(new OrderAdjustmentDto
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
            return list;
        }

        private async Task<List<EntitlementDto>> LoadEntitlementsAsync(DbConnection conn, string orderId)
        {
            var list = new List<EntitlementDto>();
            await using var cmd = _db.CreateCommand(conn, Order_SelectEntitlements);

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

            return list;
        }

        private async Task<List<InvoiceSummaryDto>> LoadInvoicesAsync(DbConnection conn, string orderId)
        {
            var list = new List<InvoiceSummaryDto>();
            await using var cmd = _db.CreateCommand(conn, Order_SelectInvoices);
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
            return list;
        }

        private async Task<List<PaymentIntentDto>> LoadIntentsGraphAsync(DbConnection conn, string orderId)
        {
            // 1) Load intents
            var intents = new List<PaymentIntentDto>();
            var dict = new Dictionary<string, PaymentIntentDto>();

            await using (var cmd = _db.CreateCommand(conn, Intent_SelectByOrder))
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
                    intents.Add(dto);
                    dict[dto.Id] = dto;
                }
            }

            // 2) Load charges
            var chargesById = new Dictionary<string, PaymentChargeDto>();
            await using (var cmd = _db.CreateCommand(conn, Charge_SelectByOrder))
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
                    if (dict.TryGetValue(intentId, out var intent))
                        intent.Charges.Add(charge);

                    chargesById[charge.Id] = charge;
                }
            }

            // 3) Load refunds
            await using (var cmd = _db.CreateCommand(conn, Refund_SelectByOrder))
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

            return intents;
        }
    }
}
