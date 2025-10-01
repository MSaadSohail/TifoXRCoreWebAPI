// <copyright file="RepoHelpers.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Saad Sohail</author>
// <date>9/16/2025</date>
// <summary></summary>

using System.Data.Common;
//
using GMS.TifoXRCoreWebAPI.Utilities.Infrastructure;
using static GMS.TifoXRCoreWebAPI.Repositories.SQL.OrderSql;

namespace TifoXRCoreWebAPI.Utilities.Infrastructure
{
    internal static class RepoHelpers
    {
        internal static async Task<bool> ExistsOrderInSpaceAsync(
            IDbProvider db, 
            DbConnection conn, 
            string orderId, 
            int spaceId)
        {
            await using var cmd = db.CreateCommand(conn, Order_ExistsInSpace);
            cmd.Parameters.Add(db.CreateParameter("@OrderId", orderId));
            cmd.Parameters.Add(db.CreateParameter("@SpaceId", spaceId));
            var o = await cmd.ExecuteScalarAsync();
            return !(o is null || o == DBNull.Value);
        }
    }
}
