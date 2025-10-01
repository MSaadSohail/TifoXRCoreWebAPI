// <copyright file="IDbProvider.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Saad Sohail</author>
// <date>09/02/2025</date>
// <summary></summary>

using System.Data.Common;

namespace GMS.TifoXRCoreWebAPI.Utilities.Infrastructure
{
    public interface IDbProvider
    {
        Task<DbConnection> OpenConnectionAsync();
        DbCommand CreateCommand(DbConnection conn, string sql, DbTransaction? tx = null);
        DbParameter CreateParameter(string name, object? value);
    }
}
