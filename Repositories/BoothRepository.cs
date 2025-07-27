using MySqlConnector;
using System.Data;
using GMS.TifoXRCoreWebAPI.Models;
using GMS.TifoXRCoreWebAPI.Models.Common;
using GMS.TifoXRCoreWebAPI.Repositories.Interfaces;

namespace GMS.TifoXRCoreWebAPI.Repositories
{
    public class BoothRepository : IBoothRepository
    {
        private readonly string _connectionString;

        public BoothRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        public async Task<List<BoothModel>> GetAllBoothsBySpaceAsync(int spaceId)
        {
            const string sql = @"
            SELECT
                b.id,
                b.space_id,
                b.name_key,
                i.locale_id,
                i.value
            FROM booth b
            INNER JOIN i18n i
              ON i.`key` = b.name_key
             AND i.space_id = b.space_id
            WHERE b.space_id = @SpaceId
            ORDER BY b.id, i.locale_id;
        ";

            await using var conn = new MySqlConnection(_connectionString);
            await conn.OpenAsync();
            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@SpaceId", spaceId);

            var dict = new Dictionary<int, BoothModel>();
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var id = reader.GetInt32("id");
                if (!dict.TryGetValue(id, out var booth))
                {
                    booth = new BoothModel
                    {
                        Id = id,
                        SpaceId = reader.GetInt32("space_id"),
                        LocalizedName = new LocalizedName
                        {
                            Key = reader.GetString("name_key"),
                            Values = new List<LocalizedValue>()
                        }
                    };
                    dict[id] = booth;
                }

                booth.LocalizedName.Values.Add(new LocalizedValue
                {
                    LocaleId = reader.GetString("locale_id"),
                    Value = reader.GetString("value")
                });
            }

            return dict.Values.ToList();
        }

        public async Task<BoothModel?> UpdateBoothAsync(int spaceId, int boothId, BoothUpdateDto dto)
        {
            await using var conn = new MySqlConnection(_connectionString);
            await conn.OpenAsync();
            await using var tx = await conn.BeginTransactionAsync();

            try
            {
                const string updBooth = @"
                UPDATE booth
                   SET name_key = @NameKey
                 WHERE id = @BoothId
                   AND space_id = @SpaceId;";
                await using (var cmd = new MySqlCommand(updBooth, conn, tx))
                {
                    cmd.Parameters.AddWithValue("@NameKey", dto.LocalizedName.Key);
                    cmd.Parameters.AddWithValue("@BoothId", boothId);
                    cmd.Parameters.AddWithValue("@SpaceId", spaceId);
                    if (await cmd.ExecuteNonQueryAsync() == 0)
                        return null;
                }

                const string updI18n = @"
                UPDATE i18n
                   SET value = @Value
                 WHERE `key` = @NameKey AND locale_id = @LocaleId AND space_id = @SpaceId;";
                const string insI18n = @"
                INSERT INTO i18n (`key`, locale_id, value, space_id)
                VALUES (@NameKey, @LocaleId, @Value, @SpaceId);";

                foreach (var loc in dto.LocalizedName.Values)
                {
                    await using var cmdUp = new MySqlCommand(updI18n, conn, tx);
                    cmdUp.Parameters.AddWithValue("@NameKey", dto.LocalizedName.Key);
                    cmdUp.Parameters.AddWithValue("@LocaleId", loc.LocaleId);
                    cmdUp.Parameters.AddWithValue("@Value", loc.Value);
                    cmdUp.Parameters.AddWithValue("@SpaceId", spaceId);

                    if (await cmdUp.ExecuteNonQueryAsync() == 0)
                    {
                        await using var cmdIn = new MySqlCommand(insI18n, conn, tx);
                        cmdIn.Parameters.AddWithValue("@NameKey", dto.LocalizedName.Key);
                        cmdIn.Parameters.AddWithValue("@LocaleId", loc.LocaleId);
                        cmdIn.Parameters.AddWithValue("@Value", loc.Value);
                        cmdIn.Parameters.AddWithValue("@SpaceId", spaceId);
                        await cmdIn.ExecuteNonQueryAsync();
                    }
                }

                await tx.CommitAsync();
                return await LoadBoothById(conn, spaceId, boothId);
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        private static async Task<BoothModel?> LoadBoothById(MySqlConnection conn, int spaceId, int boothId)
        {
            const string sql = @"
            SELECT b.id, b.space_id, b.name_key, i.locale_id, i.value
            FROM booth b
            LEFT JOIN i18n i ON i.`key` = b.name_key AND i.space_id = b.space_id
            WHERE b.id = @BoothId AND b.space_id = @SpaceId
            ORDER BY i.locale_id;";
            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@BoothId", boothId);
            cmd.Parameters.AddWithValue("@SpaceId", spaceId);

            await using var reader = await cmd.ExecuteReaderAsync();
            BoothModel? booth = null;
            while (await reader.ReadAsync())
            {
                booth ??= new BoothModel
                {
                    Id = reader.GetInt32("id"),
                    SpaceId = reader.GetInt32("space_id"),
                    LocalizedName = new LocalizedName
                    {
                        Key = reader.GetString("name_key"),
                        Values = new List<LocalizedValue>()
                    }
                };
                booth.LocalizedName.Values.Add(new LocalizedValue
                {
                    LocaleId = reader.GetString("locale_id"),
                    Value = reader.GetString("value")
                });
            }
            return booth;
        }


        public async Task<BoothModel> CreateBoothAsync(int spaceId, BoothCreateDto boothDto)
        {
            await using var conn = new MySqlConnection(_connectionString);
            await conn.OpenAsync();
            await using var tx = await conn.BeginTransactionAsync();

            try
            {
                // 1) Insert booth
                const string insertBoothSql = @"
            INSERT INTO booth (space_id, name_key)
            VALUES (@SpaceId, @NameKey);";
                int newId;
                await using (var cmd = new MySqlCommand(insertBoothSql, conn, tx))
                {
                    cmd.Parameters.AddWithValue("@SpaceId", spaceId);
                    cmd.Parameters.AddWithValue("@NameKey", boothDto.LocalizedName.Key);
                    await cmd.ExecuteNonQueryAsync();
                    newId = Convert.ToInt32(cmd.LastInsertedId);
                }

                // 2) Insert i18n
                const string insertI18nSql = @"
            INSERT INTO i18n (`key`, locale_id, value, space_id)
            VALUES (@NameKey, @LocaleId, @Value, @SpaceId);";
                foreach (var val in boothDto.LocalizedName.Values)
                {
                    await using var cmdI18n = new MySqlCommand(insertI18nSql, conn, tx);
                    cmdI18n.Parameters.AddWithValue("@NameKey", boothDto.LocalizedName.Key);
                    cmdI18n.Parameters.AddWithValue("@LocaleId", val.LocaleId);
                    cmdI18n.Parameters.AddWithValue("@Value", val.Value);
                    cmdI18n.Parameters.AddWithValue("@SpaceId", spaceId);
                    await cmdI18n.ExecuteNonQueryAsync();
                }

                await tx.CommitAsync();

                return new BoothModel
                {
                    Id = newId,
                    SpaceId = spaceId,
                    LocalizedName = new LocalizedName
                    {
                        Key = boothDto.LocalizedName.Key,
                        Values = boothDto.LocalizedName.Values
                    }
                };
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        public async Task<bool> DeleteBoothCascadeAsync(int spaceId, int boothId)
        {
            await using var conn = new MySqlConnection(_connectionString);
            await conn.OpenAsync();
            await using var tx = await conn.BeginTransactionAsync();

            try
            {
                // 1. Fetch boothKey
                const string fetchBooth = @"SELECT b.name_key FROM booth b WHERE b.id = @BoothId AND b.space_id = @SpaceId;";
                string boothKey;
                await using (var cmd = new MySqlCommand(fetchBooth, conn, tx))
                {
                    cmd.Parameters.AddWithValue("@BoothId", boothId);
                    cmd.Parameters.AddWithValue("@SpaceId", spaceId);
                    var o = await cmd.ExecuteScalarAsync();
                    if (o == null) return false;
                    boothKey = o.ToString()!;
                }

                // 2. Fetch portals
                const string fetchPortals = @"
            SELECT p.id, p.text_field_key, p.corresponding_media_id, p.thumbnail_media_id
            FROM portal p
            WHERE p.booth_id = @BoothId AND p.space_id = @SpaceId;
        ";

                var portals = new List<(int Id, string Key, string? C, string? T)>();
                await using (var cmd = new MySqlCommand(fetchPortals, conn, tx))
                {
                    cmd.Parameters.AddWithValue("@BoothId", boothId);
                    cmd.Parameters.AddWithValue("@SpaceId", spaceId);
                    await using var r = await cmd.ExecuteReaderAsync();
                    while (await r.ReadAsync())
                    {
                        portals.Add((
                            r.GetInt32("id"),
                            r.GetString("text_field_key"),
                            r.IsDBNull("corresponding_media_id") ? null : r.GetString("corresponding_media_id"),
                            r.IsDBNull("thumbnail_media_id") ? null : r.GetString("thumbnail_media_id")
                        ));
                    }
                }

                // 3. Delete each portal's i18n & portal
                foreach (var (pid, key, _, _) in portals)
                {
                    await using (var delI18n = new MySqlCommand(@"DELETE FROM i18n WHERE `key`=@K AND space_id=@S;", conn, tx))
                    {
                        delI18n.Parameters.AddWithValue("@K", key);
                        delI18n.Parameters.AddWithValue("@S", spaceId);
                        await delI18n.ExecuteNonQueryAsync();
                    }

                    await using (var delPortal = new MySqlCommand(@"DELETE FROM portal WHERE id=@P AND space_id=@S;", conn, tx))
                    {
                        delPortal.Parameters.AddWithValue("@P", pid);
                        delPortal.Parameters.AddWithValue("@S", spaceId);
                        await delPortal.ExecuteNonQueryAsync();
                    }
                }

                // 4. Delete media_localization & media
                foreach (var (_, _, corr, thumb) in portals)
                {
                    if (!string.IsNullOrEmpty(corr))
                    {
                        await using (var cmd = new MySqlCommand("DELETE FROM media_localization WHERE media_id=@M;", conn, tx))
                        {
                            cmd.Parameters.AddWithValue("@M", corr);
                            await cmd.ExecuteNonQueryAsync();
                        }
                        await using (var cmd = new MySqlCommand("DELETE FROM media WHERE id=@M;", conn, tx))
                        {
                            cmd.Parameters.AddWithValue("@M", corr);
                            await cmd.ExecuteNonQueryAsync();
                        }
                    }
                    if (!string.IsNullOrEmpty(thumb))
                    {
                        await using (var cmd = new MySqlCommand("DELETE FROM media_localization WHERE media_id=@M;", conn, tx))
                        {
                            cmd.Parameters.AddWithValue("@M", thumb);
                            await cmd.ExecuteNonQueryAsync();
                        }
                        await using (var cmd = new MySqlCommand("DELETE FROM media WHERE id=@M;", conn, tx))
                        {
                            cmd.Parameters.AddWithValue("@M", thumb);
                            await cmd.ExecuteNonQueryAsync();
                        }
                    }
                }

                // 5. Delete booth's i18n & booth row
                await using (var delBi18n = new MySqlCommand("DELETE FROM i18n WHERE `key`=@K AND space_id=@S;", conn, tx))
                {
                    delBi18n.Parameters.AddWithValue("@K", boothKey);
                    delBi18n.Parameters.AddWithValue("@S", spaceId);
                    await delBi18n.ExecuteNonQueryAsync();
                }
                await using (var delBooth = new MySqlCommand("DELETE FROM booth WHERE id=@B AND space_id=@S;", conn, tx))
                {
                    delBooth.Parameters.AddWithValue("@B", boothId);
                    delBooth.Parameters.AddWithValue("@S", spaceId);
                    await delBooth.ExecuteNonQueryAsync();
                }

                await tx.CommitAsync();
                return true;
            }
            catch (MySqlException)
            {
                await tx.RollbackAsync();
                throw;
            }
        }



    }
}
