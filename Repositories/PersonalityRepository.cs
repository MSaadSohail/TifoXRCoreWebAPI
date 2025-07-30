// <copyright file="PersonalityRepository.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Syed Hussain</author>
// <date>07/23/2025</date>
// <summary>Personality APIs</summary>
using MySqlConnector;
using System.Data;
using GMS.TifoXRCoreWebAPI.Models;
using GMS.TifoXRCoreWebAPI.Models.Common;
using GMS.TifoXRCoreWebAPI.Repositories.Interfaces;
using GMS.TifoXRCoreWebAPI.Repositories;

namespace GMS.TifoXRCoreWebAPI.Repositories
{
    public class PersonalityRepository : IPersonalityRepository
    {
        private readonly string _connectionString;

        public PersonalityRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        public async Task<PersonalityData?> GetPersonalityByIdAsync(int id)
        {
            const string sql = @"
        SELECT 
            p.id,
            p.name,
            p.sport_id,
            p.entity_id,
            p.country_name_key,
            p.bio_data_key,
            p.media_id,
            p.creation_time,
            p.modified_time,
            p.modified_by,

            i.locale_id          AS locale_id,
            i18n_country.value   AS country_value,
            i18n_bio.value       AS bio_value,
            ml.media_link        AS media_link

        FROM personality p
        LEFT JOIN i18n i 
            ON i.`key` IN (p.country_name_key, p.bio_data_key)
        LEFT JOIN i18n i18n_country 
            ON i18n_country.`key` = p.country_name_key AND i18n_country.locale_id = i.locale_id
        LEFT JOIN i18n i18n_bio 
            ON i18n_bio.`key` = p.bio_data_key AND i18n_bio.locale_id = i.locale_id
        LEFT JOIN media_localization ml 
            ON ml.media_id = p.media_id AND ml.locale_id = i.locale_id
        WHERE p.id = @PersonalityId
        ORDER BY i.locale_id;";

            await using var conn = new MySqlConnection(_connectionString);
            await conn.OpenAsync();
            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@PersonalityId", id);

            await using var reader = await cmd.ExecuteReaderAsync();

            PersonalityData? personality = null;
            var countryValues = new List<LocalizedValue>();
            var bioValues = new List<LocalizedValue>();
            var mediaLocalizations = new List<MediaLocalization>();

            while (await reader.ReadAsync())
            {
                if (personality == null)
                {
                    personality = new PersonalityData
                    {
                        Id = reader.GetInt32("id"),
                        Name = reader.GetString("name"),
                        SportId = reader.IsDBNull("sport_id") ? (int?)null : reader.GetInt32("sport_id"),
                        EntityId = reader.IsDBNull("entity_id") ? (int?)null : reader.GetInt32("entity_id"),
                        CreationTime = reader.GetDateTime("creation_time"),
                        ModifiedTime = reader.GetDateTime("modified_time"),
                        ModifiedBy = reader.GetString("modified_by"),
                        LocalizedCountry = new LocalizedPairs
                        {
                            Key = reader.IsDBNull("country_name_key") ? null : reader.GetString("country_name_key"),
                            Values = countryValues
                        },
                        LocalizedBio = new LocalizedPairs
                        {
                            Key = reader.IsDBNull("bio_data_key") ? null : reader.GetString("bio_data_key"),
                            Values = bioValues
                        },
                        Media = reader.IsDBNull("media_id") ? null : new MediaData
                        {
                            Id = reader.GetString("media_id"),
                            LinkLocalizations = mediaLocalizations
                        }
                    };
                }

                if (!reader.IsDBNull("locale_id"))
                {
                    var locale = reader.GetString("locale_id");

                    if (!reader.IsDBNull("country_value"))
                    {
                        countryValues.Add(new LocalizedValue
                        {
                            LocaleId = locale,
                            Value = reader.GetString("country_value")
                        });
                    }

                    if (!reader.IsDBNull("bio_value"))
                    {
                        bioValues.Add(new LocalizedValue
                        {
                            LocaleId = locale,
                            Value = reader.GetString("bio_value")
                        });
                    }

                    if (!reader.IsDBNull("media_link"))
                    {
                        if (!mediaLocalizations.Any(x => x.LocaleId == locale))
                        {
                            mediaLocalizations.Add(new MediaLocalization
                            {
                                LocaleId = locale,
                                MediaLink = reader.GetString("media_link")
                            });
                        }
                    }
                }
            }

            return personality;
        }

        public async Task<PersonalityData> CreatePersonalityAsync(PersonalityCreateDto dto)
        {
            await using var conn = new MySqlConnection(_connectionString);
            await conn.OpenAsync();
            await using var tx = await conn.BeginTransactionAsync();

            try
            {
                var countryKey = dto.LocalizedCountry?.Key;
                var bioKey = dto.LocalizedBio?.Key;

                if (string.IsNullOrWhiteSpace(countryKey) || string.IsNullOrWhiteSpace(bioKey))
                    throw new ArgumentException("Both country and bio localization keys must be provided.");

                // Insert media if exists
                string? mediaId = null;
                if (dto.Media != null && dto.Media.LinkLocalizations.Any())
                {
                    mediaId = await InsertOrUpdateMediaAsync(conn, tx, dto.SpaceId, dto.Media);
                }

                // Insert into personality
                const string insertSql = @"
    INSERT INTO personality (
        name, sport_id, entity_id,
        country_name_key, bio_data_key, media_id,
        creation_time, modified_time, modified_by
    )
    VALUES (
        @Name, @SportId, @EntityId,
        @CountryKey, @BioKey, @MediaId,
        @CreationTime, @ModifiedTime, @ModifiedBy
    );
    SELECT LAST_INSERT_ID();";

                int newId;
                await using (var cmd = new MySqlCommand(insertSql, conn, tx))
                {
                    cmd.Parameters.AddWithValue("@Name", dto.Name);
                    cmd.Parameters.AddWithValue("@SportId", (object?)dto.SportId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@EntityId", (object?)dto.EntityId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@CountryKey", countryKey);
                    cmd.Parameters.AddWithValue("@BioKey", bioKey);
                    cmd.Parameters.AddWithValue("@MediaId", (object?)mediaId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@CreationTime", DateTime.UtcNow);
                    cmd.Parameters.AddWithValue("@ModifiedTime", DateTime.UtcNow);
                    cmd.Parameters.AddWithValue("@ModifiedBy", dto.ModifiedBy ?? "system");

                    newId = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                }

                // Insert i18n for country and bio
                const string insertI18n = @"
    INSERT INTO i18n (`key`, locale_id, value, space_id)
    VALUES (@Key, @LocaleId, @Value, @SpaceId);";

                foreach (var loc in dto.LocalizedCountry.Values)
                {
                    await using var cmd = new MySqlCommand(insertI18n, conn, tx);
                    cmd.Parameters.AddWithValue("@Key", dto.LocalizedCountry.Key);
                    cmd.Parameters.AddWithValue("@LocaleId", loc.LocaleId);
                    cmd.Parameters.AddWithValue("@Value", (object?)loc.Value ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@SpaceId", dto.SpaceId);
                    await cmd.ExecuteNonQueryAsync();
                }

                foreach (var loc in dto.LocalizedBio.Values)
                {
                    await using var cmd = new MySqlCommand(insertI18n, conn, tx);
                    cmd.Parameters.AddWithValue("@Key", dto.LocalizedBio.Key);
                    cmd.Parameters.AddWithValue("@LocaleId", loc.LocaleId);
                    cmd.Parameters.AddWithValue("@Value", (object?)loc.Value ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@SpaceId", dto.SpaceId);
                    await cmd.ExecuteNonQueryAsync();
                }

                await tx.CommitAsync();
                return (await GetPersonalityByIdAsync(newId))!;
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }


        private static async Task<string> InsertOrUpdateMediaAsync(
            MySqlConnection conn,
            MySqlTransaction tx,
            int spaceId,
            MediaUpdateDto dto
        )
        {
            // 1) Ask MySQL itself for a new UUID()
            string mediaId;
            {
                await using var uuidCmd = new MySqlCommand("SELECT UUID()", conn, tx);
                mediaId = (await uuidCmd.ExecuteScalarAsync())!.ToString()!;
            }

            // 2) Insert into media, using that server‐generated UUID
            const string insMedia = @"
                INSERT INTO media
                  (id, space_id, media_type_id, text_key, description_key)
                VALUES
                  (@Id, @SpaceId, @MediaTypeId, @TextKey, @DescKey);
            ";

            await using (var cmd = new MySqlCommand(insMedia, conn, tx))
            {
                cmd.Parameters.AddWithValue("@Id", mediaId);
                if (spaceId == 0)
                    throw new InvalidOperationException("SpaceId is zero or not set properly.");
                cmd.Parameters.AddWithValue("@SpaceId", spaceId);
                cmd.Parameters.AddWithValue("@MediaTypeId", dto.MediaTypeId);
                // allow nulls
                cmd.Parameters.AddWithValue("@TextKey", (object?)dto.TextKey ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@DescKey", (object?)dto.DescriptionKey ?? DBNull.Value);
                await cmd.ExecuteNonQueryAsync();
            }

            // 3) Now insert each localization (their id column will default to UUID())
            const string insLoc = @"
                INSERT INTO media_localization
                  (media_id, locale_id, media_link)
                VALUES
                  (@MediaId, @LocaleId, @MediaLink);
            ";

            foreach (var loc in dto.LinkLocalizations)
            {
                await using var cmdLoc = new MySqlCommand(insLoc, conn, tx);
                cmdLoc.Parameters.AddWithValue("@MediaId", mediaId);
                cmdLoc.Parameters.AddWithValue("@LocaleId", loc.LocaleId);
                cmdLoc.Parameters.AddWithValue("@MediaLink", loc.MediaLink);
                await cmdLoc.ExecuteNonQueryAsync();
            }

            return mediaId;
        }
    }
}
