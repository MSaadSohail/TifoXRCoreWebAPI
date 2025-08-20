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
using System.Data.Common;
using TifoXRCoreWebAPI.Utilities.Infrastructure.Interface;

namespace GMS.TifoXRCoreWebAPI.Repositories
{
    public class TeleportTableRepository(IConfiguration configuration, IDbProvider db) : ITeleportTableRepository
    {
        private readonly IDbProvider _db = db;

        private readonly string _connStr = configuration.GetConnectionString("DefaultConnection");

        #region GET

        /// <summary>
        /// Retrieves the teleport table for the specified space, including all its buttons and localizations.
        /// </summary>
        /// <param name="spaceId">The space ID to fetch the teleport table for.</param>
        /// <returns>The teleport table with button and localization data, or null if not found.</returns>
        public async Task<TeleportTableData> GetTeleportTableBySpaceAsync(int spaceId)
        {
            //AppLogger.Info("GetTeleportTableBySpaceAsync called!");

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

            await using var conn = await _db.OpenConnectionAsync();
            await using var cmd = _db.CreateCommand(conn, sql);

            cmd.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));

            TeleportTableData? table = null;
            var buttonMap = new Dictionary<int, ButtonData>();
            var tableLocals = new List<LocalizedValue>();
            var buttonLocals = new Dictionary<int, List<LocalizedValue>>();

            await using var reader = await cmd.ExecuteReaderAsync();

            // Cache ordinals once for speed and to allow name-based access on DbDataReader
            bool ordReady = false;
            int o_table_id = -1, o_space_id = -1, o_is_active = -1, o_table_name_key = -1, o_table_locale_id = -1, o_table_localized_value = -1;
            int o_button_id = -1, o_button_name_key = -1, o_button_is_active = -1, o_map_spot_id = -1;
            int o_map_spot_x = -1, o_map_spot_y = -1, o_map_spot_z = -1;
            int o_button_locale_id = -1, o_button_localized_value = -1;

            while (await reader.ReadAsync())
            {
                if (!ordReady)
                {
                    o_table_id = reader.GetOrdinal("table_id");
                    o_space_id = reader.GetOrdinal("space_id");
                    o_is_active = reader.GetOrdinal("is_active");
                    o_table_name_key = reader.GetOrdinal("table_name_key");
                    o_table_locale_id = reader.GetOrdinal("table_locale_id");
                    o_table_localized_value = reader.GetOrdinal("table_localized_value");

                    o_button_id = reader.GetOrdinal("button_id");
                    o_button_name_key = reader.GetOrdinal("button_name_key");
                    o_button_is_active = reader.GetOrdinal("button_is_active");
                    o_map_spot_id = reader.GetOrdinal("map_spot_id");

                    o_map_spot_x = reader.GetOrdinal("map_spot_x");
                    o_map_spot_y = reader.GetOrdinal("map_spot_y");
                    o_map_spot_z = reader.GetOrdinal("map_spot_z");

                    o_button_locale_id = reader.GetOrdinal("button_locale_id");
                    o_button_localized_value = reader.GetOrdinal("button_localized_value");

                    ordReady = true;
                }

                table ??= new TeleportTableData
                {
                    Id = reader.GetInt32(o_table_id),
                    SpaceId = reader.GetInt32(o_space_id),
                    IsActive = reader.GetBoolean(o_is_active),
                    NameKey = reader.GetString(o_table_name_key),
                    LocalizedPairs = new LocalizedPairs
                    {
                        Key = reader.GetString(o_table_name_key),
                        Values = tableLocals
                    },
                    Buttons = []
                };

                // Table i18n
                if (!reader.IsDBNull(o_table_locale_id) && !reader.IsDBNull(o_table_localized_value))
                {
                    var localeId = reader.GetString(o_table_locale_id);
                    var value = reader.GetString(o_table_localized_value);

                    if (!tableLocals.Any(v => v.LocaleId == localeId))
                        tableLocals.Add(new LocalizedValue { LocaleId = localeId, Value = value });
                }

                // Button (may be null if table has no buttons)
                if (!reader.IsDBNull(o_button_id))
                {
                    var buttonId = reader.GetInt32(o_button_id);

                    if (!buttonMap.TryGetValue(buttonId, out var btn))
                    {
                        btn = new ButtonData
                        {
                            Id = buttonId,
                            NameKey = reader.IsDBNull(o_button_name_key) ? string.Empty : reader.GetString(o_button_name_key),
                            IsActive = !reader.IsDBNull(o_button_is_active) && reader.GetBoolean(o_button_is_active),
                            LocalizedPairs = new LocalizedPairs
                            {
                                Key = reader.IsDBNull(o_button_name_key) ? string.Empty : reader.GetString(o_button_name_key),
                                Values = []
                            },
                            MapSpot = reader.IsDBNull(o_map_spot_id)
                                ? null
                                : new MapSpotData
                                {
                                    Id = reader.GetInt32(o_map_spot_id),
                                    X = reader.IsDBNull(o_map_spot_x) ? 0 : reader.GetDecimal(o_map_spot_x),
                                    Y = reader.IsDBNull(o_map_spot_y) ? 0 : reader.GetDecimal(o_map_spot_y),
                                    Z = reader.IsDBNull(o_map_spot_z) ? 0 : reader.GetDecimal(o_map_spot_z)
                                }
                        };

                        buttonMap[buttonId] = btn;
                        buttonLocals[buttonId] = btn.LocalizedPairs.Values;
                    }

                    // Button i18n
                    if (!reader.IsDBNull(o_button_locale_id) && !reader.IsDBNull(o_button_localized_value))
                    {
                        var localeId = reader.GetString(o_button_locale_id);
                        var value = reader.GetString(o_button_localized_value);
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
            if (dto == null) throw new ArgumentNullException(nameof(dto));

            await using var conn = await _db.OpenConnectionAsync();
            await using var tx = await conn.BeginTransactionAsync();

            try
            {
                // 1) Insert teleport_table row
                const string insTable = @"
            INSERT INTO teleport_table (space_id, name_key, is_active)
            VALUES (@SpaceId, @NameKey, @IsActive);";

                await using (var cmd = _db.CreateCommand(conn, insTable))
                {
                    cmd.Transaction = tx;
                    cmd.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));
                    cmd.Parameters.Add(_db.CreateParameter("@NameKey", dto.NameKey));
                    cmd.Parameters.Add(_db.CreateParameter("@IsActive", dto.IsActive));
                    await cmd.ExecuteNonQueryAsync();
                }

                // Retrieve the new table id (MySQL)
                int tableId;
                const string getLastIdSql = "SELECT LAST_INSERT_ID();";
                await using (var idCmd = _db.CreateCommand(conn, getLastIdSql))
                {
                    idCmd.Transaction = tx;
                    var obj = await idCmd.ExecuteScalarAsync();
                    tableId = Convert.ToInt32(obj);
                }

                // 2) Insert table name localizations (if provided)
                if (dto.LocalizedPairs?.Values != null && !string.IsNullOrWhiteSpace(dto.LocalizedPairs.Key))
                {
                    const string insI18n = @"
                INSERT INTO i18n (`key`, locale_id, value, space_id)
                VALUES (@Key, @LocaleId, @Value, @SpaceId);";

                    foreach (var loc in dto.LocalizedPairs.Values)
                    {
                        await using var cmdI18n = _db.CreateCommand(conn, insI18n);
                        cmdI18n.Transaction = tx;
                        cmdI18n.Parameters.Add(_db.CreateParameter("@Key", dto.LocalizedPairs.Key));
                        cmdI18n.Parameters.Add(_db.CreateParameter("@LocaleId", loc.LocaleId));
                        cmdI18n.Parameters.Add(_db.CreateParameter("@Value", loc.Value));
                        cmdI18n.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));
                        await cmdI18n.ExecuteNonQueryAsync();
                    }
                }

                // 3) Insert buttons (if any)
                var createdButtons = new List<ButtonData>();
                if (dto.Buttons != null)
                {
                    foreach (var btnDto in dto.Buttons)
                    {
                        var button = await CreateTeleportTableButtonAsync(spaceId, tableId, btnDto, conn, tx);
                        createdButtons.Add(button);
                    }
                }

                // 4) Commit
                await tx.CommitAsync();

                // 5) Return composed result
                return new TeleportTableData
                {
                    Id = tableId,
                    SpaceId = spaceId,
                    NameKey = dto.NameKey,
                    IsActive = dto.IsActive,
                    LocalizedPairs = dto.LocalizedPairs,
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
        TeleportTableUpdateDto dto)
        {
            await using var conn = await _db.OpenConnectionAsync();
            await using var tx = await conn.BeginTransactionAsync();

            try
            {
                // --- Load supported locales for this space ---
                var supportedLocales = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                const string fetchLocales = @"
            SELECT locale_id
            FROM supported_languages
            WHERE space_id = @SpaceId;";

                await using (var localeCmd = _db.CreateCommand(conn, fetchLocales))
                {
                    localeCmd.Transaction = tx;
                    localeCmd.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));

                    await using var rdr = await localeCmd.ExecuteReaderAsync();
                    while (await rdr.ReadAsync())
                    {
                        var locale = rdr.IsDBNull(0) ? null : rdr.GetString(0);
                        if (!string.IsNullOrWhiteSpace(locale)) supportedLocales.Add(locale!);
                    }
                }

                // --- Update main teleport table ---
                const string updTable = @"
            UPDATE teleport_table
               SET is_active = @IsActive,
                   name_key  = @NameKey
             WHERE id = @TableId
               AND space_id = @SpaceId;";

                int affected;
                await using (var cmd = _db.CreateCommand(conn, updTable))
                {
                    cmd.Transaction = tx;
                    cmd.Parameters.Add(_db.CreateParameter("@IsActive", dto.IsActive));
                    cmd.Parameters.Add(_db.CreateParameter("@NameKey", dto.NameKey));
                    cmd.Parameters.Add(_db.CreateParameter("@TableId", tableId));
                    cmd.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));
                    affected = await cmd.ExecuteNonQueryAsync();
                }

                // MySQL: 0 affected rows if no value changed — verify existence.
                if (affected == 0)
                {
                    const string checkSql = @"
                SELECT COUNT(*)
                  FROM teleport_table
                 WHERE id = @TableId
                   AND space_id = @SpaceId;";

                    await using var checkCmd = _db.CreateCommand(conn, checkSql);
                    checkCmd.Transaction = tx;
                    checkCmd.Parameters.Add(_db.CreateParameter("@TableId", tableId));
                    checkCmd.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));

                    var exists = Convert.ToInt32(await checkCmd.ExecuteScalarAsync()) > 0;
                    if (!exists)
                    {
                        await tx.RollbackAsync();
                        return null; // Real 404 (table does not exist)
                    }
                }

                // --- Upsert table name localizations ---
                const string updI18n = @"
            UPDATE i18n
               SET value = @Value
             WHERE `key`     = @NameKey
               AND locale_id = @LocaleId
               AND space_id  = @SpaceId;";

                const string insI18n = @"
            INSERT INTO i18n (`key`, locale_id, value, space_id)
            VALUES (@NameKey, @LocaleId, @Value, @SpaceId);";

                if (dto.LocalizedPairs?.Values != null)
                {
                    foreach (var loc in dto.LocalizedPairs.Values)
                    {
                        if (string.IsNullOrWhiteSpace(loc.LocaleId) || !supportedLocales.Contains(loc.LocaleId))
                            continue;

                        await using var cu = _db.CreateCommand(conn, updI18n);
                        cu.Transaction = tx;
                        cu.Parameters.Add(_db.CreateParameter("@NameKey", dto.NameKey));
                        cu.Parameters.Add(_db.CreateParameter("@LocaleId", loc.LocaleId));
                        cu.Parameters.Add(_db.CreateParameter("@Value", loc.Value ?? string.Empty));
                        cu.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));

                        if (await cu.ExecuteNonQueryAsync() == 0)
                        {
                            await using var ci = _db.CreateCommand(conn, insI18n);
                            ci.Transaction = tx;
                            ci.Parameters.Add(_db.CreateParameter("@NameKey", dto.NameKey));
                            ci.Parameters.Add(_db.CreateParameter("@LocaleId", loc.LocaleId));
                            ci.Parameters.Add(_db.CreateParameter("@Value", loc.Value ?? string.Empty));
                            ci.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));
                            await ci.ExecuteNonQueryAsync();
                        }
                    }
                }

                // --- Handle buttons (insert/update) and update map_spot coords ---
                if (dto.Buttons != null && dto.Buttons.Count > 0)
                {
                    const string updBtn = @"
                UPDATE teleport_table_button
                   SET name_key   = @NameKey,
                       map_spot_id = @MapSpotId,
                       is_active  = @IsActive
                 WHERE id = @BtnId
                   AND table_id = @TableId;";

                    const string insBtn = @"
                INSERT INTO teleport_table_button (table_id, name_key, map_spot_id, is_active)
                VALUES (@TableId, @NameKey, @MapSpotId, @IsActive);";

                    const string updMapSpot = @"
                UPDATE map_spot
                   SET x = @X, y = @Y, z = @Z
                 WHERE id = @MapSpotId;";

                    foreach (var btn in dto.Buttons)
                    {
                        int btnId;

                        if (btn.Id.HasValue)
                        {
                            await using var cmdU = _db.CreateCommand(conn, updBtn);
                            cmdU.Transaction = tx;
                            cmdU.Parameters.Add(_db.CreateParameter("@NameKey", btn.NameKey));
                            cmdU.Parameters.Add(_db.CreateParameter("@MapSpotId", (object?)(btn.MapSpot?.Id ?? (int?)null) ?? DBNull.Value));
                            cmdU.Parameters.Add(_db.CreateParameter("@IsActive", btn.IsActive));
                            cmdU.Parameters.Add(_db.CreateParameter("@BtnId", btn.Id.Value));
                            cmdU.Parameters.Add(_db.CreateParameter("@TableId", tableId));
                            await cmdU.ExecuteNonQueryAsync();

                            btnId = btn.Id.Value;
                        }
                        else
                        {
                            await using var cmdI = _db.CreateCommand(conn, insBtn);
                            cmdI.Transaction = tx;
                            cmdI.Parameters.Add(_db.CreateParameter("@TableId", tableId));
                            cmdI.Parameters.Add(_db.CreateParameter("@NameKey", btn.NameKey));
                            cmdI.Parameters.Add(_db.CreateParameter("@MapSpotId", (object?)(btn.MapSpot?.Id ?? (int?)null) ?? DBNull.Value));
                            cmdI.Parameters.Add(_db.CreateParameter("@IsActive", btn.IsActive));
                            await cmdI.ExecuteNonQueryAsync();

                            // get new id
                            await using var idCmd = _db.CreateCommand(conn, "SELECT LAST_INSERT_ID();");
                            idCmd.Transaction = tx;
                            btnId = Convert.ToInt32(await idCmd.ExecuteScalarAsync());
                        }

                        // Upsert map spot coordinates if present (expects existing map_spot.id)
                        if (btn.MapSpot != null && btn.MapSpot.Id > 0)
                        {
                            await using var mapCmd = _db.CreateCommand(conn, updMapSpot);
                            mapCmd.Transaction = tx;
                            mapCmd.Parameters.Add(_db.CreateParameter("@X", btn.MapSpot.X));
                            mapCmd.Parameters.Add(_db.CreateParameter("@Y", btn.MapSpot.Y));
                            mapCmd.Parameters.Add(_db.CreateParameter("@Z", btn.MapSpot.Z));
                            mapCmd.Parameters.Add(_db.CreateParameter("@MapSpotId", btn.MapSpot.Id));
                            await mapCmd.ExecuteNonQueryAsync();
                        }

                        // Upsert button localizations
                        if (btn.LocalizedPairs?.Values != null)
                        {
                            foreach (var loc in btn.LocalizedPairs.Values)
                            {
                                if (string.IsNullOrWhiteSpace(loc.LocaleId) || !supportedLocales.Contains(loc.LocaleId))
                                    continue;

                                await using var cuBtn = _db.CreateCommand(conn, updI18n);
                                cuBtn.Transaction = tx;
                                cuBtn.Parameters.Add(_db.CreateParameter("@NameKey", btn.NameKey));
                                cuBtn.Parameters.Add(_db.CreateParameter("@LocaleId", loc.LocaleId));
                                cuBtn.Parameters.Add(_db.CreateParameter("@Value", loc.Value ?? string.Empty));
                                cuBtn.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));

                                if (await cuBtn.ExecuteNonQueryAsync() == 0)
                                {
                                    await using var ciBtn = _db.CreateCommand(conn, insI18n);
                                    ciBtn.Transaction = tx;
                                    ciBtn.Parameters.Add(_db.CreateParameter("@NameKey", btn.NameKey));
                                    ciBtn.Parameters.Add(_db.CreateParameter("@LocaleId", loc.LocaleId));
                                    ciBtn.Parameters.Add(_db.CreateParameter("@Value", loc.Value ?? string.Empty));
                                    ciBtn.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));
                                    await ciBtn.ExecuteNonQueryAsync();
                                }
                            }
                        }
                    }
                }

                await tx.CommitAsync();

                // Reload using the same connection (no transaction).
                return await LoadTeleportTableByIdAsync(conn, spaceId, tableId);
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
            await using var conn = await _db.OpenConnectionAsync();
            await using var tx = await conn.BeginTransactionAsync();

            try
            {
                // 1) Get the table name_key (for i18n deletion)
                string? nameKey = null;
                const string getNameKey = @"
            SELECT name_key 
              FROM teleport_table 
             WHERE id = @TableId AND space_id = @SpaceId;";

                await using (var cmd = _db.CreateCommand(conn, getNameKey))
                {
                    cmd.Transaction = tx;
                    cmd.Parameters.Add(_db.CreateParameter("@TableId", tableId));
                    cmd.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));

                    var result = await cmd.ExecuteScalarAsync();
                    nameKey = result == null || result == DBNull.Value ? null : Convert.ToString(result);
                }

                // 2) Get all button name_keys (for i18n deletion)
                var buttonKeys = new List<string>();
                const string getButtonKeys = @"
            SELECT name_key 
              FROM teleport_table_button 
             WHERE table_id = @TableId;";

                await using (var cmd = _db.CreateCommand(conn, getButtonKeys))
                {
                    cmd.Transaction = tx;
                    cmd.Parameters.Add(_db.CreateParameter("@TableId", tableId));

                    await using var reader = await cmd.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        var key = reader.IsDBNull(0) ? null : reader.GetString(0);
                        if (!string.IsNullOrWhiteSpace(key)) buttonKeys.Add(key!);
                    }
                }

                // 3) Delete button i18n (only if we have keys)
                if (buttonKeys.Count > 0)
                {
                    // Build IN clause with parameterized keys
                    var paramNames = new List<string>(buttonKeys.Count);
                    for (int i = 0; i < buttonKeys.Count; i++)
                        paramNames.Add($"@BtnKey{i}");

                    var delBtnI18nSql =
                        $"DELETE FROM i18n WHERE `key` IN ({string.Join(",", paramNames)}) AND space_id = @SpaceId;";

                    await using var delBtnI18nCmd = _db.CreateCommand(conn, delBtnI18nSql);
                    delBtnI18nCmd.Transaction = tx;

                    for (int i = 0; i < buttonKeys.Count; i++)
                        delBtnI18nCmd.Parameters.Add(_db.CreateParameter($"@BtnKey{i}", buttonKeys[i]));

                    delBtnI18nCmd.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));

                    await delBtnI18nCmd.ExecuteNonQueryAsync();
                }

                // 4) Delete table i18n
                if (!string.IsNullOrWhiteSpace(nameKey))
                {
                    const string delTableI18n = @"
                DELETE FROM i18n 
                 WHERE `key` = @NameKey AND space_id = @SpaceId;";

                    await using var delTableI18nCmd = _db.CreateCommand(conn, delTableI18n);
                    delTableI18nCmd.Transaction = tx;
                    delTableI18nCmd.Parameters.Add(_db.CreateParameter("@NameKey", nameKey!));
                    delTableI18nCmd.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));
                    await delTableI18nCmd.ExecuteNonQueryAsync();
                }

                // 5) Delete all buttons
                const string delBtns = @"DELETE FROM teleport_table_button WHERE table_id = @TableId;";
                await using (var delBtnsCmd = _db.CreateCommand(conn, delBtns))
                {
                    delBtnsCmd.Transaction = tx;
                    delBtnsCmd.Parameters.Add(_db.CreateParameter("@TableId", tableId));
                    await delBtnsCmd.ExecuteNonQueryAsync();
                }

                // 6) Delete the table itself
                const string delTable = @"
            DELETE FROM teleport_table 
             WHERE id = @TableId AND space_id = @SpaceId;";

                int affected;
                await using (var delTableCmd = _db.CreateCommand(conn, delTable))
                {
                    delTableCmd.Transaction = tx;
                    delTableCmd.Parameters.Add(_db.CreateParameter("@TableId", tableId));
                    delTableCmd.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));
                    affected = await delTableCmd.ExecuteNonQueryAsync();
                }

                await tx.CommitAsync();
                return affected > 0;
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

            // 1) Fetch all teleport_table ids for the space
            await using var conn = await _db.OpenConnectionAsync();
            const string getIdsSql = @"SELECT id FROM teleport_table WHERE space_id = @SpaceId;";

            await using (var cmd = _db.CreateCommand(conn, getIdsSql))
            {
                cmd.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));

                await using var rdr = await cmd.ExecuteReaderAsync();
                while (await rdr.ReadAsync())
                {
                    // first column is id
                    var id = rdr.IsDBNull(0) ? (int?)null : rdr.GetInt32(0);
                    if (id.HasValue) tableIds.Add(id.Value);
                }
            }

            if (tableIds.Count == 0) return 0;

            // 2) Delete each table using the existing delete routine
            var deletedCount = 0;
            foreach (var tableId in tableIds)
            {
                if (await DeleteTeleportTableAsync(spaceId, tableId))
                    deletedCount++;
            }

            return deletedCount;
        }


        /// <summary>
        /// Deletes a single button from a teleport table, including its i18n localizations.
        /// </summary>
        /// <param name="buttonId">The button ID to delete.</param>
        /// <param nameId="tableId">The table ID the button belongs to.</param>
        /// <returns>True if deleted, false otherwise.</returns>
        public async Task<bool> DeleteTeleportTableButtonAsync(int buttonId, int tableId)
        {
            await using var conn = await _db.OpenConnectionAsync();
            await using var tx = await conn.BeginTransactionAsync();

            try
            {
                // 1) Fetch the button's name_key (for i18n cleanup)
                string? btnKey = null;
                const string getKeySql = @"
            SELECT name_key
              FROM teleport_table_button
             WHERE id = @ButtonId AND table_id = @TableId;";

                await using (var getKeyCmd = _db.CreateCommand(conn, getKeySql))
                {
                    getKeyCmd.Transaction = tx;
                    getKeyCmd.Parameters.Add(_db.CreateParameter("@ButtonId", buttonId));
                    getKeyCmd.Parameters.Add(_db.CreateParameter("@TableId", tableId));

                    var result = await getKeyCmd.ExecuteScalarAsync();
                    btnKey = result == null || result == DBNull.Value ? null : Convert.ToString(result);
                }

                // 2) Delete i18n for this button (no space filter to match original behavior)
                if (!string.IsNullOrWhiteSpace(btnKey))
                {
                    const string delBtnI18nSql = @"DELETE FROM i18n WHERE `key` = @BtnKey;";
                    await using var delBtnI18nCmd = _db.CreateCommand(conn, delBtnI18nSql);
                    delBtnI18nCmd.Transaction = tx;
                    delBtnI18nCmd.Parameters.Add(_db.CreateParameter("@BtnKey", btnKey!));
                    await delBtnI18nCmd.ExecuteNonQueryAsync();
                }

                // 3) Delete the button
                const string delBtnSql = @"
            DELETE FROM teleport_table_button
             WHERE id = @ButtonId AND table_id = @TableId;";

                int affected;
                await using (var delBtnCmd = _db.CreateCommand(conn, delBtnSql))
                {
                    delBtnCmd.Transaction = tx;
                    delBtnCmd.Parameters.Add(_db.CreateParameter("@ButtonId", buttonId));
                    delBtnCmd.Parameters.Add(_db.CreateParameter("@TableId", tableId));
                    affected = await delBtnCmd.ExecuteNonQueryAsync();
                }

                await tx.CommitAsync();
                return affected > 0;
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
        private async Task<TeleportTableData?> LoadTeleportTableByIdAsync(
            DbConnection conn, int spaceId, int tableId)
        {
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
        LEFT JOIN i18n i  ON i.`key` = t.name_key AND i.space_id = t.space_id
        LEFT JOIN teleport_table_button b ON b.table_id = t.id
        LEFT JOIN map_spot ms ON ms.id = b.map_spot_id
        LEFT JOIN i18n bi ON bi.`key` = b.name_key AND bi.space_id = t.space_id
        WHERE t.space_id = @SpaceId AND t.id = @TableId
        ORDER BY b.id, bi.locale_id;";

            await using var cmd = _db.CreateCommand(conn, sql);
            cmd.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));
            cmd.Parameters.Add(_db.CreateParameter("@TableId", tableId));

            await using var reader = await cmd.ExecuteReaderAsync();

            TeleportTableData? table = null;
            var buttonMap = new Dictionary<int, ButtonData>();
            var tableLocals = new List<LocalizedValue>();
            var buttonLocals = new Dictionary<int, List<LocalizedValue>>();

            bool ordReady = false;
            int o_table_id = -1, o_space_id = -1, o_is_active = -1, o_table_name_key = -1, o_table_locale_id = -1, o_table_localized_value = -1;
            int o_button_id = -1, o_button_name_key = -1, o_button_is_active = -1, o_map_spot_id = -1;
            int o_map_spot_x = -1, o_map_spot_y = -1, o_map_spot_z = -1;
            int o_button_locale_id = -1, o_button_localized_value = -1;

            while (await reader.ReadAsync())
            {
                if (!ordReady)
                {
                    o_table_id = reader.GetOrdinal("table_id");
                    o_space_id = reader.GetOrdinal("space_id");
                    o_is_active = reader.GetOrdinal("is_active");
                    o_table_name_key = reader.GetOrdinal("table_name_key");
                    o_table_locale_id = reader.GetOrdinal("table_locale_id");
                    o_table_localized_value = reader.GetOrdinal("table_localized_value");

                    o_button_id = reader.GetOrdinal("button_id");
                    o_button_name_key = reader.GetOrdinal("button_name_key");
                    o_button_is_active = reader.GetOrdinal("button_is_active");
                    o_map_spot_id = reader.GetOrdinal("map_spot_id");

                    o_map_spot_x = reader.GetOrdinal("map_spot_x");
                    o_map_spot_y = reader.GetOrdinal("map_spot_y");
                    o_map_spot_z = reader.GetOrdinal("map_spot_z");

                    o_button_locale_id = reader.GetOrdinal("button_locale_id");
                    o_button_localized_value = reader.GetOrdinal("button_localized_value");

                    ordReady = true;
                }

                table ??= new TeleportTableData
                {
                    Id = reader.GetInt32(o_table_id),
                    SpaceId = reader.GetInt32(o_space_id),
                    IsActive = reader.GetBoolean(o_is_active),
                    NameKey = reader.GetString(o_table_name_key),
                    LocalizedPairs = new LocalizedPairs
                    {
                        Key = reader.GetString(o_table_name_key),
                        Values = tableLocals
                    },
                    Buttons = []
                };

                // Table i18n
                if (!reader.IsDBNull(o_table_locale_id) && !reader.IsDBNull(o_table_localized_value))
                {
                    var localeId = reader.GetString(o_table_locale_id);
                    var value = reader.GetString(o_table_localized_value);
                    if (!tableLocals.Any(v => v.LocaleId == localeId))
                        tableLocals.Add(new LocalizedValue { LocaleId = localeId, Value = value });
                }

                // Button
                if (!reader.IsDBNull(o_button_id))
                {
                    var buttonId = reader.GetInt32(o_button_id);
                    if (!buttonMap.TryGetValue(buttonId, out var btn))
                    {
                        btn = new ButtonData
                        {
                            Id = buttonId,
                            NameKey = reader.IsDBNull(o_button_name_key) ? string.Empty : reader.GetString(o_button_name_key),
                            IsActive = !reader.IsDBNull(o_button_is_active) && reader.GetBoolean(o_button_is_active),
                            LocalizedPairs = new LocalizedPairs
                            {
                                Key = reader.IsDBNull(o_button_name_key) ? string.Empty : reader.GetString(o_button_name_key),
                                Values = []
                            },
                            MapSpot = reader.IsDBNull(o_map_spot_id)
                                ? null
                                : new MapSpotData
                                {
                                    Id = reader.GetInt32(o_map_spot_id),
                                    X = reader.IsDBNull(o_map_spot_x) ? 0 : reader.GetDecimal(o_map_spot_x),
                                    Y = reader.IsDBNull(o_map_spot_y) ? 0 : reader.GetDecimal(o_map_spot_y),
                                    Z = reader.IsDBNull(o_map_spot_z) ? 0 : reader.GetDecimal(o_map_spot_z)
                                }
                        };

                        buttonMap[buttonId] = btn;
                        buttonLocals[buttonId] = btn.LocalizedPairs.Values;
                    }

                    // Button i18n
                    if (!reader.IsDBNull(o_button_locale_id) && !reader.IsDBNull(o_button_localized_value))
                    {
                        var localeId = reader.GetString(o_button_locale_id);
                        var value = reader.GetString(o_button_localized_value);
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
        DbConnection? externalConn = null,
        DbTransaction? externalTx = null)
        {
            if (btnDto == null) throw new ArgumentNullException(nameof(btnDto));

            var useExternal = externalConn != null && externalTx != null;

            await using var conn = useExternal ? null : await _db.OpenConnectionAsync();
            var connection = externalConn ?? conn!;
            var tx = externalTx ?? await connection.BeginTransactionAsync();

            try
            {
                // 1) Insert or reuse map_spot
                int? mapSpotId = null;
                if (btnDto.MapSpot != null)
                {
                    if (btnDto.MapSpot.Id > 0)
                    {
                        mapSpotId = btnDto.MapSpot.Id;
                    }
                    else
                    {
                        const string insMapSpot = @"
                    INSERT INTO map_spot (x, y, z)
                    VALUES (@X, @Y, @Z);";

                        await using (var cmdMap = _db.CreateCommand(connection, insMapSpot))
                        {
                            cmdMap.Transaction = tx;
                            cmdMap.Parameters.Add(_db.CreateParameter("@X", btnDto.MapSpot.X));
                            cmdMap.Parameters.Add(_db.CreateParameter("@Y", btnDto.MapSpot.Y));
                            cmdMap.Parameters.Add(_db.CreateParameter("@Z", btnDto.MapSpot.Z));
                            await cmdMap.ExecuteNonQueryAsync();
                        }

                        // fetch new id
                        const string getLastIdSql = "SELECT LAST_INSERT_ID();";
                        await using (var idCmd = _db.CreateCommand(connection, getLastIdSql))
                        {
                            idCmd.Transaction = tx;
                            var obj = await idCmd.ExecuteScalarAsync();
                            mapSpotId = Convert.ToInt32(obj);
                        }
                    }
                }

                // 2) Insert button
                const string insBtn = @"
            INSERT INTO teleport_table_button (table_id, name_key, map_spot_id, is_active)
            VALUES (@TableId, @NameKey, @MapSpotId, @IsActive);";

                int buttonId;
                await using (var cmdBtn = _db.CreateCommand(connection, insBtn))
                {
                    cmdBtn.Transaction = tx;
                    cmdBtn.Parameters.Add(_db.CreateParameter("@TableId", tableId));
                    cmdBtn.Parameters.Add(_db.CreateParameter("@NameKey", btnDto.NameKey ?? string.Empty));
                    cmdBtn.Parameters.Add(_db.CreateParameter("@MapSpotId", (object?)mapSpotId ?? DBNull.Value));
                    cmdBtn.Parameters.Add(_db.CreateParameter("@IsActive", btnDto.IsActive));
                    await cmdBtn.ExecuteNonQueryAsync();
                }

                // fetch new button id
                const string getButtonIdSql = "SELECT LAST_INSERT_ID();";
                await using (var idBtn = _db.CreateCommand(connection, getButtonIdSql))
                {
                    idBtn.Transaction = tx;
                    var obj = await idBtn.ExecuteScalarAsync();
                    buttonId = Convert.ToInt32(obj);
                }

                // 3) Insert i18n for button name (if provided)
                if (btnDto.LocalizedPairs?.Values != null &&
                    !string.IsNullOrWhiteSpace(btnDto.LocalizedPairs.Key))
                {
                    const string insI18n = @"
                INSERT INTO i18n (`key`, locale_id, value, space_id)
                VALUES (@Key, @LocaleId, @Value, @SpaceId);";

                    foreach (var loc in btnDto.LocalizedPairs.Values)
                    {
                        await using var cmdI18n = _db.CreateCommand(connection, insI18n);
                        cmdI18n.Transaction = tx;
                        cmdI18n.Parameters.Add(_db.CreateParameter("@Key", btnDto.LocalizedPairs.Key));
                        cmdI18n.Parameters.Add(_db.CreateParameter("@LocaleId", loc.LocaleId));
                        cmdI18n.Parameters.Add(_db.CreateParameter("@Value", loc.Value));
                        cmdI18n.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));
                        await cmdI18n.ExecuteNonQueryAsync();
                    }
                }

                if (!useExternal)
                    await tx.CommitAsync();

                // 4) Return hydrated result (coords only if provided in DTO)
                return new ButtonData
                {
                    Id = buttonId,
                    NameKey = btnDto.NameKey ?? string.Empty,
                    IsActive = btnDto.IsActive,
                    LocalizedPairs = btnDto.LocalizedPairs ?? new LocalizedPairs
                    {
                        Key = btnDto.NameKey ?? string.Empty,
                        Values = new List<LocalizedValue>()
                    },
                    MapSpot = mapSpotId.HasValue
                        ? new MapSpotData
                        {
                            Id = mapSpotId.Value,
                            X = btnDto.MapSpot?.X ?? 0,
                            Y = btnDto.MapSpot?.Y ?? 0,
                            Z = btnDto.MapSpot?.Z ?? 0
                        }
                        : null
                };
            }
            catch
            {
                if (!useExternal)
                    await tx.RollbackAsync();
                throw;
            }
        }

        #endregion
    }
}
