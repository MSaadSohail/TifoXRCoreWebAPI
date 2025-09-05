// © 2025 Global Mobile Software LLC. All rights reserved.
// Author: Urvashi Dhingra
// Date: 08/19/2025
// Summary:
// Generic row builder for ADO.NET DataTables used in repository tests.
// Includes a helper preset for the teleport SELECT shape (used by GET).
//
// IMPORTANT: The class name intentionally conflicts with System.Data.DataRowBuilder,
// so tests should alias this type:
//   using TestDataRowBuilder = TifoXRCoreWebAPI.Tests.TestDoubles.Builders.DataRowBuilder;

using System.Data;

namespace GMS.TifoXRCoreWebAPI.Tests.TestDoubles.Builders
{
    /// <summary>
    /// Fluent builder for adding a single row to a <see cref="DataTable"/>.
    /// </summary>
    public sealed class DataRowBuilder
    {
        private readonly Dictionary<string, object?> _values = new(StringComparer.OrdinalIgnoreCase);

        public static DataRowBuilder Row() => new DataRowBuilder();

        /// <summary>Pre-seeded builder for the teleport SELECT shape used by GET.</summary>
        public static DataRowBuilder TeleportSelectRow()
            => new DataRowBuilder()
                .With("table_id", 1)
                .With("space_id", 123)
                .With("is_active", true)
                .With("table_name_key", "teleport.table");

        // ---------------- Generic fluent API ----------------

        public DataRowBuilder With(string columnName, object? value)
        {
            _values[columnName] = value ?? DBNull.Value;
            return this;
        }

        /// <summary>Adds the built row to the table. Missing columns default to DBNull.</summary>
        public void AddTo(DataTable table)
        {
            var row = table.NewRow();

            // Fill provided values; leave everything else as DBNull
            foreach (DataColumn col in table.Columns)
            {
                if (_values.TryGetValue(col.ColumnName, out var val))
                    row[col.ColumnName] = val ?? DBNull.Value;
                else
                    row[col.ColumnName] = DBNull.Value;
            }

            table.Rows.Add(row);
        }

        // ----------- Teleport-specific convenience methods -----------

        public DataRowBuilder WithTeleportTableDefaults(
            int tableId = 1,
            int spaceId = 123,
            bool isActive = true,
            string tableNameKey = "teleport.table")
            => With("table_id", tableId)
              .With("space_id", spaceId)
              .With("is_active", isActive)
              .With("table_name_key", tableNameKey);

        public DataRowBuilder WithTableLocale(string? localeId, string? value)
            => With("table_locale_id", (object?)localeId ?? DBNull.Value)
             .With("table_localized_value", (object?)value ?? DBNull.Value);

        public DataRowBuilder WithButton(int id, string nameKey, bool isActive)
            => With("button_id", id)
             .With("button_name_key", nameKey)
             .With("button_is_active", isActive);

        public DataRowBuilder WithButtonLocale(string? localeId, string? value)
            => With("button_locale_id", (object?)localeId ?? DBNull.Value)
             .With("button_localized_value", (object?)value ?? DBNull.Value);

        public DataRowBuilder WithMapSpot(int id, decimal x, decimal y, decimal z)
            => With("map_spot_id", id)
             .With("map_spot_x", x)
             .With("map_spot_y", y)
             .With("map_spot_z", z);
    }
}
