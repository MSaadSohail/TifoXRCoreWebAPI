// <copyright file="BoothRepository.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Syed Hussain</author>
// <date>08/18/2025</date>
// <summary>Class to handle booth SQL side</summary>
using GMS.TifoXRCoreWebAPI.Models;
using GMS.TifoXRCoreWebAPI.Models.Common;
using GMS.TifoXRCoreWebAPI.Repositories.Interfaces;
//using GMS.TifoXRCoreWebAPI.Utilities.Logger.Interface;
using System.Data;
using System.Data.Common;
using TifoXRCoreWebAPI.Utilities.Infrastructure.Interface;

namespace GMS.TifoXRCoreWebAPI.Repositories
{
    public class BoothRepository : IBoothRepository
    {

        private readonly IDbProvider _db;
        //private readonly IAppLogger<BoothRepository>? _log;

        public BoothRepository(IConfiguration configuration, IDbProvider db /*IAppLogger<BoothRepository> log*/)
        {
            _db = db;
            //_log = log;
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
            ORDER BY b.id, i.locale_id;";

            await using var conn = await _db.OpenConnectionAsync();
            await using var cmd = _db.CreateCommand(conn, sql);
            cmd.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));

            var booths = new Dictionary<int, BoothModel>();

            await using var reader = await cmd.ExecuteReaderAsync();

            bool ordReady = false;
            int o_id = -1, o_space_id = -1, o_name_key = -1, o_map_spot_id = -1;
            int o_x = -1, o_y = -1, o_z = -1, o_locale_id = -1, o_value = -1;

            while (await reader.ReadAsync())
            {
                if (!ordReady)
                {
                    o_id = reader.GetOrdinal("id");
                    o_space_id = reader.GetOrdinal("space_id");
                    o_name_key = reader.GetOrdinal("name_key");
                    o_map_spot_id = reader.GetOrdinal("map_spot_id");
                    o_x = reader.GetOrdinal("x");
                    o_y = reader.GetOrdinal("y");
                    o_z = reader.GetOrdinal("z");
                    o_locale_id = reader.GetOrdinal("locale_id");
                    o_value = reader.GetOrdinal("value");
                    ordReady = true;
                }

                var id = reader.GetInt32(o_id);

                if (!booths.TryGetValue(id, out var booth))
                {
                    var hasCoords = !reader.IsDBNull(o_x); // x/y/z are null when no map_spot row
                    booth = new BoothModel
                    {
                        Id = id,
                        SpaceId = reader.GetInt32(o_space_id),
                        MapSpotId = reader.IsDBNull(o_map_spot_id) ? default : reader.GetInt32(o_map_spot_id),
                        MapSpot = hasCoords
                            ? new MapSpotModel
                            {
                                X = reader.IsDBNull(o_x) ? 0 : reader.GetDecimal(o_x),
                                Y = reader.IsDBNull(o_y) ? 0 : reader.GetDecimal(o_y),
                                Z = reader.IsDBNull(o_z) ? 0 : reader.GetDecimal(o_z)
                            }
                            : null,
                        LocalizedPairs = new LocalizedPairs
                        {
                            Key = reader.GetString(o_name_key),
                            Values = new List<LocalizedValue>()
                        }
                    };

                    booths[id] = booth;
                }

                var locId = reader.GetString(o_locale_id);
                var val = reader.GetString(o_value);

                // De-dupe per locale just in case
                if (!booth.LocalizedPairs.Values.Any(v => v.LocaleId == locId))
                {
                    booth.LocalizedPairs.Values.Add(new LocalizedValue
                    {
                        LocaleId = locId,
                        Value = val
                    });
                }
            }

            return booths.Values.ToList();
        }
        //===The following function is an example of how an AppLogger logging can be implemented in the method===
        /*public async Task<List<BoothModel>> GetAllBoothsBySpaceAsync(int spaceId)
        {
            const string SqlBoothsBySpace = "BoothsBySpace"; // stable query name for logs

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
    ORDER BY b.id, i.locale_id;";

            using (_log.WithProperties(("SpaceId", spaceId)))
            {
                var sw = System.Diagnostics.Stopwatch.StartNew();
                var booths = new Dictionary<int, BoothModel>();
                int rows = 0;

                try
                {
                    await using var conn = await _db.OpenConnectionAsync();
                    await using var cmd = _db.CreateCommand(conn, sql);
                    cmd.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));

                    await using var reader = await cmd.ExecuteReaderAsync();

                    bool ordReady = false;
                    int o_id = -1, o_space_id = -1, o_name_key = -1, o_map_spot_id = -1;
                    int o_x = -1, o_y = -1, o_z = -1, o_locale_id = -1, o_value = -1;

                    while (await reader.ReadAsync())
                    {
                        if (!ordReady)
                        {
                            o_id = reader.GetOrdinal("id");
                            o_space_id = reader.GetOrdinal("space_id");
                            o_name_key = reader.GetOrdinal("name_key");
                            o_map_spot_id = reader.GetOrdinal("map_spot_id");
                            o_x = reader.GetOrdinal("x");
                            o_y = reader.GetOrdinal("y");
                            o_z = reader.GetOrdinal("z");
                            o_locale_id = reader.GetOrdinal("locale_id");
                            o_value = reader.GetOrdinal("value");
                            ordReady = true;
                        }

                        var id = reader.GetInt32(o_id);
                        if (!booths.TryGetValue(id, out var booth))
                        {
                            var hasCoords = !reader.IsDBNull(o_x);
                            booth = new BoothModel
                            {
                                Id = id,
                                SpaceId = reader.GetInt32(o_space_id),
                                MapSpotId = reader.IsDBNull(o_map_spot_id) ? default : reader.GetInt32(o_map_spot_id),
                                MapSpot = hasCoords
                                    ? new MapSpotModel
                                    {
                                        X = reader.IsDBNull(o_x) ? 0 : reader.GetDecimal(o_x),
                                        Y = reader.IsDBNull(o_y) ? 0 : reader.GetDecimal(o_y),
                                        Z = reader.IsDBNull(o_z) ? 0 : reader.GetDecimal(o_z)
                                    }
                                    : null,
                                LocalizedPairs = new LocalizedPairs
                                {
                                    Key = reader.GetString(o_name_key),
                                    Values = new List<LocalizedValue>()
                                }
                            };
                            booths[id] = booth;
                        }

                        var locId = reader.GetString(o_locale_id);
                        var val = reader.GetString(o_value);

                        if (!booth.LocalizedPairs.Values.Any(v => v.LocaleId == locId))
                        {
                            booth.LocalizedPairs.Values.Add(new LocalizedValue
                            {
                                LocaleId = locId,
                                Value = val
                            });
                        }

                        rows++;
                    }

                    sw.Stop();

                    // Push a DEBUG summary (only if enabled) so prod stays quiet
                    if (_log.IsEnabled(LogLevel.Debug))
                    {
                        _log.Debug("DB query {Query} completed (ElapsedMs={ElapsedMs}, Rows={RowCount})",
                                   SqlBoothsBySpace, sw.ElapsedMilliseconds, rows);
                        // Optional: include parameters ONLY at Debug
                        _log.Debug("DB params {Params}", new { SpaceId = spaceId });
                    }

                    // Warn if the query is slow (tune threshold to your SLO)
                    if (sw.ElapsedMilliseconds > 500)
                    {
                        _log.Warn("Slow DB query {Query} (ElapsedMs={ElapsedMs}, Rows={RowCount})",
                                  SqlBoothsBySpace, sw.ElapsedMilliseconds, rows);
                    }

                    return booths.Values.ToList();
                }
                catch (DbException ex)
                {
                    sw.Stop();
                    // One structured error with key context; do NOT dump SQL/params at Error level
                    _log.Error(ex,
                        "DB failure executing {Query} (ElapsedMs={ElapsedMs})",
                        SqlBoothsBySpace, sw.ElapsedMilliseconds);

                    // (Optional) include params at Debug for forensics
                    if (_log.IsEnabled(LogLevel.Debug))
                        _log.Debug("DB params {Params}", new { SpaceId = spaceId });

                    throw; // let GlobalException produce the API error
                }
            }
        }*/


        public async Task<BoothModel?> UpdateBoothAsync(int spaceId, int boothId, BoothUpdateDto dto)
        {
            await using var conn = await _db.OpenConnectionAsync();
            await using var tx = await conn.BeginTransactionAsync();

            try
            {
                // 1) Validate map_spot exists
                const string validateSpotSql = @"SELECT COUNT(1) FROM map_spot WHERE id = @MapSpotId;";
                await using (var cmd = _db.CreateCommand(conn, validateSpotSql))
                {
                    cmd.Transaction = tx;
                    cmd.Parameters.Add(_db.CreateParameter("@MapSpotId", dto.MapSpotId));
                    var exists = Convert.ToInt32(await cmd.ExecuteScalarAsync()) > 0;
                    if (!exists)
                        throw new InvalidOperationException($"MapSpot with ID {dto.MapSpotId} does not exist.");
                }

                // 2) Update booth row
                const string updBooth = @"
                UPDATE booth
                   SET name_key = @NameKey,
                       map_spot_id = @MapSpotId
                 WHERE id = @BoothId
                   AND space_id = @SpaceId;";
                int affected;
                await using (var cmd = _db.CreateCommand(conn, updBooth))
                {
                    cmd.Transaction = tx;
                    cmd.Parameters.Add(_db.CreateParameter("@NameKey", dto.LocalizedPairs.Key));
                    cmd.Parameters.Add(_db.CreateParameter("@BoothId", boothId));
                    cmd.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));
                    cmd.Parameters.Add(_db.CreateParameter("@MapSpotId", dto.MapSpotId));
                    affected = await cmd.ExecuteNonQueryAsync();
                }
                if (affected == 0)
                {
                    await tx.RollbackAsync();
                    return null; // not found (or unchanged under MySQL's "0 rows affected" semantics)
                }

                // 3) Validate locales (skip if none provided)
                var allLocales = dto.LocalizedPairs?.Values?
                    .Select(v => v.LocaleId)
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList() ?? new List<string>();

                if (allLocales.Count > 0)
                {
                    const string checkSupportedLangSqlBase = @"
                    SELECT locale_id 
                      FROM supported_languages 
                     WHERE locale_id IN ({0}) AND space_id = @SpaceId;";

                    var paramNames = allLocales.Select((_, i) => $"@loc{i}").ToList();
                    var dynamicQuery = string.Format(checkSupportedLangSqlBase, string.Join(", ", paramNames));

                    var supported = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    await using (var checkCmd = _db.CreateCommand(conn, dynamicQuery))
                    {
                        checkCmd.Transaction = tx;
                        for (int i = 0; i < allLocales.Count; i++)
                            checkCmd.Parameters.Add(_db.CreateParameter(paramNames[i], allLocales[i]));
                        checkCmd.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));

                        await using var rdr = await checkCmd.ExecuteReaderAsync();
                        while (await rdr.ReadAsync())
                            supported.Add(rdr.IsDBNull(0) ? "" : rdr.GetString(0));
                    }

                    var unsupported = allLocales.Where(l => !supported.Contains(l)).ToList();
                    if (unsupported.Count > 0)
                        throw new InvalidOperationException($"Unsupported locales: {string.Join(", ", unsupported)}");
                }

                // 4) Upsert i18n for booth name_key
                const string updI18n = @"
                UPDATE i18n
                   SET value = @Value
                 WHERE `key` = @NameKey AND locale_id = @LocaleId AND space_id = @SpaceId;";
                const string insI18n = @"
                INSERT INTO i18n (`key`, locale_id, value, space_id)
                VALUES (@NameKey, @LocaleId, @Value, @SpaceId);";

                if (dto.LocalizedPairs?.Values != null)
                {
                    foreach (var loc in dto.LocalizedPairs.Values)
                    {
                        await using var cmdUp = _db.CreateCommand(conn, updI18n);
                        cmdUp.Transaction = tx;
                        cmdUp.Parameters.Add(_db.CreateParameter("@NameKey", dto.LocalizedPairs.Key));
                        cmdUp.Parameters.Add(_db.CreateParameter("@LocaleId", loc.LocaleId));
                        cmdUp.Parameters.Add(_db.CreateParameter("@Value", loc.Value ?? string.Empty));
                        cmdUp.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));

                        if (await cmdUp.ExecuteNonQueryAsync() == 0)
                        {
                            await using var cmdIn = _db.CreateCommand(conn, insI18n);
                            cmdIn.Transaction = tx;
                            cmdIn.Parameters.Add(_db.CreateParameter("@NameKey", dto.LocalizedPairs.Key));
                            cmdIn.Parameters.Add(_db.CreateParameter("@LocaleId", loc.LocaleId));
                            cmdIn.Parameters.Add(_db.CreateParameter("@Value", loc.Value ?? string.Empty));
                            cmdIn.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));
                            await cmdIn.ExecuteNonQueryAsync();
                        }
                    }
                }

                await tx.CommitAsync();

                // 5) Reload and return the updated booth
                return await LoadBoothByIdAsync(conn, spaceId, boothId);
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        private async Task<BoothModel?> LoadBoothByIdAsync(DbConnection conn, int spaceId, int boothId)
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
            WHERE b.space_id = @SpaceId AND b.id = @BoothId
            ORDER BY i.locale_id;";

            await using var cmd = _db.CreateCommand(conn, sql);
            cmd.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));
            cmd.Parameters.Add(_db.CreateParameter("@BoothId", boothId));

            await using var reader = await cmd.ExecuteReaderAsync();

            BoothModel? booth = null;
            var values = new List<LocalizedValue>();

            bool ordReady = false;
            int o_id = -1, o_space_id = -1, o_name_key = -1, o_map_spot_id = -1;
            int o_x = -1, o_y = -1, o_z = -1, o_locale_id = -1, o_value = -1;

            while (await reader.ReadAsync())
            {
                if (!ordReady)
                {
                    o_id = reader.GetOrdinal("id");
                    o_space_id = reader.GetOrdinal("space_id");
                    o_name_key = reader.GetOrdinal("name_key");
                    o_map_spot_id = reader.GetOrdinal("map_spot_id");
                    o_x = reader.GetOrdinal("x");
                    o_y = reader.GetOrdinal("y");
                    o_z = reader.GetOrdinal("z");
                    o_locale_id = reader.GetOrdinal("locale_id");
                    o_value = reader.GetOrdinal("value");
                    ordReady = true;
                }

                booth ??= new BoothModel
                {
                    Id = reader.GetInt32(o_id),
                    SpaceId = reader.GetInt32(o_space_id),
                    MapSpotId = reader.IsDBNull(o_map_spot_id) ? 0 : reader.GetInt32(o_map_spot_id),
                    MapSpot = reader.IsDBNull(o_x) ? null : new MapSpotModel
                    {
                        X = reader.IsDBNull(o_x) ? 0 : reader.GetDecimal(o_x),
                        Y = reader.IsDBNull(o_y) ? 0 : reader.GetDecimal(o_y),
                        Z = reader.IsDBNull(o_z) ? 0 : reader.GetDecimal(o_z),
                    },
                    LocalizedPairs = new LocalizedPairs
                    {
                        Key = reader.GetString(o_name_key),
                        Values = values
                    }
                };

                // accumulate locales
                var locId = reader.GetString(o_locale_id);
                if (!values.Any(v => v.LocaleId == locId))
                {
                    values.Add(new LocalizedValue
                    {
                        LocaleId = locId,
                        Value = reader.GetString(o_value)
                    });
                }
            }

            return booth;
        }


        public async Task<BoothModel> CreateBoothAsync(int spaceId, BoothCreateDto boothDto)
        {
            await using var conn = await _db.OpenConnectionAsync();
            await using var tx = await conn.BeginTransactionAsync();

            try
            {
                // 1) Validate that the map_spot exists and load its coords
                const string validateMapSpotSql = @"SELECT x, y, z FROM map_spot WHERE id = @MapSpotId;";
                MapSpotModel? mapSpot = null;

                await using (var cmd = _db.CreateCommand(conn, validateMapSpotSql))
                {
                    cmd.Transaction = tx;
                    cmd.Parameters.Add(_db.CreateParameter("@MapSpotId", boothDto.MapSpotId));

                    await using var reader = await cmd.ExecuteReaderAsync();
                    if (!await reader.ReadAsync())
                        throw new InvalidOperationException($"MapSpot with ID {boothDto.MapSpotId} does not exist.");

                    var oX = reader.GetOrdinal("x");
                    var oY = reader.GetOrdinal("y");
                    var oZ = reader.GetOrdinal("z");

                    mapSpot = new MapSpotModel
                    {
                        X = reader.IsDBNull(oX) ? 0 : reader.GetDecimal(oX),
                        Y = reader.IsDBNull(oY) ? 0 : reader.GetDecimal(oY),
                        Z = reader.IsDBNull(oZ) ? 0 : reader.GetDecimal(oZ)
                    };
                }

                // 2) Insert booth (references existing map_spot)
                const string insertBoothSql = @"
INSERT INTO booth (space_id, name_key, map_spot_id)
VALUES (@SpaceId, @NameKey, @MapSpotId);";

                await using (var cmd = _db.CreateCommand(conn, insertBoothSql))
                {
                    cmd.Transaction = tx;
                    cmd.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));
                    cmd.Parameters.Add(_db.CreateParameter("@NameKey", boothDto.LocalizedPairs.Key));
                    cmd.Parameters.Add(_db.CreateParameter("@MapSpotId", boothDto.MapSpotId));
                    await cmd.ExecuteNonQueryAsync();
                }

                int newBoothId;
                await using (var idCmd = _db.CreateCommand(conn, "SELECT LAST_INSERT_ID();"))
                {
                    idCmd.Transaction = tx;
                    newBoothId = Convert.ToInt32(await idCmd.ExecuteScalarAsync());
                }

                // 3) Insert i18n for supported locales only (batch validate)
                var insertedValues = new List<LocalizedValue>();
                var values = boothDto.LocalizedPairs?.Values ?? new List<LocalizedValue>();
                var allLocales = values
                    .Select(v => v.LocaleId)
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                var supported = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                if (allLocales.Count > 0)
                {
                    var paramNames = allLocales.Select((_, i) => $"@loc{i}").ToList();
                    var checkSupportedLangSql = $@"
                    SELECT locale_id
                      FROM supported_languages
                     WHERE locale_id IN ({string.Join(", ", paramNames)})
                       AND space_id = @SpaceId;";

                    await using (var checkCmd = _db.CreateCommand(conn, checkSupportedLangSql))
                    {
                        checkCmd.Transaction = tx;
                        for (int i = 0; i < allLocales.Count; i++)
                            checkCmd.Parameters.Add(_db.CreateParameter(paramNames[i], allLocales[i]));
                        checkCmd.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));

                        await using var rdr = await checkCmd.ExecuteReaderAsync();
                        while (await rdr.ReadAsync())
                            supported.Add(rdr.IsDBNull(0) ? "" : rdr.GetString(0));
                    }
                }

                const string insertI18nSql = @"
INSERT INTO i18n (`key`, locale_id, value, space_id)
VALUES (@NameKey, @LocaleId, @Value, @SpaceId);";

                foreach (var val in values)
                {
                    if (!supported.Contains(val.LocaleId)) continue;

                    await using var cmdI18n = _db.CreateCommand(conn, insertI18nSql);
                    cmdI18n.Transaction = tx;
                    cmdI18n.Parameters.Add(_db.CreateParameter("@NameKey", boothDto.LocalizedPairs.Key));
                    cmdI18n.Parameters.Add(_db.CreateParameter("@LocaleId", val.LocaleId));
                    cmdI18n.Parameters.Add(_db.CreateParameter("@Value", val.Value ?? string.Empty));
                    cmdI18n.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));
                    await cmdI18n.ExecuteNonQueryAsync();

                    insertedValues.Add(new LocalizedValue { LocaleId = val.LocaleId, Value = val.Value });
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
            await using var conn = await _db.OpenConnectionAsync();
            await using var tx = await conn.BeginTransactionAsync();

            try
            {
                // 1) Fetch booth name_key
                const string fetchBooth = @"
            SELECT b.name_key
            FROM booth b
            WHERE b.id = @BoothId AND b.space_id = @SpaceId;";
                string? boothKey;

                await using (var cmd = _db.CreateCommand(conn, fetchBooth))
                {
                    cmd.Transaction = tx;
                    cmd.Parameters.Add(_db.CreateParameter("@BoothId", boothId));
                    cmd.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));
                    var o = await cmd.ExecuteScalarAsync();
                    if (o == null || o == DBNull.Value)
                    {
                        await tx.RollbackAsync();
                        return false;
                    }
                    boothKey = Convert.ToString(o);
                }

                // 2) Fetch portals for this booth
                const string fetchPortals = @"
            SELECT p.id, p.text_field_key, p.corresponding_media_id, p.thumbnail_media_id
            FROM portal p
            WHERE p.booth_id = @BoothId AND p.space_id = @SpaceId;";

                var portals = new List<(int Id, string Key, string? C, string? T)>();
                await using (var cmd = _db.CreateCommand(conn, fetchPortals))
                {
                    cmd.Transaction = tx;
                    cmd.Parameters.Add(_db.CreateParameter("@BoothId", boothId));
                    cmd.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));

                    await using var r = await cmd.ExecuteReaderAsync();

                    int o_id = -1, o_key = -1, o_c = -1, o_t = -1;
                    bool ordReady = false;

                    while (await r.ReadAsync())
                    {
                        if (!ordReady)
                        {
                            o_id = r.GetOrdinal("id");
                            o_key = r.GetOrdinal("text_field_key");
                            o_c = r.GetOrdinal("corresponding_media_id");
                            o_t = r.GetOrdinal("thumbnail_media_id");
                            ordReady = true;
                        }

                        portals.Add((
                            r.GetInt32(o_id),
                            r.GetString(o_key),
                            r.IsDBNull(o_c) ? null : r.GetString(o_c),
                            r.IsDBNull(o_t) ? null : r.GetString(o_t)
                        ));
                    }
                }

                // 3) Delete each portal's i18n and the portal row
                foreach (var (pid, key, _, _) in portals)
                {
                    const string delI18nSql = @"DELETE FROM i18n   WHERE `key` = @K AND space_id = @S;";
                    const string delPortalSql = @"DELETE FROM portal WHERE id = @P AND space_id = @S;";

                    await using (var delI18n = _db.CreateCommand(conn, delI18nSql))
                    {
                        delI18n.Transaction = tx;
                        delI18n.Parameters.Add(_db.CreateParameter("@K", key));
                        delI18n.Parameters.Add(_db.CreateParameter("@S", spaceId));
                        await delI18n.ExecuteNonQueryAsync();
                    }

                    await using (var delPortal = _db.CreateCommand(conn, delPortalSql))
                    {
                        delPortal.Transaction = tx;
                        delPortal.Parameters.Add(_db.CreateParameter("@P", pid));
                        delPortal.Parameters.Add(_db.CreateParameter("@S", spaceId));
                        await delPortal.ExecuteNonQueryAsync();
                    }
                }

                // 4) Delete media_localization & media for corresponding/thumbnail media
                const string delMediaLocSql = @"DELETE FROM media_localization WHERE media_id = @M;";
                const string delMediaSql = @"DELETE FROM media               WHERE id       = @M;";

                foreach (var (_, _, corr, thumb) in portals)
                {
                    if (!string.IsNullOrWhiteSpace(corr))
                    {
                        await using (var cmd = _db.CreateCommand(conn, delMediaLocSql))
                        {
                            cmd.Transaction = tx;
                            cmd.Parameters.Add(_db.CreateParameter("@M", corr!));
                            await cmd.ExecuteNonQueryAsync();
                        }
                        await using (var cmd = _db.CreateCommand(conn, delMediaSql))
                        {
                            cmd.Transaction = tx;
                            cmd.Parameters.Add(_db.CreateParameter("@M", corr!));
                            await cmd.ExecuteNonQueryAsync();
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(thumb))
                    {
                        await using (var cmd = _db.CreateCommand(conn, delMediaLocSql))
                        {
                            cmd.Transaction = tx;
                            cmd.Parameters.Add(_db.CreateParameter("@M", thumb!));
                            await cmd.ExecuteNonQueryAsync();
                        }
                        await using (var cmd = _db.CreateCommand(conn, delMediaSql))
                        {
                            cmd.Transaction = tx;
                            cmd.Parameters.Add(_db.CreateParameter("@M", thumb!));
                            await cmd.ExecuteNonQueryAsync();
                        }
                    }
                }

                // 5) Delete booth's i18n & booth
                const string delBi18nSql = @"DELETE FROM i18n  WHERE `key` = @K AND space_id = @S;";
                const string delBoothSql = @"DELETE FROM booth WHERE id = @B AND space_id = @S;";

                await using (var delBi18n = _db.CreateCommand(conn, delBi18nSql))
                {
                    delBi18n.Transaction = tx;
                    delBi18n.Parameters.Add(_db.CreateParameter("@K", boothKey!));
                    delBi18n.Parameters.Add(_db.CreateParameter("@S", spaceId));
                    await delBi18n.ExecuteNonQueryAsync();
                }

                await using (var delBooth = _db.CreateCommand(conn, delBoothSql))
                {
                    delBooth.Transaction = tx;
                    delBooth.Parameters.Add(_db.CreateParameter("@B", boothId));
                    delBooth.Parameters.Add(_db.CreateParameter("@S", spaceId));
                    await delBooth.ExecuteNonQueryAsync();
                }

                await tx.CommitAsync();
                return true;
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

    }
}
