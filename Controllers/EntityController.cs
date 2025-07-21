using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using TifoXRCoreWebAPI.Models.Common;
using TifoXRCoreWebAPI.Models;

namespace TifoXRCoreWebAPI.Controllers
{
    public class EntityController : Controller
    {
        private readonly string _connectionString;

        public EntityController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        [HttpGet("api/entity/{id}")]
        [ProducesResponseType(typeof(EntityData), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<EntityData>> GetEntityById(int id)
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
            i_name.locale_id     AS name_locale_id,
            i_name.value         AS name_value,
            i_desc.value         AS desc_value
        FROM entity e
        LEFT JOIN i18n i_name
            ON i_name.key = e.name_key
        LEFT JOIN i18n i_desc
            ON i_desc.key = e.description_key
           AND i_desc.locale_id = i_name.locale_id
        WHERE e.id = @EntityId
        ORDER BY i_name.locale_id;
    ";

            await using var conn = new MySqlConnection(_connectionString);
            await conn.OpenAsync();
            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@EntityId", id);

            await using var reader = await cmd.ExecuteReaderAsync();
            EntityData entity = null;

            while (await reader.ReadAsync())
            {
                if (entity == null)
                {
                    entity = new EntityData
                    {
                        Id = reader.GetInt32("id"),
                        EntityTypeId = reader.GetInt32("entity_type_id"),
                        ParentEntityId = reader.IsDBNull(reader.GetOrdinal("parent_entity_id"))
                                         ? null
                                         : reader.GetInt32("parent_entity_id"),
                        LocalizedName = new LocalizedName
                        {
                            Key = reader.GetString("name_key"),
                            Values = new List<LocalizedValue>()
                        },
                        LocalizedDescription = new LocalizedName
                        {
                            Key = reader.GetString("description_key"),
                            Values = new List<LocalizedValue>()
                        },
                        CreationTime = reader.GetDateTime("creation_time"),
                        ModifiedTime = reader.GetDateTime("modified_time"),
                        ModifiedBy = reader.GetString("modified_by")
                    };
                }

                var localeId = reader.GetString("name_locale_id");
                var nameValue = reader.IsDBNull(reader.GetOrdinal("name_value")) ? null : reader.GetString("name_value");
                var descValue = reader.IsDBNull(reader.GetOrdinal("desc_value")) ? null : reader.GetString("desc_value");

                entity.LocalizedName.Values.Add(new LocalizedValue
                {
                    LocaleId = localeId,
                    Value = nameValue
                });

                entity.LocalizedDescription.Values.Add(new LocalizedValue
                {
                    LocaleId = localeId,
                    Value = descValue
                });
            }

            if (entity == null)
                return NotFound();

            return Ok(entity);
        }
        private static async Task<EntityData> LoadEntityById(MySqlConnection conn, int entityId)
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
            ln.locale_id      AS ln_locale_id,
            ln.value          AS ln_value,
            ld.value          AS ld_value
        FROM entity AS e
        LEFT JOIN i18n AS ln
            ON ln.`key` = e.name_key
        LEFT JOIN i18n AS ld
            ON ld.`key` = e.description_key
           AND ld.locale_id = ln.locale_id
        WHERE e.id = @EntityId
        ORDER BY ln.locale_id;
    ";

            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@EntityId", entityId);

            await using var reader = await cmd.ExecuteReaderAsync();
            EntityData entity = null;

            var localizedNames = new List<LocalizedValue>();
            var localizedDescriptions = new List<LocalizedValue>();

            while (await reader.ReadAsync())
            {
                if (entity == null)
                {
                    entity = new EntityData
                    {
                        Id = reader.GetInt32("id"),
                        EntityTypeId = reader.GetInt32("entity_type_id"),
                        ParentEntityId = reader.IsDBNull(reader.GetOrdinal("parent_entity_id"))
                            ? (int?)null
                            : reader.GetInt32("parent_entity_id"),
                        CreationTime = reader.GetDateTime("creation_time"),
                        ModifiedTime = reader.GetDateTime("modified_time"),
                        ModifiedBy = reader.GetString("modified_by"),
                        LocalizedName = new LocalizedName
                        {
                            Key = reader.GetString("name_key"),
                            Values = localizedNames
                        },
                        LocalizedDescription = new LocalizedName
                        {
                            Key = reader.GetString("description_key"),
                            Values = localizedDescriptions
                        }
                    };
                }

                var localeId = reader.GetString("ln_locale_id");

                if (!reader.IsDBNull(reader.GetOrdinal("ln_value")))
                {
                    localizedNames.Add(new LocalizedValue
                    {
                        LocaleId = localeId,
                        Value = reader.GetString("ln_value")
                    });
                }

                if (!reader.IsDBNull(reader.GetOrdinal("ld_value")))
                {
                    localizedDescriptions.Add(new LocalizedValue
                    {
                        LocaleId = localeId,
                        Value = reader.GetString("ld_value")
                    });
                }
            }

            return entity;
        }



        [HttpPost("api/entity")]
        [ProducesResponseType(typeof(EntityData), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<EntityData>> CreateEntity([FromBody] Entity entityDto)
        {
            if (entityDto == null || entityDto.LocalizedName == null)
                return BadRequest();

            await using var conn = new MySqlConnection(_connectionString);
            await conn.OpenAsync();
            await using var tx = await conn.BeginTransactionAsync();

            try
            {
                const string insertEntitySql = @"
            INSERT INTO entity (entity_type_id, parent_entity_id, name_key, description_key, creation_time, modified_time, modified_by)
            VALUES (@EntityTypeId, @ParentEntityId, @NameKey, @DescriptionKey, @CreationTime, @ModifiedTime, @ModifiedBy);
            SELECT LAST_INSERT_ID();";

                int newId;
                await using (var cmd = new MySqlCommand(insertEntitySql, conn, tx))
                {
                    cmd.Parameters.AddWithValue("@EntityTypeId", entityDto.EntityTypeId);
                    cmd.Parameters.AddWithValue("@ParentEntityId", (object?)entityDto.ParentEntityId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@NameKey", entityDto.LocalizedName.Key);
                    cmd.Parameters.AddWithValue("@DescriptionKey",
                        string.IsNullOrEmpty(entityDto.LocalizedDescription?.Key)
                            ? (object)DBNull.Value
                            : entityDto.LocalizedDescription.Key);
                    cmd.Parameters.AddWithValue("@CreationTime", DateTime.UtcNow);
                    cmd.Parameters.AddWithValue("@ModifiedTime", DateTime.UtcNow);
                    cmd.Parameters.AddWithValue("@ModifiedBy", "system"); // Optional: Make dynamic if needed

                    newId = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                }

                const string insertI18nSql = @"
            INSERT INTO i18n (`key`, locale_id, value, space_id)
            VALUES (@Key, @LocaleId, @Value, @SpaceId);";

                foreach (var loc in entityDto.LocalizedName.Values)
                {
                    await using var nameCmd = new MySqlCommand(insertI18nSql, conn, tx);
                    nameCmd.Parameters.AddWithValue("@Key", entityDto.LocalizedName.Key);
                    nameCmd.Parameters.AddWithValue("@LocaleId", loc.LocaleId);
                    nameCmd.Parameters.AddWithValue("@Value", (object?)loc.Value ?? DBNull.Value);
                    nameCmd.Parameters.AddWithValue("@SpaceId", entityDto.SpaceId);
                    await nameCmd.ExecuteNonQueryAsync();
                }

                if (entityDto.LocalizedDescription?.Values != null)
                {
                    foreach (var loc in entityDto.LocalizedDescription.Values)
                    {
                        await using var descCmd = new MySqlCommand(insertI18nSql, conn, tx);
                        descCmd.Parameters.AddWithValue("@Key", entityDto.LocalizedDescription.Key);
                        descCmd.Parameters.AddWithValue("@LocaleId", loc.LocaleId);
                        descCmd.Parameters.AddWithValue("@Value", (object?)loc.Value ?? DBNull.Value);
                        descCmd.Parameters.AddWithValue("@SpaceId", entityDto.SpaceId);
                        await descCmd.ExecuteNonQueryAsync();
                    }
                }

                await tx.CommitAsync();

                // Load and return newly created entity
                var created = await LoadEntityById(conn, newId);
                return CreatedAtAction(nameof(GetEntityById), new { id = newId }, created);

            }
            catch (MySqlException ex)
            {
                await tx.RollbackAsync();
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPut("api/entity/{id}")]
        [ProducesResponseType(typeof(EntityData), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<EntityData>> UpdateEntity(int id, [FromBody] Entity entityDto)
        {
            if (entityDto == null || entityDto.LocalizedName == null)
                return BadRequest();

            await using var conn = new MySqlConnection(_connectionString);
            await conn.OpenAsync();
            await using var tx = await conn.BeginTransactionAsync();

            try
            {
                // 1) Update main entity table
                const string updateEntitySql = @"
            UPDATE entity
            SET entity_type_id = @EntityTypeId,
                parent_entity_id = @ParentEntityId,
                name_key = @NameKey,
                description_key = @DescriptionKey,
                modified_time = @ModifiedTime,
                modified_by = @ModifiedBy
            WHERE id = @Id;
        ";

                await using (var cmd = new MySqlCommand(updateEntitySql, conn, tx))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    cmd.Parameters.AddWithValue("@EntityTypeId", entityDto.EntityTypeId);
                    cmd.Parameters.AddWithValue("@ParentEntityId", (object?)entityDto.ParentEntityId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@NameKey", entityDto.LocalizedName.Key);
                    cmd.Parameters.AddWithValue("@DescriptionKey",
                        string.IsNullOrEmpty(entityDto.LocalizedDescription?.Key)
                            ? (object)DBNull.Value
                            : entityDto.LocalizedDescription.Key);
                    cmd.Parameters.AddWithValue("@ModifiedTime", DateTime.UtcNow);
                    cmd.Parameters.AddWithValue("@ModifiedBy", "system");

                    await cmd.ExecuteNonQueryAsync();
                }

                // 2) Upsert i18n values for name
                const string updateI18nSql = @"
            UPDATE i18n
            SET value = @Value
            WHERE `key` = @Key AND locale_id = @LocaleId AND space_id = @SpaceId;
        ";

                const string insertI18nSql = @"
            INSERT INTO i18n (`key`, locale_id, value, space_id)
            VALUES (@Key, @LocaleId, @Value, @SpaceId);
        ";

                foreach (var loc in entityDto.LocalizedName.Values)
                {
                    await using var uCmd = new MySqlCommand(updateI18nSql, conn, tx);
                    uCmd.Parameters.AddWithValue("@Key", entityDto.LocalizedName.Key);
                    uCmd.Parameters.AddWithValue("@LocaleId", loc.LocaleId);
                    uCmd.Parameters.AddWithValue("@Value", (object?)loc.Value ?? DBNull.Value);
                    uCmd.Parameters.AddWithValue("@SpaceId", entityDto.SpaceId);

                    if (await uCmd.ExecuteNonQueryAsync() == 0)
                    {
                        await using var iCmd = new MySqlCommand(insertI18nSql, conn, tx);
                        iCmd.Parameters.AddWithValue("@Key", entityDto.LocalizedName.Key);
                        iCmd.Parameters.AddWithValue("@LocaleId", loc.LocaleId);
                        iCmd.Parameters.AddWithValue("@Value", (object?)loc.Value ?? DBNull.Value);
                        iCmd.Parameters.AddWithValue("@SpaceId", entityDto.SpaceId);
                        await iCmd.ExecuteNonQueryAsync();
                    }
                }

                // 3) Upsert i18n values for description
                if (entityDto.LocalizedDescription?.Values != null)
                {
                    foreach (var loc in entityDto.LocalizedDescription.Values)
                    {
                        await using var uCmd = new MySqlCommand(updateI18nSql, conn, tx);
                        uCmd.Parameters.AddWithValue("@Key", entityDto.LocalizedDescription.Key);
                        uCmd.Parameters.AddWithValue("@LocaleId", loc.LocaleId);
                        uCmd.Parameters.AddWithValue("@Value", (object?)loc.Value ?? DBNull.Value);
                        uCmd.Parameters.AddWithValue("@SpaceId", entityDto.SpaceId);

                        if (await uCmd.ExecuteNonQueryAsync() == 0)
                        {
                            await using var iCmd = new MySqlCommand(insertI18nSql, conn, tx);
                            iCmd.Parameters.AddWithValue("@Key", entityDto.LocalizedDescription.Key);
                            iCmd.Parameters.AddWithValue("@LocaleId", loc.LocaleId);
                            iCmd.Parameters.AddWithValue("@Value", (object?)loc.Value ?? DBNull.Value);
                            iCmd.Parameters.AddWithValue("@SpaceId", entityDto.SpaceId);
                            await iCmd.ExecuteNonQueryAsync();
                        }
                    }
                }

                await tx.CommitAsync();

                // 4) Reload and return updated entity
                var updated = await LoadEntityById(conn, id);
                return Ok(updated);
            }
            catch (MySqlException ex)
            {
                await tx.RollbackAsync();
                return StatusCode(500, new { error = ex.Message });
            }
        }


    }
}

    