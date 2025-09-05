namespace GMS.TifoXRCoreWebAPI.Utilities.Infrastructure
{
    public interface ISqlDialect
    {
        // Helps where vendor syntax differs
        string AppendIdentitySelect(string insertSql, string idColumn = "id");
    }
}
