// <copyright file="OrderRepository.Invoices.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Saad Sohail</author>
// <date>09/16/2025</date>
// <summary></summary>

using static GMS.TifoXRCoreWebAPI.Repositories.SQL.PaymentIntentSql;

namespace GMS.TifoXRCoreWebAPI.Repositories
{
    /// <summary>Invoice persistence derived from order+charge snapshot.</summary>
    public sealed partial class OrderRepository : IOrderRepository
    {
        public async Task InsertInvoiceFromOrderAsync(int spaceId, string orderId, string chargeId, int gatewayId)
        {
            const int InvoiceStatusPaid = 3;    //FIX ME: Get from db
            const decimal MinorDivisor = 100m;  //TODO: Verify in query
            int? TaxItemTypeId = null;   
            int? FeeItemTypeId = null;

            await using var conn = await _db.OpenConnectionAsync();

            // Order snapshot
            string userId; int spaceIdDb; int currencyId; string? notes;
            decimal subtotal, discount, tax, fee;

            await using (var cmd = _db.CreateCommand(conn, Order_Snapshot))
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
                notes = r.IsDBNull(r.GetOrdinal("remarks")) ? null : r.GetString(r.GetOrdinal("remarks"));

                subtotal = r.GetDecimal(r.GetOrdinal("subtotal_major"));
                discount = r.GetDecimal(r.GetOrdinal("discount_major"));
                tax = r.GetDecimal(r.GetOrdinal("tax_major"));
                fee = r.GetDecimal(r.GetOrdinal("fee_major"));
            }

            // Charge snapshot
            string? providerChargeId = null;
            int gatewayIdFromDb = gatewayId;

            await using (var cmd = _db.CreateCommand(conn, Charge_Snapshot))
            {
                cmd.Parameters.Add(_db.CreateParameter("@ChargeId", chargeId));

                await using var r = await cmd.ExecuteReaderAsync();

                if (await r.ReadAsync())
                {
                    providerChargeId = r.IsDBNull(r.GetOrdinal("provider_charge_id")) 
                        ? null 
                        : r.GetString(r.GetOrdinal("provider_charge_id"));

                    if (!r.IsDBNull(r.GetOrdinal("payment_gateway_id"))) 
                        gatewayIdFromDb = r.GetInt32(r.GetOrdinal("payment_gateway_id"));
                }
            }

            var total = subtotal - discount + tax + fee;

            // Insert invoice
            var invoiceId = Guid.NewGuid().ToString();
            var invoiceNo = $"INV-{DateTime.UtcNow:yyyyMMddHHmmss}-{invoiceId[..8]}";
            var billToName = userId;
            string? billToEmail = null;    // populate if you capture emails
            string? metadata = null;       // attach any invoice metadata as JSON if needed

            await using (var cmd = _db.CreateCommand(conn, Invoice_Insert))
            {
                cmd.Parameters.Add(_db.CreateParameter("@Id", invoiceId));
                cmd.Parameters.Add(_db.CreateParameter("@No", invoiceNo));
                cmd.Parameters.Add(_db.CreateParameter("@UserId", userId));
                cmd.Parameters.Add(_db.CreateParameter("@OrderId", orderId));
                cmd.Parameters.Add(_db.CreateParameter("@Status", InvoiceStatusPaid));
                cmd.Parameters.Add(_db.CreateParameter("@ChargeId", chargeId));
                cmd.Parameters.Add(_db.CreateParameter("@GatewayId", gatewayIdFromDb));
                cmd.Parameters.Add(_db.CreateParameter("@ProvChargeId", (object?)providerChargeId ?? DBNull.Value));
                cmd.Parameters.Add(_db.CreateParameter("@SpaceId", spaceIdDb));
                cmd.Parameters.Add(_db.CreateParameter("@CurrencyId", currencyId));
                cmd.Parameters.Add(_db.CreateParameter("@Subtotal", subtotal));
                cmd.Parameters.Add(_db.CreateParameter("@Discount", discount));
                cmd.Parameters.Add(_db.CreateParameter("@Tax", tax));
                cmd.Parameters.Add(_db.CreateParameter("@Fee", fee));
                cmd.Parameters.Add(_db.CreateParameter("@Total", total));
                cmd.Parameters.Add(_db.CreateParameter("@BillToName", billToName));
                cmd.Parameters.Add(_db.CreateParameter("@BillToEmail", (object?)billToEmail ?? DBNull.Value));
                cmd.Parameters.Add(_db.CreateParameter("@Notes", (object?)notes ?? DBNull.Value));
                cmd.Parameters.Add(_db.CreateParameter("@Metadata", (object?)metadata ?? DBNull.Value));
                cmd.Parameters.Add(_db.CreateParameter("@ModBy", "system"));

                await cmd.ExecuteNonQueryAsync();
            }
        }
    }
}
