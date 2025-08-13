using System.Data.Common;

namespace TifoXRCoreWebAPI.Utilities.Infrastructure.Interface
{
    public interface IDbProvider
    {
        Task<DbConnection> OpenConnectionAsync();
        DbCommand CreateCommand(DbConnection conn, string sql, DbTransaction? tx = null);
        DbParameter CreateParameter(string name, object? value);
    }
}
