using TifoXRCoreWebAPI.Utilities.Infrastructure.Interface;

namespace TifoXRCoreWebAPI.Utilities.Infrastructure
{
    public sealed class MySqlDialect : ISqlDialect
    {
        public string AppendIdentitySelect(string insertSql, string idColumn = "id")
            => $"{insertSql}; SELECT LAST_INSERT_ID();";
    }
}
