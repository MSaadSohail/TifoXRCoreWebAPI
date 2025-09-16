// <copyright file="OrderRepository.Read.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Saad Sohail</author>
// <date>9/16/2025</date>
// <summary></summary>

using System.Data.Common;
//
using GMS.TifoXRCoreWebAPI.Models;
using GMS.TifoXRCoreWebAPI.Utilities.Infrastructure;
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

                // Mapper should initialize the lists (LineItems/Adjustments/PaymentIntents/Entitlements/Invoices).
                dto = DtoMappers.MapOrderHeader(r);
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

            while (await r.ReadAsync())
                list.Add(DtoMappers.MapOrderLine(r));

            return list;
        }

        private async Task<List<OrderAdjustmentDto>> LoadAdjustmentsAsync(DbConnection conn, string orderId)
        {
            var list = new List<OrderAdjustmentDto>();

            await using var cmd = _db.CreateCommand(conn, Order_SelectAdjustments);

            cmd.Parameters.Add(_db.CreateParameter("@OrderId", orderId));

            await using var r = await cmd.ExecuteReaderAsync();

            while (await r.ReadAsync())
                list.Add(DtoMappers.MapOrderAdjustment(r));

            return list;
        }

        private async Task<List<EntitlementDto>> LoadEntitlementsAsync(DbConnection conn, string orderId)
        {
            var list = new List<EntitlementDto>();

            await using var cmd = _db.CreateCommand(conn, Order_SelectEntitlements);

            cmd.Parameters.Add(_db.CreateParameter("@OrderId", orderId));

            await using var r = await cmd.ExecuteReaderAsync();

            while (await r.ReadAsync())
                list.Add(DtoMappers.MapEntitlement(r));

            return list;
        }

        private async Task<List<InvoiceSummaryDto>> LoadInvoicesAsync(DbConnection conn, string orderId)
        {
            var list = new List<InvoiceSummaryDto>();

            await using var cmd = _db.CreateCommand(conn, Order_SelectInvoices);

            cmd.Parameters.Add(_db.CreateParameter("@OrderId", orderId));

            await using var r = await cmd.ExecuteReaderAsync();

            while (await r.ReadAsync())
                list.Add(DtoMappers.MapInvoiceSummary(r));

            return list;
        }

        private async Task<List<PaymentIntentDto>> LoadIntentsGraphAsync(DbConnection conn, string orderId)
        {
            var intents = new List<PaymentIntentDto>();
            var dict = new Dictionary<string, PaymentIntentDto>();

            // 1) intents
            await using (var cmd = _db.CreateCommand(conn, Intent_SelectByOrder))
            {
                cmd.Parameters.Add(_db.CreateParameter("@OrderId", orderId));

                await using var r = await cmd.ExecuteReaderAsync();

                while (await r.ReadAsync())
                {
                    var dto = DtoMappers.MapPaymentIntent(r);

                    intents.Add(dto);
                    dict[dto.Id] = dto;
                }
            }

            // 2) charges
            var chargesById = new Dictionary<string, PaymentChargeDto>();
            await using (var cmd = _db.CreateCommand(conn, Charge_SelectByOrder))
            {
                cmd.Parameters.Add(_db.CreateParameter("@OrderId", orderId));

                await using var r = await cmd.ExecuteReaderAsync();

                var o_intent = r.GetOrdinal("payment_intent_id");

                while (await r.ReadAsync())
                {
                    var charge = DtoMappers.MapPaymentCharge(r);
                    var intentId = r.GetString(o_intent);

                    if (dict.TryGetValue(intentId, out var intent))
                        intent.Charges.Add(charge);

                    chargesById[charge.Id] = charge;
                }
            }

            // 3) refunds
            await using (var cmd = _db.CreateCommand(conn, Refund_SelectByOrder))
            {
                cmd.Parameters.Add(_db.CreateParameter("@OrderId", orderId));

                await using var r = await cmd.ExecuteReaderAsync();

                var o_charge = r.GetOrdinal("payment_charge_id");

                while (await r.ReadAsync())
                {
                    var refund = DtoMappers.MapPaymentRefund(r);
                    var chargeId = r.GetString(o_charge);

                    if (chargesById.TryGetValue(chargeId, out var charge))
                        charge.Refunds.Add(refund);
                }
            }

            return intents;
        }
    }
}
