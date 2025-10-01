// <copyright file="OrderRepository.Entitlements.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Saad Sohail</author>
// <date>09/16/2025</date>
// <summary></summary>

using static GMS.TifoXRCoreWebAPI.Repositories.SQL.PaymentIntentSql;

namespace GMS.TifoXRCoreWebAPI.Repositories
{
    /// <summary>Entitlement grant (idempotent) after an order is paid.</summary>
    public sealed partial class OrderRepository : IOrderRepository
    {
        public async Task GrantEntitlementsAsync(string orderId)
        {
            const int GrantedStatus = 2;   // your entitlement status for "granted"       //FIX ME: Get from look up tables
            const int OrderStatusPaid = 3; // your order status for "paid"

            await using var conn = await _db.OpenConnectionAsync();
            await using var cmd = _db.CreateCommand(conn, Entitlement_Insert);

            cmd.Parameters.Add(_db.CreateParameter("@OrderId", orderId));
            cmd.Parameters.Add(_db.CreateParameter("@OrderStatusPaid", OrderStatusPaid));
            cmd.Parameters.Add(_db.CreateParameter("@GrantedStatus", GrantedStatus));
            cmd.Parameters.Add(_db.CreateParameter("@ModBy", "system"));

            await cmd.ExecuteNonQueryAsync();
        }

        public async Task RevokeEntitlementsAsync(string orderId, string reason)
        {
            const int RevokedStatus = 3; // your entitlement status for "revoked"       //FIX ME: Get from look up tables

            await using var conn = await _db.OpenConnectionAsync();
            await using var cmd = _db.CreateCommand(conn, Entitlement_RevokeByOrder);

            cmd.Parameters.Add(_db.CreateParameter("@OrderId", orderId));
            cmd.Parameters.Add(_db.CreateParameter("@RevokedStatus", RevokedStatus));
            cmd.Parameters.Add(_db.CreateParameter("@Reason", (object?)reason ?? DBNull.Value));
            cmd.Parameters.Add(_db.CreateParameter("@ModBy", "system"));

            await cmd.ExecuteNonQueryAsync();
        }
    }
}

