using System.Data.Common;
using System.Reflection;

namespace GMS.TifoXRCoreWebAPI.Utilities.Infrastructure
{
    public sealed class DefaultDbProvider(DbProviderOptions options) : IDbProvider
    {
        private readonly DbProviderFactory _factory = TryGetFactory(options.ProviderInvariantName)
                       ?? RegisterAndGet(options.ProviderInvariantName)
                       ?? throw new ArgumentException(
                            $"The specified invariant name '{options.ProviderInvariantName}' wasn't found or could not be registered.");
        private readonly string _connStr = options.ConnectionString;

        public async Task<DbConnection> OpenConnectionAsync()
        {
            var conn = _factory.CreateConnection()!;
            conn.ConnectionString = _connStr;
            await conn.OpenAsync();
            return conn;
        }

        public DbCommand CreateCommand(DbConnection conn, string sql, DbTransaction? tx = null)
        {
            var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            cmd.Transaction = tx;
            return cmd;
        }

        public DbParameter CreateParameter(string name, object? value)
        {
            var p = _factory.CreateParameter()!;
            p.ParameterName = name;
            p.Value = value ?? DBNull.Value;
            return p;
        }

        static DbProviderFactory? TryGetFactory(string name)
        {
            try { return DbProviderFactories.GetFactory(name); }
            catch { return null; }
        }

        static DbProviderFactory? RegisterAndGet(string name)
        {
            // map invariant -> provider factory type
            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["MySqlConnector"] = "MySqlConnector.MySqlConnectorFactory, MySqlConnector",
                ["MySql.Data.MySqlClient"] = "MySql.Data.MySqlClient.MySqlClientFactory, MySql.Data",
                ["Microsoft.Data.SqlClient"] = "Microsoft.Data.SqlClient.SqlClientFactory, Microsoft.Data.SqlClient",
                ["System.Data.SqlClient"] = "System.Data.SqlClient.SqlClientFactory, System.Data.SqlClient"
            };

            if (!map.TryGetValue(name, out var typeName)) return null;

            var t = Type.GetType(typeName, throwOnError: false);
            var instance = t?
                .GetProperty("Instance", BindingFlags.Public | BindingFlags.Static)?
                .GetValue(null) as DbProviderFactory;

            if (instance == null) return null;

            DbProviderFactories.RegisterFactory(name, instance);
            return DbProviderFactories.GetFactory(name);
        }
    }
}
