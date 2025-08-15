// <copyright file="BoothRepository.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Syed Hussain</author>
// <date>07/28/2025</date>
// <summary>Class to handle booth SQL side</summary>
using GMS.TifoXRCoreWebAPI.Models;
using GMS.TifoXRCoreWebAPI.Models.Common;
using GMS.TifoXRCoreWebAPI.Repositories.Interfaces;
using MySqlConnector;
using System.Data;

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
        b.map_spot_id,
        ms.x,
        ms.y,
        ms.z,
        i.locale_id,
        i.value
    FROM booth b
    INNER JOIN i18n i
        ON i.`key` = b.name_key
       AND i.space_id = b.space_id
    LEFT JOIN map_spot ms
        ON b.map_spot_id = ms.id
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
                        MapSpotId = reader.GetInt32("map_spot_id"),
                        MapSpot = reader.IsDBNull("x") ? null : new MapSpotModel
                        {
                            X = reader.GetDecimal("x"),
                            Y = reader.GetDecimal("y"),
                            Z = reader.GetDecimal("z")
                        },
                        LocalizedPairs = new LocalizedPairs
                        {
                            Key = reader.GetString("name_key"),
                            Values = new List<LocalizedValue>()
                        }
                    };
                    dict[id] = booth;
                }

                booth.LocalizedPairs.Values.Add(new LocalizedValue
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
                // 1) Validate that the new map_spot_id exists
                const string validateSpotSql = @"SELECT COUNT(1) FROM map_spot WHERE id = @MapSpotId;";
                await using (var cmd = new MySqlCommand(validateSpotSql, conn, tx))
                {
                    cmd.Parameters.AddWithValue("@MapSpotId", dto.MapSpotId);
                    var exists = Convert.ToInt32(await cmd.ExecuteScalarAsync()) > 0;
                    if (!exists)
                        throw new InvalidOperationException($"MapSpot with ID {dto.MapSpotId} does not exist.");
                }

                // 2) Update booth: name_key + map_spot_id
                const string updBooth = @"
        UPDATE booth
           SET name_key = @NameKey,
               map_spot_id = @MapSpotId
         WHERE id = @BoothId
           AND space_id = @SpaceId;";
                await using (var cmd = new MySqlCommand(updBooth, conn, tx))
                {
                    cmd.Parameters.AddWithValue("@NameKey", dto.LocalizedPairs.Key);
                    cmd.Parameters.AddWithValue("@BoothId", boothId);
                    cmd.Parameters.AddWithValue("@SpaceId", spaceId);
                    cmd.Parameters.AddWithValue("@MapSpotId", dto.MapSpotId);
                    if (await cmd.ExecuteNonQueryAsync() == 0)
                        return null;
                }

                // 3) Validate locales
                const string checkSupportedLangSql = @"
        SELECT locale_id FROM supported_languages 
        WHERE locale_id IN ({0}) AND space_id = @SpaceId;";
                var allLocales = dto.LocalizedPairs.Values.Select(v => v.LocaleId).Distinct().ToList();
                var parameterNames = allLocales.Select((l, i) => $"@loc{i}").ToList();
                var localeParamMap = allLocales.Zip(parameterNames, (val, param) => new { val, param }).ToList();
                var dynamicQuery = string.Format(checkSupportedLangSql, string.Join(", ", parameterNames));

                await using (var checkCmd = new MySqlCommand(dynamicQuery, conn, tx))
                {
                    foreach (var pair in localeParamMap)
                        checkCmd.Parameters.AddWithValue(pair.param, pair.val);

                    checkCmd.Parameters.AddWithValue("@SpaceId", spaceId);

                    var supportedLocales = new HashSet<string>();
                    await using var reader = await checkCmd.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                        supportedLocales.Add(reader.GetString("locale_id"));

                    var unsupportedLocales = allLocales.Where(l => !supportedLocales.Contains(l)).ToList();
                    if (unsupportedLocales.Any())
                        throw new InvalidOperationException($"Unsupported locales: {string.Join(", ", unsupportedLocales)}");
                }

                // 4) Upsert i18n
                const string updI18n = @"
        UPDATE i18n
           SET value = @Value
         WHERE `key` = @NameKey AND locale_id = @LocaleId AND space_id = @SpaceId;";
                const string insI18n = @"
        INSERT INTO i18n (`key`, locale_id, value, space_id)
        VALUES (@NameKey, @LocaleId, @Value, @SpaceId);";

                foreach (var loc in dto.LocalizedPairs.Values)
                {
                    await using var cmdUp = new MySqlCommand(updI18n, conn, tx);
                    cmdUp.Parameters.AddWithValue("@NameKey", dto.LocalizedPairs.Key);
                    cmdUp.Parameters.AddWithValue("@LocaleId", loc.LocaleId);
                    cmdUp.Parameters.AddWithValue("@Value", loc.Value);
                    cmdUp.Parameters.AddWithValue("@SpaceId", spaceId);

                    if (await cmdUp.ExecuteNonQueryAsync() == 0)
                    {
                        await using var cmdIn = new MySqlCommand(insI18n, conn, tx);
                        cmdIn.Parameters.AddWithValue("@NameKey", dto.LocalizedPairs.Key);
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
                SELECT 
                    b.id, 
                    b.space_id, 
                    b.name_key, 
                    b.map_spot_id,
                    ms.x,
                    ms.y,
                    ms.z,
                    i.locale_id, 
                    i.value
                FROM booth b
                INNER JOIN map_spot ms 
                    ON b.map_spot_id = ms.id
                LEFT JOIN i18n i 
                    ON i.`key` = b.name_key AND i.space_id = b.space_id
                LEFT JOIN supported_languages sl
                    ON sl.locale_id = i.locale_id AND sl.space_id = b.space_id
                WHERE b.id = @BoothId AND b.space_id = @SpaceId
                  AND (i.locale_id IS NULL OR sl.locale_id IS NOT NULL)
                ORDER BY i.locale_id;
                ";

            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@BoothId", boothId);
            cmd.Parameters.AddWithValue("@SpaceId", spaceId);

            await using var reader = await cmd.ExecuteReaderAsync();
            BoothModel? booth = null;

            while (await reader.ReadAsync())
            {
                if (booth == null)
                {
                    booth = new BoothModel
                    {
                        Id = reader.GetInt32("id"),
                        SpaceId = reader.GetInt32("space_id"),
                        MapSpotId = reader.GetInt32("map_spot_id"),
                        MapSpot = new MapSpotModel
                        {
                            X = reader.GetDecimal("x"),
                            Y = reader.GetDecimal("y"),
                            Z = reader.GetDecimal("z")
                        },
                        LocalizedPairs = new LocalizedPairs
                        {
                            Key = reader.GetString("name_key"),
                            Values = new List<LocalizedValue>()
                        }
                    };
                }

                if (!reader.IsDBNull("locale_id") && !reader.IsDBNull("value"))
                {
                    booth.LocalizedPairs.Values.Add(new LocalizedValue
                    {
                        LocaleId = reader.GetString("locale_id"),
                        Value = reader.GetString("value")
                    });
                }
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
                // 1) Validate that the map_spot_id exists
                const string validateMapSpotSql = @"SELECT id, x, y, z FROM map_spot WHERE id = @MapSpotId;";
                MapSpotModel? mapSpot = null;

                await using (var cmd = new MySqlCommand(validateMapSpotSql, conn, tx))
                {
                    cmd.Parameters.AddWithValue("@MapSpotId", boothDto.MapSpotId);
                    await using var reader = await cmd.ExecuteReaderAsync();

                    if (!await reader.ReadAsync())
                        throw new InvalidOperationException($"MapSpot with ID {boothDto.MapSpotId} does not exist.");

                    mapSpot = new MapSpotModel
                    {
                        X = reader.GetDecimal("x"),
                        Y = reader.GetDecimal("y"),
                        Z = reader.GetDecimal("z")
                    };
                }

                // 2) Insert into booth with existing map_spot_id
                const string insertBoothSql = @"
        INSERT INTO booth (space_id, name_key, map_spot_id)
        VALUES (@SpaceId, @NameKey, @MapSpotId);";

                int newBoothId;
                await using (var cmd = new MySqlCommand(insertBoothSql, conn, tx))
                {
                    cmd.Parameters.AddWithValue("@SpaceId", spaceId);
                    cmd.Parameters.AddWithValue("@NameKey", boothDto.LocalizedPairs.Key);
                    cmd.Parameters.AddWithValue("@MapSpotId", boothDto.MapSpotId);
                    await cmd.ExecuteNonQueryAsync();
                    newBoothId = Convert.ToInt32(cmd.LastInsertedId);
                }

                // 3) Insert into i18n only for supported locales
                const string insertI18nSql = @"
        INSERT INTO i18n (`key`, locale_id, value, space_id)
        VALUES (@NameKey, @LocaleId, @Value, @SpaceId);";

                const string checkSupportedLangSql = @"
        SELECT 1
        FROM supported_languages 
        WHERE locale_id = @LocaleId AND space_id = @SpaceId
        LIMIT 1;";

                var insertedValues = new List<LocalizedValue>();

                foreach (var val in boothDto.LocalizedPairs.Values)
                {
                    bool isSupported = false;

                    await using (var checkCmd = new MySqlCommand(checkSupportedLangSql, conn, tx))
                    {
                        checkCmd.Parameters.AddWithValue("@LocaleId", val.LocaleId);
                        checkCmd.Parameters.AddWithValue("@SpaceId", spaceId);
                        var result = await checkCmd.ExecuteScalarAsync();
                        isSupported = result != null;
                    }

                    if (!isSupported)
                        continue;

                    await using var cmdI18n = new MySqlCommand(insertI18nSql, conn, tx);
                    cmdI18n.Parameters.AddWithValue("@NameKey", boothDto.LocalizedPairs.Key);
                    cmdI18n.Parameters.AddWithValue("@LocaleId", val.LocaleId);
                    cmdI18n.Parameters.AddWithValue("@Value", val.Value);
                    cmdI18n.Parameters.AddWithValue("@SpaceId", spaceId);
                    await cmdI18n.ExecuteNonQueryAsync();

                    insertedValues.Add(val);
                }

                await tx.CommitAsync();

                return new BoothModel
                {
                    Id = newBoothId,
                    SpaceId = spaceId,
                    MapSpotId = boothDto.MapSpotId,
                    MapSpot = mapSpot!,
                    LocalizedPairs = new LocalizedPairs
                    {
                        Key = boothDto.LocalizedPairs.Key,
                        Values = insertedValues
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
