// <copyright file="PortalRepository.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Saad Sohail</author>
// <date>07/23/2025</date>
// <summary>Class to handle portal SQL side</summary>

using MySqlConnector;
using System.Collections.Generic;
using System.Data;
using TifoXRCoreWebAPI.Models;
using TifoXRCoreWebAPI.Models.Common;
using TifoXRCoreWebAPI.Repositories.Interfaces;

namespace TifoXRCoreWebAPI.Repositories
{
    public class PortalRepository : IPortalRepository
    {
        private readonly string _connectionString;

        public PortalRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        public async Task<List<PortalModel>> GetPortalsBySpaceAsync(int spaceId)
        {
            const string sql = @"
                SELECT 
                    p.id                          AS portal_id,
                    p.space_id,
                    p.booth_id,
                    p.portal_type_id,
                    p.event_id,
                    p.external_link,
                    p.corresponding_media_id      AS corr_media_id,
                    p.thumbnail_media_id          AS thumb_media_id,
                    p.text_field_key              AS text_key,
                    
                    cm.media_type_id              AS corr_media_type_id,
                    tm.media_type_id              AS thumb_media_type_id,

                    sl.locale_id                  AS locale_id,
                    i.value                       AS localized_text_value,
                    ml1.media_link                AS corresponding_media_link,
                    ml2.media_link                AS thumbnail_media_link
                
                FROM portal p
                
                -- Get only supported languages for the space
                LEFT JOIN supported_languages sl
                  ON sl.space_id = p.space_id
                
                -- Portal name translations for supported locales
                LEFT JOIN i18n i 
                  ON p.text_field_key = i.`key`
                 AND i.space_id      = p.space_id
                 AND i.locale_id     = sl.locale_id
                
                -- Join corresponding media to get type ID
                LEFT JOIN media cm
                  ON cm.id = p.corresponding_media_id
                 AND cm.space_id = p.space_id
                
                -- Join thumbnail media to get type ID
                LEFT JOIN media tm
                  ON tm.id = p.thumbnail_media_id
                 AND tm.space_id = p.space_id
                
                -- Localized links for corresponding media
                LEFT JOIN media_localization ml1 
                  ON ml1.media_id   = p.corresponding_media_id
                 AND ml1.locale_id  = sl.locale_id
                
                -- Localized links for thumbnail media
                LEFT JOIN media_localization ml2 
                  ON ml2.media_id   = p.thumbnail_media_id
                 AND ml2.locale_id  = sl.locale_id
                
                WHERE p.space_id = @SpaceId
                ORDER BY p.id, sl.locale_id;
            ";

            await using var conn = new MySqlConnection(_connectionString);
            await conn.OpenAsync();
            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@SpaceId", spaceId);

            var map = new Dictionary<int, PortalModel>();
            await using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                var id = reader.GetInt32("portal_id");
                if (!map.TryGetValue(id, out var portal))
                {
                    portal = new PortalModel
                    {
                        PortalId = id,
                        SpaceId = reader.GetInt32("space_id"),
                        BoothId = reader.IsDBNull("booth_id") ? null : reader.GetInt32("booth_id"),
                        PortalTypeId = reader.IsDBNull("portal_type_id") ? null : reader.GetInt32("portal_type_id"),
                        EventId = reader.IsDBNull("event_id") ? null : reader.GetInt32("event_id"),
                        ExternalLink = reader.IsDBNull("external_link") ? null : reader.GetString("external_link"),

                        TextFieldKey = new LocalizedResource
                        {
                            Key = reader.GetString("text_key"),
                            Localizations = new Dictionary<string, string>()
                        },

                        CorrespondingMedia = reader.IsDBNull("corr_media_id")
                            ? null
                            : new MediaData
                            {
                                Id = reader.GetString("corr_media_id"),
                                MediaTypeId = reader.IsDBNull("corr_media_type_id") ? 0 :
                                reader.GetInt32("corr_media_type_id"),
                                Localizations = new List<MediaLocalization>()
                            },

                        ThumbnailMedia = reader.IsDBNull("thumb_media_id")
                            ? null
                            : new MediaData
                            {
                                Id = reader.GetString("thumb_media_id"),
                                MediaTypeId = reader.IsDBNull("thumb_media_type_id") ? 0 :
                                reader.GetInt32("thumb_media_type_id"),
                                Localizations = new List<MediaLocalization>()
                            }
                    };
                    map[id] = portal;
                }

                // for each supported locale, add entries (value or media_link may be null if missing)
                if (!reader.IsDBNull("locale_id"))
                {
                    var locale = reader.GetString("locale_id");
                    var textValue = reader.IsDBNull("localized_text_value") ? null : reader.GetString("localized_text_value");
                    
                    if (textValue != null)
                        portal.TextFieldKey.Localizations[locale] = textValue;

                    if (portal.CorrespondingMedia != null)
                    {
                        portal.CorrespondingMedia.Localizations.Add(new MediaLocalization
                        {
                            LocaleId = locale,
                            MediaLink = reader.IsDBNull("corresponding_media_link")
                                        ? null
                                        : reader.GetString("corresponding_media_link")
                        });
                    }

                    if (portal.ThumbnailMedia != null)
                    {
                        portal.ThumbnailMedia.Localizations.Add(new MediaLocalization
                        {
                            LocaleId = locale,
                            MediaLink = reader.IsDBNull("thumbnail_media_link")
                                        ? null
                                        : reader.GetString("thumbnail_media_link")
                        });
                    }
                }
            }

            return [.. map.Values];
        }

        public async Task<List<PortalModel>> GetPortalsByBoothAsync(int spaceId, int boothId)
        {
            const string sql = @"
                SELECT
                    p.id                      AS portal_id,
                    p.space_id,
                    p.booth_id,
                    p.portal_type_id,
                    p.event_id,
                    p.corresponding_media_id  AS corr_media_id,
                    cm.media_type_id          AS corr_media_type_id,
                    p.thumbnail_media_id      AS thumb_media_id,
                    tm.media_type_id          AS thumb_media_type_id,
                    p.text_field_key          AS text_key,
                    p.external_link,
                
                    sl.locale_id              AS locale_id,
                    i.value                   AS localized_text_value,
                
                    ml1.media_link            AS corr_media_link,
                    ml2.media_link            AS thumb_media_link
                
                FROM portal p
                
                -- Get only supported languages for the space
                LEFT JOIN supported_languages sl
                    ON sl.space_id = p.space_id

                -- Portal name translations for supported locales                
                LEFT JOIN i18n i 
                    ON i.`key`      = p.text_field_key
                   AND i.space_id   = p.space_id
                   AND i.locale_id  = sl.locale_id
                
                -- Join corresponding media to get type ID
                LEFT JOIN media cm
                    ON cm.id        = p.corresponding_media_id
                   AND cm.space_id  = p.space_id
                
                -- Join thumbnail media to get type ID
                LEFT JOIN media tm
                    ON tm.id        = p.thumbnail_media_id
                   AND tm.space_id  = p.space_id

                -- Localized links for corresponding media
                LEFT JOIN media_localization ml1 
                    ON ml1.media_id  = p.corresponding_media_id
                   AND ml1.locale_id = sl.locale_id
                
                -- Localized links for thumbnail media
                LEFT JOIN media_localization ml2 
                    ON ml2.media_id  = p.thumbnail_media_id
                   AND ml2.locale_id = sl.locale_id
                
                WHERE p.space_id = @SpaceId
                  AND p.booth_id = @BoothId
                ORDER BY p.id, sl.locale_id;
            ";

            await using var conn = new MySqlConnection(_connectionString);
            await conn.OpenAsync();
            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@SpaceId", spaceId);
            cmd.Parameters.AddWithValue("@BoothId", boothId);

            var map = new Dictionary<int, PortalModel>();
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var id = reader.GetInt32("portal_id");
                if (!map.TryGetValue(id, out var portal))
                {
                    portal = new PortalModel
                    {
                        PortalId = id,
                        SpaceId = reader.GetInt32("space_id"),
                        BoothId = reader.GetInt32("booth_id"),
                        PortalTypeId = reader.IsDBNull("portal_type_id") ? null : reader.GetInt32("portal_type_id"),
                        EventId = reader.IsDBNull("event_id") ? null : reader.GetInt32("event_id"),
                        ExternalLink = reader.IsDBNull("external_link") ? null : reader.GetString("external_link"),

                        TextFieldKey = new LocalizedResource
                        {
                            Key = reader.GetString("text_key"),
                            Localizations = new Dictionary<string, string>()
                        },

                        CorrespondingMedia = reader.IsDBNull("corr_media_id")
                            ? null
                            : new MediaData
                            {
                                Id = reader.GetString("corr_media_id"),
                                MediaTypeId = reader.IsDBNull("corr_media_type_id") ? 0 :
                                reader.GetInt32("corr_media_type_id"),
                                Localizations = new List<MediaLocalization>()
                            },

                        ThumbnailMedia = reader.IsDBNull("thumb_media_id")
                            ? null
                            : new MediaData
                            {
                                Id = reader.GetString("thumb_media_id"),
                                MediaTypeId = reader.IsDBNull("thumb_media_type_id") ? 0 :
                                reader.GetInt32("thumb_media_type_id"),
                                Localizations = new List<MediaLocalization>()
                            }
                    };

                    map[id] = portal;
                }

                if (!reader.IsDBNull("locale_id"))
                {
                    var locale = reader.GetString("locale_id");
                    var textValue = reader.IsDBNull("localized_text_value") ? null : reader.GetString("localized_text_value");
                    if (textValue != null)
                        portal.TextFieldKey.Localizations[locale] = textValue;

                    if (portal.CorrespondingMedia != null)
                    {
                        portal.CorrespondingMedia.Localizations.Add(new MediaLocalization
                        {
                            LocaleId = locale,
                            MediaLink = reader.IsDBNull("corr_media_link")
                                        ? null!
                                        : reader.GetString("corr_media_link")
                        });
                    }

                    if (portal.ThumbnailMedia != null)
                    {
                        portal.ThumbnailMedia.Localizations.Add(new MediaLocalization
                        {
                            LocaleId = locale,
                            MediaLink = reader.IsDBNull("thumb_media_link")
                                        ? null!
                                        : reader.GetString("thumb_media_link")
                        });
                    }
                }
            }

            return [.. map.Values];
        }

        public async Task<PortalModel?> GetPortalByIdAsync(int spaceId, int portalId)
        {
            const string sql = @"
                SELECT 
                    p.id                          AS portal_id,
                    p.space_id,
                    p.booth_id,
                    p.portal_type_id,
                    p.event_id,
                    p.external_link,
                    p.text_field_key              AS text_key,

                    -- Corresponding media info
                    cm.id                         AS corr_media_id,
                    cm.media_type_id              AS corr_media_type_id,
                    cm.text_key                   AS corr_media_text_key,
                    cm.description_key            AS corr_media_desc_key,
                
                    -- Thumbnail media info
                    tm.id                         AS thumb_media_id,
                    tm.media_type_id              AS thumb_media_type_id,
                    tm.text_key                   AS thumb_media_text_key,
                    tm.description_key            AS thumb_media_desc_key,
                
                    -- Portal text (i18n)
                    p.text_field_key              AS i18n_key,
                    i.locale_id                   AS locale_id,
                    i.value                       AS localized_text_value,
                
                    -- Media localizations for corresponding/thumbnail media
                    ml1.media_link                AS corresponding_media_link,
                    ml2.media_link                AS thumbnail_media_link
                
                FROM portal p
                
                -- Corresponding media
                LEFT JOIN media cm 
                    ON p.corresponding_media_id = cm.id
                    AND cm.space_id = p.space_id
                
                -- Thumbnail media
                LEFT JOIN media tm 
                    ON p.thumbnail_media_id = tm.id
                    AND tm.space_id = p.space_id
                
                -- Portal i18n for text field
                LEFT JOIN i18n i 
                    ON p.text_field_key = i.`key`
                    AND i.space_id = p.space_id
                
                -- Media localizations for corresponding/thumbnail
                LEFT JOIN media_localization ml1 
                    ON ml1.media_id = p.corresponding_media_id
                    AND ml1.locale_id = i.locale_id
                
                LEFT JOIN media_localization ml2 
                    ON ml2.media_id = p.thumbnail_media_id
                    AND ml2.locale_id = i.locale_id
                
                WHERE p.id = @PortalId
                  AND p.space_id = @SpaceId
                ORDER BY i.locale_id;
            ";

            await using var conn = new MySqlConnection(_connectionString);
            await conn.OpenAsync();
            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@PortalId", portalId);
            cmd.Parameters.AddWithValue("@SpaceId", spaceId);

            await using var reader = await cmd.ExecuteReaderAsync();

            PortalModel portal = null!;

            while (await reader.ReadAsync())
            {
                portal ??= new PortalModel
                {
                    PortalId = reader.GetInt32("portal_id"),
                    SpaceId = reader.GetInt32("space_id"),
                    BoothId = reader.IsDBNull("booth_id") ? null : reader.GetInt32("booth_id"),
                    PortalTypeId = reader.IsDBNull("portal_type_id") ? null : reader.GetInt32("portal_type_id"),
                    EventId = reader.IsDBNull("event_id") ? null : reader.GetInt32("event_id"),
                    ExternalLink = reader.IsDBNull("external_link") ? null : reader.GetString("external_link"),

                    TextFieldKey = new LocalizedResource
                    {
                        Key = reader.GetString("text_key"),
                        Localizations = new Dictionary<string, string>()
                    },

                    CorrespondingMedia = reader.IsDBNull("corr_media_id")
                            ? null
                            : new MediaData
                            {
                                Id = reader.GetString("corr_media_id"),
                                MediaTypeId = reader.IsDBNull("corr_media_type_id") ? 0 : 
                                reader.GetInt32("corr_media_type_id"),
                                Localizations = new List<MediaLocalization>()
                            },

                    ThumbnailMedia = reader.IsDBNull("thumb_media_id")
                            ? null
                            : new MediaData
                            {
                                Id = reader.GetString("thumb_media_id"),
                                MediaTypeId = reader.IsDBNull("thumb_media_type_id") ? 0 : 
                                reader.GetInt32("thumb_media_type_id"),
                                Localizations = new List<MediaLocalization>()
                            }
                };

                // for this locale:
                if (!reader.IsDBNull("locale_id"))
                {
                    var locale = reader.GetString("locale_id");

                    var textValue = reader.IsDBNull("localized_text_value") ? null : reader.GetString("localized_text_value");
                    if (textValue != null)
                        portal.TextFieldKey.Localizations[locale] = textValue;

                    // add corresponding media link
                    if (portal.CorrespondingMedia != null)
                    {
                        portal.CorrespondingMedia.Localizations.Add(new MediaLocalization
                        {
                            LocaleId = locale,
                            MediaLink = reader.IsDBNull("corresponding_media_link")
                                        ? null!
                                        : reader.GetString("corresponding_media_link")
                        });
                    }

                    // add thumbnail media link
                    if (portal.ThumbnailMedia != null)
                    {
                        portal.ThumbnailMedia.Localizations.Add(new MediaLocalization
                        {
                            LocaleId = locale,
                            MediaLink = reader.IsDBNull("thumbnail_media_link")
                                        ? null!
                                        : reader.GetString("thumbnail_media_link")
                        });
                    }
                }
            }

            return portal;
        }

        public async Task<PortalModel> CreatePortalAsync(int spaceId, PortalCreateDto portalDto)
        {
            if (portalDto == null)
                throw new ArgumentNullException(nameof(portalDto));

            await using var conn = new MySqlConnection(_connectionString);
            await conn.OpenAsync();
            await using var tx = await conn.BeginTransactionAsync();

            try
            {
                // 1) Load supported locales for this space
                var supportedLocales = new HashSet<string>();
                const string fetchLocales = @"
                    SELECT locale_id
                      FROM supported_languages
                     WHERE space_id = @SpaceId;
                ";

                await using (var cmd = new MySqlCommand(fetchLocales, conn, tx))
                {
                    cmd.Parameters.AddWithValue("@SpaceId", spaceId);
                    await using var rdr = await cmd.ExecuteReaderAsync();
                    while (await rdr.ReadAsync())
                        supportedLocales.Add(rdr.GetString("locale_id"));
                }

                // 2) Optionally insert corresponding media first
                string? corrMediaId = null;
                if (portalDto.CorrespondingMedia != null)
                {
                    // Filter localizations to supported only
                    portalDto.CorrespondingMedia.Localizations =
                        portalDto.CorrespondingMedia.Localizations
                            .Where(x => supportedLocales.Contains(x.LocaleId))
                            .ToList();

                    if (portalDto.CorrespondingMedia.Localizations.Count > 0)
                        corrMediaId = await InsertOrUpdateMediaAsync(conn, tx, spaceId, portalDto.CorrespondingMedia);
                }

                // 3) Optionally insert thumbnail media
                string? thumbMediaId = null;
                if (portalDto.ThumbnailMedia != null)
                {
                    portalDto.ThumbnailMedia.Localizations =
                        portalDto.ThumbnailMedia.Localizations
                            .Where(x => supportedLocales.Contains(x.LocaleId))
                            .ToList();

                    if (portalDto.ThumbnailMedia.Localizations.Count > 0)
                        thumbMediaId = await InsertOrUpdateMediaAsync(conn, tx, spaceId, portalDto.ThumbnailMedia);
                }

                // 4) Insert portal record
                const string insertPortal = @"
                    INSERT INTO portal
                      (space_id, portal_type_id, event_id,
                       corresponding_media_id, thumbnail_media_id,
                       text_field_key, external_link)
                    VALUES
                      (@SpaceId, @PortalTypeId, @EventId,
                       @CorrId,   @ThumbId,
                       @TextKey,  @ExternalLink);
                ";
                int newPortalId;
                await using (var cmd = new MySqlCommand(insertPortal, conn, tx))
                {
                    cmd.Parameters.AddWithValue("@SpaceId", spaceId);
                    cmd.Parameters.AddWithValue("@PortalTypeId", portalDto.PortalTypeId);
                    cmd.Parameters.AddWithValue("@EventId", portalDto.EventId);
                    cmd.Parameters.AddWithValue("@CorrId", corrMediaId);
                    cmd.Parameters.AddWithValue("@ThumbId", thumbMediaId);
                    cmd.Parameters.AddWithValue("@TextKey", portalDto.LocalizedName.Key);
                    cmd.Parameters.AddWithValue("@ExternalLink", portalDto.ExternalLink);
                    await cmd.ExecuteNonQueryAsync();
                    newPortalId = Convert.ToInt32(cmd.LastInsertedId);
                }

                // 5) Insert i18n entries for the portal’s text_key, for supported locales only
                const string insertI18n = @"
                    INSERT INTO i18n (`key`, locale_id, value, space_id)
                    VALUES (@TextKey, @LocaleId, @Value, @SpaceId);
                ";

                foreach (var loc in portalDto.LocalizedName.Localizations
                    .Where(x => supportedLocales.Contains(x.Key)))
                {
                    await using var cmdI = new MySqlCommand(insertI18n, conn, tx);
                    cmdI.Parameters.AddWithValue("@TextKey", portalDto.LocalizedName.Key);
                    cmdI.Parameters.AddWithValue("@LocaleId", loc.Key);
                    cmdI.Parameters.AddWithValue("@Value", loc.Value);
                    cmdI.Parameters.AddWithValue("@SpaceId", spaceId);
                    await cmdI.ExecuteNonQueryAsync();
                }

                await tx.CommitAsync();

                // 6) Reload full portal and return
                return await GetPortalByIdAsync(spaceId, newPortalId);
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        public async Task<PortalModel?> UpdatePortalAsync(int spaceId, int portalId, PortalUpdateDto dto)
        {
            await using var conn = new MySqlConnection(_connectionString);
            await conn.OpenAsync();
            await using var tx = await conn.BeginTransactionAsync();

            try
            {
                // 1. Get existing text_key
                string textKey;
                {
                    const string fetch = @"
                        SELECT text_field_key
                        FROM portal
                        WHERE id = @PortalId
                          AND space_id = @SpaceId;
                    ";

                    await using var c = new MySqlCommand(fetch, conn, tx);
                    c.Parameters.AddWithValue("@PortalId", portalId);
                    c.Parameters.AddWithValue("@SpaceId", spaceId);

                    var o = await c.ExecuteScalarAsync();
                    if (o == null) return null;
                    textKey = o.ToString()!;
                }

                // 2. Update external_link
                const string updLink = @"
                    UPDATE portal
                    SET external_link = @ExternalLink
                    WHERE id = @PortalId
                      AND space_id = @SpaceId;
                ";

                await using (var cmdL = new MySqlCommand(updLink, conn, tx))
                {
                    cmdL.Parameters.AddWithValue("@ExternalLink", (object?)dto.ExternalLink ?? DBNull.Value);
                    cmdL.Parameters.AddWithValue("@PortalId", portalId);
                    cmdL.Parameters.AddWithValue("@SpaceId", spaceId);
                    await cmdL.ExecuteNonQueryAsync();
                }

                // 3. Upsert i18n for textKey
                const string updI18n = @"
                    UPDATE i18n SET value=@Value
                    WHERE `key`=@TextKey 
                      AND locale_id=@LocaleId 
                      AND space_id=@SpaceId;
                ";

                const string insI18n = @"
                    INSERT INTO i18n (`key`,locale_id,value,space_id)
                    VALUES(@TextKey,@LocaleId,@Value,@SpaceId);
                ";

                foreach (var kvp in dto.LocalizedName.Localizations)
                {
                    string localeId = kvp.Key;
                    string value = kvp.Value;

                    await using var cu = new MySqlCommand(updI18n, conn, tx);
                    cu.Parameters.AddWithValue("@TextKey", textKey);
                    cu.Parameters.AddWithValue("@LocaleId", localeId);
                    cu.Parameters.AddWithValue("@Value", value);
                    cu.Parameters.AddWithValue("@SpaceId", spaceId);

                    if (await cu.ExecuteNonQueryAsync() == 0)
                    {
                        await using var ci = new MySqlCommand(insI18n, conn, tx);
                        ci.Parameters.AddWithValue("@TextKey", textKey);
                        ci.Parameters.AddWithValue("@LocaleId", localeId);
                        ci.Parameters.AddWithValue("@Value", value);
                        ci.Parameters.AddWithValue("@SpaceId", spaceId);
                        await ci.ExecuteNonQueryAsync();
                    }
                }

                // 4. Helper to upsert one media block
                async Task UpsertMedia(MediaUpdateDto mDto, string columnIdName)
                {
                    var mediaId = string.IsNullOrWhiteSpace(mDto.Id)
                                    ? Guid.NewGuid().ToString()
                                    : mDto.Id;

                    // a) Update portal set column = @MediaId
                    var updPortalMedia = $@"
                        UPDATE portal
                        SET {columnIdName} = @MediaId
                        WHERE id=@PortalId AND space_id=@SpaceId;
                    ";

                    await using (var cm = new MySqlCommand(updPortalMedia, conn, tx))
                    {
                        cm.Parameters.AddWithValue("@MediaId", mediaId);
                        cm.Parameters.AddWithValue("@PortalId", portalId);
                        cm.Parameters.AddWithValue("@SpaceId", spaceId);
                        if (await cm.ExecuteNonQueryAsync() == 0)
                            throw new InvalidOperationException("Portal not found");
                    }

                    // b) Upsert media table
                    const string updMedia = @"
                        UPDATE media
                        SET media_type_id=@MediaTypeId, text_key=@TextKey, description_key=@DescKey
                        WHERE id=@MediaId AND space_id=@SpaceId;
                    ";

                    const string insMedia = @"
                        INSERT INTO media (id,space_id,media_type_id,text_key,description_key)
                        VALUES(@MediaId,@SpaceId,@MediaTypeId,@TextKey,@DescKey);
                    ";

                    await using (var cm = new MySqlCommand(updMedia, conn, tx))
                    {
                        cm.Parameters.AddWithValue("@MediaId", mediaId);
                        cm.Parameters.AddWithValue("@SpaceId", spaceId);
                        cm.Parameters.AddWithValue("@MediaTypeId", mDto.MediaTypeId);
                        cm.Parameters.AddWithValue("@TextKey", mDto.TextKey);
                        cm.Parameters.AddWithValue("@DescKey", mDto.DescriptionKey);
                        if (await cm.ExecuteNonQueryAsync() == 0)
                        {
                            await using var ci = new MySqlCommand(insMedia, conn, tx);
                            ci.Parameters.AddWithValue("@MediaId", mediaId);
                            ci.Parameters.AddWithValue("@SpaceId", spaceId);
                            ci.Parameters.AddWithValue("@MediaTypeId", mDto.MediaTypeId);
                            ci.Parameters.AddWithValue("@TextKey", mDto.TextKey);
                            ci.Parameters.AddWithValue("@DescKey", mDto.DescriptionKey);
                            await ci.ExecuteNonQueryAsync();
                        }
                    }

                    // c) Upsert localizations
                    const string updLoc = @"
                        UPDATE media_localization
                        SET media_link=@MediaLink
                        WHERE media_id=@MediaId AND locale_id=@LocaleId;
                    ";

                    const string insLoc = @"
                        INSERT INTO media_localization (id,media_id,locale_id,media_link)
                        VALUES(@Id,@MediaId,@LocaleId,@MediaLink);
                    ";

                    foreach (var loc in mDto.Localizations)
                    {
                        await using var cl = new MySqlCommand(updLoc, conn, tx);
                        cl.Parameters.AddWithValue("@MediaId", mediaId);
                        cl.Parameters.AddWithValue("@LocaleId", loc.LocaleId);
                        cl.Parameters.AddWithValue("@MediaLink", loc.MediaLink);
                        if (await cl.ExecuteNonQueryAsync() == 0)
                        {
                            await using var ci = new MySqlCommand(insLoc, conn, tx);
                            ci.Parameters.AddWithValue("@Id", Guid.NewGuid().ToString());
                            ci.Parameters.AddWithValue("@MediaId", mediaId);
                            ci.Parameters.AddWithValue("@LocaleId", loc.LocaleId);
                            ci.Parameters.AddWithValue("@MediaLink", loc.MediaLink);
                            await ci.ExecuteNonQueryAsync();
                        }
                    }
                }

                // 5. Apply to correspondingMedia & thumbnailMedia if provided
                if (dto.CorrespondingMedia != null)
                    await UpsertMedia(dto.CorrespondingMedia, "corresponding_media_id");
                if (dto.ThumbnailMedia != null)
                    await UpsertMedia(dto.ThumbnailMedia, "thumbnail_media_id");

                await tx.CommitAsync();

                return await GetPortalByIdAsync(spaceId, portalId);
            }
            catch (Exception)
            {
                await tx.RollbackAsync();
                return null;
            }
        }

        public async Task<PortalModel?> UpdatePortalAsync(int spaceId, int boothId, int portalId, PortalUpdateDto dto)
        {
            await using var conn = new MySqlConnection(_connectionString);
            await conn.OpenAsync();
            await using var tx = await conn.BeginTransactionAsync();

            try
            {
                // Fetch the text_key, confirm portal and booth match
                const string fetch = @"
                    SELECT text_field_key
                    FROM portal
                    WHERE id = @PortalId
                      AND space_id = @SpaceId
                      AND booth_id = @BoothId;
                ";

                string textKey;
                await using (var c = new MySqlCommand(fetch, conn, tx))
                {
                    c.Parameters.AddWithValue("@PortalId", portalId);
                    c.Parameters.AddWithValue("@SpaceId", spaceId);
                    c.Parameters.AddWithValue("@BoothId", boothId);

                    var o = await c.ExecuteScalarAsync();
                    if (o == null)
                    {
                        await tx.RollbackAsync();
                        return null; // Not found or booth mismatch
                    }
                    textKey = o.ToString()!;
                }

                // Update external_link
                const string updLink = @"
                    UPDATE portal
                    SET external_link = @ExternalLink
                    WHERE id = @PortalId
                      AND space_id = @SpaceId
                      AND booth_id = @BoothId;
                ";

                await using (var cmdL = new MySqlCommand(updLink, conn, tx))
                {
                    cmdL.Parameters.AddWithValue("@ExternalLink", (object?)dto.ExternalLink ?? DBNull.Value);
                    cmdL.Parameters.AddWithValue("@PortalId", portalId);
                    cmdL.Parameters.AddWithValue("@SpaceId", spaceId);
                    cmdL.Parameters.AddWithValue("@BoothId", boothId);
                    await cmdL.ExecuteNonQueryAsync();
                }

                // Upsert i18n for textKey
                const string updI18n = @"
                    UPDATE i18n SET value=@Value
                    WHERE `key`=@TextKey 
                      AND locale_id=@LocaleId 
                      AND space_id=@SpaceId;
                ";

                const string insI18n = @"
                    INSERT INTO i18n (`key`,locale_id,value,space_id)
                    VALUES(@TextKey,@LocaleId,@Value,@SpaceId);
                ";

                foreach (var kvp in dto.LocalizedName.Localizations)
                {
                    string localeId = kvp.Key;
                    string value = kvp.Value;

                    await using var cu = new MySqlCommand(updI18n, conn, tx);
                    cu.Parameters.AddWithValue("@TextKey", textKey);
                    cu.Parameters.AddWithValue("@LocaleId", localeId);
                    cu.Parameters.AddWithValue("@Value", value);
                    cu.Parameters.AddWithValue("@SpaceId", spaceId);

                    if (await cu.ExecuteNonQueryAsync() == 0)
                    {
                        await using var ci = new MySqlCommand(insI18n, conn, tx);
                        ci.Parameters.AddWithValue("@TextKey", textKey);
                        ci.Parameters.AddWithValue("@LocaleId", localeId);
                        ci.Parameters.AddWithValue("@Value", value);
                        ci.Parameters.AddWithValue("@SpaceId", spaceId);
                        await ci.ExecuteNonQueryAsync();
                    }
                }


                // Helper to upsert one media block
                async Task UpsertMedia(MediaUpdateDto mDto, string columnIdName)
                {
                    var mediaId = string.IsNullOrWhiteSpace(mDto.Id)
                        ? Guid.NewGuid().ToString()
                        : mDto.Id;

                    // Update portal to link to new media ID
                    var updPortalMedia = $@"
                        UPDATE portal
                        SET {columnIdName} = @MediaId
                        WHERE id = @PortalId AND space_id = @SpaceId AND booth_id = @BoothId;
                    ";

                    await using (var cm = new MySqlCommand(updPortalMedia, conn, tx))
                    {
                        cm.Parameters.AddWithValue("@MediaId", mediaId);
                        cm.Parameters.AddWithValue("@PortalId", portalId);
                        cm.Parameters.AddWithValue("@SpaceId", spaceId);
                        cm.Parameters.AddWithValue("@BoothId", boothId);
                        if (await cm.ExecuteNonQueryAsync() == 0)
                            throw new InvalidOperationException("Portal not found");
                    }

                    // Upsert media
                    const string updMedia = @"
                        UPDATE media
                        SET media_type_id=@MediaTypeId, text_key=@TextKey, description_key=@DescKey
                        WHERE id=@MediaId AND space_id=@SpaceId;
                    ";

                    const string insMedia = @"
                        INSERT INTO media (id,space_id,media_type_id,text_key,description_key)
                        VALUES(@MediaId,@SpaceId,@MediaTypeId,@TextKey,@DescKey);
                    ";

                    await using (var cm = new MySqlCommand(updMedia, conn, tx))
                    {
                        cm.Parameters.AddWithValue("@MediaId", mediaId);
                        cm.Parameters.AddWithValue("@SpaceId", spaceId);
                        cm.Parameters.AddWithValue("@MediaTypeId", mDto.MediaTypeId);
                        cm.Parameters.AddWithValue("@TextKey", mDto.TextKey);
                        cm.Parameters.AddWithValue("@DescKey", mDto.DescriptionKey);
                        if (await cm.ExecuteNonQueryAsync() == 0)
                        {
                            await using var ci = new MySqlCommand(insMedia, conn, tx);
                            ci.Parameters.AddWithValue("@MediaId", mediaId);
                            ci.Parameters.AddWithValue("@SpaceId", spaceId);
                            ci.Parameters.AddWithValue("@MediaTypeId", mDto.MediaTypeId);
                            ci.Parameters.AddWithValue("@TextKey", mDto.TextKey);
                            ci.Parameters.AddWithValue("@DescKey", mDto.DescriptionKey);
                            await ci.ExecuteNonQueryAsync();
                        }
                    }

                    // Upsert localizations
                    const string updLoc = @"
                        UPDATE media_localization
                        SET media_link=@MediaLink
                        WHERE media_id=@MediaId AND locale_id=@LocaleId;
                    ";

                    const string insLoc = @"
                        INSERT INTO media_localization (id,media_id,locale_id,media_link)
                        VALUES(@Id,@MediaId,@LocaleId,@MediaLink);
                    ";

                    foreach (var loc in mDto.Localizations)
                    {
                        await using var cl = new MySqlCommand(updLoc, conn, tx);
                        cl.Parameters.AddWithValue("@MediaId", mediaId);
                        cl.Parameters.AddWithValue("@LocaleId", loc.LocaleId);
                        cl.Parameters.AddWithValue("@MediaLink", loc.MediaLink);
                        if (await cl.ExecuteNonQueryAsync() == 0)
                        {
                            await using var ci = new MySqlCommand(insLoc, conn, tx);
                            ci.Parameters.AddWithValue("@Id", Guid.NewGuid().ToString());
                            ci.Parameters.AddWithValue("@MediaId", mediaId);
                            ci.Parameters.AddWithValue("@LocaleId", loc.LocaleId);
                            ci.Parameters.AddWithValue("@MediaLink", loc.MediaLink);
                            await ci.ExecuteNonQueryAsync();
                        }
                    }
                }

                if (dto.CorrespondingMedia != null)
                    await UpsertMedia(dto.CorrespondingMedia, "corresponding_media_id");
                if (dto.ThumbnailMedia != null)
                    await UpsertMedia(dto.ThumbnailMedia, "thumbnail_media_id");

                await tx.CommitAsync();

                // Load updated portal for response (you should filter by booth as well if needed)
                return await GetPortalByIdAsync(spaceId, portalId);
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        public async Task<bool> DeletePortalAsync(int spaceId, int portalId)
        {
            await using var conn = new MySqlConnection(_connectionString);
            await conn.OpenAsync();
            await using var tx = await conn.BeginTransactionAsync();

            try
            {
                // 1. Fetch portal row to get keys and media IDs
                const string fetchSql = @"
                    SELECT text_field_key, corresponding_media_id, thumbnail_media_id
                    FROM portal
                    WHERE id = @PortalId
                      AND space_id = @SpaceId;
                ";

                string textKey;
                string? corrId;
                string? thumbId;

                await using (var fetchCmd = new MySqlCommand(fetchSql, conn, tx))
                {
                    fetchCmd.Parameters.AddWithValue("@PortalId", portalId);
                    fetchCmd.Parameters.AddWithValue("@SpaceId", spaceId);
                    await using var reader = await fetchCmd.ExecuteReaderAsync();
                    if (!await reader.ReadAsync())
                    {
                        await tx.RollbackAsync();
                        return false; // Not found
                    }
                    textKey = reader.GetString("text_field_key");
                    corrId = reader.IsDBNull("corresponding_media_id") ? null : reader.GetString("corresponding_media_id");
                    thumbId = reader.IsDBNull("thumbnail_media_id") ? null : reader.GetString("thumbnail_media_id");
                }

                // 2. Delete i18n rows for this portal’s key
                const string delI18n = @"
                    DELETE FROM i18n
                    WHERE `key` = @TextKey
                      AND space_id = @SpaceId;
                ";

                await using (var cmdI18n = new MySqlCommand(delI18n, conn, tx))
                {
                    cmdI18n.Parameters.AddWithValue("@TextKey", textKey);
                    cmdI18n.Parameters.AddWithValue("@SpaceId", spaceId);
                    await cmdI18n.ExecuteNonQueryAsync();
                }

                // 3. Delete portal row itself
                const string delPortal = @"
                    DELETE FROM portal
                    WHERE id = @PortalId
                      AND space_id = @SpaceId;
                ";

                await using (var cmdPortal = new MySqlCommand(delPortal, conn, tx))
                {
                    cmdPortal.Parameters.AddWithValue("@PortalId", portalId);
                    cmdPortal.Parameters.AddWithValue("@SpaceId", spaceId);
                    await cmdPortal.ExecuteNonQueryAsync();
                }

                // 4. Delete media_localization and media for corresponding_media_id
                if (!string.IsNullOrEmpty(corrId))
                {
                    const string delLoc1 = @"
                        DELETE FROM media_localization
                        WHERE media_id = @MediaId;
                    ";

                    await using (var cmdLoc = new MySqlCommand(delLoc1, conn, tx))
                    {
                        cmdLoc.Parameters.AddWithValue("@MediaId", corrId);
                        await cmdLoc.ExecuteNonQueryAsync();
                    }

                    const string delMed1 = @"
                        DELETE FROM media
                        WHERE id = @MediaId;
                    ";

                    await using (var cmdMed = new MySqlCommand(delMed1, conn, tx))
                    {
                        cmdMed.Parameters.AddWithValue("@MediaId", corrId);
                        await cmdMed.ExecuteNonQueryAsync();
                    }
                }

                // 5. Delete media_localization and media for thumbnail_media_id
                if (!string.IsNullOrEmpty(thumbId))
                {
                    const string delLoc2 = @"
                        DELETE FROM media_localization
                        WHERE media_id = @MediaId;
                    ";

                    await using (var cmdLoc = new MySqlCommand(delLoc2, conn, tx))
                    {
                        cmdLoc.Parameters.AddWithValue("@MediaId", thumbId);
                        await cmdLoc.ExecuteNonQueryAsync();
                    }

                    const string delMed2 = @"
                        DELETE FROM media
                        WHERE id = @MediaId;
                    ";

                    await using var cmdMed = new MySqlCommand(delMed2, conn, tx);
                    cmdMed.Parameters.AddWithValue("@MediaId", thumbId);
                    await cmdMed.ExecuteNonQueryAsync();
                }

                await tx.CommitAsync();
                return true; // Success
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        public async Task<bool> DeletePortalAsync(int spaceId, int boothId, int portalId)
        {
            await using var conn = new MySqlConnection(_connectionString);
            await conn.OpenAsync();
            await using var tx = await conn.BeginTransactionAsync();

            try
            {
                // 1. Fetch portal row to get keys and media IDs, and confirm booth match
                const string fetchSql = @"
            SELECT text_field_key, corresponding_media_id, thumbnail_media_id
            FROM portal
            WHERE id = @PortalId
              AND space_id = @SpaceId
              AND booth_id = @BoothId;
        ";

                string textKey;
                string? corrId;
                string? thumbId;

                await using (var fetchCmd = new MySqlCommand(fetchSql, conn, tx))
                {
                    fetchCmd.Parameters.AddWithValue("@PortalId", portalId);
                    fetchCmd.Parameters.AddWithValue("@SpaceId", spaceId);
                    fetchCmd.Parameters.AddWithValue("@BoothId", boothId);
                    await using var reader = await fetchCmd.ExecuteReaderAsync();
                    if (!await reader.ReadAsync())
                    {
                        await tx.RollbackAsync();
                        return false; // Not found or booth mismatch
                    }
                    textKey = reader.GetString("text_field_key");
                    corrId = reader.IsDBNull("corresponding_media_id") ? null : reader.GetString("corresponding_media_id");
                    thumbId = reader.IsDBNull("thumbnail_media_id") ? null : reader.GetString("thumbnail_media_id");
                }

                // 2. Delete i18n rows for this portal’s key
                const string delI18n = @"
            DELETE FROM i18n
            WHERE `key` = @TextKey
              AND space_id = @SpaceId;
        ";

                await using (var cmdI18n = new MySqlCommand(delI18n, conn, tx))
                {
                    cmdI18n.Parameters.AddWithValue("@TextKey", textKey);
                    cmdI18n.Parameters.AddWithValue("@SpaceId", spaceId);
                    await cmdI18n.ExecuteNonQueryAsync();
                }

                // 3. Delete portal row itself
                const string delPortal = @"
            DELETE FROM portal
            WHERE id = @PortalId
              AND space_id = @SpaceId
              AND booth_id = @BoothId;
        ";

                await using (var cmdPortal = new MySqlCommand(delPortal, conn, tx))
                {
                    cmdPortal.Parameters.AddWithValue("@PortalId", portalId);
                    cmdPortal.Parameters.AddWithValue("@SpaceId", spaceId);
                    cmdPortal.Parameters.AddWithValue("@BoothId", boothId);
                    await cmdPortal.ExecuteNonQueryAsync();
                }

                // 4. Delete media_localization and media for corresponding_media_id
                if (!string.IsNullOrEmpty(corrId))
                {
                    const string delLoc1 = @"
                DELETE FROM media_localization
                WHERE media_id = @MediaId;
            ";
                    await using (var cmdLoc = new MySqlCommand(delLoc1, conn, tx))
                    {
                        cmdLoc.Parameters.AddWithValue("@MediaId", corrId);
                        await cmdLoc.ExecuteNonQueryAsync();
                    }
                    const string delMed1 = @"
                DELETE FROM media
                WHERE id = @MediaId;
            ";
                    await using (var cmdMed = new MySqlCommand(delMed1, conn, tx))
                    {
                        cmdMed.Parameters.AddWithValue("@MediaId", corrId);
                        await cmdMed.ExecuteNonQueryAsync();
                    }
                }

                // 5. Delete media_localization and media for thumbnail_media_id
                if (!string.IsNullOrEmpty(thumbId))
                {
                    const string delLoc2 = @"
                DELETE FROM media_localization
                WHERE media_id = @MediaId;
            ";
                    await using (var cmdLoc = new MySqlCommand(delLoc2, conn, tx))
                    {
                        cmdLoc.Parameters.AddWithValue("@MediaId", thumbId);
                        await cmdLoc.ExecuteNonQueryAsync();
                    }
                    const string delMed2 = @"
                DELETE FROM media
                WHERE id = @MediaId;
            ";
                    await using (var cmdMed = new MySqlCommand(delMed2, conn, tx))
                    {
                        cmdMed.Parameters.AddWithValue("@MediaId", thumbId);
                        await cmdMed.ExecuteNonQueryAsync();
                    }
                }

                await tx.CommitAsync();
                return true; // Success
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

            foreach (var loc in dto.Localizations)
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
