// <copyright file="OrderRepository.Entitlements.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Saad Sohail</author>
// <date>9/16/2025</date>
// <summary></summary>

namespace GMS.TifoXRCoreWebAPI.Repositories
{
    /// <summary>Entitlement grant (idempotent) after an order is paid.</summary>
    public sealed partial class OrderRepository : IOrderRepository
    {
        public async Task GrantEntitlementsAsync(string orderId)
        {
            const int GrantedStatus = 2;   // your entitlement status for "granted"
            const int OrderStatusPaid = 3; // your order status for "paid"

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
                  AND e.id IS NULL;";

            await using var conn = await _db.OpenConnectionAsync();
            await using var cmd = _db.CreateCommand(conn, sql);
            cmd.Parameters.Add(_db.CreateParameter("@OrderId", orderId));
            cmd.Parameters.Add(_db.CreateParameter("@OrderStatusPaid", OrderStatusPaid));
            cmd.Parameters.Add(_db.CreateParameter("@GrantedStatus", GrantedStatus));
            cmd.Parameters.Add(_db.CreateParameter("@ModBy", "system"));
            await cmd.ExecuteNonQueryAsync();
        }
    }
}

