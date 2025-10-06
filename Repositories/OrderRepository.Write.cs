// <copyright file="OrderRepository.Write.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Saad Sohail</author>
// <date>09/16/2025</date>
// <summary></summary>

using System.Text.Json;
using GMS.TifoXRCoreWebAPI.Models;
using static GMS.TifoXRCoreWebAPI.Repositories.SQL.OrderSql;

namespace GMS.TifoXRCoreWebAPI.Repositories
{
    /// <summary>Order create + lines/adjustments only (no gateway calls).</summary>
    public sealed partial class OrderRepository : IOrderRepository
    {
        public async Task<CreateOrderResponse> CreateOrderAsync(
            int pathSpaceId,
            CreateOrderRequest req,
            int? gatewayPreferredId = null)
        {
            const int PendingOrderStatusId = 2;         // TODO: look up from config/table if you prefer
            const string ModifiedBy = "system";         // TODO: Update this accordingly

            if (req is null)
                throw new ArgumentNullException(nameof(req));

            if (string.IsNullOrWhiteSpace(req.IdempotencyKey)) 
                throw new ArgumentException("IdempotencyKey is required.");

            if (string.IsNullOrWhiteSpace(req.UserId)) 
                throw new ArgumentException("UserId is required.");

            if (req.LineItems is null || req.LineItems.Count == 0) 
                throw new ArgumentException("At least one line is required.");

            await using var conn = await _db.OpenConnectionAsync();
            await using var tx = await conn.BeginTransactionAsync();

            try
            {
                string? existingOrderId = null;
                long existingNet = 0;

                //0) Find existing order  with same idempotency
                await using (var findCmd = _db.CreateCommand(conn, Order_FindExisting))
                {
                    findCmd.Transaction = tx;
                    findCmd.Parameters.Add(_db.CreateParameter("@IdemKey", req.IdempotencyKey));

                    await using var r = await findCmd.ExecuteReaderAsync();

                    if (await r.ReadAsync())
                    {
                        var space = r.GetInt32(r.GetOrdinal("space_id"));

                        if (space != pathSpaceId)
                            throw new InvalidOperationException("Idempotency key already used for a different space.");

                        existingOrderId = r.GetString(r.GetOrdinal("id"));
                        existingNet = r.GetInt64(r.GetOrdinal("total_net_amount"));
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

                // 1) totals
                long totalGross = 0, totalDiscount = 0, totalTax = 0, totalFees = 0;

                foreach (var l in req.LineItems)
                    totalGross += checked(l.UnitAmountMinor * l.Quantity);

                if (req.Adjustments is { Count: > 0 })
                {
                    foreach (var a in req.Adjustments)
                    {
                        if (a.AmountMinor < 0) totalDiscount += checked(-a.AmountMinor);

                        else
                        {
                            var code = a.Code ?? string.Empty;
                            if (code.StartsWith("TAX", StringComparison.OrdinalIgnoreCase) ||
                                code.StartsWith("VAT", StringComparison.OrdinalIgnoreCase))
                                totalTax += a.AmountMinor;
                            else
                                totalFees += a.AmountMinor;
                        }
                    }
                }

                var totalNet = checked(totalGross - totalDiscount + totalTax + totalFees);

                // 2) insert order
                var orderId = Guid.NewGuid().ToString();
                var sessionId = string.IsNullOrWhiteSpace(req.SessionId) ? Guid.NewGuid().ToString() : req.SessionId;

                
                await using (var cmd = _db.CreateCommand(conn, Order_Insert))
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

                // 3) lines
                

                foreach (var item in req.LineItems)
                {
                    var lineItemId = Guid.NewGuid().ToString();
                    var unitAmountDecimal = item.UnitAmountMinor / 100m;
                    var metaJson = item.Metadata is null ? null : JsonSerializer.Serialize(item.Metadata);

                    await using var lineCmd = _db.CreateCommand(conn, Order_InsertLine);
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

                // 4) adjustments
                if (req.Adjustments is { Count: > 0 })
                {
                    foreach (var a in req.Adjustments)
                    {
                        var adjId = Guid.NewGuid().ToString();
                        var metaJson = a.Metadata is null ? null : JsonSerializer.Serialize(a.Metadata);

                        await using var adjCmd = _db.CreateCommand(conn, Order_InsertAdjustment);
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
    }
}
