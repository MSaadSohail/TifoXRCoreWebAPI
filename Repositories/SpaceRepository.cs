// <copyright file="SpaceRepository.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Syed Hussain</author>
// <date>07/28/2025</date>
// <summary>Class to handle space SQL side</summary>
using MySqlConnector;
using GMS.TifoXRCoreWebAPI.Models;
using GMS.TifoXRCoreWebAPI.Models.Common;
using GMS.TifoXRCoreWebAPI.Repositories.Interfaces;

namespace GMS.TifoXRCoreWebAPI.Repositories
{
    public class SpaceRepository : ISpaceRepository
    {
        private readonly string _connStr;
        public SpaceRepository(IConfiguration cfg)
            => _connStr = cfg.GetConnectionString("DefaultConnection");

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

            await using var conn = new MySqlConnection(_connStr);
            await conn.OpenAsync();
            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Id", id);

            await using var reader = await cmd.ExecuteReaderAsync();
            SpaceData? space = null;
            var descs = new List<LocalizedValue>();

            while (await reader.ReadAsync())
            {
                if (space == null)
                {
                    space = new SpaceData
                    {
                        Id = reader.GetInt32("id"),
                        PlatformTypeId = reader.GetInt32("platform_type_id"),
                        EntityId = reader.GetInt32("entity_id"),
                        Sku = reader.IsDBNull(reader.GetOrdinal("sku")) ? null : reader.GetString("sku"),
                        Link = reader.IsDBNull(reader.GetOrdinal("link")) ? null : reader.GetString("link"),
                        IsPublished = reader.GetBoolean("is_published"),
                        IsLive = reader.GetBoolean("is_live"),
                        CreationTime = reader.GetDateTime("creation_time"),
                        ModifiedTime = reader.GetDateTime("modified_time"),
                        ModifiedBy = reader.GetString("modified_by"),
                        LocalizedDescription = new LocalizedPair
                        {
                            Key = reader.GetString("description_key"),
                            Values = descs
                        }
                    };
                }

                if (!reader.IsDBNull(reader.GetOrdinal("locale_id")))
                {
                    descs.Add(new LocalizedValue
                    {
                        LocaleId = reader.GetString("locale_id"),
                        Value = reader.IsDBNull(reader.GetOrdinal("value")) ? null : reader.GetString("value")
                    });
                }
            }

            return space;
        }
        public async Task<SpaceData> CreateSpaceAsync(Space spaceDto)
        {
            await using var conn = new MySqlConnection(_connStr);
            await conn.OpenAsync();
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
                        );
                        SELECT LAST_INSERT_ID();";

                int newId;
                await using (var cmd = new MySqlCommand(insertSql, conn, tx))
                {
                    cmd.Parameters.AddWithValue("@PlatformTypeId", spaceDto.PlatformTypeId);
                    cmd.Parameters.AddWithValue("@EntityId", spaceDto.EntityId);
                    cmd.Parameters.AddWithValue("@Sku", (object?)spaceDto.Sku ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Link", (object?)spaceDto.Link ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@IsPublished", spaceDto.IsPublished);
                    cmd.Parameters.AddWithValue("@IsLive", spaceDto.IsLive);
                    cmd.Parameters.AddWithValue("@DescriptionKey", spaceDto.LocalizedDescription.Key);
                    cmd.Parameters.AddWithValue("@CreationTime", DateTime.UtcNow);
                    cmd.Parameters.AddWithValue("@ModifiedTime", DateTime.UtcNow);
                    cmd.Parameters.AddWithValue("@ModifiedBy", spaceDto.ModifiedBy ?? "system");

                    newId = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                }

                const string insertI18n = @"
                        INSERT INTO i18n (`key`, locale_id, value, space_id)
                        VALUES (@Key, @LocaleId, @Value, @SpaceId);";

                foreach (var loc in spaceDto.LocalizedDescription.Values)
                {
                    await using var cmd = new MySqlCommand(insertI18n, conn, tx);
                    cmd.Parameters.AddWithValue("@Key", spaceDto.LocalizedDescription.Key);
                    cmd.Parameters.AddWithValue("@LocaleId", loc.LocaleId);
                    cmd.Parameters.AddWithValue("@Value", (object?)loc.Value ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@SpaceId", newId);
                    await cmd.ExecuteNonQueryAsync();
                }

                await tx.CommitAsync();
                return (await GetSpaceByIdAsync(newId))!;
            }
            catch (MySqlException)
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        public async Task<SpaceData?> UpdateSpaceAsync(int id, Space spaceDto)
        {
            await using var conn = new MySqlConnection(_connStr);
            await conn.OpenAsync();
            await using var tx = await conn.BeginTransactionAsync();

            try
            {
                const string updateSql = @"
            UPDATE space
            SET platform_type_id = @PlatformTypeId,
                entity_id = @EntityId,
                sku = @Sku,
                link = @Link,
                is_published = @IsPublished,
                is_live = @IsLive,
                description_key = @DescriptionKey,
                modified_time = @ModifiedTime,
                modified_by = @ModifiedBy
            WHERE id = @Id;";

                await using (var cmd = new MySqlCommand(updateSql, conn, tx))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    cmd.Parameters.AddWithValue("@PlatformTypeId", spaceDto.PlatformTypeId);
                    cmd.Parameters.AddWithValue("@EntityId", spaceDto.EntityId);
                    cmd.Parameters.AddWithValue("@Sku", (object?)spaceDto.Sku ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Link", (object?)spaceDto.Link ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@IsPublished", spaceDto.IsPublished);
                    cmd.Parameters.AddWithValue("@IsLive", spaceDto.IsLive);
                    cmd.Parameters.AddWithValue("@DescriptionKey", spaceDto.LocalizedDescription.Key);
                    cmd.Parameters.AddWithValue("@ModifiedTime", DateTime.UtcNow);
                    cmd.Parameters.AddWithValue("@ModifiedBy", spaceDto.ModifiedBy ?? "system");

                    if (await cmd.ExecuteNonQueryAsync() == 0)
                        return null;
                }

                const string updateI18n = @"
            UPDATE i18n
            SET value = @Value
            WHERE `key` = @Key AND locale_id = @LocaleId AND space_id = @SpaceId;";
                const string insertI18n = @"
            INSERT INTO i18n (`key`, locale_id, value, space_id)
            VALUES (@Key, @LocaleId, @Value, @SpaceId);";

                foreach (var loc in spaceDto.LocalizedDescription.Values)
                {
                    await using var updateCmd = new MySqlCommand(updateI18n, conn, tx);
                    updateCmd.Parameters.AddWithValue("@Key", spaceDto.LocalizedDescription.Key);
                    updateCmd.Parameters.AddWithValue("@LocaleId", loc.LocaleId);
                    updateCmd.Parameters.AddWithValue("@Value", (object?)loc.Value ?? DBNull.Value);
                    updateCmd.Parameters.AddWithValue("@SpaceId", spaceDto.SpaceId);

                    int affected = await updateCmd.ExecuteNonQueryAsync();
                    if (affected == 0)
                    {
                        await using var insertCmd = new MySqlCommand(insertI18n, conn, tx);
                        insertCmd.Parameters.AddWithValue("@Key", spaceDto.LocalizedDescription.Key);
                        insertCmd.Parameters.AddWithValue("@LocaleId", loc.LocaleId);
                        insertCmd.Parameters.AddWithValue("@Value", (object?)loc.Value ?? DBNull.Value);
                        insertCmd.Parameters.AddWithValue("@SpaceId", spaceDto.SpaceId);
                        await insertCmd.ExecuteNonQueryAsync();
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

    }
}
