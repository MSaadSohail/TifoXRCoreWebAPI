// <copyright file="EntityRepository.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Syed Hussain</author>
// <date>07/28/2025</date>
// <summary>Class to handle entity SQL side</summary>

using System.Data.Common;
//
using GMS.TifoXRCoreWebAPI.Models;
using GMS.TifoXRCoreWebAPI.Models.Common;
using GMS.TifoXRCoreWebAPI.Utilities.Infrastructure;

namespace GMS.TifoXRCoreWebAPI.Repositories
{
    public sealed class EntityRepository(IDbProvider db) : IEntityRepository
    {
        private readonly IDbProvider _db = db ?? throw new ArgumentNullException(nameof(db));

        // GET
        public async Task<EntityData> GetByIdAsync(int id)
        {
            await using var conn = await _db.OpenConnectionAsync();
            return await LoadEntityById(conn, id);
        }

        // POST
        public async Task<EntityData> CreateAsync(Entity dto)
        {
            await using var conn = await _db.OpenConnectionAsync();
            await using var tx = await conn.BeginTransactionAsync();

            try
            {
                // insert into entity
                const string insertEntitySql = @"
                        INSERT INTO entity (
                            entity_type_id, parent_entity_id,
                            name_key, description_key,
                            creation_time, modified_time, modified_by)
                        VALUES (
                            @EntityTypeId, @ParentEntityId,
                            @NameKey, @DescriptionKey,
                            @CreationTime, @ModifiedTime, @ModifiedBy);
                        SELECT CAST(SCOPE_IDENTITY() AS INT);";

                int newId;
                await using (var cmd = _db.CreateCommand(conn, insertEntitySql, tx))
                {
                    cmd.Parameters.Add(_db.CreateParameter("@EntityTypeId", dto.EntityTypeId));
                    cmd.Parameters.Add(_db.CreateParameter("@ParentEntityId", dto.ParentEntityId));
                    cmd.Parameters.Add(_db.CreateParameter("@NameKey", dto.LocalizedPairs.Key));
                    cmd.Parameters.Add(_db.CreateParameter(
                        "@DescriptionKey",
                        string.IsNullOrWhiteSpace(dto.LocalizedDescription?.Key)
                            ? DBNull.Value
                            : dto.LocalizedDescription!.Key));
                    cmd.Parameters.Add(_db.CreateParameter("@CreationTime", DateTime.UtcNow));
                    cmd.Parameters.Add(_db.CreateParameter("@ModifiedTime", DateTime.UtcNow));
                    cmd.Parameters.Add(_db.CreateParameter("@ModifiedBy", "system"));
                    newId = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                }

                // insert into i18n
                const string insertI18nSql = @"
                        INSERT INTO i18n ([key], locale_id, [value], space_id)
                        VALUES (@Key, @LocaleId, @Value, @SpaceId);";

                foreach (var loc in dto.LocalizedPairs.Values)
                {
                    await using var nameCmd = _db.CreateCommand(conn, insertI18nSql, tx);
                    nameCmd.Parameters.Add(_db.CreateParameter("@Key", dto.LocalizedPairs.Key));
                    nameCmd.Parameters.Add(_db.CreateParameter("@LocaleId", loc.LocaleId));
                    nameCmd.Parameters.Add(_db.CreateParameter("@Value", loc.Value));
                    nameCmd.Parameters.Add(_db.CreateParameter("@SpaceId", dto.SpaceId));
                    await nameCmd.ExecuteNonQueryAsync();
                }

                if (dto.LocalizedDescription?.Values != null)
                {
                    foreach (var loc in dto.LocalizedDescription.Values)
                    {
                        await using var descCmd = _db.CreateCommand(conn, insertI18nSql, tx);
                        descCmd.Parameters.Add(_db.CreateParameter("@Key", dto.LocalizedDescription.Key));
                        descCmd.Parameters.Add(_db.CreateParameter("@LocaleId", loc.LocaleId));
                        descCmd.Parameters.Add(_db.CreateParameter("@Value", loc.Value));
                        descCmd.Parameters.Add(_db.CreateParameter("@SpaceId", dto.SpaceId));
                        await descCmd.ExecuteNonQueryAsync();
                    }
                }

                await tx.CommitAsync();
                return (await LoadEntityById(conn, newId))!;
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        // PUT
        public async Task<EntityData> UpdateAsync(int id, Entity dto)
        {
            await using var conn = await _db.OpenConnectionAsync();
            await using var tx = await conn.BeginTransactionAsync();

            try
            {
                // update entity
                const string updateEntitySql = @"
                        UPDATE entity
                        SET entity_type_id   = @EntityTypeId,
                            parent_entity_id = @ParentEntityId,
                            name_key         = @NameKey,
                            description_key  = @DescriptionKey,
                            modified_time    = @ModifiedTime,
                            modified_by      = @ModifiedBy
                        WHERE id = @Id;";

                await using (var cmd = _db.CreateCommand(conn, updateEntitySql, tx))
                {
                    cmd.Parameters.Add(_db.CreateParameter("@Id", id));
                    cmd.Parameters.Add(_db.CreateParameter("@EntityTypeId", dto.EntityTypeId));
                    cmd.Parameters.Add(_db.CreateParameter("@ParentEntityId", dto.ParentEntityId));
                    cmd.Parameters.Add(_db.CreateParameter("@NameKey", dto.LocalizedPairs.Key));
                    cmd.Parameters.Add(_db.CreateParameter(
                        "@DescriptionKey",
                        string.IsNullOrWhiteSpace(dto.LocalizedDescription?.Key)
                            ? DBNull.Value
                            : dto.LocalizedDescription!.Key));
                    cmd.Parameters.Add(_db.CreateParameter("@ModifiedTime", DateTime.UtcNow));
                    cmd.Parameters.Add(_db.CreateParameter("@ModifiedBy", "system"));

                    if (await cmd.ExecuteNonQueryAsync() == 0)
                    {
                        await tx.RollbackAsync();
                        return null;
                    }
                }

                // upsert i18n rows
                const string updateI18nSql = @"
                        UPDATE i18n
                        SET [value] = @Value
                        WHERE [key] = @Key AND locale_id = @LocaleId AND space_id = @SpaceId;";

                const string insertI18nSql = @"
                        INSERT INTO i18n ([key], locale_id, [value], space_id)
                        VALUES (@Key, @LocaleId, @Value, @SpaceId);";

                async Task UpsertAsync(LocalizedPairs ln)
                {
                    foreach (var loc in ln.Values)
                    {
                        await using var uCmd = _db.CreateCommand(conn, updateI18nSql, tx);
                        uCmd.Parameters.Add(_db.CreateParameter("@Key", ln.Key));
                        uCmd.Parameters.Add(_db.CreateParameter("@LocaleId", loc.LocaleId));
                        uCmd.Parameters.Add(_db.CreateParameter("@Value", loc.Value));
                        uCmd.Parameters.Add(_db.CreateParameter("@SpaceId", dto.SpaceId));

                        if (await uCmd.ExecuteNonQueryAsync() == 0)
                        {
                            await using var iCmd = _db.CreateCommand(conn, insertI18nSql, tx);
                            iCmd.Parameters.Add(_db.CreateParameter("@Key", ln.Key));
                            iCmd.Parameters.Add(_db.CreateParameter("@LocaleId", loc.LocaleId));
                            iCmd.Parameters.Add(_db.CreateParameter("@Value", loc.Value));
                            iCmd.Parameters.Add(_db.CreateParameter("@SpaceId", dto.SpaceId));
                            await iCmd.ExecuteNonQueryAsync();
                        }
                    }
                }

                await UpsertAsync(dto.LocalizedPairs);

                if (dto.LocalizedDescription?.Values != null)
                    await UpsertAsync(dto.LocalizedDescription);

                await tx.CommitAsync();
                return await LoadEntityById(conn, id);
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        // Helper Function
        private async Task<EntityData> LoadEntityById(DbConnection conn, int entityId)
        {
            const string sql = @"
                    SELECT
                        e.id,
                        e.entity_type_id,
                        e.parent_entity_id,
                        e.name_key,
                        e.description_key,
                        e.creation_time,
                        e.modified_time,
                        e.modified_by,
                        i_name.locale_id AS name_locale_id,
                        i_name.[value]   AS name_value,
                        i_desc.[value]   AS desc_value
                    FROM entity e
                    LEFT JOIN i18n i_name
                           ON i_name.[key] = e.name_key
                    LEFT JOIN i18n i_desc
                           ON i_desc.[key] = e.description_key
                          AND i_desc.locale_id = i_name.locale_id
                    WHERE e.id = @EntityId
                    ORDER BY i_name.locale_id;";

            await using var cmd = _db.CreateCommand(conn, sql);
            cmd.Parameters.Add(_db.CreateParameter("@EntityId", entityId));

            await using var rdr = await cmd.ExecuteReaderAsync();
            EntityData entity = null;
            var names = new List<LocalizedValue>();
            var descs = new List<LocalizedValue>();

            bool ordReady = false;
            int oId = -1, oEntityTypeId = -1, oParentEntityId = -1, oNameKey = -1, oDescriptionKey = -1;
            int oCreationTime = -1, oModifiedTime = -1, oModifiedBy = -1, oNameLocaleId = -1, oNameValue = -1, oDescValue = -1;

            while (await rdr.ReadAsync())
            {
                if (!ordReady)
                {
                    oId = rdr.GetOrdinal("id");
                    oEntityTypeId = rdr.GetOrdinal("entity_type_id");
                    oParentEntityId = rdr.GetOrdinal("parent_entity_id");
                    oNameKey = rdr.GetOrdinal("name_key");
                    oDescriptionKey = rdr.GetOrdinal("description_key");
                    oCreationTime = rdr.GetOrdinal("creation_time");
                    oModifiedTime = rdr.GetOrdinal("modified_time");
                    oModifiedBy = rdr.GetOrdinal("modified_by");
                    oNameLocaleId = rdr.GetOrdinal("name_locale_id");
                    oNameValue = rdr.GetOrdinal("name_value");
                    oDescValue = rdr.GetOrdinal("desc_value");
                    ordReady = true;
                }

                if (entity is null)
                {
                    entity = new EntityData
                    {
                        Id = rdr.GetInt32(oId),
                        EntityTypeId = rdr.GetInt32(oEntityTypeId),
                        ParentEntityId = rdr.IsDBNull(oParentEntityId)
                            ? null
                            : rdr.GetInt32(oParentEntityId),
                        CreationTime = rdr.GetDateTime(oCreationTime),
                        ModifiedTime = rdr.GetDateTime(oModifiedTime),
                        ModifiedBy = rdr.GetString(oModifiedBy),
                        LocalizedPairs = new LocalizedPairs
                        {
                            Key = rdr.GetString(oNameKey),
                            Values = names
                        },
                        LocalizedDescription = new LocalizedPairs
                        {
                            Key = rdr.IsDBNull(oDescriptionKey) ? string.Empty : rdr.GetString(oDescriptionKey),
                            Values = descs
                        }
                    };
                }

                var locale = rdr.GetString(oNameLocaleId);

                if (!rdr.IsDBNull(oNameValue))
                {
                    names.Add(new LocalizedValue
                    {
                        LocaleId = locale,
                        Value = rdr.GetString(oNameValue)
                    });
                }

                if (!rdr.IsDBNull(oDescValue))
                {
                    descs.Add(new LocalizedValue
                    {
                        LocaleId = locale,
                        Value = rdr.GetString(oDescValue)
                    });
                }
            }

            return entity;
        }
    }
}
