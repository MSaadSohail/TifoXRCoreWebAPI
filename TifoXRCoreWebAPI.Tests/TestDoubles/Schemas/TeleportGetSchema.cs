// <copyright file="TeleportGetSchema.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Urvashi Dhingra</author>
// <date>08/15/2025</date>
// <summary>Helper class that builds an empty DataTable schema matching the expected SELECT aliases for GetTeleportTableBySpaceAsync.</summary>

using System.Data;

namespace TifoXRCoreWebAPI.Tests.TestDoubles.Schemas
{
    /// <summary>
    /// Builds the exact columns that GetTeleportTableBySpaceAsync expects from its SELECT aliases.
    /// Use this to produce a DataTable and then call CreateDataReader() for a fresh reader per test.
    /// </summary>
    public static class TeleportGetSchema
    {
        public static DataTable CreateEmptySchema()
        {
            var t = new DataTable();
            t.Columns.Add("table_id", typeof(int));
            t.Columns.Add("space_id", typeof(int));
            t.Columns.Add("is_active", typeof(bool));
            t.Columns.Add("table_name_key", typeof(string));
            t.Columns.Add("table_locale_id", typeof(string));
            t.Columns.Add("table_localized_value", typeof(string));
            t.Columns.Add("button_id", typeof(int));
            t.Columns.Add("button_name_key", typeof(string));
            t.Columns.Add("button_is_active", typeof(bool));
            t.Columns.Add("map_spot_id", typeof(int));
            t.Columns.Add("map_spot_x", typeof(decimal));
            t.Columns.Add("map_spot_y", typeof(decimal));
            t.Columns.Add("map_spot_z", typeof(decimal));
            t.Columns.Add("button_locale_id", typeof(string));
            t.Columns.Add("button_localized_value", typeof(string));
            return t;
        }
    }
}
