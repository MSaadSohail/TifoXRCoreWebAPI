// Repositories/CharityRepository.cs
using GMS.TifoXRCoreWebAPI.Models;
using GMS.TifoXRCoreWebAPI.Models.Common;
using GMS.TifoXRCoreWebAPI.Repositories.Interfaces;
using System.Data.Common;
using GMS.TifoXRCoreWebAPI.Utilities.Infrastructure;

namespace GMS.TifoXRCoreWebAPI.Repositories
{
    public sealed class CharityRepository : ICharityRepository
    {
        private readonly IDbProvider _db;
        public CharityRepository(IConfiguration configuration, IDbProvider db) => _db = db;

        public async Task<CharityData> CreateCharityAsync(CharityCreateDto dto)
        {
            ArgumentNullException.ThrowIfNull(dto);
            if (dto.SpaceId <= 0) throw new ArgumentException("SpaceId must be provided.");

            // Auto-generate i18n keys when not provided
            var nameKey = string.IsNullOrWhiteSpace(dto.LocalizedName?.Key)
                ? Guid.NewGuid().ToString()
                : dto.LocalizedName!.Key!;
            var descKey = string.IsNullOrWhiteSpace(dto.LocalizedDescription?.Key)
                ? Guid.NewGuid().ToString()
                : dto.LocalizedDescription!.Key!;

            await using var conn = await _db.OpenConnectionAsync();
            await using var tx = await conn.BeginTransactionAsync();

            try
            {
                // (1) Optional media insert
                string? mediaId = null;
                if (dto.Media != null && dto.Media.LinkLocalizations != null && dto.Media.LinkLocalizations.Count > 0)
                {
                    mediaId = await InsertOrUpdateMediaAsync(conn, tx, dto.SpaceId, dto.Media);
                }

                // (2) Insert charity (space_id instead of entity_id)
                const string insertSql = @"
INSERT INTO charity
(space_id, name_key, description_key, website_url, logo_media_id, donation_url, is_active, creation_time, modified_time, modified_by)
VALUES
(@SpaceId, @NameKey, @DescKey, @WebsiteUrl, @LogoMediaId, @DonationUrl, @IsActive, @CreationTime, @ModifiedTime, @ModifiedBy);";

                await using (var cmd = _db.CreateCommand(conn, insertSql))
                {
                    cmd.Transaction = tx;
                    cmd.Parameters.Add(_db.CreateParameter("@SpaceId", dto.SpaceId));
                    cmd.Parameters.Add(_db.CreateParameter("@NameKey", nameKey));
                    cmd.Parameters.Add(_db.CreateParameter("@DescKey", descKey));
                    cmd.Parameters.Add(_db.CreateParameter("@WebsiteUrl", (object?)dto.WebsiteUrl ?? DBNull.Value));
                    cmd.Parameters.Add(_db.CreateParameter("@LogoMediaId", (object?)mediaId ?? DBNull.Value));
                    cmd.Parameters.Add(_db.CreateParameter("@DonationUrl", (object?)dto.DonationUrl ?? DBNull.Value));
                    cmd.Parameters.Add(_db.CreateParameter("@IsActive", dto.IsActive ?? true));
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

                // (3) Insert i18n rows (space-scoped)
                const string insI18n = @"
INSERT INTO i18n (`key`, locale_id, value, space_id)
VALUES (@Key, @LocaleId, @Value, @SpaceId);";

                if (dto.LocalizedName?.Values != null)
                {
                    foreach (var loc in dto.LocalizedName.Values)
                    {
                        await using var cmd = _db.CreateCommand(conn, insI18n);
                        cmd.Transaction = tx;
                        cmd.Parameters.Add(_db.CreateParameter("@Key", nameKey));
                        cmd.Parameters.Add(_db.CreateParameter("@LocaleId", loc.LocaleId));
                        cmd.Parameters.Add(_db.CreateParameter("@Value", (object?)loc.Value ?? DBNull.Value));
                        cmd.Parameters.Add(_db.CreateParameter("@SpaceId", dto.SpaceId));
                        await cmd.ExecuteNonQueryAsync();
                    }
                }

                if (dto.LocalizedDescription?.Values != null)
                {
                    foreach (var loc in dto.LocalizedDescription.Values)
                    {
                        await using var cmd = _db.CreateCommand(conn, insI18n);
                        cmd.Transaction = tx;
                        cmd.Parameters.Add(_db.CreateParameter("@Key", descKey));
                        cmd.Parameters.Add(_db.CreateParameter("@LocaleId", loc.LocaleId));
                        cmd.Parameters.Add(_db.CreateParameter("@Value", (object?)loc.Value ?? DBNull.Value));
                        cmd.Parameters.Add(_db.CreateParameter("@SpaceId", dto.SpaceId));
                        await cmd.ExecuteNonQueryAsync();
                    }
                }

                await tx.CommitAsync();

                // (4) Reload and return
                return (await GetCharityByIdAsync(newId, dto.SpaceId))!;
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        // Personality-style single-pass loader; space-scoped
        public async Task<CharityData?> GetCharityByIdAsync(int id, int spaceId)
        {
            const string sql = @"
    SELECT
        c.id,
        c.space_id,
        c.name_key,
        c.description_key,
        c.website_url,
        c.logo_media_id,
        c.donation_url,
        c.is_active,
        c.creation_time,
        c.modified_time,
        c.modified_by,

        -- media meta for MediaData
        m.media_type_id       AS media_type_id,
        m.text_key            AS media_text_key,
        m.description_key     AS media_desc_key,

        -- align locale rows across i18n + media_localization
        iname.locale_id       AS locale_id,
        iname.value           AS name_value,
        idesc.value           AS description_value,
        ml.media_link         AS media_link
    FROM charity c
    LEFT JOIN i18n AS iname
           ON iname.`key` = c.name_key
          AND iname.space_id = @SpaceId
    LEFT JOIN i18n AS idesc
           ON idesc.`key` = c.description_key
          AND idesc.locale_id = iname.locale_id
          AND idesc.space_id  = iname.space_id
    LEFT JOIN media AS m
           ON m.id = c.logo_media_id
          AND m.space_id = @SpaceId
    LEFT JOIN media_localization AS ml
           ON ml.media_id = c.logo_media_id
          AND ml.locale_id = iname.locale_id
    WHERE c.id = @CharityId
      AND c.space_id = @SpaceId
    ORDER BY iname.locale_id;";

            await using var conn = await _db.OpenConnectionAsync();
            await using var cmd = _db.CreateCommand(conn, sql);
            cmd.Parameters.Add(_db.CreateParameter("@CharityId", id));
            cmd.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));

            await using var reader = await cmd.ExecuteReaderAsync();

            CharityData? charity = null;
            var nameValues = new List<LocalizedValue>();
            var descValues = new List<LocalizedValue>();
            var mediaLocs = new List<MediaLocalization>();

            bool ordReady = false;
            int o_id = -1, o_space_id = -1, o_name_key = -1, o_desc_key = -1, o_website_url = -1, o_logo_media_id = -1,
                o_donation_url = -1, o_is_active = -1, o_creation_time = -1, o_modified_time = -1, o_modified_by = -1,
                o_media_type_id = -1, o_media_text_key = -1, o_media_desc_key = -1,
                o_locale_id = -1, o_name_value = -1, o_desc_value = -1, o_media_link = -1;

            string? logoMediaId = null;
            int? mediaTypeId = null; string? mediaTextKey = null; string? mediaDescKey = null;

            while (await reader.ReadAsync())
            {
                if (!ordReady)
                {
                    o_id = reader.GetOrdinal("id");
                    o_space_id = reader.GetOrdinal("space_id");
                    o_name_key = reader.GetOrdinal("name_key");
                    o_desc_key = reader.GetOrdinal("description_key");
                    o_website_url = reader.GetOrdinal("website_url");
                    o_logo_media_id = reader.GetOrdinal("logo_media_id");
                    o_donation_url = reader.GetOrdinal("donation_url");
                    o_is_active = reader.GetOrdinal("is_active");
                    o_creation_time = reader.GetOrdinal("creation_time");
                    o_modified_time = reader.GetOrdinal("modified_time");
                    o_modified_by = reader.GetOrdinal("modified_by");

                    o_media_type_id = reader.GetOrdinal("media_type_id");
                    o_media_text_key = reader.GetOrdinal("media_text_key");
                    o_media_desc_key = reader.GetOrdinal("media_desc_key");

                    o_locale_id = reader.GetOrdinal("locale_id");
                    o_name_value = reader.GetOrdinal("name_value");
                    o_desc_value = reader.GetOrdinal("description_value");
                    o_media_link = reader.GetOrdinal("media_link");

                    ordReady = true;
                }

                if (charity == null)
                {
                    logoMediaId = reader.IsDBNull(o_logo_media_id) ? null : reader.GetString(o_logo_media_id);
                    mediaTypeId = reader.IsDBNull(o_media_type_id) ? (int?)null : reader.GetInt32(o_media_type_id);
                    mediaTextKey = reader.IsDBNull(o_media_text_key) ? null : reader.GetString(o_media_text_key);
                    mediaDescKey = reader.IsDBNull(o_media_desc_key) ? null : reader.GetString(o_media_desc_key);

                    charity = new CharityData
                    {
                        Id = reader.GetInt32(o_id),
                        SpaceId = reader.GetInt32(o_space_id),
                        WebsiteUrl = reader.IsDBNull(o_website_url) ? null : reader.GetString(o_website_url),
                        DonationUrl = reader.IsDBNull(o_donation_url) ? null : reader.GetString(o_donation_url),
                        IsActive = reader.GetBoolean(o_is_active),
                        CreationTime = reader.GetDateTime(o_creation_time),
                        ModifiedTime = reader.GetDateTime(o_modified_time),
                        ModifiedBy = reader.GetString(o_modified_by),

                        LocalizedName = new LocalizedPairs
                        {
                            Key = reader.IsDBNull(o_name_key) ? null : reader.GetString(o_name_key),
                            Values = nameValues
                        },
                        LocalizedDescription = new LocalizedPairs
                        {
                            Key = reader.IsDBNull(o_desc_key) ? null : reader.GetString(o_desc_key),
                            Values = descValues
                        },

                        Media = string.IsNullOrWhiteSpace(logoMediaId)
                            ? null
                            : new MediaData
                            {
                                Id = logoMediaId!,
                                MediaTypeId = mediaTypeId ?? 0,
                                TextKey = mediaTextKey,
                                DescriptionKey = mediaDescKey,
                                LinkLocalizations = mediaLocs
                            }
                    };
                }

                if (!reader.IsDBNull(o_locale_id))
                {
                    var locale = reader.GetString(o_locale_id);

                    if (!reader.IsDBNull(o_name_value))
                        nameValues.Add(new LocalizedValue { LocaleId = locale, Value = reader.GetString(o_name_value) });

                    if (!reader.IsDBNull(o_desc_value))
                        descValues.Add(new LocalizedValue { LocaleId = locale, Value = reader.GetString(o_desc_value) });

                    if (!reader.IsDBNull(o_media_link) && !string.IsNullOrWhiteSpace(logoMediaId))
                    {
                        if (!mediaLocs.Any(x => x.LocaleId == locale))
                        {
                            mediaLocs.Add(new MediaLocalization
                            {
                                LocaleId = locale,
                                MediaLink = reader.GetString(o_media_link)
                            });
                        }
                    }
                }
            }

            return charity;
        }

        private async Task<string> InsertOrUpdateMediaAsync(
            DbConnection conn, DbTransaction tx, int spaceId, MediaUpdateDto dto)
        {
            if (spaceId == 0) throw new InvalidOperationException("SpaceId is zero or not set properly.");

            string mediaId;
            await using (var uuidCmd = _db.CreateCommand(conn, "SELECT UUID();"))
            {
                uuidCmd.Transaction = tx;
                mediaId = Convert.ToString(await uuidCmd.ExecuteScalarAsync())!;
            }

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

        // Repositories/CharityRepository.cs  (add this method)
        public async Task<List<CharityData>> GetCharitiesBySpaceAsync(int spaceId)
        {
            const string sql = @"
    SELECT
        c.id,
        c.space_id,
        c.name_key,
        c.description_key,
        c.website_url,
        c.logo_media_id,
        c.donation_url,
        c.is_active,
        c.creation_time,
        c.modified_time,
        c.modified_by,

        m.media_type_id       AS media_type_id,
        m.text_key            AS media_text_key,
        m.description_key     AS media_desc_key,

        iname.locale_id       AS locale_id,
        iname.value           AS name_value,
        idesc.value           AS description_value,
        ml.media_link         AS media_link
    FROM charity c
    LEFT JOIN i18n AS iname
           ON iname.`key`   = c.name_key
          AND iname.space_id = @SpaceId
    LEFT JOIN i18n AS idesc
           ON idesc.`key`    = c.description_key
          AND idesc.locale_id= iname.locale_id
          AND idesc.space_id = iname.space_id
    LEFT JOIN media AS m
           ON m.id      = c.logo_media_id
          AND m.space_id= @SpaceId
    LEFT JOIN media_localization AS ml
           ON ml.media_id = c.logo_media_id
          AND ml.locale_id= iname.locale_id
    WHERE c.space_id = @SpaceId
    ORDER BY c.id, iname.locale_id;";

            await using var conn = await _db.OpenConnectionAsync();
            await using var cmd = _db.CreateCommand(conn, sql);
            cmd.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));

            var map = new Dictionary<int, CharityData>();
            await using var reader = await cmd.ExecuteReaderAsync();

            bool ordReady = false;
            int o_id = -1, o_space_id = -1, o_name_key = -1, o_desc_key = -1, o_website_url = -1, o_logo_media_id = -1,
                o_donation_url = -1, o_is_active = -1, o_creation_time = -1, o_modified_time = -1, o_modified_by = -1,
                o_media_type_id = -1, o_media_text_key = -1, o_media_desc_key = -1,
                o_locale_id = -1, o_name_value = -1, o_desc_value = -1, o_media_link = -1;

            while (await reader.ReadAsync())
            {
                if (!ordReady)
                {
                    o_id = reader.GetOrdinal("id");
                    o_space_id = reader.GetOrdinal("space_id");
                    o_name_key = reader.GetOrdinal("name_key");
                    o_desc_key = reader.GetOrdinal("description_key");
                    o_website_url = reader.GetOrdinal("website_url");
                    o_logo_media_id = reader.GetOrdinal("logo_media_id");
                    o_donation_url = reader.GetOrdinal("donation_url");
                    o_is_active = reader.GetOrdinal("is_active");
                    o_creation_time = reader.GetOrdinal("creation_time");
                    o_modified_time = reader.GetOrdinal("modified_time");
                    o_modified_by = reader.GetOrdinal("modified_by");

                    o_media_type_id = reader.GetOrdinal("media_type_id");
                    o_media_text_key = reader.GetOrdinal("media_text_key");
                    o_media_desc_key = reader.GetOrdinal("media_desc_key");

                    o_locale_id = reader.GetOrdinal("locale_id");
                    o_name_value = reader.GetOrdinal("name_value");
                    o_desc_value = reader.GetOrdinal("description_value");
                    o_media_link = reader.GetOrdinal("media_link");

                    ordReady = true;
                }

                var id = reader.GetInt32(o_id);
                if (!map.TryGetValue(id, out var charity))
                {
                    var logoMediaId = reader.IsDBNull(o_logo_media_id) ? null : reader.GetString(o_logo_media_id);
                    var mediaTypeId = reader.IsDBNull(o_media_type_id) ? (int?)null : reader.GetInt32(o_media_type_id);
                    var mediaTextKey = reader.IsDBNull(o_media_text_key) ? null : reader.GetString(o_media_text_key);
                    var mediaDescKey = reader.IsDBNull(o_media_desc_key) ? null : reader.GetString(o_media_desc_key);

                    var nameVals = new List<LocalizedValue>();
                    var descVals = new List<LocalizedValue>();
                    var mediaLocs = new List<MediaLocalization>();

                    charity = new CharityData
                    {
                        Id = id,
                        SpaceId = reader.GetInt32(o_space_id),
                        WebsiteUrl = reader.IsDBNull(o_website_url) ? null : reader.GetString(o_website_url),
                        DonationUrl = reader.IsDBNull(o_donation_url) ? null : reader.GetString(o_donation_url),
                        IsActive = reader.GetBoolean(o_is_active),
                        CreationTime = reader.GetDateTime(o_creation_time),
                        ModifiedTime = reader.GetDateTime(o_modified_time),
                        ModifiedBy = reader.GetString(o_modified_by),

                        LocalizedName = new LocalizedPairs { Key = reader.IsDBNull(o_name_key) ? null : reader.GetString(o_name_key), Values = nameVals },
                        LocalizedDescription = new LocalizedPairs { Key = reader.IsDBNull(o_desc_key) ? null : reader.GetString(o_desc_key), Values = descVals },

                        Media = string.IsNullOrWhiteSpace(logoMediaId)
                            ? null
                            : new MediaData
                            {
                                Id = logoMediaId!,
                                MediaTypeId = mediaTypeId ?? 0,
                                TextKey = mediaTextKey,
                                DescriptionKey = mediaDescKey,
                                LinkLocalizations = mediaLocs
                            }
                    };

                    map[id] = charity;
                }

                // add per-locale values (de-dup per charity+locale)
                if (!reader.IsDBNull(o_locale_id))
                {
                    var locale = reader.GetString(o_locale_id);

                    if (!reader.IsDBNull(o_name_value) &&
                        !charity.LocalizedName.Values.Any(v => v.LocaleId == locale))
                    {
                        charity.LocalizedName.Values.Add(new LocalizedValue
                        {
                            LocaleId = locale,
                            Value = reader.GetString(o_name_value)
                        });
                    }

                    if (!reader.IsDBNull(o_desc_value) &&
                        !charity.LocalizedDescription.Values.Any(v => v.LocaleId == locale))
                    {
                        charity.LocalizedDescription.Values.Add(new LocalizedValue
                        {
                            LocaleId = locale,
                            Value = reader.GetString(o_desc_value)
                        });
                    }

                    if (charity.Media != null &&
                        !reader.IsDBNull(o_media_link) &&
                        !charity.Media.LinkLocalizations.Any(x => x.LocaleId == locale))
                    {
                        charity.Media.LinkLocalizations.Add(new MediaLocalization
                        {
                            LocaleId = locale,
                            MediaLink = reader.GetString(o_media_link)
                        });
                    }
                }
            }

            return map.Values.ToList();
        }
        public async Task<CharityData> UpdateCharityAsync(int spaceId, int id, CharityUpdateDto dto)
        {
            ArgumentNullException.ThrowIfNull(dto);
            if (spaceId <= 0) throw new ArgumentException("spaceId must be provided.");

            await using var conn = await _db.OpenConnectionAsync();
            await using var tx = await conn.BeginTransactionAsync();

            try
            {
                // Load immutable keys + existing media id
                const string loadSql = @"
SELECT name_key, description_key, logo_media_id
FROM charity
WHERE id = @Id AND space_id = @SpaceId
FOR UPDATE;";
                string? nameKey = null, descKey = null, existingMediaId = null;

                await using (var lc = _db.CreateCommand(conn, loadSql))
                {
                    lc.Transaction = tx;
                    lc.Parameters.Add(_db.CreateParameter("@Id", id));
                    lc.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));
                    await using var r = await lc.ExecuteReaderAsync();
                    if (!await r.ReadAsync())
                        throw new InvalidOperationException("Charity not found for given id/space.");
                    nameKey = r.GetString(0);
                    descKey = r.GetString(1);
                    existingMediaId = r.IsDBNull(2) ? null : r.GetString(2);
                }

                // Reject key changes if client tries to send them
                if (!string.IsNullOrWhiteSpace(dto.LocalizedName?.Key) && !string.Equals(dto.LocalizedName!.Key, nameKey, StringComparison.Ordinal))
                    throw new InvalidOperationException("name_key cannot be changed.");
                if (!string.IsNullOrWhiteSpace(dto.LocalizedDescription?.Key) && !string.Equals(dto.LocalizedDescription!.Key, descKey, StringComparison.Ordinal))
                    throw new InvalidOperationException("description_key cannot be changed.");

                // Upsert media if provided (meta and/or links)
                string? mediaId = existingMediaId;
                if (dto.Media != null)
                    mediaId = await UpsertCharityMediaAsync(conn, tx, id, spaceId, existingMediaId, dto.Media);

                // Partial scalar update: keep existing when null (COALESCE)
                const string updSql = @"
UPDATE charity SET
    website_url   = COALESCE(@WebsiteUrl, website_url),
    logo_media_id = COALESCE(@MediaId,    logo_media_id),
    donation_url  = COALESCE(@DonationUrl, donation_url),
    is_active     = COALESCE(@IsActive,    is_active),
    modified_time = @ModifiedTime,
    modified_by   = @ModifiedBy
WHERE id = @Id AND space_id = @SpaceId;";

                await using (var cmd = _db.CreateCommand(conn, updSql))
                {
                    cmd.Transaction = tx;
                    cmd.Parameters.Add(_db.CreateParameter("@Id", id));
                    cmd.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));
                    cmd.Parameters.Add(_db.CreateParameter("@WebsiteUrl", (object?)dto.WebsiteUrl ?? DBNull.Value));
                    cmd.Parameters.Add(_db.CreateParameter("@DonationUrl", (object?)dto.DonationUrl ?? DBNull.Value));
                    cmd.Parameters.Add(_db.CreateParameter("@IsActive", dto.IsActive.HasValue ? (object)dto.IsActive.Value : DBNull.Value));
                    cmd.Parameters.Add(_db.CreateParameter("@MediaId", (object?)mediaId ?? DBNull.Value));
                    cmd.Parameters.Add(_db.CreateParameter("@ModifiedTime", DateTime.UtcNow));
                    cmd.Parameters.Add(_db.CreateParameter("@ModifiedBy", string.IsNullOrWhiteSpace(dto.ModifiedBy) ? "system" : dto.ModifiedBy!));
                    if (await cmd.ExecuteNonQueryAsync() == 0)
                        throw new InvalidOperationException("Update failed or no rows affected.");
                }

                // Upsert i18n VALUES (keys are fixed)
                const string updI18n = @"
UPDATE i18n
   SET value = @Value
 WHERE `key` = @K AND locale_id = @LocaleId AND space_id = @SpaceId;";
                const string insI18n = @"
INSERT INTO i18n (`key`, locale_id, value, space_id)
VALUES (@K, @LocaleId, @Value, @SpaceId);";

                if (dto.LocalizedName?.Values != null)
                {
                    foreach (var loc in dto.LocalizedName.Values)
                    {
                        await using var cu = _db.CreateCommand(conn, updI18n);
                        cu.Transaction = tx;
                        cu.Parameters.Add(_db.CreateParameter("@K", nameKey!));
                        cu.Parameters.Add(_db.CreateParameter("@LocaleId", loc.LocaleId));
                        cu.Parameters.Add(_db.CreateParameter("@Value", (object?)loc.Value ?? DBNull.Value));
                        cu.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));

                        if (await cu.ExecuteNonQueryAsync() == 0)
                        {
                            await using var ci = _db.CreateCommand(conn, insI18n);
                            ci.Transaction = tx;
                            ci.Parameters.Add(_db.CreateParameter("@K", nameKey!));
                            ci.Parameters.Add(_db.CreateParameter("@LocaleId", loc.LocaleId));
                            ci.Parameters.Add(_db.CreateParameter("@Value", (object?)loc.Value ?? DBNull.Value));
                            ci.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));
                            await ci.ExecuteNonQueryAsync();
                        }
                    }
                }

                if (dto.LocalizedDescription?.Values != null)
                {
                    foreach (var loc in dto.LocalizedDescription.Values)
                    {
                        await using var cu = _db.CreateCommand(conn, updI18n);
                        cu.Transaction = tx;
                        cu.Parameters.Add(_db.CreateParameter("@K", descKey!));
                        cu.Parameters.Add(_db.CreateParameter("@LocaleId", loc.LocaleId));
                        cu.Parameters.Add(_db.CreateParameter("@Value", (object?)loc.Value ?? DBNull.Value));
                        cu.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));

                        if (await cu.ExecuteNonQueryAsync() == 0)
                        {
                            await using var ci = _db.CreateCommand(conn, insI18n);
                            ci.Transaction = tx;
                            ci.Parameters.Add(_db.CreateParameter("@K", descKey!));
                            ci.Parameters.Add(_db.CreateParameter("@LocaleId", loc.LocaleId));
                            ci.Parameters.Add(_db.CreateParameter("@Value", (object?)loc.Value ?? DBNull.Value));
                            ci.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));
                            await ci.ExecuteNonQueryAsync();
                        }
                    }
                }

                await tx.CommitAsync();
                return (await GetCharityByIdAsync(id, spaceId))!;
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        private async Task<string?> UpsertCharityMediaAsync(
            DbConnection conn,
            DbTransaction tx,
            int charityId,
            int spaceId,
            string? existingMediaId,
            MediaUpdateDto mDto)
        {
            var mediaId = string.IsNullOrWhiteSpace(existingMediaId) ? Guid.NewGuid().ToString() : existingMediaId!;

            if (string.IsNullOrWhiteSpace(existingMediaId))
            {
                const string setMediaSql = @"UPDATE charity SET logo_media_id = @Mid WHERE id = @Id AND space_id = @SpaceId;";
                await using var sm = _db.CreateCommand(conn, setMediaSql);
                sm.Transaction = tx;
                sm.Parameters.Add(_db.CreateParameter("@Mid", mediaId));
                sm.Parameters.Add(_db.CreateParameter("@Id", charityId));
                sm.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));
                if (await sm.ExecuteNonQueryAsync() == 0)
                    throw new InvalidOperationException("Failed to set new media id on charity.");
            }

            const string updMedia = @"
UPDATE media
   SET media_type_id = @TypeId, text_key = @TextKey, description_key = @DescKey
 WHERE id = @Mid AND space_id = @SpaceId;";
            const string insMedia = @"
INSERT INTO media (id, space_id, media_type_id, text_key, description_key)
VALUES (@Mid, @SpaceId, @TypeId, @TextKey, @DescKey);";

            await using (var cm = _db.CreateCommand(conn, updMedia))
            {
                cm.Transaction = tx;
                cm.Parameters.Add(_db.CreateParameter("@Mid", mediaId));
                cm.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));
                cm.Parameters.Add(_db.CreateParameter("@TypeId", mDto.MediaTypeId));
                cm.Parameters.Add(_db.CreateParameter("@TextKey", (object?)mDto.TextKey ?? DBNull.Value));
                cm.Parameters.Add(_db.CreateParameter("@DescKey", (object?)mDto.DescriptionKey ?? DBNull.Value));

                if (await cm.ExecuteNonQueryAsync() == 0)
                {
                    await using var ci = _db.CreateCommand(conn, insMedia);
                    ci.Transaction = tx;
                    ci.Parameters.Add(_db.CreateParameter("@Mid", mediaId));
                    ci.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));
                    ci.Parameters.Add(_db.CreateParameter("@TypeId", mDto.MediaTypeId));
                    ci.Parameters.Add(_db.CreateParameter("@TextKey", (object?)mDto.TextKey ?? DBNull.Value));
                    ci.Parameters.Add(_db.CreateParameter("@DescKey", (object?)mDto.DescriptionKey ?? DBNull.Value));
                    await ci.ExecuteNonQueryAsync();
                }
            }

            if (mDto.LinkLocalizations != null && mDto.LinkLocalizations.Count > 0)
            {
                const string updLoc = @"
UPDATE media_localization
   SET media_link = @Link
 WHERE media_id = @Mid AND locale_id = @Loc;";
                const string insLoc = @"
INSERT INTO media_localization (media_id, locale_id, media_link)
VALUES (@Mid, @Loc, @Link);";

                foreach (var loc in mDto.LinkLocalizations)
                {
                    await using var cl = _db.CreateCommand(conn, updLoc);
                    cl.Transaction = tx;
                    cl.Parameters.Add(_db.CreateParameter("@Mid", mediaId));
                    cl.Parameters.Add(_db.CreateParameter("@Loc", loc.LocaleId));
                    cl.Parameters.Add(_db.CreateParameter("@Link", loc.MediaLink));

                    if (await cl.ExecuteNonQueryAsync() == 0)
                    {
                        await using var ci = _db.CreateCommand(conn, insLoc);
                        ci.Transaction = tx;
                        ci.Parameters.Add(_db.CreateParameter("@Mid", mediaId));
                        ci.Parameters.Add(_db.CreateParameter("@Loc", loc.LocaleId));
                        ci.Parameters.Add(_db.CreateParameter("@Link", loc.MediaLink));
                        await ci.ExecuteNonQueryAsync();
                    }
                }
            }

            return mediaId;
        }

        // Repositories/CharityRepository.cs  (add this method)
        public async Task<bool> DeleteCharityAsync(int spaceId, int id)
        {
            await using var conn = await _db.OpenConnectionAsync();
            await using var tx = await conn.BeginTransactionAsync();

            try
            {
                // 1) Load keys & media id, lock the row
                const string loadSql = @"
            SELECT name_key, description_key, logo_media_id
            FROM charity
            WHERE id = @Id AND space_id = @SpaceId
            FOR UPDATE;";
                string? nameKey = null, descKey = null, mediaId = null;

                await using (var lc = _db.CreateCommand(conn, loadSql))
                {
                    lc.Transaction = tx;
                    lc.Parameters.Add(_db.CreateParameter("@Id", id));
                    lc.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));

                    await using var r = await lc.ExecuteReaderAsync();
                    if (!await r.ReadAsync())
                    {
                        await tx.RollbackAsync();
                        return false; // not found
                    }

                    nameKey = r.IsDBNull(0) ? null : r.GetString(0);
                    descKey = r.IsDBNull(1) ? null : r.GetString(1);
                    mediaId = r.IsDBNull(2) ? null : r.GetString(2);
                }

                // 2) Delete charity FIRST (break FK to media)
                const string delCharity = @"DELETE FROM charity WHERE id = @Id AND space_id = @SpaceId;";
                int affected;
                await using (var dc = _db.CreateCommand(conn, delCharity))
                {
                    dc.Transaction = tx;
                    dc.Parameters.Add(_db.CreateParameter("@Id", id));
                    dc.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));
                    affected = await dc.ExecuteNonQueryAsync();
                }
                if (affected == 0)
                {
                    await tx.RollbackAsync();
                    return false;
                }

                // 3) Delete media_localization & media (now safe)
                if (!string.IsNullOrWhiteSpace(mediaId))
                {
                    const string delMediaLoc = @"DELETE FROM media_localization WHERE media_id = @Mid;";
                    await using (var dml = _db.CreateCommand(conn, delMediaLoc))
                    {
                        dml.Transaction = tx;
                        dml.Parameters.Add(_db.CreateParameter("@Mid", mediaId!));
                        await dml.ExecuteNonQueryAsync();
                    }

                    const string delMedia = @"DELETE FROM media WHERE id = @Mid AND space_id = @SpaceId;";
                    await using (var dm = _db.CreateCommand(conn, delMedia))
                    {
                        dm.Transaction = tx;
                        dm.Parameters.Add(_db.CreateParameter("@Mid", mediaId!));
                        dm.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));
                        await dm.ExecuteNonQueryAsync();
                    }
                }

                // 4) Delete i18n rows for name/description keys (space-scoped)
                if (!string.IsNullOrWhiteSpace(nameKey))
                {
                    const string delI18n = @"DELETE FROM i18n WHERE `key` = @K AND space_id = @SpaceId;";
                    await using var di = _db.CreateCommand(conn, delI18n);
                    di.Transaction = tx;
                    di.Parameters.Add(_db.CreateParameter("@K", nameKey!));
                    di.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));
                    await di.ExecuteNonQueryAsync();
                }
                if (!string.IsNullOrWhiteSpace(descKey))
                {
                    const string delI18n = @"DELETE FROM i18n WHERE `key` = @K AND space_id = @SpaceId;";
                    await using var di = _db.CreateCommand(conn, delI18n);
                    di.Transaction = tx;
                    di.Parameters.Add(_db.CreateParameter("@K", descKey!));
                    di.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));
                    await di.ExecuteNonQueryAsync();
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
