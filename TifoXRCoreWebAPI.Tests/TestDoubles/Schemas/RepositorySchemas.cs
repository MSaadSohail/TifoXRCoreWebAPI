// © 2025 Global Mobile Software LLC. All rights reserved.
// Author: Urvashi Dhingra
// Date: 08/19/2025
// Summary:
// Generic schema helpers for repository tests. Each method returns an empty
// DataTable with the exact columns/types expected by the repository code.

using System.Data;

namespace TifoXRCoreWebAPI.Tests.TestDoubles.Schemas
{
    public static class RepositorySchemas
    {
        /// <summary>
        /// Schema for the SELECT used by TeleportTableRepository.GetTeleportTableBySpaceAsync.
        /// </summary>
        public static DataTable CreateTeleportSelectSchema()
        {
            var t = new DataTable();

            // Table fields
            t.Columns.Add("table_id", typeof(int));
            t.Columns.Add("space_id", typeof(int));
            t.Columns.Add("is_active", typeof(bool));
            t.Columns.Add("table_name_key", typeof(string));
            t.Columns.Add("table_locale_id", typeof(string));
            t.Columns.Add("table_localized_value", typeof(string));

            // Button fields
            t.Columns.Add("button_id", typeof(int));
            t.Columns.Add("button_name_key", typeof(string));
            t.Columns.Add("button_is_active", typeof(bool));

            // Map spot fields
            t.Columns.Add("map_spot_id", typeof(int));
            t.Columns.Add("map_spot_x", typeof(decimal));
            t.Columns.Add("map_spot_y", typeof(decimal));
            t.Columns.Add("map_spot_z", typeof(decimal));

            // Button i18n fields
            t.Columns.Add("button_locale_id", typeof(string));
            t.Columns.Add("button_localized_value", typeof(string));

            return t;
        }
    }
}
