// <copyright file="PersonalityRepository.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Syed Hussain</author>
// <date>09/04/2025</date>
// <summary>Personality APIs</summary>
using GMS.TifoXRCoreWebAPI.Models;
using GMS.TifoXRCoreWebAPI.Models.Common;
using GMS.TifoXRCoreWebAPI.Repositories;
using GMS.TifoXRCoreWebAPI.Repositories.Interfaces;
using System.Data;
using TifoXRCoreWebAPI.Utilities.Infrastructure.Interface;

namespace GMS.TifoXRCoreWebAPI.Repositories
{
    public class PersonalityRepository : IPersonalityRepository
    {
        private readonly IDbProvider _db;

        public PersonalityRepository(IConfiguration configuration, IDbProvider db)
        {
            _db = db;
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

                ic.locale_id              AS locale_id,
                ic.value                  AS country_value,
                ib.value                  AS bio_value,
                ml.media_link             AS media_link
            FROM personality p
            LEFT JOIN i18n AS ic
                   ON ic.`key` = p.country_name_key
            LEFT JOIN i18n AS ib
                   ON ib.`key` = p.bio_data_key
                  AND ib.locale_id = ic.locale_id
            LEFT JOIN media_localization AS ml
                   ON ml.media_id = p.media_id
                  AND ml.locale_id = ic.locale_id
            WHERE p.id = @PersonalityId
            ORDER BY ic.locale_id;";

            await using var conn = await _db.OpenConnectionAsync();
            await using var cmd = _db.CreateCommand(conn, sql);
            cmd.Parameters.Add(_db.CreateParameter("@PersonalityId", id));

            await using var reader = await cmd.ExecuteReaderAsync();

            PersonalityData? personality = null;
            var countryValues = new List<LocalizedValue>();
            var bioValues = new List<LocalizedValue>();
            var mediaLocalizations = new List<MediaLocalization>();

            bool ordReady = false;
            int o_id = -1, o_name = -1, o_sport_id = -1, o_entity_id = -1, o_country_name_key = -1, o_bio_data_key = -1, o_media_id = -1;
            int o_creation_time = -1, o_modified_time = -1, o_modified_by = -1;
            int o_locale_id = -1, o_country_value = -1, o_bio_value = -1, o_media_link = -1;

            while (await reader.ReadAsync())
            {
                if (!ordReady)
                {
                    o_id = reader.GetOrdinal("id");
                    o_name = reader.GetOrdinal("name");
                    o_sport_id = reader.GetOrdinal("sport_id");
                    o_entity_id = reader.GetOrdinal("entity_id");
                    o_country_name_key = reader.GetOrdinal("country_name_key");
                    o_bio_data_key = reader.GetOrdinal("bio_data_key");
                    o_media_id = reader.GetOrdinal("media_id");
                    o_creation_time = reader.GetOrdinal("creation_time");
                    o_modified_time = reader.GetOrdinal("modified_time");
                    o_modified_by = reader.GetOrdinal("modified_by");

                    o_locale_id = reader.GetOrdinal("locale_id");
                    o_country_value = reader.GetOrdinal("country_value");
                    o_bio_value = reader.GetOrdinal("bio_value");
                    o_media_link = reader.GetOrdinal("media_link");

                    ordReady = true;
                }

                if (personality == null)
                {
                    personality = new PersonalityData
                    {
                        Id = reader.GetInt32(o_id),
                        Name = reader.GetString(o_name),
                        SportId = reader.IsDBNull(o_sport_id) ? (int?)null : reader.GetInt32(o_sport_id),
                        EntityId = reader.IsDBNull(o_entity_id) ? (int?)null : reader.GetInt32(o_entity_id),
                        CreationTime = reader.GetDateTime(o_creation_time),
                        ModifiedTime = reader.GetDateTime(o_modified_time),
                        ModifiedBy = reader.GetString(o_modified_by),

                        LocalizedCountry = new LocalizedPairs
                        {
                            Key = reader.IsDBNull(o_country_name_key) ? null : reader.GetString(o_country_name_key),
                            Values = countryValues
                        },
                        LocalizedBio = new LocalizedPairs
                        {
                            Key = reader.IsDBNull(o_bio_data_key) ? null : reader.GetString(o_bio_data_key),
                            Values = bioValues
                        },
                        Media = reader.IsDBNull(o_media_id)
                            ? null
                            : new MediaData
                            {
                                Id = reader.GetString(o_media_id),
                                LinkLocalizations = mediaLocalizations
                            }
                    };
                }

                if (!reader.IsDBNull(o_locale_id))
                {
                    var locale = reader.GetString(o_locale_id);

                    if (!reader.IsDBNull(o_country_value))
                    {
                        countryValues.Add(new LocalizedValue
                        {
                            LocaleId = locale,
                            Value = reader.GetString(o_country_value)
                        });
                    }

                    if (!reader.IsDBNull(o_bio_value))
                    {
                        bioValues.Add(new LocalizedValue
                        {
                            LocaleId = locale,
                            Value = reader.GetString(o_bio_value)
                        });
                    }

                    if (!reader.IsDBNull(o_media_link))
                    {
                        if (!mediaLocalizations.Any(x => x.LocaleId == locale))
                        {
                            mediaLocalizations.Add(new MediaLocalization
                            {
                                LocaleId = locale,
                                MediaLink = reader.GetString(o_media_link)
                            });
                        }
                    }
                }
            }

            return personality;
        }


        public async Task<PersonalityData> CreatePersonalityAsync(PersonalityCreateDto dto)
        {
            ArgumentNullException.ThrowIfNull(dto);

            var countryKey = dto.LocalizedCountry?.Key;
            var bioKey = dto.LocalizedBio?.Key;

            if (string.IsNullOrWhiteSpace(countryKey) || string.IsNullOrWhiteSpace(bioKey))
                throw new ArgumentException("Both country and bio localization keys must be provided.");

            await using var conn = await _db.OpenConnectionAsync();
            await using var tx = await conn.BeginTransactionAsync();

            try
            {
                // 1) Insert media if present (requires at least one link localization)
                string? mediaId = null;
                if (dto.Media != null && dto.Media.LinkLocalizations != null && dto.Media.LinkLocalizations.Any())
                {
                    mediaId = await InsertOrUpdateMediaAsync(conn, tx, dto.SpaceId, dto.Media);
                }

                // 2) Insert into personality
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
                );";

                await using (var cmd = _db.CreateCommand(conn, insertSql))
                {
                    cmd.Transaction = tx;
                    cmd.Parameters.Add(_db.CreateParameter("@Name", dto.Name));
                    cmd.Parameters.Add(_db.CreateParameter("@SportId", (object?)dto.SportId ?? DBNull.Value));
                    cmd.Parameters.Add(_db.CreateParameter("@EntityId", (object?)dto.EntityId ?? DBNull.Value));
                    cmd.Parameters.Add(_db.CreateParameter("@CountryKey", countryKey!));
                    cmd.Parameters.Add(_db.CreateParameter("@BioKey", bioKey!));
                    cmd.Parameters.Add(_db.CreateParameter("@MediaId", (object?)mediaId ?? DBNull.Value));
                    cmd.Parameters.Add(_db.CreateParameter("@CreationTime", DateTime.UtcNow));
                    cmd.Parameters.Add(_db.CreateParameter("@ModifiedTime", DateTime.UtcNow));
                    cmd.Parameters.Add(_db.CreateParameter("@ModifiedBy", string.IsNullOrWhiteSpace(dto.ModifiedBy) ? "system" : dto.ModifiedBy!));
                    await cmd.ExecuteNonQueryAsync();
                }

                int newId;
                await using (var idCmd = _db.CreateCommand(conn, "SELECT LAST_INSERT_ID();"))
                {
                    idCmd.Transaction = tx;
                    newId = Convert.ToInt32(await idCmd.ExecuteScalarAsync());
                }

                // 3) Insert i18n for country and bio (space-scoped)
                const string insertI18n = @"
                INSERT INTO i18n (`key`, locale_id, value, space_id)
                VALUES (@Key, @LocaleId, @Value, @SpaceId);";

                if (dto.LocalizedCountry?.Values != null)
                {
                    foreach (var loc in dto.LocalizedCountry.Values)
                    {
                        await using var cmd = _db.CreateCommand(conn, insertI18n);
                        cmd.Transaction = tx;
                        cmd.Parameters.Add(_db.CreateParameter("@Key", dto.LocalizedCountry.Key!));
                        cmd.Parameters.Add(_db.CreateParameter("@LocaleId", loc.LocaleId));
                        cmd.Parameters.Add(_db.CreateParameter("@Value", (object?)loc.Value ?? DBNull.Value));
                        cmd.Parameters.Add(_db.CreateParameter("@SpaceId", dto.SpaceId));
                        await cmd.ExecuteNonQueryAsync();
                    }
                }

                if (dto.LocalizedBio?.Values != null)
                {
                    foreach (var loc in dto.LocalizedBio.Values)
                    {
                        await using var cmd = _db.CreateCommand(conn, insertI18n);
                        cmd.Transaction = tx;
                        cmd.Parameters.Add(_db.CreateParameter("@Key", dto.LocalizedBio.Key!));
                        cmd.Parameters.Add(_db.CreateParameter("@LocaleId", loc.LocaleId));
                        cmd.Parameters.Add(_db.CreateParameter("@Value", (object?)loc.Value ?? DBNull.Value));
                        cmd.Parameters.Add(_db.CreateParameter("@SpaceId", dto.SpaceId));
                        await cmd.ExecuteNonQueryAsync();
                    }
                }

                await tx.CommitAsync();

                // 4) Reload and return
                return (await GetPersonalityByIdAsync(newId))!;
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        // Provider-agnostic; despite the name, this inserts a new media row (matches your original behavior)
        private async Task<string> InsertOrUpdateMediaAsync(
            System.Data.Common.DbConnection conn,
            System.Data.Common.DbTransaction tx,
            int spaceId,
            MediaUpdateDto dto)
        {
            if (spaceId == 0)
                throw new InvalidOperationException("SpaceId is zero or not set properly.");

            // 1) Get a new UUID from the DB (MySQL)
            string mediaId;
            await using (var uuidCmd = _db.CreateCommand(conn, "SELECT UUID();"))
            {
                uuidCmd.Transaction = tx;
                var o = await uuidCmd.ExecuteScalarAsync();
                mediaId = Convert.ToString(o)!;
            }

            // 2) Insert into media
            const string insMedia = @"
            INSERT INTO media (id, space_id, media_type_id, text_key, description_key)
            VALUES (@Id, @SpaceId, @MediaTypeId, @TextKey, @DescKey);";

            await using (var cmd = _db.CreateCommand(conn, insMedia))
            {
                cmd.Transaction = tx;
                cmd.Parameters.Add(_db.CreateParameter("@Id", mediaId));
                cmd.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));
                cmd.Parameters.Add(_db.CreateParameter("@MediaTypeId", dto.MediaTypeId));
                cmd.Parameters.Add(_db.CreateParameter("@TextKey", (object?)dto.TextKey ?? DBNull.Value));
                cmd.Parameters.Add(_db.CreateParameter("@DescKey", (object?)dto.DescriptionKey ?? DBNull.Value));
                await cmd.ExecuteNonQueryAsync();
            }

            // 3) Insert media_localization rows
            if (dto.LinkLocalizations != null)
            {
                const string insLoc = @"
            INSERT INTO media_localization (media_id, locale_id, media_link)
            VALUES (@MediaId, @LocaleId, @MediaLink);";

                foreach (var loc in dto.LinkLocalizations)
                {
                    await using var cmdLoc = _db.CreateCommand(conn, insLoc);
                    cmdLoc.Transaction = tx;
                    cmdLoc.Parameters.Add(_db.CreateParameter("@MediaId", mediaId));
                    cmdLoc.Parameters.Add(_db.CreateParameter("@LocaleId", loc.LocaleId));
                    cmdLoc.Parameters.Add(_db.CreateParameter("@MediaLink", loc.MediaLink));
                    await cmdLoc.ExecuteNonQueryAsync();
                }
            }

            return mediaId;
        }

        public async Task<PersonalityData> UpdatePersonalityAsync(int id, PersonalityUpdateDto dto)
        {
            await using var conn = await _db.OpenConnectionAsync();
            await using var tx = await conn.BeginTransactionAsync();

            try
            {
                // Validate
                if (string.IsNullOrWhiteSpace(dto.LocalizedCountry?.Key) ||
                    string.IsNullOrWhiteSpace(dto.LocalizedBio?.Key))
                    throw new ArgumentException("Both country and bio localization keys must be provided.");

                // Upsert media if present (and has links)
                string? mediaId = null;
                if (dto.Media != null && dto.Media.LinkLocalizations.Any())
                {
                    mediaId = await UpsertPersonalityMediaAsync(conn, tx, id, dto.SpaceId, dto.Media);
                }

                // Update personality
                const string updateSql = @"
UPDATE personality SET
    name = @Name,
    sport_id = @SportId,
    entity_id = @EntityId,
    country_name_key = @CountryKey,
    bio_data_key = @BioKey,
    media_id = @MediaId,
    modified_time = @ModifiedTime,
    modified_by = @ModifiedBy
WHERE id = @Id;";

                await using (var cmd = _db.CreateCommand(conn, updateSql))
                {
                    cmd.Transaction = tx;
                    cmd.Parameters.Add(_db.CreateParameter("@Id", id));
                    cmd.Parameters.Add(_db.CreateParameter("@Name", dto.Name));
                    cmd.Parameters.Add(_db.CreateParameter("@SportId", (object?)dto.SportId ?? DBNull.Value));
                    cmd.Parameters.Add(_db.CreateParameter("@EntityId", (object?)dto.EntityId ?? DBNull.Value));
                    cmd.Parameters.Add(_db.CreateParameter("@CountryKey", dto.LocalizedCountry.Key));
                    cmd.Parameters.Add(_db.CreateParameter("@BioKey", dto.LocalizedBio.Key));
                    cmd.Parameters.Add(_db.CreateParameter("@MediaId", (object?)mediaId ?? DBNull.Value));
                    cmd.Parameters.Add(_db.CreateParameter("@ModifiedTime", DateTime.UtcNow));
                    cmd.Parameters.Add(_db.CreateParameter("@ModifiedBy", string.IsNullOrWhiteSpace(dto.ModifiedBy) ? "system" : dto.ModifiedBy!));
                    await cmd.ExecuteNonQueryAsync();
                }

                // Upsert i18n (country)
                const string updI18n = @"
UPDATE i18n
   SET value = @Value
 WHERE `key` = @NameKey AND locale_id = @LocaleId AND space_id = @SpaceId;";
                const string insI18n = @"
INSERT INTO i18n (`key`, locale_id, value, space_id)
VALUES (@NameKey, @LocaleId, @Value, @SpaceId);";

                if (dto.LocalizedCountry?.Values != null)
                {
                    foreach (var loc in dto.LocalizedCountry.Values)
                    {
                        await using var cmdUp = _db.CreateCommand(conn, updI18n);
                        cmdUp.Transaction = tx;
                        cmdUp.Parameters.Add(_db.CreateParameter("@NameKey", dto.LocalizedCountry.Key));
                        cmdUp.Parameters.Add(_db.CreateParameter("@LocaleId", loc.LocaleId));
                        cmdUp.Parameters.Add(_db.CreateParameter("@Value", loc.Value));
                        cmdUp.Parameters.Add(_db.CreateParameter("@SpaceId", dto.SpaceId));

                        if (await cmdUp.ExecuteNonQueryAsync() == 0)
                        {
                            await using var cmdIn = _db.CreateCommand(conn, insI18n);
                            cmdIn.Transaction = tx;
                            cmdIn.Parameters.Add(_db.CreateParameter("@NameKey", dto.LocalizedCountry.Key));
                            cmdIn.Parameters.Add(_db.CreateParameter("@LocaleId", loc.LocaleId));
                            cmdIn.Parameters.Add(_db.CreateParameter("@Value", loc.Value));
                            cmdIn.Parameters.Add(_db.CreateParameter("@SpaceId", dto.SpaceId));
                            await cmdIn.ExecuteNonQueryAsync();
                        }
                    }
                }

                // Upsert i18n (bio)
                if (dto.LocalizedBio?.Values != null)
                {
                    foreach (var loc in dto.LocalizedBio.Values)
                    {
                        await using var cmdUp = _db.CreateCommand(conn, updI18n);
                        cmdUp.Transaction = tx;
                        cmdUp.Parameters.Add(_db.CreateParameter("@NameKey", dto.LocalizedBio.Key));
                        cmdUp.Parameters.Add(_db.CreateParameter("@LocaleId", loc.LocaleId));
                        cmdUp.Parameters.Add(_db.CreateParameter("@Value", loc.Value));
                        cmdUp.Parameters.Add(_db.CreateParameter("@SpaceId", dto.SpaceId));

                        if (await cmdUp.ExecuteNonQueryAsync() == 0)
                        {
                            await using var cmdIn = _db.CreateCommand(conn, insI18n);
                            cmdIn.Transaction = tx;
                            cmdIn.Parameters.Add(_db.CreateParameter("@NameKey", dto.LocalizedBio.Key));
                            cmdIn.Parameters.Add(_db.CreateParameter("@LocaleId", loc.LocaleId));
                            cmdIn.Parameters.Add(_db.CreateParameter("@Value", loc.Value));
                            cmdIn.Parameters.Add(_db.CreateParameter("@SpaceId", dto.SpaceId));
                            await cmdIn.ExecuteNonQueryAsync();
                        }
                    }
                }

                await tx.CommitAsync();
                return (await GetPersonalityByIdAsync(id))!;
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        // Provider-agnostic helper
        private async Task<string> UpsertPersonalityMediaAsync(
            System.Data.Common.DbConnection conn,
            System.Data.Common.DbTransaction tx,
            int personalityId,
            int spaceId,
            MediaUpdateDto mDto)
        {
            // 1) Get existing media_id
            string? existingMediaId = null;
            const string getMediaIdSql = "SELECT media_id FROM personality WHERE id = @PersonalityId;";
            await using (var getCmd = _db.CreateCommand(conn, getMediaIdSql))
            {
                getCmd.Transaction = tx;
                getCmd.Parameters.Add(_db.CreateParameter("@PersonalityId", personalityId));
                var result = await getCmd.ExecuteScalarAsync();
                if (result != null && result != DBNull.Value)
                    existingMediaId = Convert.ToString(result);
            }

            var mediaId = string.IsNullOrWhiteSpace(existingMediaId)
                ? Guid.NewGuid().ToString()
                : existingMediaId!;

            // 2) If new media id, update personality with it
            if (string.IsNullOrWhiteSpace(existingMediaId))
            {
                const string updatePersonalityMedia = @"
UPDATE personality
   SET media_id = @MediaId
 WHERE id = @PersonalityId;";
                await using (var cm = _db.CreateCommand(conn, updatePersonalityMedia))
                {
                    cm.Transaction = tx;
                    cm.Parameters.Add(_db.CreateParameter("@MediaId", mediaId));
                    cm.Parameters.Add(_db.CreateParameter("@PersonalityId", personalityId));
                    if (await cm.ExecuteNonQueryAsync() == 0)
                        throw new InvalidOperationException("Personality not found");
                }
            }

            // 3) Upsert media row
            const string updateMedia = @"
UPDATE media
   SET media_type_id = @MediaTypeId, text_key = @TextKey, description_key = @DescKey
 WHERE id = @MediaId AND space_id = @SpaceId;";
            const string insertMedia = @"
INSERT INTO media (id, space_id, media_type_id, text_key, description_key)
VALUES (@MediaId, @SpaceId, @MediaTypeId, @TextKey, @DescKey);";

            await using (var cm2 = _db.CreateCommand(conn, updateMedia))
            {
                cm2.Transaction = tx;
                cm2.Parameters.Add(_db.CreateParameter("@MediaId", mediaId));
                cm2.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));
                cm2.Parameters.Add(_db.CreateParameter("@MediaTypeId", mDto.MediaTypeId));
                cm2.Parameters.Add(_db.CreateParameter("@TextKey", (object?)mDto.TextKey ?? DBNull.Value));
                cm2.Parameters.Add(_db.CreateParameter("@DescKey", (object?)mDto.DescriptionKey ?? DBNull.Value));

                if (await cm2.ExecuteNonQueryAsync() == 0)
                {
                    await using var ci2 = _db.CreateCommand(conn, insertMedia);
                    ci2.Transaction = tx;
                    ci2.Parameters.Add(_db.CreateParameter("@MediaId", mediaId));
                    ci2.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));
                    ci2.Parameters.Add(_db.CreateParameter("@MediaTypeId", mDto.MediaTypeId));
                    ci2.Parameters.Add(_db.CreateParameter("@TextKey", (object?)mDto.TextKey ?? DBNull.Value));
                    ci2.Parameters.Add(_db.CreateParameter("@DescKey", (object?)mDto.DescriptionKey ?? DBNull.Value));
                    await ci2.ExecuteNonQueryAsync();
                }
            }

            // 4) Upsert media_localization rows
            const string updateLoc = @"
UPDATE media_localization
   SET media_link = @MediaLink
 WHERE media_id = @MediaId AND locale_id = @LocaleId;";
            const string insertLoc = @"
INSERT INTO media_localization (id, media_id, locale_id, media_link)
VALUES (@Id, @MediaId, @LocaleId, @MediaLink);";

            if (mDto.LinkLocalizations != null)
            {
                foreach (var loc in mDto.LinkLocalizations)
                {
                    await using var cl = _db.CreateCommand(conn, updateLoc);
                    cl.Transaction = tx;
                    cl.Parameters.Add(_db.CreateParameter("@MediaId", mediaId));
                    cl.Parameters.Add(_db.CreateParameter("@LocaleId", loc.LocaleId));
                    cl.Parameters.Add(_db.CreateParameter("@MediaLink", loc.MediaLink));

                    if (await cl.ExecuteNonQueryAsync() == 0)
                    {
                        await using var ci = _db.CreateCommand(conn, insertLoc);
                        ci.Transaction = tx;
                        ci.Parameters.Add(_db.CreateParameter("@Id", Guid.NewGuid().ToString()));
                        ci.Parameters.Add(_db.CreateParameter("@MediaId", mediaId));
                        ci.Parameters.Add(_db.CreateParameter("@LocaleId", loc.LocaleId));
                        ci.Parameters.Add(_db.CreateParameter("@MediaLink", loc.MediaLink));
                        await ci.ExecuteNonQueryAsync();
                    }
                }
            }

            return mediaId;
        }


        public async Task<bool> DeletePersonalityAsync(int id)
        {
            await using var conn = await _db.OpenConnectionAsync();
            await using var tx = await conn.BeginTransactionAsync();

            try
            {
                // 1) Fetch keys + media id
                const string fetchSql = @"
            SELECT country_name_key, bio_data_key, media_id
            FROM personality
            WHERE id = @Id;";

                string? countryKey = null, bioKey = null, mediaId = null;

                await using (var fetch = _db.CreateCommand(conn, fetchSql))
                {
                    fetch.Transaction = tx;
                    fetch.Parameters.Add(_db.CreateParameter("@Id", id));

                    await using var r = await fetch.ExecuteReaderAsync();
                    if (!await r.ReadAsync())
                    {
                        await tx.RollbackAsync();
                        return false; // not found
                    }

                    var oCountry = r.GetOrdinal("country_name_key");
                    var oBio = r.GetOrdinal("bio_data_key");
                    var oMedia = r.GetOrdinal("media_id");

                    countryKey = r.IsDBNull(oCountry) ? null : r.GetString(oCountry);
                    bioKey = r.IsDBNull(oBio) ? null : r.GetString(oBio);
                    mediaId = r.IsDBNull(oMedia) ? null : r.GetString(oMedia);
                }

                // 2) Delete i18n rows for the keys (no space filter)
                if (!string.IsNullOrWhiteSpace(countryKey))
                {
                    const string delI18nCountry = "DELETE FROM i18n WHERE `key` = @Key;";
                    await using var c1 = _db.CreateCommand(conn, delI18nCountry);
                    c1.Transaction = tx;
                    c1.Parameters.Add(_db.CreateParameter("@Key", countryKey!));
                    await c1.ExecuteNonQueryAsync();
                }

                if (!string.IsNullOrWhiteSpace(bioKey))
                {
                    const string delI18nBio = "DELETE FROM i18n WHERE `key` = @Key;";
                    await using var c2 = _db.CreateCommand(conn, delI18nBio);
                    c2.Transaction = tx;
                    c2.Parameters.Add(_db.CreateParameter("@Key", bioKey!));
                    await c2.ExecuteNonQueryAsync();
                }

                // 3) Delete the personality row
                const string delPersonality = "DELETE FROM personality WHERE id = @Id;";
                await using (var dp = _db.CreateCommand(conn, delPersonality))
                {
                    dp.Transaction = tx;
                    dp.Parameters.Add(_db.CreateParameter("@Id", id));
                    await dp.ExecuteNonQueryAsync();
                }

                // 4) Delete attached media + localizations (if any)
                if (!string.IsNullOrWhiteSpace(mediaId))
                {
                    const string delMediaLoc = "DELETE FROM media_localization WHERE media_id = @MediaId;";
                    await using (var dml = _db.CreateCommand(conn, delMediaLoc))
                    {
                        dml.Transaction = tx;
                        dml.Parameters.Add(_db.CreateParameter("@MediaId", mediaId!));
                        await dml.ExecuteNonQueryAsync();
                    }

                    const string delMedia = "DELETE FROM media WHERE id = @MediaId;";
                    await using (var dm = _db.CreateCommand(conn, delMedia))
                    {
                        dm.Transaction = tx;
                        dm.Parameters.Add(_db.CreateParameter("@MediaId", mediaId!));
                        await dm.ExecuteNonQueryAsync();
                    }
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
