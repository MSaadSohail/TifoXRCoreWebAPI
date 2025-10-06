// <copyright file="SpaceRepository.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Syed Hussain</author>
// <date>07/28/2025</date>
// <summary>Class to handle space SQL side</summary>

using MySqlConnector;
//
using GMS.TifoXRCoreWebAPI.Models;
using GMS.TifoXRCoreWebAPI.Models.Common;
using GMS.TifoXRCoreWebAPI.Repositories.Interfaces;
using GMS.TifoXRCoreWebAPI.Utilities.Infrastructure;
namespace GMS.TifoXRCoreWebAPI.Repositories
{
    public class SpaceRepository : ISpaceRepository
    {
        private readonly string _connStr;
        private readonly IDbProvider _db;
        public SpaceRepository(IConfiguration cfg, IDbProvider db)
        { 
            _connStr = cfg.GetConnectionString("DefaultConnection");
            _db = db;

        }
        public async Task<SpaceData?> GetSpaceByIdAsync(int id)
        {
            const string sql = @"
        SELECT 
            s.id,
            s.platform_type_id,
            s.entity_id,
            s.sku,
            s.link,
            s.is_published,
            s.is_live,
            s.description_key,
            s.creation_time,
            s.modified_time,
            s.modified_by,
            i.locale_id,
            i.value
        FROM space s
        LEFT JOIN i18n i 
            ON i.`key` = s.description_key 
           AND i.space_id = s.id
        WHERE s.id = @Id
        ORDER BY i.locale_id;";

            await using var conn = await _db.OpenConnectionAsync();
            await using var cmd = _db.CreateCommand(conn, sql);
            cmd.Parameters.Add(_db.CreateParameter("@Id", id));

            await using var reader = await cmd.ExecuteReaderAsync();

            SpaceData? space = null;
            var descs = new List<LocalizedValue>();

            bool ordReady = false;
            int o_id = -1, o_platform_type_id = -1, o_entity_id = -1, o_sku = -1, o_link = -1, o_is_published = -1, o_is_live = -1;
            int o_description_key = -1, o_creation_time = -1, o_modified_time = -1, o_modified_by = -1, o_locale_id = -1, o_value = -1;

            while (await reader.ReadAsync())
            {
                if (!ordReady)
                {
                    o_id = reader.GetOrdinal("id");
                    o_platform_type_id = reader.GetOrdinal("platform_type_id");
                    o_entity_id = reader.GetOrdinal("entity_id");
                    o_sku = reader.GetOrdinal("sku");
                    o_link = reader.GetOrdinal("link");
                    o_is_published = reader.GetOrdinal("is_published");
                    o_is_live = reader.GetOrdinal("is_live");
                    o_description_key = reader.GetOrdinal("description_key");
                    o_creation_time = reader.GetOrdinal("creation_time");
                    o_modified_time = reader.GetOrdinal("modified_time");
                    o_modified_by = reader.GetOrdinal("modified_by");
                    o_locale_id = reader.GetOrdinal("locale_id");
                    o_value = reader.GetOrdinal("value");
                    ordReady = true;
                }

                if (space == null)
                {
                    space = new SpaceData
                    {
                        Id = reader.GetInt32(o_id),
                        PlatformTypeId = reader.GetInt32(o_platform_type_id),
                        EntityId = reader.GetInt32(o_entity_id),
                        Sku = reader.IsDBNull(o_sku) ? null : reader.GetString(o_sku),
                        Link = reader.IsDBNull(o_link) ? null : reader.GetString(o_link),
                        IsPublished = reader.GetBoolean(o_is_published),
                        IsLive = reader.GetBoolean(o_is_live),
                        CreationTime = reader.GetDateTime(o_creation_time),
                        ModifiedTime = reader.GetDateTime(o_modified_time),
                        ModifiedBy = reader.GetString(o_modified_by),
                        LocalizedDescription = new LocalizedPairs
                        {
                            Key = reader.IsDBNull(o_description_key) ? null : reader.GetString(o_description_key),
                            Values = descs
                        }
                    };
                }

                if (!reader.IsDBNull(o_locale_id))
                {
                    var locale = reader.GetString(o_locale_id);
                    // prevent accidental duplicates per locale
                    if (!descs.Any(d => d.LocaleId == locale))
                    {
                        descs.Add(new LocalizedValue
                        {
                            LocaleId = locale,
                            Value = reader.IsDBNull(o_value) ? null : reader.GetString(o_value)
                        });
                    }
                }
            }

            return space;
        }

        public async Task<SpaceData> CreateSpaceAsync(Space spaceDto)
        {
            await using var conn = await _db.OpenConnectionAsync();
            await using var tx = await conn.BeginTransactionAsync();

            try
            {
                const string insertSql = @"
            INSERT INTO space (
                platform_type_id, entity_id, sku, link,
                is_published, is_live, description_key,
                creation_time, modified_time, modified_by
            )
            VALUES (
                @PlatformTypeId, @EntityId, @Sku, @Link,
                @IsPublished, @IsLive, @DescriptionKey,
                @CreationTime, @ModifiedTime, @ModifiedBy
            );";

                // 1) Insert space
                await using (var cmd = _db.CreateCommand(conn, insertSql))
                {
                    cmd.Transaction = tx;
                    cmd.Parameters.Add(_db.CreateParameter("@PlatformTypeId", spaceDto.PlatformTypeId));
                    cmd.Parameters.Add(_db.CreateParameter("@EntityId", spaceDto.EntityId));
                    cmd.Parameters.Add(_db.CreateParameter("@Sku", (object?)spaceDto.Sku ?? DBNull.Value));
                    cmd.Parameters.Add(_db.CreateParameter("@Link", (object?)spaceDto.Link ?? DBNull.Value));
                    cmd.Parameters.Add(_db.CreateParameter("@IsPublished", spaceDto.IsPublished));
                    cmd.Parameters.Add(_db.CreateParameter("@IsLive", spaceDto.IsLive));
                    cmd.Parameters.Add(_db.CreateParameter("@DescriptionKey", spaceDto.LocalizedDescription.Key));
                    cmd.Parameters.Add(_db.CreateParameter("@CreationTime", DateTime.UtcNow));
                    cmd.Parameters.Add(_db.CreateParameter("@ModifiedTime", DateTime.UtcNow));
                    cmd.Parameters.Add(_db.CreateParameter("@ModifiedBy", string.IsNullOrWhiteSpace(spaceDto.ModifiedBy) ? "system" : spaceDto.ModifiedBy!));
                    await cmd.ExecuteNonQueryAsync();
                }

                // 2) Get new id
                int newId;
                await using (var idCmd = _db.CreateCommand(conn, "SELECT LAST_INSERT_ID();"))
                {
                    idCmd.Transaction = tx;
                    newId = Convert.ToInt32(await idCmd.ExecuteScalarAsync());
                }

                // 3) Insert localized descriptions
                const string insertI18n = @"
            INSERT INTO i18n (`key`, locale_id, value, space_id)
            VALUES (@Key, @LocaleId, @Value, @SpaceId);";

                if (spaceDto.LocalizedDescription?.Values != null)
                {
                    foreach (var loc in spaceDto.LocalizedDescription.Values)
                    {
                        await using var i18nCmd = _db.CreateCommand(conn, insertI18n);
                        i18nCmd.Transaction = tx;
                        i18nCmd.Parameters.Add(_db.CreateParameter("@Key", spaceDto.LocalizedDescription.Key));
                        i18nCmd.Parameters.Add(_db.CreateParameter("@LocaleId", loc.LocaleId));
                        i18nCmd.Parameters.Add(_db.CreateParameter("@Value", (object?)loc.Value ?? DBNull.Value));
                        i18nCmd.Parameters.Add(_db.CreateParameter("@SpaceId", newId));
                        await i18nCmd.ExecuteNonQueryAsync();
                    }
                }

                await tx.CommitAsync();

                // 4) Return the created space
                return (await GetSpaceByIdAsync(newId))!;
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }


        public async Task<SpaceData?> UpdateSpaceAsync(int id, Space spaceDto)
        {
            await using var conn = await _db.OpenConnectionAsync();
            await using var tx = await conn.BeginTransactionAsync();

            try
            {
                const string updateSql = @"
UPDATE space
   SET platform_type_id = @PlatformTypeId,
       entity_id        = @EntityId,
       sku              = @Sku,
       link             = @Link,
       is_published     = @IsPublished,
       is_live          = @IsLive,
       description_key  = @DescriptionKey,
       modified_time    = @ModifiedTime,
       modified_by      = @ModifiedBy
 WHERE id = @Id;";

                int affected;
                await using (var cmd = _db.CreateCommand(conn, updateSql))
                {
                    cmd.Transaction = tx;
                    cmd.Parameters.Add(_db.CreateParameter("@Id", id));
                    cmd.Parameters.Add(_db.CreateParameter("@PlatformTypeId", spaceDto.PlatformTypeId));
                    cmd.Parameters.Add(_db.CreateParameter("@EntityId", spaceDto.EntityId));
                    cmd.Parameters.Add(_db.CreateParameter("@Sku", (object?)spaceDto.Sku ?? DBNull.Value));
                    cmd.Parameters.Add(_db.CreateParameter("@Link", (object?)spaceDto.Link ?? DBNull.Value));
                    cmd.Parameters.Add(_db.CreateParameter("@IsPublished", spaceDto.IsPublished));
                    cmd.Parameters.Add(_db.CreateParameter("@IsLive", spaceDto.IsLive));
                    cmd.Parameters.Add(_db.CreateParameter("@DescriptionKey", spaceDto.LocalizedDescription.Key));
                    cmd.Parameters.Add(_db.CreateParameter("@ModifiedTime", DateTime.UtcNow));
                    cmd.Parameters.Add(_db.CreateParameter("@ModifiedBy", string.IsNullOrWhiteSpace(spaceDto.ModifiedBy) ? "system" : spaceDto.ModifiedBy!));

                    affected = await cmd.ExecuteNonQueryAsync();
                }

                // If no rows were updated, treat as not found (rollback for cleanliness).
                if (affected == 0)
                {
                    await tx.RollbackAsync();
                    return null;
                }

                const string updateI18n = @"
UPDATE i18n
   SET value = @Value
 WHERE `key` = @Key AND locale_id = @LocaleId AND space_id = @SpaceId;";
                const string insertI18n = @"
INSERT INTO i18n (`key`, locale_id, value, space_id)
VALUES (@Key, @LocaleId, @Value, @SpaceId);";

                if (spaceDto.LocalizedDescription?.Values != null)
                {
                    foreach (var loc in spaceDto.LocalizedDescription.Values)
                    {
                        // UPDATE first
                        int upd;
                        await using (var u = _db.CreateCommand(conn, updateI18n))
                        {
                            u.Transaction = tx;
                            u.Parameters.Add(_db.CreateParameter("@Key", spaceDto.LocalizedDescription.Key));
                            u.Parameters.Add(_db.CreateParameter("@LocaleId", loc.LocaleId));
                            u.Parameters.Add(_db.CreateParameter("@Value", (object?)loc.Value ?? DBNull.Value));
                            u.Parameters.Add(_db.CreateParameter("@SpaceId", id));
                            upd = await u.ExecuteNonQueryAsync();
                        }

                        // If nothing updated, INSERT
                        if (upd == 0)
                        {
                            await using var ins = _db.CreateCommand(conn, insertI18n);
                            ins.Transaction = tx;
                            ins.Parameters.Add(_db.CreateParameter("@Key", spaceDto.LocalizedDescription.Key));
                            ins.Parameters.Add(_db.CreateParameter("@LocaleId", loc.LocaleId));
                            ins.Parameters.Add(_db.CreateParameter("@Value", (object?)loc.Value ?? DBNull.Value));
                            ins.Parameters.Add(_db.CreateParameter("@SpaceId", id));
                            await ins.ExecuteNonQueryAsync();
                        }
                    }
                }

                await tx.CommitAsync();
                return await GetSpaceByIdAsync(id);
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        public async Task<bool> DeleteSpaceAsync(int id)
        {
            await using var conn = await _db.OpenConnectionAsync();
            await using var tx = await conn.BeginTransactionAsync();

            try
            {
                // 0) Confirm space exists
                const string existsSql = "SELECT 1 FROM space WHERE id = @Id;";
                await using (var ex = _db.CreateCommand(conn, existsSql))
                {
                    ex.Transaction = tx;
                    ex.Parameters.Add(_db.CreateParameter("@Id", id));
                    var exists = await ex.ExecuteScalarAsync();
                    if (exists == null)
                    {
                        await tx.RollbackAsync();
                        return false;
                    }
                }

                // 1) Delete portals
                const string delPortals = @"DELETE FROM portal WHERE space_id = @Id;";
                await using (var dp = _db.CreateCommand(conn, delPortals))
                {
                    dp.Transaction = tx;
                    dp.Parameters.Add(_db.CreateParameter("@Id", id));
                    await dp.ExecuteNonQueryAsync();
                }

                // 2) Delete teleport table buttons then tables
                const string delTtb = @"
DELETE ttb
  FROM teleport_table_button ttb
  JOIN teleport_table tt ON tt.id = ttb.table_id
 WHERE tt.space_id = @Id;";
                await using (var dttb = _db.CreateCommand(conn, delTtb))
                {
                    dttb.Transaction = tx;
                    dttb.Parameters.Add(_db.CreateParameter("@Id", id));
                    await dttb.ExecuteNonQueryAsync();
                }

                const string delTt = @"DELETE FROM teleport_table WHERE space_id = @Id;";
                await using (var dtt = _db.CreateCommand(conn, delTt))
                {
                    dtt.Transaction = tx;
                    dtt.Parameters.Add(_db.CreateParameter("@Id", id));
                    await dtt.ExecuteNonQueryAsync();
                }

                // 3) Delete booths
                const string delBooths = @"DELETE FROM booth WHERE space_id = @Id;";
                await using (var dbh = _db.CreateCommand(conn, delBooths))
                {
                    dbh.Transaction = tx;
                    dbh.Parameters.Add(_db.CreateParameter("@Id", id));
                    await dbh.ExecuteNonQueryAsync();
                }

                // 4) Delete media (+ localizations) belonging to this space
                const string delMediaLoc = @"
DELETE ml
  FROM media_localization ml
  JOIN media m ON m.id = ml.media_id
 WHERE m.space_id = @Id;";
                await using (var dml = _db.CreateCommand(conn, delMediaLoc))
                {
                    dml.Transaction = tx;
                    dml.Parameters.Add(_db.CreateParameter("@Id", id));
                    await dml.ExecuteNonQueryAsync();
                }

                const string delMedia = @"DELETE FROM media WHERE space_id = @Id;";
                await using (var dm = _db.CreateCommand(conn, delMedia))
                {
                    dm.Transaction = tx;
                    dm.Parameters.Add(_db.CreateParameter("@Id", id));
                    await dm.ExecuteNonQueryAsync();
                }

                // 5) Delete i18n rows for this space
                const string delI18n = @"DELETE FROM i18n WHERE space_id = @Id;";
                await using (var di = _db.CreateCommand(conn, delI18n))
                {
                    di.Transaction = tx;
                    di.Parameters.Add(_db.CreateParameter("@Id", id));
                    await di.ExecuteNonQueryAsync();
                }

                // 6) Delete supported languages
                const string delLangs = @"DELETE FROM supported_languages WHERE space_id = @Id;";
                await using (var dsl = _db.CreateCommand(conn, delLangs))
                {
                    dsl.Transaction = tx;
                    dsl.Parameters.Add(_db.CreateParameter("@Id", id));
                    await dsl.ExecuteNonQueryAsync();
                }

                // 7) Delete the space
                const string delSpace = @"DELETE FROM space WHERE id = @Id;";
                await using (var ds = _db.CreateCommand(conn, delSpace))
                {
                    ds.Transaction = tx;
                    ds.Parameters.Add(_db.CreateParameter("@Id", id));
                    await ds.ExecuteNonQueryAsync();
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
