using MySqlConnector;
using System.Data;
using TifoXRCoreWebAPI.Models;
using TifoXRCoreWebAPI.Models.Common;
using TifoXRCoreWebAPI.Repositories.Interfaces;

namespace TifoXRCoreWebAPI.Repositories
{
    public class TeleportRepository : ITeleportRepository
    {
        private readonly string _connStr;
        public TeleportRepository(IConfiguration configuration)
            => _connStr = configuration.GetConnectionString("DefaultConnection");

        public async Task<List<TeleportTableData>> GetTeleportTablesBySpaceAsync(int spaceId)
        {
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
                  ON i.`key`     = t.name_key
                 AND i.space_id  = t.space_id
                WHERE t.space_id = @SpaceId
                ORDER BY t.id, i.locale_id;";

            var result = new List<TeleportTableData>();
            await using var conn = new MySqlConnection(_connStr);
            await conn.OpenAsync();

            var tableMap = new Dictionary<int, TeleportTableData>();
            // load tables
            await using (var cmd = new MySqlCommand(tableSql, conn))
            {
                cmd.Parameters.AddWithValue("@SpaceId", spaceId);
                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var id = reader.GetInt32("id");
                    if (!tableMap.TryGetValue(id, out var table))
                    {
                        table = new TeleportTableData
                        {
                            Id = id,
                            SpaceId = spaceId,
                            IsActive = reader.GetBoolean("is_active"),
                            LocalizedName = new LocalizedName
                            {
                                Key = reader.GetString("name_key"),
                                Values = new List<LocalizedValue>()
                            },
                            Buttons = new List<ButtonData>()
                        };
                        tableMap[id] = table;
                    }
                    if (!reader.IsDBNull("locale_id"))
                    {
                        table.LocalizedName.Values.Add(new LocalizedValue
                        {
                            LocaleId = reader.GetString("locale_id"),
                            Value = reader.GetString("value")
                        });
                    }
                }
            }
            // load buttons per table
            const string btnSql = @"
                SELECT
                    b.id,
                    b.text_key,
                    i.locale_id,
                    i.value
                FROM teleport_table_button AS b
                LEFT JOIN i18n AS i
                  ON i.`key`    = b.text_key
                 AND i.space_id = @SpaceId
                WHERE b.table_id = @TableId
                ORDER BY b.id, i.locale_id;";

            foreach (var table in tableMap.Values)
            {
                var btnMap = new Dictionary<int, ButtonData>();
                await using var btnCmd = new MySqlCommand(btnSql, conn);
                btnCmd.Parameters.AddWithValue("@SpaceId", spaceId);
                btnCmd.Parameters.AddWithValue("@TableId", table.Id);
                await using var btnReader = await btnCmd.ExecuteReaderAsync();
                while (await btnReader.ReadAsync())
                {
                    var btnId = btnReader.GetInt32("id");
                    if (!btnMap.TryGetValue(btnId, out var btn))
                    {
                        btn = new ButtonData
                        {
                            Id = btnId,
                            LocalizedName = new LocalizedName
                            {
                                Key = btnReader.GetString("text_key"),
                                Values = new List<LocalizedValue>()
                            }
                        };
                        btnMap[btnId] = btn;
                    }
                    if (!btnReader.IsDBNull("locale_id"))
                    {
                        btn.LocalizedName.Values.Add(new LocalizedValue
                        {
                            LocaleId = btnReader.GetString("locale_id"),
                            Value = btnReader.GetString("value")
                        });
                    }
                }
                table.Buttons = btnMap.Values.ToList();
            }

            result.AddRange(tableMap.Values);
            return result;
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
                // upsert table active flag and name_key
                const string updTable = @"
                    UPDATE teleport_table
                       SET is_active = @IsActive,
                           name_key  = @NameKey
                     WHERE id = @TableId
                       AND space_id = @SpaceId;";
                await using (var cmd = new MySqlCommand(updTable, conn, tx))
                {
                    cmd.Parameters.AddWithValue("@IsActive", dto.IsActive);
                    cmd.Parameters.AddWithValue("@NameKey", dto.LocalizedName.Key);
                    cmd.Parameters.AddWithValue("@TableId", tableId);
                    cmd.Parameters.AddWithValue("@SpaceId", spaceId);
                    if (await cmd.ExecuteNonQueryAsync() == 0)
                        return null;
                }

                // upsert i18n for table name
                const string updI18n = @"
                    UPDATE i18n
                       SET value = @Value
                     WHERE `key`     = @NameKey
                       AND locale_id = @LocaleId
                       AND space_id  = @SpaceId;";
                const string insI18n = @"
                    INSERT INTO i18n (`key`, locale_id, value, space_id)
                    VALUES (@NameKey, @LocaleId, @Value, @SpaceId);";
                foreach (var loc in dto.LocalizedName.Values)
                {
                    await using var cu = new MySqlCommand(updI18n, conn, tx);
                    cu.Parameters.AddWithValue("@NameKey", dto.LocalizedName.Key);
                    cu.Parameters.AddWithValue("@LocaleId", loc.LocaleId);
                    cu.Parameters.AddWithValue("@Value", loc.Value);
                    cu.Parameters.AddWithValue("@SpaceId", spaceId);
                    if (await cu.ExecuteNonQueryAsync() == 0)
                    {
                        await using var ci = new MySqlCommand(insI18n, conn, tx);
                        ci.Parameters.AddWithValue("@NameKey", dto.LocalizedName.Key);
                        ci.Parameters.AddWithValue("@LocaleId", loc.LocaleId);
                        ci.Parameters.AddWithValue("@Value", loc.Value);
                        ci.Parameters.AddWithValue("@SpaceId", spaceId);
                        await ci.ExecuteNonQueryAsync();
                    }
                }

                //  upsert buttons
                if (dto.Buttons != null)
                {
                    const string updBtn = @"
                        UPDATE teleport_table_button
                           SET text_key = @TextKey
                         WHERE id = @BtnId
                           AND table_id = @TableId;";
                    const string insBtn = @"
                        INSERT INTO teleport_table_button (table_id, text_key)
                        VALUES (@TableId, @TextKey);";
                    foreach (var btn in dto.Buttons)
                    {
                        int btnId;
                        if (btn.Id.HasValue)
                        {
                            await using var cmdU = new MySqlCommand(updBtn, conn, tx);
                            cmdU.Parameters.AddWithValue("@TextKey", btn.LocalizedName.Key);
                            cmdU.Parameters.AddWithValue("@BtnId", btn.Id.Value);
                            cmdU.Parameters.AddWithValue("@TableId", tableId);
                            if (await cmdU.ExecuteNonQueryAsync() == 0)
                                return null;
                            btnId = btn.Id.Value;
                        }
                        else
                        {
                            await using var cmdI = new MySqlCommand(insBtn, conn, tx);
                            cmdI.Parameters.AddWithValue("@TableId", tableId);
                            cmdI.Parameters.AddWithValue("@TextKey", btn.LocalizedName.Key);
                            await cmdI.ExecuteNonQueryAsync();
                            btnId = Convert.ToInt32(cmdI.LastInsertedId);
                        }

                        // i18n for button
                        foreach (var loc in btn.LocalizedName.Values)
                        {
                            await using var cu = new MySqlCommand(updI18n, conn, tx);
                            cu.Parameters.AddWithValue("@NameKey", btn.LocalizedName.Key);
                            cu.Parameters.AddWithValue("@LocaleId", loc.LocaleId);
                            cu.Parameters.AddWithValue("@Value", loc.Value);
                            cu.Parameters.AddWithValue("@SpaceId", spaceId);
                            if (await cu.ExecuteNonQueryAsync() == 0)
                            {
                                await using var ci = new MySqlCommand(insI18n, conn, tx);
                                ci.Parameters.AddWithValue("@NameKey", btn.LocalizedName.Key);
                                ci.Parameters.AddWithValue("@LocaleId", loc.LocaleId);
                                ci.Parameters.AddWithValue("@Value", loc.Value);
                                ci.Parameters.AddWithValue("@SpaceId", spaceId);
                                await ci.ExecuteNonQueryAsync();
                            }
                        }
                    }
                }

                await tx.CommitAsync();
                // reload and return
                return await LoadTeleportTableById(conn, spaceId, tableId);
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        private static async Task<TeleportTableData> LoadTeleportTableById(
            MySqlConnection conn,
            int spaceId,
            int tableId)
        {
            const string sql = @"
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
                ORDER BY i.locale_id;";
            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@TableId", tableId);
            cmd.Parameters.AddWithValue("@SpaceId", spaceId);
            await using var reader = await cmd.ExecuteReaderAsync();
            TeleportTableData table = null!;
            var values = new List<LocalizedValue>();
            while (await reader.ReadAsync())
            {
                if (table == null)
                {
                    table = new TeleportTableData
                    {
                        Id = reader.GetInt32("id"),
                        SpaceId = spaceId,
                        IsActive = reader.GetBoolean("is_active"),
                        LocalizedName = new LocalizedName
                        {
                            Key = reader.GetString("name_key"),
                            Values = values
                        },
                        Buttons = new List<ButtonData>()
                    };
                }
                if (!reader.IsDBNull("locale_id"))
                {
                    values.Add(new LocalizedValue
                    {
                        LocaleId = reader.GetString("locale_id"),
                        Value = reader.GetString("value")
                    });
                }
            }
            return table;
        }
    }
}
