namespace TifoXRCoreWebAPI.Utilities.Infrastructure
{
    public sealed class DbProviderOptions
    {
        public string ProviderInvariantName { get; set; } = ""; // e.g., "MySqlConnector", "Microsoft.Data.SqlClient"
        public string ConnectionString { get; set; } = "";
    }
}
