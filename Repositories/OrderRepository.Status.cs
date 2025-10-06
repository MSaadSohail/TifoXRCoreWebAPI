// <copyright file="OrderRepository.Status.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Saad Sohail</author>
// <date>09/25/2025</date>
// <summary></summary>

using static GMS.TifoXRCoreWebAPI.Repositories.SQL.OrderSql;

namespace GMS.TifoXRCoreWebAPI.Repositories
{
    public sealed partial class OrderRepository : IOrderRepository
    {
        public async Task RevokeOrderAsync(string orderId, string? reason)
        {
            const int RevokedStatusId = 4; // your order status for "revoked"       //FIX ME: Get from look up tables

            await using var conn = await _db.OpenConnectionAsync();
            await using var cmd = _db.CreateCommand(conn, Order_UpdateStatus);

            cmd.Parameters.Add(_db.CreateParameter("@OrderId", orderId));
            cmd.Parameters.Add(_db.CreateParameter("@StatusId", RevokedStatusId));
            cmd.Parameters.Add(_db.CreateParameter("@ChangeReason", (object?)reason ?? DBNull.Value));
            cmd.Parameters.Add(_db.CreateParameter("@ModBy", "system"));

            await cmd.ExecuteNonQueryAsync();
        }
    }
}
