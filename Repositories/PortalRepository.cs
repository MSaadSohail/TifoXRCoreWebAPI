// <copyright file="PortalRepository.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Saad Sohail</author>
// <date>07/23/2025</date>
// <summary>Class to handle portal SQL side</summary>

using MySqlConnector;
using System.Data;
using GMS.TifoXRCoreWebAPI.Models;
using GMS.TifoXRCoreWebAPI.Models.Common;
using GMS.TifoXRCoreWebAPI.Repositories.Interfaces;

namespace GMS.TifoXRCoreWebAPI.Repositories
{
    public class PortalRepository(IConfiguration configuration) : IPortalRepository
    {
        private readonly string _connectionString = configuration.GetConnectionString("DefaultConnection");

        #region GET
        /// <summary>
        /// Retrieves all portals in a given space, including their localized names, associated media, and media localizations.
        /// </summary>
        /// <param name="spaceId">The ID of the space to query portals for.</param>
        /// <returns>A list of portal models with all localization and media details.</returns>
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
                
                LEFT JOIN supported_languages sl
                  ON sl.space_id = p.space_id
                
                LEFT JOIN i18n i 
                  ON p.text_field_key = i.`key`
                 AND i.space_id      = p.space_id
                 AND i.locale_id     = sl.locale_id
                
                LEFT JOIN media cm
                  ON cm.id = p.corresponding_media_id
                 AND cm.space_id = p.space_id
                
                LEFT JOIN media tm
                  ON tm.id = p.thumbnail_media_id
                 AND tm.space_id = p.space_id
                
                LEFT JOIN media_localization ml1 
                  ON ml1.media_id   = p.corresponding_media_id
                 AND ml1.locale_id  = sl.locale_id
                
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
                        BoothId = reader.IsDBNull("booth_id") ? null :
                        reader.GetInt32("booth_id"),

                        PortalTypeId = reader.IsDBNull("portal_type_id") ? null :
                        reader.GetInt32("portal_type_id"),

                        EventId = reader.IsDBNull("event_id") ? null : reader.GetInt32("event_id"),

                        ExternalLink = reader.IsDBNull("external_link") ? null :
                        reader.GetString("external_link"),

                        LocalizedPairs = new LocalizedPairs
                        {
                            Key = reader.GetString("text_key"),
                            Values = new List<LocalizedValue>()
                        },

                        CorrespondingMedia = reader.IsDBNull("corr_media_id")
                            ? null
                            : new MediaData
                            {
                                Id = reader.GetString("corr_media_id"),
                                MediaTypeId = reader.IsDBNull("corr_media_type_id") ? 0 :
                                reader.GetInt32("corr_media_type_id"),
                                LinkLocalizations = new List<MediaLocalization>()
                            },

                        ThumbnailMedia = reader.IsDBNull("thumb_media_id")
                            ? null
                            : new MediaData
                            {
                                Id = reader.GetString("thumb_media_id"),
                                MediaTypeId = reader.IsDBNull("thumb_media_type_id") ? 0 : reader.GetInt32("thumb_media_type_id"),
                                LinkLocalizations = new List<MediaLocalization>()
                            }
                    };

                    map[id] = portal;
                }

                if (!reader.IsDBNull("locale_id"))
                {
                    var locale = reader.GetString("locale_id");

                    // -- Localized Name --
                    var textValue = reader.IsDBNull("localized_text_value") ? null : reader.GetString("localized_text_value");
                    if (textValue != null && !portal.LocalizedPairs.Values.Any(v => v.LocaleId == locale))
                    {
                        portal.LocalizedPairs.Values.Add(new LocalizedValue
                        {
                            LocaleId = locale,
                            Value = textValue
                        });
                    }

                    // -- CorrespondingMedia Localized Links --
                    if (portal.CorrespondingMedia != null)
                    {
                        var link = reader.IsDBNull("corresponding_media_link") ? null : reader.GetString("corresponding_media_link");

                        if (link != null && !portal.CorrespondingMedia.LinkLocalizations.Any(l => l.LocaleId == locale))
                        {
                            portal.CorrespondingMedia.LinkLocalizations.Add(new MediaLocalization
                            {
                                LocaleId = locale,
                                MediaLink = link
                            });
                        }
                    }

                    // -- ThumbnailMedia Localized Links --
                    if (portal.ThumbnailMedia != null)
                    {
                        var link = reader.IsDBNull("thumbnail_media_link") ? null : reader.GetString("thumbnail_media_link");

                        if (link != null && !portal.ThumbnailMedia.LinkLocalizations.Any(l => l.LocaleId == locale))
                        {
                            portal.ThumbnailMedia.LinkLocalizations.Add(new MediaLocalization
                            {
                                LocaleId = locale,
                                MediaLink = link
                            });
                        }
                    }
                }
            }

            return [.. map.Values];
        }


        /// <summary>
        /// Retrieves all portals for a given booth within a space, including their localized names and media details.
        /// </summary>
        /// <param name="spaceId">The ID of the space containing the booth.</param>
        /// <param name="boothId">The ID of the booth whose portals to fetch.</param>
        /// <returns>A list of portal models for the booth, including all localizations and media.</returns>
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
                
                LEFT JOIN supported_languages sl
                    ON sl.space_id = p.space_id

                LEFT JOIN i18n i 
                    ON i.`key`      = p.text_field_key
                   AND i.space_id   = p.space_id
                   AND i.locale_id  = sl.locale_id
                
                LEFT JOIN media cm
                    ON cm.id        = p.corresponding_media_id
                   AND cm.space_id  = p.space_id
                
                LEFT JOIN media tm
                    ON tm.id        = p.thumbnail_media_id
                   AND tm.space_id  = p.space_id

                LEFT JOIN media_localization ml1 
                    ON ml1.media_id  = p.corresponding_media_id
                   AND ml1.locale_id = sl.locale_id
                
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

                        LocalizedPairs = new LocalizedPairs
                        {
                            Key = reader.GetString("text_key"),
                            Values = []
                        },

                        CorrespondingMedia = reader.IsDBNull("corr_media_id")
                            ? null
                            : new MediaData
                            {
                                Id = reader.GetString("corr_media_id"),
                                MediaTypeId = reader.IsDBNull("corr_media_type_id") ? 0 :
                                reader.GetInt32("corr_media_type_id"),
                                LinkLocalizations = new List<MediaLocalization>()
                            },

                        ThumbnailMedia = reader.IsDBNull("thumb_media_id")
                            ? null
                            : new MediaData
                            {
                                Id = reader.GetString("thumb_media_id"),
                                MediaTypeId = reader.IsDBNull("thumb_media_type_id") ? 0 :
                                reader.GetInt32("thumb_media_type_id"),
                                LinkLocalizations = new List<MediaLocalization>()
                            }
                    };

                    map[id] = portal;
                }

                if (!reader.IsDBNull("locale_id"))
                {
                    var locale = reader.GetString("locale_id");

                    // --- Localized Name ---
                    var textValue = reader.IsDBNull("localized_text_value") ? null :
                        reader.GetString("localized_text_value");

                    if (textValue != null && !portal.LocalizedPairs.Values.Any(v => v.LocaleId == locale))
                    {
                        portal.LocalizedPairs.Values.Add(new LocalizedValue
                        {
                            LocaleId = locale,
                            Value = textValue
                        });
                    }

                    // --- CorrespondingMedia Localized Links ---
                    if (portal.CorrespondingMedia != null)
                    {
                        var link = reader.IsDBNull("corr_media_link") ? null :
                            reader.GetString("corr_media_link");

                        if (link != null && !portal.CorrespondingMedia.LinkLocalizations.Any(l => l.LocaleId == locale))
                        {
                            portal.CorrespondingMedia.LinkLocalizations.Add(new MediaLocalization
                            {
                                LocaleId = locale,
                                MediaLink = link
                            });
                        }
                    }

                    // --- ThumbnailMedia Localized Links ---
                    if (portal.ThumbnailMedia != null)
                    {
                        var link = reader.IsDBNull("thumb_media_link") ? null :
                            reader.GetString("thumb_media_link");

                        if (link != null && !portal.ThumbnailMedia.LinkLocalizations.Any(l => l.LocaleId == locale))
                        {
                            portal.ThumbnailMedia.LinkLocalizations.Add(new MediaLocalization
                            {
                                LocaleId = locale,
                                MediaLink = link
                            });
                        }
                    }
                }
            }

            return map.Values.ToList();
        }


        /// <summary>
        /// Retrieves a single portal by its ID and space, with full localization and media details.
        /// </summary>
        /// <param name="spaceId">The ID of the space containing the portal.</param>
        /// <param name="portalId">The ID of the portal to retrieve.</param>
        /// <returns>The portal model with all localizations and media, or null if not found.</returns>
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
                
                LEFT JOIN media cm 
                    ON p.corresponding_media_id = cm.id
                    AND cm.space_id = p.space_id
                
                LEFT JOIN media tm 
                    ON p.thumbnail_media_id = tm.id
                    AND tm.space_id = p.space_id
                
                LEFT JOIN i18n i 
                    ON p.text_field_key = i.`key`
                    AND i.space_id = p.space_id
                
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

            PortalModel? portal = null;

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

                    LocalizedPairs = new LocalizedPairs
                    {
                        Key = reader.GetString("text_key"),
                        Values = new List<LocalizedValue>()
                    },

                    CorrespondingMedia = reader.IsDBNull("corr_media_id")
                        ? null
                        : new MediaData
                        {
                            Id = reader.GetString("corr_media_id"),
                            MediaTypeId = reader.IsDBNull("corr_media_type_id") ? 0 : reader.GetInt32("corr_media_type_id"),
                            LinkLocalizations = new List<MediaLocalization>()
                        },

                    ThumbnailMedia = reader.IsDBNull("thumb_media_id")
                        ? null
                        : new MediaData
                        {
                            Id = reader.GetString("thumb_media_id"),
                            MediaTypeId = reader.IsDBNull("thumb_media_type_id") ? 0 : reader.GetInt32("thumb_media_type_id"),
                            LinkLocalizations = new List<MediaLocalization>()
                        }
                };

                if (!reader.IsDBNull("locale_id"))
                {
                    var locale = reader.GetString("locale_id");

                    // --- Portal Text Localizations ---
                    var textValue = reader.IsDBNull("localized_text_value") ? null : reader.GetString("localized_text_value");
                    if (textValue != null && !portal.LocalizedPairs.Values.Any(v => v.LocaleId == locale))
                    {
                        portal.LocalizedPairs.Values.Add(new LocalizedValue
                        {
                            LocaleId = locale,
                            Value = textValue
                        });
                    }

                    // --- CorrespondingMedia Localized Links ---
                    if (portal.CorrespondingMedia != null)
                    {
                        var link = reader.IsDBNull("corresponding_media_link") ? null : reader.GetString("corresponding_media_link");
                        if (link != null && !portal.CorrespondingMedia.LinkLocalizations.Any(l => l.LocaleId == locale))
                        {
                            portal.CorrespondingMedia.LinkLocalizations.Add(new MediaLocalization
                            {
                                LocaleId = locale,
                                MediaLink = link
                            });
                        }
                    }

                    // --- ThumbnailMedia Localized Links ---
                    if (portal.ThumbnailMedia != null)
                    {
                        var link = reader.IsDBNull("thumbnail_media_link") ? null : reader.GetString("thumbnail_media_link");
                        if (link != null && !portal.ThumbnailMedia.LinkLocalizations.Any(l => l.LocaleId == locale))
                        {
                            portal.ThumbnailMedia.LinkLocalizations.Add(new MediaLocalization
                            {
                                LocaleId = locale,
                                MediaLink = link
                            });
                        }
                    }
                }
            }

            return portal;
        }

        #endregion

        #region POST

        /// <summary>
        /// Creates a new portal in the specified space, including insertion of localized names and associated media records.
        /// </summary>
        /// <param name="spaceId">The ID of the space to create the portal in.</param>
        /// <param name="portalDto">The data for the new portal, including localizations and media.</param>
        /// <returns>The fully constructed portal model after creation, including assigned IDs.</returns>
        public async Task<PortalModel> CreatePortalAsync(int spaceId, PortalCreateDto portalDto)
        {
            ArgumentNullException.ThrowIfNull(portalDto);

            await using var conn = new MySqlConnection(_connectionString);
            await conn.OpenAsync();

            await using var tx = await conn.BeginTransactionAsync();

            try
            {
                // 1) Load supported locales for this space
                var supportedLocales = new HashSet<string>();
                const string fetchLocales = @"
                    SELECT locale_id FROM supported_languages WHERE space_id = @SpaceId;";
                
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
                    corrMediaId = await FilterAndInsertMediaAsync(
                        conn, tx, spaceId,
                        portalDto.CorrespondingMedia,
                        supportedLocales,
                        InsertMediaAsync
                    );
                }

                // 3) Optionally insert thumbnail media
                string? thumbMediaId = null;
                if (portalDto.ThumbnailMedia != null)
                {
                    thumbMediaId = await FilterAndInsertMediaAsync(
                        conn, tx, spaceId,
                        portalDto.ThumbnailMedia,
                        supportedLocales,
                        InsertMediaAsync
                    );
                }

                // 4) Insert portal record, let DB set text_field_key
                const string insertPortal = @"
                    INSERT INTO portal
                      (space_id, booth_id, portal_type_id, event_id,
                       corresponding_media_id, thumbnail_media_id,
                       external_link)
                    VALUES
                      (@SpaceId, @boothId, @PortalTypeId, @EventId,
                       @CorrId, @ThumbId, @ExternalLink);
                ";

                int newPortalId;
                await using (var cmd = new MySqlCommand(insertPortal, conn, tx))
                {
                    cmd.Parameters.AddWithValue("@SpaceId", spaceId);
                    cmd.Parameters.AddWithValue("@boothId", portalDto.BoothId);
                    cmd.Parameters.AddWithValue("@PortalTypeId", portalDto.PortalTypeId);
                    cmd.Parameters.AddWithValue("@EventId", portalDto.EventId);
                    cmd.Parameters.AddWithValue("@CorrId", corrMediaId);
                    cmd.Parameters.AddWithValue("@ThumbId", thumbMediaId);
                    cmd.Parameters.AddWithValue("@ExternalLink", portalDto.ExternalLink ?? (object)DBNull.Value);

                    await cmd.ExecuteNonQueryAsync();
                    newPortalId = Convert.ToInt32(cmd.LastInsertedId);
                }

                // 5) Fetch the generated text_field_key from portal table
                string textFieldKey;
                const string fetchTextFieldKey = @"
                    SELECT text_field_key FROM portal WHERE id = @PortalId;";
                
                await using (var cmd = new MySqlCommand(fetchTextFieldKey, conn, tx))
                {
                    cmd.Parameters.AddWithValue("@PortalId", newPortalId);
                    textFieldKey = (string)await cmd.ExecuteScalarAsync();
                }

                // 6) Insert i18n entries for the portal’s text_field_key, for supported locales only
                const string insertI18n = @"
                    INSERT INTO i18n (`key`, locale_id, value, space_id)
                    VALUES (@TextKey, @LocaleId, @Value, @SpaceId);
                ";

                if (portalDto.LocalizedPairs?.Values != null)
                {
                    foreach (var loc in portalDto.LocalizedPairs.Values.Where(x => supportedLocales.Contains(x.LocaleId)))
                    {
                        await using var cmdI = new MySqlCommand(insertI18n, conn, tx);
                        cmdI.Parameters.AddWithValue("@TextKey", textFieldKey);
                        cmdI.Parameters.AddWithValue("@LocaleId", loc.LocaleId);
                        cmdI.Parameters.AddWithValue("@Value", loc.Value);
                        cmdI.Parameters.AddWithValue("@SpaceId", spaceId);
                        await cmdI.ExecuteNonQueryAsync();
                    }
                }

                await tx.CommitAsync();

                // 7) Reload full portal and return
                return await GetPortalByIdAsync(spaceId, newPortalId);
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }


        #endregion

        #region PUT

        /// <summary>
        /// Updates the details, localizations, and associated media for a specific portal by its ID and space.
        /// </summary>
        /// <param name="spaceId">The ID of the space containing the portal.</param>
        /// <param name="portalId">The ID of the portal to update.</param>
        /// <param name="dto">The update data, including new localizations and media details.</param>
        /// <returns>The updated portal model, or null if the portal does not exist.</returns>
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
                      AND space_id = @SpaceId;";

                await using (var cmdL = new MySqlCommand(updLink, conn, tx))
                {
                    cmdL.Parameters.AddWithValue("@ExternalLink", (object?)dto.ExternalLink ?? DBNull.Value);
                    cmdL.Parameters.AddWithValue("@PortalId", portalId);
                    cmdL.Parameters.AddWithValue("@SpaceId", spaceId);

                    await cmdL.ExecuteNonQueryAsync();
                }

                // 3. Upsert i18n for textKey using LocalizedName.Values (list)
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

                if (dto.LocalizedPairs?.Values != null)
                {
                    foreach (var loc in dto.LocalizedPairs.Values)
                    {
                        string localeId = loc.LocaleId;
                        string value = loc.Value;

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
                }

                // 4. Helper to upsert one media block (now using MediaLocalization list)
                async Task UpsertMedia(MediaUpdateDto mDto, string columnIdName)
                {
                    var mediaId = string.IsNullOrWhiteSpace(mDto.Id)
                        ? Guid.NewGuid().ToString()
                        : mDto.Id;

                    // a) Update portal set column = @MediaId
                    var updPortalMedia = $@"
                        UPDATE portal
                        SET {columnIdName} = @MediaId
                        WHERE id=@PortalId AND space_id=@SpaceId;";

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

                    if (mDto.LinkLocalizations != null)
                    {
                        foreach (var loc in mDto.LinkLocalizations)
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


        /// <summary>
        /// Updates the details, localizations, and associated media for a specific portal that belongs to a booth.
        /// </summary>
        /// <param name="spaceId">The ID of the space.</param>
        /// <param name="boothId">The ID of the booth the portal belongs to.</param>
        /// <param name="portalId">The ID of the portal to update.</param>
        /// <param name="dto">The update data, including new localizations and media details.</param>
        /// <returns>The updated portal model, or null if the portal or booth association is not found.</returns>
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

                // Upsert i18n for textKey using list of LocalizedValue
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

                if (dto.LocalizedPairs?.Values != null)
                {
                    foreach (var loc in dto.LocalizedPairs.Values)
                    {
                        string localeId = loc.LocaleId;
                        string value = loc.Value;

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
                }

                // Helper to upsert one media block using MediaLocalization list
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

                    // Upsert localizations using MediaLocalization list
                    const string updLoc = @"
                        UPDATE media_localization
                        SET media_link=@MediaLink
                        WHERE media_id=@MediaId AND locale_id=@LocaleId;
                    ";

                    const string insLoc = @"
                        INSERT INTO media_localization (id,media_id,locale_id,media_link)
                        VALUES(@Id,@MediaId,@LocaleId,@MediaLink);
                    ";

                    if (mDto.LinkLocalizations != null)
                    {
                        foreach (var loc in mDto.LinkLocalizations)
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

        #endregion

        #region DELETE

        /// <summary>
        /// Deletes a portal by its ID and space, including all dependent localizations and media records.
        /// </summary>
        /// <param name="spaceId">The ID of the space containing the portal.</param>
        /// <param name="portalId">The ID of the portal to delete.</param>
        /// <returns>True if the portal and all associated records were deleted; false if not found.</returns>
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

        /// <summary>
        /// Deletes a portal by its ID, booth, and space, ensuring the booth association matches, and removes all dependent localizations and media records.
        /// </summary>
        /// <param name="spaceId">The ID of the space containing the portal.</param>
        /// <param name="boothId">The ID of the booth the portal belongs to.</param>
        /// <param name="portalId">The ID of the portal to delete.</param>
        /// <returns>True if the portal and all associated records were deleted; false if not found or booth association mismatch.</returns>
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

        #endregion

        #region HELPER_METHODS

        /// <summary>
        /// Inserts a new media record into the database, including its type, keys, localized text and description entries, and localized media links.
        /// </summary>
        /// <param name="conn">An open MySql database connection.</param>
        /// <param name="tx">The current MySql transaction.</param>
        /// <param name="spaceId">The space ID the media belongs to.</param>
        /// <param name="dto">The data transfer object containing all information for the media, including localization dictionaries.</param>
        /// <returns>The ID (UUID) of the newly inserted media row.</returns>
        private async Task<string> InsertMediaAsync(
            MySqlConnection conn,
            MySqlTransaction tx,
            int spaceId,
            MediaCreateDto dto
        )
        {
            // 1. Get UUID for new media row
            string mediaId;
            {
                await using var uuidCmd = new MySqlCommand("SELECT UUID()", conn, tx);

                mediaId = (await uuidCmd.ExecuteScalarAsync())!.ToString()!;
            }

            // 2. Insert media row
            const string insMedia = @"
                INSERT INTO media (id, space_id, media_type_id, text_key, description_key)
                VALUES (@Id, @SpaceId, @MediaTypeId, @TextKey, @DescKey);
            ";

            await using (var cmd = new MySqlCommand(insMedia, conn, tx))
            {
                cmd.Parameters.AddWithValue("@Id", mediaId);
                cmd.Parameters.AddWithValue("@SpaceId", spaceId);
                cmd.Parameters.AddWithValue("@MediaTypeId", dto.MediaTypeId);
                cmd.Parameters.AddWithValue("@TextKey", (object?)dto.TextKey ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@DescKey", (object?)dto.DescriptionKey ?? DBNull.Value);

                await cmd.ExecuteNonQueryAsync();
            }

            // 3. Insert i18n text values (TextLocalizations is a List<LocalizedValue>)
            if (dto.TextKey != null && dto.TextLocalizations != null)
            {
                foreach (var loc in dto.TextLocalizations)
                {
                    const string insI18n = @"
                        INSERT INTO i18n (`key`, locale_id, value)
                        VALUES (@Key, @LocaleId, @Value);
                    ";

                    await using var cmdI = new MySqlCommand(insI18n, conn, tx);

                    cmdI.Parameters.AddWithValue("@Key", dto.TextKey);
                    cmdI.Parameters.AddWithValue("@LocaleId", loc.LocaleId);
                    cmdI.Parameters.AddWithValue("@Value", loc.Value);

                    await cmdI.ExecuteNonQueryAsync();
                }
            }

            // 4. Insert i18n description values (DescriptionLocalizations is a List<LocalizedValue>)
            if (dto.DescriptionKey != null && dto.DescriptionLocalizations != null)
            {
                foreach (var loc in dto.DescriptionLocalizations)
                {
                    const string insI18n = @"
                        INSERT INTO i18n (`key`, locale_id, value)
                        VALUES (@Key, @LocaleId, @Value);
                    ";

                    await using var cmdI = new MySqlCommand(insI18n, conn, tx);

                    cmdI.Parameters.AddWithValue("@Key", dto.DescriptionKey);
                    cmdI.Parameters.AddWithValue("@LocaleId", loc.LocaleId);
                    cmdI.Parameters.AddWithValue("@Value", loc.Value);

                    await cmdI.ExecuteNonQueryAsync();
                }
            }

            // 5. Insert media links (media_localization, using List<MediaLocalization>)
            if (dto.LinkLocalizations != null)
            {
                foreach (var loc in dto.LinkLocalizations)
                {
                    const string insLoc = @"
                        INSERT INTO media_localization (media_id, locale_id, media_link)
                        VALUES (@MediaId, @LocaleId, @MediaLink);
                    ";

                    await using var cmdLoc = new MySqlCommand(insLoc, conn, tx);

                    cmdLoc.Parameters.AddWithValue("@MediaId", mediaId);
                    cmdLoc.Parameters.AddWithValue("@LocaleId", loc.LocaleId);
                    cmdLoc.Parameters.AddWithValue("@MediaLink", loc.MediaLink);

                    await cmdLoc.ExecuteNonQueryAsync();
                }
            }

            return mediaId;
        }


        /// <summary>
        /// Filters the given media DTO’s localizations to only those supported by the space, and inserts the media if any localizations remain.
        /// </summary>
        /// <param name="conn">An open MySql database connection.</param>
        /// <param name="tx">The current MySql transaction.</param>
        /// <param name="spaceId">The space ID the media will be associated with.</param>
        /// <param name="mediaDto">The media creation DTO to filter and insert.</param>
        /// <param name="supportedLocales">A set of supported locale IDs for the space.</param>
        /// <param name="insertFunc">A delegate to the actual insert function for the media.</param>
        /// <returns>The ID of the inserted media if inserted, or null if there were no localizations to insert.</returns>
        private static async Task<string?> FilterAndInsertMediaAsync(
            MySqlConnection conn,
            MySqlTransaction tx,
            int spaceId,
            MediaCreateDto? mediaDto,
            HashSet<string> supportedLocales,
            Func<MySqlConnection,
            MySqlTransaction,
            int, MediaCreateDto,
            Task<string>> insertFunc
        )
        {
            if (mediaDto == null)
                return null;

            // Filter LinkLocalizations (List<MediaLocalization>)
            if (mediaDto.LinkLocalizations != null)
            {
                mediaDto.LinkLocalizations = [.. mediaDto.LinkLocalizations.Where(
                    x => supportedLocales.Contains(x.LocaleId)
                )];
            }

            // Filter TextLocalizations (List<LocalizedValue>)
            if (mediaDto.TextLocalizations != null)
            {
                mediaDto.TextLocalizations = [.. mediaDto.TextLocalizations.Where(
                    x => supportedLocales.Contains(x.LocaleId)
                )];
            }

            // Filter DescriptionLocalizations (List<LocalizedValue>)
            if (mediaDto.DescriptionLocalizations != null)
            {
                mediaDto.DescriptionLocalizations = [.. mediaDto.DescriptionLocalizations.Where(
                    x => supportedLocales.Contains(x.LocaleId)
                )];
            }

            // Only insert if there are any link localizations
            if (mediaDto.LinkLocalizations != null && mediaDto.LinkLocalizations.Count > 0)
                return await insertFunc(conn, tx, spaceId, mediaDto);

            return null;
        }

        #endregion
    }
}
