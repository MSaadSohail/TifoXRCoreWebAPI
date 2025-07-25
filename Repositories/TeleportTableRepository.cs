// <copyright file="TeleportRepository.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Saad Sohail</author>
// <date>07/23/2025</date>
// <summary>Class to handle teleport tables SQL side</summary>

using MySqlConnector;
using System.Data;
using TifoXRCoreWebAPI.Models;
using TifoXRCoreWebAPI.Repositories.Interfaces;
using TifoXRCoreWebAPI.Utilities;

namespace TifoXRCoreWebAPI.Repositories
{
    public class TeleportTableRepository : ITeleportTableRepository
    {
        private readonly string _connStr;
        public TeleportTableRepository(IConfiguration configuration)
            => _connStr = configuration.GetConnectionString("DefaultConnection");

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
                    b.booth_to_visit,
                    bi.locale_id AS button_locale_id,
                    bi.value AS button_localized_value
                FROM teleport_table t
                LEFT JOIN i18n i
                    ON i.`key` = t.name_key AND i.space_id = t.space_id
                LEFT JOIN teleport_table_button b
                    ON b.table_id = t.id
                LEFT JOIN i18n bi
                    ON bi.`key` = b.name_key AND bi.space_id = t.space_id
                WHERE t.space_id = @SpaceId
                ORDER BY b.id, bi.locale_id;
            ";

            await using var conn = new MySqlConnection(_connStr);
            await conn.OpenAsync();

            TeleportTableData? table = null;
            Dictionary<int, ButtonData> buttonMap = new();

            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@SpaceId", spaceId);

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                if (table == null)
                {
                    table = new TeleportTableData
                    {
                        Id = reader.GetInt32("table_id"),
                        SpaceId = reader.GetInt32("space_id"),
                        IsActive = reader.GetBoolean("is_active"),
                        NameKey = reader.GetString("table_name_key"),
                        LocalizedName = new (),
                        Buttons = new ()
                    };
                }

                // Table i18n
                if (!reader.IsDBNull("table_locale_id") && !reader.IsDBNull("table_localized_value"))
                {
                    var localeId = reader.GetString("table_locale_id");
                    var value = reader.GetString("table_localized_value");
                    if (!table.LocalizedName.ContainsKey(localeId))
                        table.LocalizedName[localeId] = value;
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
                            BoothToVisit = reader.GetInt32("booth_to_visit"),
                            LocalizedName = new ()
                        };
                        buttonMap[buttonId] = btn;
                    }
                    // Button i18n
                    if (!reader.IsDBNull("button_locale_id") && !reader.IsDBNull("button_localized_value"))
                    {
                        var localeId = reader.GetString("button_locale_id");
                        var value = reader.GetString("button_localized_value");
                        if (!btn.LocalizedName.ContainsKey(localeId))
                            btn.LocalizedName[localeId] = value;
                    }
                }
            }

            if (table != null)
                table.Buttons = buttonMap.Values.ToList();

            return table;
        }

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
                       AND space_id = @SpaceId;";
                
                await using (var cmd = new MySqlCommand(updTable, conn, tx))
                {
                    cmd.Parameters.AddWithValue("@IsActive", dto.IsActive);
                    cmd.Parameters.AddWithValue("@NameKey", dto.NameKey);
                    cmd.Parameters.AddWithValue("@TableId", tableId);
                    cmd.Parameters.AddWithValue("@SpaceId", spaceId);
                    if (await cmd.ExecuteNonQueryAsync() == 0)
                        return null;
                }

                // --- Upsert table name localizations (only supported) ---
                const string updI18n = @"
                    UPDATE i18n
                       SET value = @Value
                     WHERE `key`     = @NameKey
                       AND locale_id = @LocaleId
                       AND space_id  = @SpaceId;";
                        const string insI18n = @"
                    INSERT INTO i18n (`key`, locale_id, value, space_id)
                    VALUES (@NameKey, @LocaleId, @Value, @SpaceId);";

                foreach (var kv in dto.LocalizedName)
                {
                    var locale = kv.Key;
                    var value = kv.Value;
                    if (!supportedLocales.Contains(locale))
                        continue; // skip unsupported
                    await using var cu = new MySqlCommand(updI18n, conn, tx);
                    cu.Parameters.AddWithValue("@NameKey", dto.NameKey);
                    cu.Parameters.AddWithValue("@LocaleId", locale);
                    cu.Parameters.AddWithValue("@Value", value);
                    cu.Parameters.AddWithValue("@SpaceId", spaceId);
                    if (await cu.ExecuteNonQueryAsync() == 0)
                    {
                        await using var ci = new MySqlCommand(insI18n, conn, tx);
                        ci.Parameters.AddWithValue("@NameKey", dto.NameKey);
                        ci.Parameters.AddWithValue("@LocaleId", locale);
                        ci.Parameters.AddWithValue("@Value", value);
                        ci.Parameters.AddWithValue("@SpaceId", spaceId);
                        await ci.ExecuteNonQueryAsync();
                    }
                }

                // --- Handle buttons (only insert/update, no delete) ---
                if (dto.Buttons != null && dto.Buttons.Count > 0)
                {
                    foreach (var btn in dto.Buttons)
                    {
                        int btnId;
                        if (btn.Id.HasValue)
                        {
                            // Update
                            const string updBtn = @"
                                UPDATE teleport_table_button
                                   SET name_key = @NameKey,
                                       booth_to_visit = @BoothToVisit
                                 WHERE id = @BtnId
                                   AND table_id = @TableId;";
                            
                            await using var cmdU = new MySqlCommand(updBtn, conn, tx);
                            cmdU.Parameters.AddWithValue("@NameKey", btn.NameKey);
                            cmdU.Parameters.AddWithValue("@BoothToVisit", btn.BoothToVisit);
                            cmdU.Parameters.AddWithValue("@BtnId", btn.Id.Value);
                            cmdU.Parameters.AddWithValue("@TableId", tableId);
                            await cmdU.ExecuteNonQueryAsync();
                            btnId = btn.Id.Value;
                        }
                        else
                        {
                            // Insert
                            const string insBtn = @"
                                INSERT INTO teleport_table_button (table_id, name_key, booth_to_visit)
                                VALUES (@TableId, @NameKey, @BoothToVisit);
                            ";
                            
                            await using var cmdI = new MySqlCommand(insBtn, conn, tx);
                            cmdI.Parameters.AddWithValue("@TableId", tableId);
                            cmdI.Parameters.AddWithValue("@NameKey", btn.NameKey);
                            cmdI.Parameters.AddWithValue("@BoothToVisit", btn.BoothToVisit);
                            await cmdI.ExecuteNonQueryAsync();
                            btnId = (int)cmdI.LastInsertedId;
                        }

                        // Upsert localizations for this button (only supported)
                        foreach (var loc in btn.LocalizedName)
                        {
                            var locale = loc.Key;
                            var value = loc.Value;
                            if (!supportedLocales.Contains(locale))
                                continue;
                            await using var cu = new MySqlCommand(updI18n, conn, tx);
                            cu.Parameters.AddWithValue("@NameKey", btn.NameKey);
                            cu.Parameters.AddWithValue("@LocaleId", locale);
                            cu.Parameters.AddWithValue("@Value", value);
                            cu.Parameters.AddWithValue("@SpaceId", spaceId);
                            if (await cu.ExecuteNonQueryAsync() == 0)
                            {
                                await using var ci = new MySqlCommand(insI18n, conn, tx);
                                ci.Parameters.AddWithValue("@NameKey", btn.NameKey);
                                ci.Parameters.AddWithValue("@LocaleId", locale);
                                ci.Parameters.AddWithValue("@Value", value);
                                ci.Parameters.AddWithValue("@SpaceId", spaceId);
                                await ci.ExecuteNonQueryAsync();
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

        private static async Task<TeleportTableData?> LoadTeleportTableById(
            MySqlConnection conn,
            int spaceId,
            int tableId)
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
                    if (table == null)
                    {
                        table = new TeleportTableData
                        {
                            Id = reader.GetInt32("id"),
                            SpaceId = reader.GetInt32("space_id"),
                            NameKey = reader.IsDBNull("name_key") ? null : reader.GetString("name_key"),
                            IsActive = reader.GetBoolean("is_active"),
                            LocalizedName = localizedName,
                            Buttons = new List<ButtonData>()
                        };
                    }
                    if (!reader.IsDBNull("locale_id") && !reader.IsDBNull("value"))
                    {
                        localizedName[reader.GetString("locale_id")] = reader.GetString("value");
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
                    b.booth_to_visit,
                    i.locale_id,
                    i.value
                FROM teleport_table_button AS b
                LEFT JOIN i18n AS i
                  ON i.`key`    = b.name_key
                 AND i.space_id = @SpaceId
                WHERE b.table_id = @TableId
                ORDER BY b.id, i.locale_id;";

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
                            NameKey = btnReader.IsDBNull("button_name_key") ? null : btnReader.GetString("button_name_key"),
                            BoothToVisit = btnReader.GetInt32("booth_to_visit"),
                            LocalizedName = new Dictionary<string, string>()
                        };
                        btnMap[btnId] = btn;
                    }
                    if (!btnReader.IsDBNull("locale_id") && !btnReader.IsDBNull("value"))
                    {
                        btn.LocalizedName[btnReader.GetString("locale_id")] = btnReader.GetString("value");
                    }
                }
            }

            table.Buttons = btnMap.Values.ToList();
            return table;
        }
    }
}
