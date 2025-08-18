// ===== Test Schema Helper =====
using System.Data;

namespace GMS.TifoXRCoreWebAPI.Tests.TestDoubles.Schemas
{
    public static class BoothGetSchema
    {
        public static DataTable CreateEmptySchema()
        {
            var t = new DataTable();
            t.Columns.Add("id", typeof(int));
            t.Columns.Add("space_id", typeof(int));
            t.Columns.Add("name_key", typeof(string));
            t.Columns.Add("map_spot_id", typeof(int));
            t.Columns.Add("x", typeof(decimal));
            t.Columns.Add("y", typeof(decimal));
            t.Columns.Add("z", typeof(decimal));
            t.Columns.Add("locale_id", typeof(string));
            t.Columns.Add("value", typeof(string));
            return t;
        }
    }
}
