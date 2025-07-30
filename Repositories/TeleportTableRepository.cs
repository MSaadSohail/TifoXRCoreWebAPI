// <copyright file="TeleportRepository.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Saad Sohail</author>
// <date>07/23/2025</date>
// <summary>Class to handle teleport tables SQL side</summary>

using GMS.TifoXRCoreWebAPI.Models;
using GMS.TifoXRCoreWebAPI.Models.Common;
using GMS.TifoXRCoreWebAPI.Repositories.Interfaces;
using GMS.TifoXRCoreWebAPI.Utilities;
using MySqlConnector;
using System.Data;

namespace GMS.TifoXRCoreWebAPI.Repositories
{
    public class TeleportTableRepository(IConfiguration configuration) : ITeleportTableRepository
    {
        private readonly string _connStr = configuration.GetConnectionString("DefaultConnection");

        #region GET

        /// <summary>
        /// Retrieves the teleport table for the specified space, including all its buttons and localizations.
        /// </summary>
        /// <param name="spaceId">The space ID to fetch the teleport table for.</param>
        /// <returns>The teleport table with button and localization data, or null if not found.</returns>
        public async Task<TeleportTableData?> GetTeleportTableBySpaceAsync(int spaceId)
        {
            AppLogger.Info("GetTeleportTableBySpaceAsync called!");

            const string sql = @"
                SELECT
                    t.id AS table_id,
                    t.space_id,
                    t.is_active,
                    t.name_key AS table_name_key,
                    i.locale_id AS table_locale_id,
                    i.value AS table_localized_value,
                
                    b.id AS button_id,
                    b.name_key AS button_name_key,
                    b.is_active AS button_is_active,
                    b.map_spot_id,
                
                    ms.x AS map_spot_x,
                    ms.y AS map_spot_y,
                    ms.z AS map_spot_z,
                
                    bi.locale_id AS button_locale_id,
                    bi.value AS button_localized_value
                
                FROM teleport_table t
                LEFT JOIN i18n i ON i.`key` = t.name_key AND i.space_id = t.space_id
                LEFT JOIN teleport_table_button b ON b.table_id = t.id
                LEFT JOIN map_spot ms ON ms.id = b.map_spot_id
                LEFT JOIN i18n bi ON bi.`key` = b.name_key AND bi.space_id = t.space_id
                WHERE t.space_id = @SpaceId
                ORDER BY b.id, bi.locale_id;
            ";

            await using var conn = new MySqlConnection(_connStr);

            await conn.OpenAsync();

            TeleportTableData? table = null;
            Dictionary<int, ButtonData> buttonMap = new();

            // Track LocalizedName values for table and each button
            List<LocalizedValue> tableLocals = new();
            Dictionary<int, List<LocalizedValue>> buttonLocals = new();

            await using var cmd = new MySqlCommand(sql, conn);

            cmd.Parameters.AddWithValue("@SpaceId", spaceId);

            await using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                table ??= new TeleportTableData
                {
                    Id = reader.GetInt32("table_id"),
                    SpaceId = reader.GetInt32("space_id"),
                    IsActive = reader.GetBoolean("is_active"),
                    NameKey = reader.GetString("table_name_key"),
                    LocalizedPair = new LocalizedPair
                    {
                        Key = reader.GetString("table_name_key"),
                        Values = tableLocals
                    },
                    Buttons = []
                };

                // Table i18n
                if (!reader.IsDBNull("table_locale_id") && !reader.IsDBNull("table_localized_value"))
                {
                    var localeId = reader.GetString("table_locale_id");
                    var value = reader.GetString("table_localized_value");

                    if (!tableLocals.Any(v => v.LocaleId == localeId))
                        tableLocals.Add(new LocalizedValue { LocaleId = localeId, Value = value });
                }

                // Button (may be null if table has no buttons)
                if (!reader.IsDBNull("button_id"))
                {
                    var buttonId = reader.GetInt32("button_id");

                    if (!buttonMap.TryGetValue(buttonId, out var btn))
                    {
                        btn = new ButtonData
                        {
                            Id = buttonId,
                            NameKey = reader.GetString("button_name_key"),
                            IsActive = reader.GetBoolean("button_is_active"),
                            LocalizedPair = new LocalizedPair   
                            {
                                Key = reader.GetString("button_name_key"),
                                Values = []
                            },
                            MapSpot = reader.IsDBNull("map_spot_id") ? null : new MapSpotData
                            {
                                Id = reader.GetInt32("map_spot_id"),
                                X = reader.IsDBNull("map_spot_x") ? 0 : reader.GetDecimal("map_spot_x"),
                                Y = reader.IsDBNull("map_spot_y") ? 0 : reader.GetDecimal("map_spot_y"),
                                Z = reader.IsDBNull("map_spot_z") ? 0 : reader.GetDecimal("map_spot_z")
                            }
                        };

                        buttonMap[buttonId] = btn;
                        buttonLocals[buttonId] = btn.LocalizedPair.Values;
                    }

                    // Button i18n
                    if (!reader.IsDBNull("button_locale_id") &&
                        !reader.IsDBNull("button_localized_value"))
                    {
                        var localeId = reader.GetString("button_locale_id");
                        var value = reader.GetString("button_localized_value");
                        var values = buttonLocals[buttonId];

                        if (!values.Any(v => v.LocaleId == localeId))
                            values.Add(new LocalizedValue { LocaleId = localeId, Value = value });
                    }
                }
            }

            if (table != null)
                table.Buttons = buttonMap.Values.ToList();

            return table;
        }

        #endregion

        #region POST

        /// <summary>
        /// Creates a new teleport table in the given space, including its localized name and any initial buttons.
        /// </summary>
        /// <param name="spaceId">The space in which to create the table.</param>
        /// <param name="dto">The creation DTO containing table data and initial buttons.</param>
        /// <returns>The created teleport table, including all created buttons and localizations.</returns>
        public async Task<TeleportTableData> CreateTeleportTableAsync(int spaceId, TeleportTableCreateDto dto)
        {
            await using var conn = new MySqlConnection(_connStr);
            await conn.OpenAsync();
            await using var tx = await conn.BeginTransactionAsync();

            try
            {
                // 1. Insert teleport_table row
                const string insTable = @"
                    INSERT INTO teleport_table (space_id, name_key, is_active)
                    VALUES (@SpaceId, @NameKey, @IsActive);
                ";

                int tableId;

                await using (var cmd = new MySqlCommand(insTable, conn, tx))
                {
                    cmd.Parameters.AddWithValue("@SpaceId", spaceId);
                    cmd.Parameters.AddWithValue("@NameKey", dto.NameKey);
                    cmd.Parameters.AddWithValue("@IsActive", dto.IsActive);

                    await cmd.ExecuteNonQueryAsync();

                    tableId = Convert.ToInt32(cmd.LastInsertedId);
                }

                // 2. Insert table name localizations
                if (dto.LocalizedPair?.Values != null && !string.IsNullOrEmpty(dto.LocalizedPair.Key))
                {
                    foreach (var loc in dto.LocalizedPair.Values)
                    {
                        const string insI18n = @"
                            INSERT INTO i18n (`key`, locale_id, value, space_id)
                            VALUES (@Key, @LocaleId, @Value, @SpaceId);";

                        await using var cmdI18n = new MySqlCommand(insI18n, conn, tx);

                        cmdI18n.Parameters.AddWithValue("@Key", dto.LocalizedPair.Key);
                        cmdI18n.Parameters.AddWithValue("@LocaleId", loc.LocaleId);
                        cmdI18n.Parameters.AddWithValue("@Value", loc.Value);
                        cmdI18n.Parameters.AddWithValue("@SpaceId", spaceId);

                        await cmdI18n.ExecuteNonQueryAsync();
                    }
                }

                // 3. Insert any provided buttons
                var createdButtons = new List<ButtonData>();

                if (dto.Buttons != null)
                {
                    foreach (var btnDto in dto.Buttons)
                    {
                        var button = await CreateTeleportTableButtonAsync(spaceId, tableId, btnDto, conn, tx);
                        createdButtons.Add(button);
                    }
                }

                await tx.CommitAsync();

                // 4. Return result
                return new TeleportTableData
                {
                    Id = tableId,
                    SpaceId = spaceId,
                    NameKey = dto.NameKey,
                    IsActive = dto.IsActive,
                    LocalizedPair = dto.LocalizedPair,
                    Buttons = createdButtons
                };
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        #endregion

        #region PUT

        /// <summary>
        /// Updates an existing teleport table and its buttons, including all localizations.
        /// </summary>
        /// <param name="spaceId">The space ID containing the table.</param>
        /// <param name="tableId">The table ID to update.</param>
        /// <param name="dto">The DTO with new values and button data.</param>
        /// <returns>The updated teleport table, or null if not found.</returns>
        public async Task<TeleportTableData?> UpdateTeleportTableAsync(
            int spaceId,
            int tableId,
            TeleportTableUpdateDto dto
        )
        {
            await using var conn = new MySqlConnection(_connStr);

            await conn.OpenAsync();

            await using var tx = await conn.BeginTransactionAsync();

            try
            {
                // --- Load supported locales for this space ---
                var supportedLocales = new HashSet<string>();
                const string fetchLocales = @"
                    SELECT locale_id
                    FROM supported_languages
                    WHERE space_id = @SpaceId;
                ";

                await using (var localeCmd = new MySqlCommand(fetchLocales, conn, tx))
                {
                    localeCmd.Parameters.AddWithValue("@SpaceId", spaceId);

                    await using var rdr = await localeCmd.ExecuteReaderAsync();

                    while (await rdr.ReadAsync())
                        supportedLocales.Add(rdr.GetString("locale_id"));
                }

                // --- Update main teleport table ---
                const string updTable = @"
                    UPDATE teleport_table
                       SET is_active = @IsActive,
                           name_key  = @NameKey
                     WHERE id = @TableId
                       AND space_id = @SpaceId;
                ";

                int affected;

                await using (var cmd = new MySqlCommand(updTable, conn, tx))
                {
                    cmd.Parameters.AddWithValue("@IsActive", dto.IsActive);
                    cmd.Parameters.AddWithValue("@NameKey", dto.NameKey);
                    cmd.Parameters.AddWithValue("@TableId", tableId);
                    cmd.Parameters.AddWithValue("@SpaceId", spaceId);

                    affected = await cmd.ExecuteNonQueryAsync();
                }

                // --- MySQL: 0 affected rows if values unchanged ---
                if (affected == 0)
                {
                    // Check if row exists
                    const string checkSql = @"
                        SELECT COUNT(*) 
                        FROM teleport_table 
                        WHERE id = @TableId 
                        AND space_id = @SpaceId;
                    ";


                    await using var checkCmd = new MySqlCommand(checkSql, conn, tx);

                    checkCmd.Parameters.AddWithValue("@TableId", tableId);
                    checkCmd.Parameters.AddWithValue("@SpaceId", spaceId);

                    var exists = Convert.ToInt32(await checkCmd.ExecuteScalarAsync()) > 0;

                    if (!exists)
                    {
                        await tx.RollbackAsync();
                        return null; // Real 404 (table does not exist)
                    }
                    // Else: the row exists, but no changes (proceed)
                }

                // --- Upsert table name localizations (using LocalizedName.Values) ---
                const string updI18n = @"
                    UPDATE i18n
                       SET value = @Value
                     WHERE `key`     = @NameKey
                       AND locale_id = @LocaleId
                       AND space_id  = @SpaceId;
                ";

                const string insI18n = @"
                    INSERT INTO i18n (`key`, locale_id, value, space_id)
                    VALUES (@NameKey, @LocaleId, @Value, @SpaceId);
                ";

                if (dto.LocalizedPair != null && dto.LocalizedPair.Values != null)
                {
                    foreach (var loc in dto.LocalizedPair.Values)
                    {
                        if (!supportedLocales.Contains(loc.LocaleId))
                            continue;

                        await using var cu = new MySqlCommand(updI18n, conn, tx);

                        cu.Parameters.AddWithValue("@NameKey", dto.NameKey);
                        cu.Parameters.AddWithValue("@LocaleId", loc.LocaleId);
                        cu.Parameters.AddWithValue("@Value", loc.Value);
                        cu.Parameters.AddWithValue("@SpaceId", spaceId);

                        if (await cu.ExecuteNonQueryAsync() == 0)
                        {
                            await using var ci = new MySqlCommand(insI18n, conn, tx);

                            ci.Parameters.AddWithValue("@NameKey", dto.NameKey);
                            ci.Parameters.AddWithValue("@LocaleId", loc.LocaleId);
                            ci.Parameters.AddWithValue("@Value", loc.Value);
                            ci.Parameters.AddWithValue("@SpaceId", spaceId);

                            await ci.ExecuteNonQueryAsync();
                        }
                    }
                }

                // --- Handle buttons (insert/update) and update map_spot coords ---
                if (dto.Buttons != null && dto.Buttons.Count > 0)
                {
                    foreach (var btn in dto.Buttons)
                    {
                        int btnId;
                        if (btn.Id.HasValue)
                        {
                            // Update button record
                            const string updBtn = @"
                                UPDATE teleport_table_button
                                   SET name_key = @NameKey,
                                       map_spot_id = @MapSpotId,
                                       is_active = @IsActive
                                 WHERE id = @BtnId
                                   AND table_id = @TableId;";

                            await using var cmdU = new MySqlCommand(updBtn, conn, tx);

                            cmdU.Parameters.AddWithValue("@NameKey", btn.NameKey);
                            cmdU.Parameters.AddWithValue("@MapSpotId", btn.MapSpot.Id);
                            cmdU.Parameters.AddWithValue("@IsActive", btn.IsActive);
                            cmdU.Parameters.AddWithValue("@BtnId", btn.Id.Value);
                            cmdU.Parameters.AddWithValue("@TableId", tableId);

                            await cmdU.ExecuteNonQueryAsync();

                            btnId = btn.Id.Value;
                        }
                        else
                        {
                            // Insert new button record
                            const string insBtn = @"
                                INSERT INTO teleport_table_button (table_id, name_key, map_spot_id, is_active)
                                VALUES (@TableId, @NameKey, @MapSpotId, @IsActive);";

                            await using var cmdI = new MySqlCommand(insBtn, conn, tx);

                            cmdI.Parameters.AddWithValue("@TableId", tableId);
                            cmdI.Parameters.AddWithValue("@NameKey", btn.NameKey);
                            cmdI.Parameters.AddWithValue("@MapSpotId", btn.MapSpot.Id);
                            cmdI.Parameters.AddWithValue("@IsActive", btn.IsActive);

                            await cmdI.ExecuteNonQueryAsync();

                            btnId = (int)cmdI.LastInsertedId;
                        }

                        // --- Upsert map spot coordinates (x, y, z) ---
                        if (btn.MapSpot != null)
                        {
                            const string updMapSpot = @"
                                UPDATE map_spot
                                   SET x = @X, y = @Y, z = @Z
                                 WHERE id = @MapSpotId;";

                            await using var mapCmd = new MySqlCommand(updMapSpot, conn, tx);

                            mapCmd.Parameters.AddWithValue("@X", btn.MapSpot.X);
                            mapCmd.Parameters.AddWithValue("@Y", btn.MapSpot.Y);
                            mapCmd.Parameters.AddWithValue("@Z", btn.MapSpot.Z);
                            mapCmd.Parameters.AddWithValue("@MapSpotId", btn.MapSpot.Id);

                            await mapCmd.ExecuteNonQueryAsync();
                        }

                        // --- Upsert localizations for this button ---
                        if (btn.LocalizedPair != null && btn.LocalizedPair.Values != null)
                        {
                            foreach (var loc in btn.LocalizedPair.Values)
                            {
                                if (!supportedLocales.Contains(loc.LocaleId))
                                    continue;

                                await using var cu = new MySqlCommand(updI18n, conn, tx);

                                cu.Parameters.AddWithValue("@NameKey", btn.NameKey);
                                cu.Parameters.AddWithValue("@LocaleId", loc.LocaleId);
                                cu.Parameters.AddWithValue("@Value", loc.Value);
                                cu.Parameters.AddWithValue("@SpaceId", spaceId);

                                if (await cu.ExecuteNonQueryAsync() == 0)
                                {
                                    await using var ci = new MySqlCommand(insI18n, conn, tx);

                                    ci.Parameters.AddWithValue("@NameKey", btn.NameKey);
                                    ci.Parameters.AddWithValue("@LocaleId", loc.LocaleId);
                                    ci.Parameters.AddWithValue("@Value", loc.Value);
                                    ci.Parameters.AddWithValue("@SpaceId", spaceId);

                                    await ci.ExecuteNonQueryAsync();
                                }
                            }
                        }
                    }
                }

                await tx.CommitAsync();
                return await LoadTeleportTableById(conn, spaceId, tableId);
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        #endregion

        #region DELETE

        /// <summary>
        /// Deletes a teleport table and all associated buttons and localizations, within a transaction.
        /// </summary>
        /// <param name="spaceId">The space containing the table.</param>
        /// <param name="tableId">The table ID to delete.</param>
        /// <returns>True if deleted, false if not found.</returns>
        public async Task<bool> DeleteTeleportTableAsync(int spaceId, int tableId)
        {
            await using var conn = new MySqlConnection(_connStr);

            await conn.OpenAsync();

            await using var tx = await conn.BeginTransactionAsync();

            try
            {
                // 1. Get the name_key for the teleport_table (for i18n deletion)
                string? nameKey = null;
                const string getNameKey = "SELECT name_key FROM teleport_table WHERE id=@TableId AND space_id=@SpaceId;";

                await using (var cmd = new MySqlCommand(getNameKey, conn, tx))
                {
                    cmd.Parameters.AddWithValue("@TableId", tableId);
                    cmd.Parameters.AddWithValue("@SpaceId", spaceId);

                    var result = await cmd.ExecuteScalarAsync();

                    nameKey = result == null || result == DBNull.Value ? null : result.ToString();
                }

                // 2. Get all button name_keys for i18n deletion
                var buttonKeys = new List<string>();

                const string getButtonKeys = "SELECT name_key FROM teleport_table_button WHERE table_id=@TableId;";

                await using (var cmd = new MySqlCommand(getButtonKeys, conn, tx))
                {
                    cmd.Parameters.AddWithValue("@TableId", tableId);

                    await using var reader = await cmd.ExecuteReaderAsync();

                    while (await reader.ReadAsync())
                        buttonKeys.Add(reader.GetString("name_key"));
                }

                // 3. Delete button i18n
                if (buttonKeys.Count > 0)
                {
                    var inClause = string.Join(",", buttonKeys.Select((k, i) => $"@BtnKey{i}"));
                    var delBtnI18n = $"DELETE FROM i18n WHERE `key` IN ({inClause}) AND space_id=@SpaceId;";

                    await using var delBtnI18nCmd = new MySqlCommand(delBtnI18n, conn, tx);

                    for (int i = 0; i < buttonKeys.Count; i++)
                        delBtnI18nCmd.Parameters.AddWithValue($"@BtnKey{i}", buttonKeys[i]);

                    delBtnI18nCmd.Parameters.AddWithValue("@SpaceId", spaceId);

                    await delBtnI18nCmd.ExecuteNonQueryAsync();
                }

                // 4. Delete table i18n
                if (!string.IsNullOrEmpty(nameKey))
                {
                    const string delTableI18n = "DELETE FROM i18n WHERE `key`=@NameKey AND space_id=@SpaceId;";

                    await using var delTableI18nCmd = new MySqlCommand(delTableI18n, conn, tx);

                    delTableI18nCmd.Parameters.AddWithValue("@NameKey", nameKey);
                    delTableI18nCmd.Parameters.AddWithValue("@SpaceId", spaceId);

                    await delTableI18nCmd.ExecuteNonQueryAsync();
                }

                // 5. Delete all buttons
                const string delBtns = "DELETE FROM teleport_table_button WHERE table_id=@TableId;";

                await using (var cmd = new MySqlCommand(delBtns, conn, tx))
                {
                    cmd.Parameters.AddWithValue("@TableId", tableId);

                    await cmd.ExecuteNonQueryAsync();
                }

                // 6. Delete the table itself
                const string delTable = "DELETE FROM teleport_table WHERE id=@TableId AND space_id=@SpaceId;";

                await using (var cmd = new MySqlCommand(delTable, conn, tx))
                {
                    cmd.Parameters.AddWithValue("@TableId", tableId);
                    cmd.Parameters.AddWithValue("@SpaceId", spaceId);

                    var affected = await cmd.ExecuteNonQueryAsync();

                    await tx.CommitAsync();

                    return affected > 0;
                }
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        /// <summary>
        /// Deletes all teleport tables in a given space, including all related data.
        /// </summary>
        /// <param name="spaceId">The space whose tables are to be deleted.</param>
        /// <returns>The number of tables deleted.</returns>
        public async Task<int> DeleteTeleportTablesBySpaceAsync(int spaceId)
        {
            var tableIds = new List<int>();

            await using var conn = new MySqlConnection(_connStr);

            await conn.OpenAsync();

            const string getIds = "SELECT id FROM teleport_table WHERE space_id=@SpaceId;";

            await using (var cmd = new MySqlCommand(getIds, conn))
            {
                cmd.Parameters.AddWithValue("@SpaceId", spaceId);
                await using var rdr = await cmd.ExecuteReaderAsync();
                while (await rdr.ReadAsync())
                    tableIds.Add(rdr.GetInt32("id"));
            }

            int count = 0;

            foreach (var tableId in tableIds)
            {
                if (await DeleteTeleportTableAsync(spaceId, tableId))
                    count++;
            }

            return count;
        }

        /// <summary>
        /// Deletes a single button from a teleport table, including its i18n localizations.
        /// </summary>
        /// <param name="buttonId">The button ID to delete.</param>
        /// <param nameId="tableId">The table ID the button belongs to.</param>
        /// <returns>True if deleted, false otherwise.</returns>
        public async Task<bool> DeleteTeleportTableButtonAsync(int buttonId, int tableId)
        {
            await using var conn = new MySqlConnection(_connStr);

            await conn.OpenAsync();

            await using var tx = await conn.BeginTransactionAsync();

            try
            {
                // 1. Get the button name_key for i18n deletion
                string? btnKey = null;
                const string getKey = "SELECT name_key FROM teleport_table_button WHERE id=@ButtonId AND table_id=@TableId;";

                await using (var cmd = new MySqlCommand(getKey, conn, tx))
                {
                    cmd.Parameters.AddWithValue("@ButtonId", buttonId);
                    cmd.Parameters.AddWithValue("@TableId", tableId);

                    var result = await cmd.ExecuteScalarAsync();

                    btnKey = result == null || result == DBNull.Value ? null : result.ToString();
                }

                // 2. Delete i18n for the button
                if (!string.IsNullOrEmpty(btnKey))
                {
                    const string delBtnI18n = "DELETE FROM i18n WHERE `key`=@BtnKey;";

                    await using var delBtnI18nCmd = new MySqlCommand(delBtnI18n, conn, tx);

                    delBtnI18nCmd.Parameters.AddWithValue("@BtnKey", btnKey);

                    await delBtnI18nCmd.ExecuteNonQueryAsync();
                }

                // 3. Delete the button itself
                const string delBtn = "DELETE FROM teleport_table_button WHERE id=@ButtonId AND table_id=@TableId;";

                await using (var cmd = new MySqlCommand(delBtn, conn, tx))
                {
                    cmd.Parameters.AddWithValue("@ButtonId", buttonId);
                    cmd.Parameters.AddWithValue("@TableId", tableId);

                    var affected = await cmd.ExecuteNonQueryAsync();

                    await tx.CommitAsync();

                    return affected > 0;
                }
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        #endregion

        #region HELPER_METHODS

        /// <summary>
        /// Loads a teleport table by ID, including all buttons and localizations.
        /// Used internally after create/update for fetching latest DB state.
        /// </summary>
        /// <param name="conn">An open MySQL connection.</param>
        /// <param name="spaceId">The space ID for the table.</param>
        /// <param name="tableId">The table ID to fetch.</param>
        /// <returns>The loaded teleport table with full button and localization info, or null if not found.</returns>
        private static async Task<TeleportTableData?> LoadTeleportTableById(
            MySqlConnection conn,
            int spaceId,
            int tableId
        )
        {
            // --- 1. Load table + localizations ---
            const string tableSql = @"
                SELECT
                    t.id,
                    t.space_id,
                    t.is_active,
                    t.name_key,
                    i.locale_id,
                    i.value
                FROM teleport_table AS t
                LEFT JOIN i18n AS i
                  ON i.`key`    = t.name_key
                 AND i.space_id = t.space_id
                WHERE t.id = @TableId
                  AND t.space_id = @SpaceId
                ORDER BY i.locale_id;
            ";

            TeleportTableData? table = null;
            var localizedName = new Dictionary<string, string>();

            await using (var cmd = new MySqlCommand(tableSql, conn))
            {
                cmd.Parameters.AddWithValue("@TableId", tableId);
                cmd.Parameters.AddWithValue("@SpaceId", spaceId);

                await using var reader = await cmd.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    table ??= new TeleportTableData
                    {
                        Id = reader.GetInt32("id"),
                        SpaceId = reader.GetInt32("space_id"),
                        NameKey = reader.IsDBNull("name_key") ? null :
                        reader.GetString("name_key"),

                        IsActive = reader.GetBoolean("is_active"),

                        LocalizedPair = new LocalizedPair
                        {
                            Key = reader.IsDBNull("name_key") ? "" : reader.GetString("name_key"),
                            Values = []
                        },
                        Buttons = []
                    };

                    if (!reader.IsDBNull("locale_id") && !reader.IsDBNull("value"))
                    {
                        table.LocalizedPair.Values.Add(new LocalizedValue
                        {
                            LocaleId = reader.GetString("locale_id"),
                            Value = reader.GetString("value")
                        });
                    }
                }
            }

            if (table == null)
                return null;

            // --- 2. Load buttons with their localizations ---
            const string btnSql = @"
                SELECT
                    b.id,
                    b.name_key AS button_name_key,
                    b.is_active,

                    b.map_spot_id,
                    ms.x AS map_spot_x,
                    ms.y AS map_spot_y,
                    ms.z AS map_spot_z,

                    i.locale_id,
                    i.value
                FROM teleport_table_button AS b
                LEFT JOIN map_spot AS ms ON ms.id = b.map_spot_id
                LEFT JOIN i18n AS i ON i.`key` = b.name_key AND i.space_id = @SpaceId
                WHERE b.table_id = @TableId
                ORDER BY b.id, i.locale_id;
            ";

            var btnMap = new Dictionary<int, ButtonData>();

            await using (var btnCmd = new MySqlCommand(btnSql, conn))
            {
                btnCmd.Parameters.AddWithValue("@SpaceId", spaceId);
                btnCmd.Parameters.AddWithValue("@TableId", tableId);

                await using var btnReader = await btnCmd.ExecuteReaderAsync();

                while (await btnReader.ReadAsync())
                {
                    var btnId = btnReader.GetInt32("id");

                    if (!btnMap.TryGetValue(btnId, out var btn))
                    {
                        btn = new ButtonData
                        {
                            Id = btnId,
                            NameKey = btnReader.IsDBNull("button_name_key") ? "" :
                            btnReader.GetString("button_name_key"),
                            IsActive = btnReader.GetBoolean("is_active"),
                            LocalizedPair = new LocalizedPair
                            {
                                Key = btnReader.IsDBNull("button_name_key") ? "" :
                                btnReader.GetString("button_name_key"),
                                Values = []
                            },

                            MapSpot = btnReader.IsDBNull("map_spot_id") ? null : new MapSpotData
                            {
                                Id = btnReader.GetInt32("map_spot_id"),
                                X = btnReader.IsDBNull("map_spot_x") ? 0 : btnReader.GetDecimal("map_spot_x"),
                                Y = btnReader.IsDBNull("map_spot_y") ? 0 : btnReader.GetDecimal("map_spot_y"),
                                Z = btnReader.IsDBNull("map_spot_z") ? 0 : btnReader.GetDecimal("map_spot_z")
                            }
                        };
                        btnMap[btnId] = btn;
                    }

                    // Add the localized value if present and not already added
                    if (!btnReader.IsDBNull("locale_id") && !btnReader.IsDBNull("value"))
                    {
                        string localeId = btnReader.GetString("locale_id");
                        string value = btnReader.GetString("value");

                        // Avoid duplicates if your SQL returns multiple rows per locale (e.g., due to joins)
                        if (!btn.LocalizedPair.Values.Any(lv => lv.LocaleId == localeId))
                        {
                            btn.LocalizedPair.Values.Add(new LocalizedValue
                            {
                                LocaleId = localeId,
                                Value = value
                            });
                        }
                    }
                }
            }

            table.Buttons = [.. btnMap.Values];
            return table;
        }

        /// <summary>
        /// Creates a new button for a teleport table, including its map spot and localized name.
        /// Used internally during table creation or for standalone button creation.
        /// </summary>
        /// <param name="spaceId">The space ID for the button.</param>
        /// <param name="tableId">The teleport table ID to attach the button to.</param>
        /// <param name="btnDto">The button creation DTO.</param>
        /// <param name="externalConn">Optional open MySQL connection for transactional use.</param>
        /// <param name="externalTx">Optional open MySQL transaction for transactional use.</param>
        /// <returns>The created button, including its map spot and localizations.</returns>
        public async Task<ButtonData> CreateTeleportTableButtonAsync(
            int spaceId,
            int tableId,
            ButtonCreateDto btnDto,
            MySqlConnection? externalConn = null,
            MySqlTransaction? externalTx = null
        )
        {
            bool useExternal = externalConn != null && externalTx != null;

            await using var conn = useExternal ? null : new MySqlConnection(_connStr);

            if (!useExternal)
                await conn.OpenAsync();
            var tx = useExternal ? externalTx : await conn!.BeginTransactionAsync();

            try
            {
                // 1. Insert map_spot
                int mapSpotId;

                if (btnDto.MapSpot.Id > 0)
                {
                    mapSpotId = btnDto.MapSpot.Id;
                }
                else
                {
                    const string insMapSpot = @"
                        INSERT INTO map_spot (x, y, z)
                        VALUES (@X, @Y, @Z);";

                    await using var cmdMap = new MySqlCommand(insMapSpot, useExternal ? externalConn : conn, tx);

                    cmdMap.Parameters.AddWithValue("@X", btnDto.MapSpot.X);
                    cmdMap.Parameters.AddWithValue("@Y", btnDto.MapSpot.Y);
                    cmdMap.Parameters.AddWithValue("@Z", btnDto.MapSpot.Z);

                    await cmdMap.ExecuteNonQueryAsync();

                    mapSpotId = Convert.ToInt32(cmdMap.LastInsertedId);
                }

                // 2. Insert button
                const string insBtn = @"
                    INSERT INTO teleport_table_button (table_id, name_key, map_spot_id, is_active)
                    VALUES (@TableId, @NameKey, @MapSpotId, @IsActive);";

                int buttonId;

                await using (var cmdBtn = new MySqlCommand(insBtn, useExternal ? externalConn : conn, tx))
                {
                    cmdBtn.Parameters.AddWithValue("@TableId", tableId);
                    cmdBtn.Parameters.AddWithValue("@NameKey", btnDto.NameKey);
                    cmdBtn.Parameters.AddWithValue("@MapSpotId", mapSpotId);
                    cmdBtn.Parameters.AddWithValue("@IsActive", btnDto.IsActive);

                    await cmdBtn.ExecuteNonQueryAsync();

                    buttonId = Convert.ToInt32(cmdBtn.LastInsertedId);
                }

                // 3. Insert i18n for button name
                if (btnDto.LocalizedPair?.Values != null && !string.IsNullOrEmpty(btnDto.LocalizedPair.Key))
                {
                    foreach (var loc in btnDto.LocalizedPair.Values)
                    {
                        const string insI18n = @"
                            INSERT INTO i18n (`key`, locale_id, value, space_id)
                            VALUES (@Key, @LocaleId, @Value, @SpaceId);";

                        await using var cmdI18n = new MySqlCommand(insI18n, useExternal ? externalConn : conn, tx);

                        cmdI18n.Parameters.AddWithValue("@Key", btnDto.LocalizedPair.Key);
                        cmdI18n.Parameters.AddWithValue("@LocaleId", loc.LocaleId);
                        cmdI18n.Parameters.AddWithValue("@Value", loc.Value);
                        cmdI18n.Parameters.AddWithValue("@SpaceId", spaceId);

                        await cmdI18n.ExecuteNonQueryAsync();
                    }
                }

                if (!useExternal)
                    await tx!.CommitAsync();

                return new ButtonData
                {
                    Id = buttonId,
                    NameKey = btnDto.NameKey,
                    IsActive = btnDto.IsActive,
                    LocalizedPair = btnDto.LocalizedPair,
                    MapSpot = new MapSpotData
                    {
                        Id = mapSpotId,
                        X = btnDto.MapSpot.X,
                        Y = btnDto.MapSpot.Y,
                        Z = btnDto.MapSpot.Z
                    }
                };
            }
            catch
            {
                if (!useExternal && tx != null)
                    await tx.RollbackAsync();
                throw;
            }
        }
        #endregion
    }
}
