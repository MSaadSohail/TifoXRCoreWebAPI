
namespace GMS.TifoXRCoreWebAPI.Utilities.Infrastructure
{
    public sealed class SqlServerDialect : ISqlDialect
    {
        public string AppendIdentitySelect(string insertSql, string idColumn = "id")
            => $"{insertSql}; SELECT CAST(SCOPE_IDENTITY() AS INT);";
    }
}
