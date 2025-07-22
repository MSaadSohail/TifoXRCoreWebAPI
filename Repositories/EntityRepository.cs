using MySqlConnector;
using System.Data;
using TifoXRCoreWebAPI.Models;
using TifoXRCoreWebAPI.Models.Common;
using TifoXRCoreWebAPI.Repositories.Interfaces;


namespace TifoXRCoreWebAPI.Repositories
{
    public sealed class EntityRepository : IEntityRepository
    {
        private readonly string _connStr;

        public EntityRepository(IConfiguration cfg)
        {
            _connStr = cfg.GetConnectionString("DefaultConnection");
        }

        // GET
        public async Task<EntityData?> GetByIdAsync(int id)
        {
            await using var conn = new MySqlConnection(_connStr);
            await conn.OpenAsync();
            return await LoadEntityById(conn, id);
        }

        // POST
        public async Task<EntityData> CreateAsync(Entity dto)
        {
            await using var conn = new MySqlConnection(_connStr);
            await conn.OpenAsync();
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
                        SELECT LAST_INSERT_ID();";

                int newId;
                await using (var cmd = new MySqlCommand(insertEntitySql, conn, tx))
                {
                    cmd.Parameters.AddWithValue("@EntityTypeId", dto.EntityTypeId);
                    cmd.Parameters.AddWithValue("@ParentEntityId", (object?)dto.ParentEntityId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@NameKey", dto.LocalizedName.Key);
                    cmd.Parameters.AddWithValue(
                        "@DescriptionKey",
                        string.IsNullOrWhiteSpace(dto.LocalizedDescription?.Key)
                            ? DBNull.Value
                            : dto.LocalizedDescription!.Key);
                    cmd.Parameters.AddWithValue("@CreationTime", DateTime.UtcNow);
                    cmd.Parameters.AddWithValue("@ModifiedTime", DateTime.UtcNow);
                    cmd.Parameters.AddWithValue("@ModifiedBy", "system");
                    newId = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                }

                // insert into i18n
                const string insertI18nSql = @"
                        INSERT INTO i18n (`key`, locale_id, value, space_id)
                        VALUES (@Key, @LocaleId, @Value, @SpaceId);";

                foreach (var loc in dto.LocalizedName.Values)
                {
                    await using var nameCmd = new MySqlCommand(insertI18nSql, conn, tx);
                    nameCmd.Parameters.AddWithValue("@Key", dto.LocalizedName.Key);
                    nameCmd.Parameters.AddWithValue("@LocaleId", loc.LocaleId);
                    nameCmd.Parameters.AddWithValue("@Value", (object?)loc.Value ?? DBNull.Value);
                    nameCmd.Parameters.AddWithValue("@SpaceId", dto.SpaceId);
                    await nameCmd.ExecuteNonQueryAsync();
                }

                if (dto.LocalizedDescription?.Values != null)
                {
                    foreach (var loc in dto.LocalizedDescription.Values)
                    {
                        await using var descCmd = new MySqlCommand(insertI18nSql, conn, tx);
                        descCmd.Parameters.AddWithValue("@Key", dto.LocalizedDescription.Key);
                        descCmd.Parameters.AddWithValue("@LocaleId", loc.LocaleId);
                        descCmd.Parameters.AddWithValue("@Value", (object?)loc.Value ?? DBNull.Value);
                        descCmd.Parameters.AddWithValue("@SpaceId", dto.SpaceId);
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
        public async Task<EntityData?> UpdateAsync(int id, Entity dto)
        {
            await using var conn = new MySqlConnection(_connStr);
            await conn.OpenAsync();
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

                await using (var cmd = new MySqlCommand(updateEntitySql, conn, tx))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    cmd.Parameters.AddWithValue("@EntityTypeId", dto.EntityTypeId);
                    cmd.Parameters.AddWithValue("@ParentEntityId", (object?)dto.ParentEntityId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@NameKey", dto.LocalizedName.Key);
                    cmd.Parameters.AddWithValue(
                        "@DescriptionKey",
                        string.IsNullOrWhiteSpace(dto.LocalizedDescription?.Key)
                            ? DBNull.Value
                            : dto.LocalizedDescription!.Key);
                    cmd.Parameters.AddWithValue("@ModifiedTime", DateTime.UtcNow);
                    cmd.Parameters.AddWithValue("@ModifiedBy", "system");

                    if (await cmd.ExecuteNonQueryAsync() == 0)
                    {
                        await tx.RollbackAsync();
                        return null;
                    }
                }

                // upsert i18n rows
                const string updateI18nSql = @"
                        UPDATE i18n
                        SET value = @Value
                        WHERE `key` = @Key AND locale_id = @LocaleId AND space_id = @SpaceId;";

                const string insertI18nSql = @"
                        INSERT INTO i18n (`key`, locale_id, value, space_id)
                        VALUES (@Key, @LocaleId, @Value, @SpaceId);";

                async Task UpsertAsync(LocalizedName ln)
                {
                    foreach (var loc in ln.Values)
                    {
                        await using var uCmd = new MySqlCommand(updateI18nSql, conn, tx);
                        uCmd.Parameters.AddWithValue("@Key", ln.Key);
                        uCmd.Parameters.AddWithValue("@LocaleId", loc.LocaleId);
                        uCmd.Parameters.AddWithValue("@Value", (object?)loc.Value ?? DBNull.Value);
                        uCmd.Parameters.AddWithValue("@SpaceId", dto.SpaceId);

                        if (await uCmd.ExecuteNonQueryAsync() == 0)
                        {
                            await using var iCmd = new MySqlCommand(insertI18nSql, conn, tx);
                            iCmd.Parameters.AddWithValue("@Key", ln.Key);
                            iCmd.Parameters.AddWithValue("@LocaleId", loc.LocaleId);
                            iCmd.Parameters.AddWithValue("@Value", (object?)loc.Value ?? DBNull.Value);
                            iCmd.Parameters.AddWithValue("@SpaceId", dto.SpaceId);
                            await iCmd.ExecuteNonQueryAsync();
                        }
                    }
                }

                await UpsertAsync(dto.LocalizedName);

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
        private static async Task<EntityData?> LoadEntityById(MySqlConnection conn, int entityId)
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
                        i_name.value     AS name_value,
                        i_desc.value     AS desc_value
                    FROM entity e
                    LEFT JOIN i18n i_name
                           ON i_name.`key` = e.name_key
                    LEFT JOIN i18n i_desc
                           ON i_desc.`key` = e.description_key
                          AND i_desc.locale_id = i_name.locale_id
                    WHERE e.id = @EntityId
                    ORDER BY i_name.locale_id;";

            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@EntityId", entityId);

            await using var rdr = await cmd.ExecuteReaderAsync();
            EntityData? entity = null;
            var names = new List<LocalizedValue>();
            var descs = new List<LocalizedValue>();

            while (await rdr.ReadAsync())
            {
                if (entity is null)
                {
                    entity = new EntityData
                    {
                        Id = rdr.GetInt32("id"),
                        EntityTypeId = rdr.GetInt32("entity_type_id"),
                        ParentEntityId = rdr.IsDBNull("parent_entity_id")
                            ? null
                            : rdr.GetInt32("parent_entity_id"),
                        CreationTime = rdr.GetDateTime("creation_time"),
                        ModifiedTime = rdr.GetDateTime("modified_time"),
                        ModifiedBy = rdr.GetString("modified_by"),
                        LocalizedName = new LocalizedName
                        {
                            Key = rdr.GetString("name_key"),
                            Values = names
                        },
                        LocalizedDescription = new LocalizedName
                        {
                            Key = rdr.GetString("description_key"),
                            Values = descs
                        }
                    };
                }

                var locale = rdr.GetString("name_locale_id");

                var nameVal = rdr.IsDBNull("name_value") ? null : rdr.GetString("name_value");
                names.Add(new LocalizedValue { LocaleId = locale, Value = nameVal });

                var descVal = rdr.IsDBNull("desc_value") ? null : rdr.GetString("desc_value");
                descs.Add(new LocalizedValue { LocaleId = locale, Value = descVal });
            }

            return entity;
        }
    }
}
