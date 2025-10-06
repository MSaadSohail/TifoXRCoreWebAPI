// <copyright file="PortalRepository.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Saad Sohail</author>
// <date>07/23/2025</date>
// <summary>Class to handle portal SQL side</summary>

using GMS.TifoXRCoreWebAPI.Models;
using GMS.TifoXRCoreWebAPI.Models.Common;
using GMS.TifoXRCoreWebAPI.Repositories.Interfaces;
using System.Data;
using System.Data.Common;
using GMS.TifoXRCoreWebAPI.Utilities.Infrastructure;

namespace GMS.TifoXRCoreWebAPI.Repositories
{
    public class PortalRepository(IConfiguration configuration, IDbProvider db) : IPortalRepository
    {
        private readonly IDbProvider _db = db;

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
        ORDER BY p.id, sl.locale_id;";

            await using var conn = await _db.OpenConnectionAsync();
            await using var cmd = _db.CreateCommand(conn, sql);
            cmd.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));

            var portals = new Dictionary<int, PortalModel>();
            await using var reader = await cmd.ExecuteReaderAsync();

            bool ordReady = false;
            int o_portal_id = -1, o_space_id = -1, o_booth_id = -1, o_portal_type_id = -1, o_event_id = -1, o_external_link = -1;
            int o_corr_media_id = -1, o_thumb_media_id = -1, o_text_key = -1, o_corr_media_type_id = -1, o_thumb_media_type_id = -1;
            int o_locale_id = -1, o_localized_text_value = -1, o_corresponding_media_link = -1, o_thumbnail_media_link = -1;

            while (await reader.ReadAsync())
            {
                if (!ordReady)
                {
                    o_portal_id = reader.GetOrdinal("portal_id");
                    o_space_id = reader.GetOrdinal("space_id");
                    o_booth_id = reader.GetOrdinal("booth_id");
                    o_portal_type_id = reader.GetOrdinal("portal_type_id");
                    o_event_id = reader.GetOrdinal("event_id");
                    o_external_link = reader.GetOrdinal("external_link");
                    o_corr_media_id = reader.GetOrdinal("corr_media_id");
                    o_thumb_media_id = reader.GetOrdinal("thumb_media_id");
                    o_text_key = reader.GetOrdinal("text_key");
                    o_corr_media_type_id = reader.GetOrdinal("corr_media_type_id");
                    o_thumb_media_type_id = reader.GetOrdinal("thumb_media_type_id");
                    o_locale_id = reader.GetOrdinal("locale_id");
                    o_localized_text_value = reader.GetOrdinal("localized_text_value");
                    o_corresponding_media_link = reader.GetOrdinal("corresponding_media_link");
                    o_thumbnail_media_link = reader.GetOrdinal("thumbnail_media_link");
                    ordReady = true;
                }

                var id = reader.GetInt32(o_portal_id);

                if (!portals.TryGetValue(id, out var portal))
                {
                    portal = new PortalModel
                    {
                        PortalId = id,
                        SpaceId = reader.GetInt32(o_space_id),
                        BoothId = reader.IsDBNull(o_booth_id) ? (int?)null : reader.GetInt32(o_booth_id),
                        PortalTypeId = reader.IsDBNull(o_portal_type_id) ? (int?)null : reader.GetInt32(o_portal_type_id),
                        EventId = reader.IsDBNull(o_event_id) ? (int?)null : reader.GetInt32(o_event_id),
                        ExternalLink = reader.IsDBNull(o_external_link) ? null : reader.GetString(o_external_link),

                        LocalizedPairs = new LocalizedPairs
                        {
                            Key = reader.IsDBNull(o_text_key) ? string.Empty : reader.GetString(o_text_key),
                            Values = new List<LocalizedValue>()
                        },

                        CorrespondingMedia = reader.IsDBNull(o_corr_media_id)
                            ? null
                            : new MediaData
                            {
                                Id = reader.GetString(o_corr_media_id),
                                MediaTypeId = reader.IsDBNull(o_corr_media_type_id) ? 0 : reader.GetInt32(o_corr_media_type_id),
                                LinkLocalizations = new List<MediaLocalization>()
                            },

                        ThumbnailMedia = reader.IsDBNull(o_thumb_media_id)
                            ? null
                            : new MediaData
                            {
                                Id = reader.GetString(o_thumb_media_id),
                                MediaTypeId = reader.IsDBNull(o_thumb_media_type_id) ? 0 : reader.GetInt32(o_thumb_media_type_id),
                                LinkLocalizations = new List<MediaLocalization>()
                            }
                    };

                    portals[id] = portal;
                }

                // Per-locale joins (may be null if no supported_languages row)
                if (!reader.IsDBNull(o_locale_id))
                {
                    var locale = reader.GetString(o_locale_id);

                    // Localized text
                    if (!reader.IsDBNull(o_localized_text_value))
                    {
                        var textVal = reader.GetString(o_localized_text_value);
                        if (!portal.LocalizedPairs.Values.Any(v => v.LocaleId == locale))
                        {
                            portal.LocalizedPairs.Values.Add(new LocalizedValue
                            {
                                LocaleId = locale,
                                Value = textVal
                            });
                        }
                    }

                    // Corresponding media link per-locale
                    if (portal.CorrespondingMedia != null && !reader.IsDBNull(o_corresponding_media_link))
                    {
                        var link = reader.GetString(o_corresponding_media_link);
                        if (!portal.CorrespondingMedia.LinkLocalizations.Any(l => l.LocaleId == locale))
                        {
                            portal.CorrespondingMedia.LinkLocalizations.Add(new MediaLocalization
                            {
                                LocaleId = locale,
                                MediaLink = link
                            });
                        }
                    }

                    // Thumbnail media link per-locale
                    if (portal.ThumbnailMedia != null && !reader.IsDBNull(o_thumbnail_media_link))
                    {
                        var link = reader.GetString(o_thumbnail_media_link);
                        if (!portal.ThumbnailMedia.LinkLocalizations.Any(l => l.LocaleId == locale))
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

            return portals.Values.ToList();
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
        ORDER BY p.id, sl.locale_id;";

            await using var conn = await _db.OpenConnectionAsync();
            await using var cmd = _db.CreateCommand(conn, sql);
            cmd.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));
            cmd.Parameters.Add(_db.CreateParameter("@BoothId", boothId));

            var portals = new Dictionary<int, PortalModel>();
            await using var reader = await cmd.ExecuteReaderAsync();

            bool ordReady = false;
            int o_portal_id = -1, o_space_id = -1, o_booth_id = -1, o_portal_type_id = -1, o_event_id = -1, o_external_link = -1;
            int o_corr_media_id = -1, o_corr_media_type_id = -1, o_thumb_media_id = -1, o_thumb_media_type_id = -1, o_text_key = -1;
            int o_locale_id = -1, o_localized_text_value = -1, o_corr_media_link = -1, o_thumb_media_link = -1;

            while (await reader.ReadAsync())
            {
                if (!ordReady)
                {
                    o_portal_id = reader.GetOrdinal("portal_id");
                    o_space_id = reader.GetOrdinal("space_id");
                    o_booth_id = reader.GetOrdinal("booth_id");
                    o_portal_type_id = reader.GetOrdinal("portal_type_id");
                    o_event_id = reader.GetOrdinal("event_id");
                    o_external_link = reader.GetOrdinal("external_link");
                    o_corr_media_id = reader.GetOrdinal("corr_media_id");
                    o_corr_media_type_id = reader.GetOrdinal("corr_media_type_id");
                    o_thumb_media_id = reader.GetOrdinal("thumb_media_id");
                    o_thumb_media_type_id = reader.GetOrdinal("thumb_media_type_id");
                    o_text_key = reader.GetOrdinal("text_key");
                    o_locale_id = reader.GetOrdinal("locale_id");
                    o_localized_text_value = reader.GetOrdinal("localized_text_value");
                    o_corr_media_link = reader.GetOrdinal("corr_media_link");
                    o_thumb_media_link = reader.GetOrdinal("thumb_media_link");
                    ordReady = true;
                }

                var id = reader.GetInt32(o_portal_id);

                if (!portals.TryGetValue(id, out var portal))
                {
                    portal = new PortalModel
                    {
                        PortalId = id,
                        SpaceId = reader.GetInt32(o_space_id),
                        BoothId = reader.IsDBNull(o_booth_id) ? (int?)null : reader.GetInt32(o_booth_id),
                        PortalTypeId = reader.IsDBNull(o_portal_type_id) ? (int?)null : reader.GetInt32(o_portal_type_id),
                        EventId = reader.IsDBNull(o_event_id) ? (int?)null : reader.GetInt32(o_event_id),
                        ExternalLink = reader.IsDBNull(o_external_link) ? null : reader.GetString(o_external_link),

                        LocalizedPairs = new LocalizedPairs
                        {
                            Key = reader.IsDBNull(o_text_key) ? string.Empty : reader.GetString(o_text_key),
                            Values = new List<LocalizedValue>()
                        },

                        CorrespondingMedia = reader.IsDBNull(o_corr_media_id)
                            ? null
                            : new MediaData
                            {
                                Id = reader.GetString(o_corr_media_id),
                                MediaTypeId = reader.IsDBNull(o_corr_media_type_id) ? 0 : reader.GetInt32(o_corr_media_type_id),
                                LinkLocalizations = new List<MediaLocalization>()
                            },

                        ThumbnailMedia = reader.IsDBNull(o_thumb_media_id)
                            ? null
                            : new MediaData
                            {
                                Id = reader.GetString(o_thumb_media_id),
                                MediaTypeId = reader.IsDBNull(o_thumb_media_type_id) ? 0 : reader.GetInt32(o_thumb_media_type_id),
                                LinkLocalizations = new List<MediaLocalization>()
                            }
                    };

                    portals[id] = portal;
                }

                if (!reader.IsDBNull(o_locale_id))
                {
                    var locale = reader.GetString(o_locale_id);

                    // Localized text
                    if (!reader.IsDBNull(o_localized_text_value))
                    {
                        var textValue = reader.GetString(o_localized_text_value);
                        if (!portal.LocalizedPairs.Values.Any(v => v.LocaleId == locale))
                        {
                            portal.LocalizedPairs.Values.Add(new LocalizedValue
                            {
                                LocaleId = locale,
                                Value = textValue
                            });
                        }
                    }

                    // Corresponding media per-locale link
                    if (portal.CorrespondingMedia != null && !reader.IsDBNull(o_corr_media_link))
                    {
                        var link = reader.GetString(o_corr_media_link);
                        if (!portal.CorrespondingMedia.LinkLocalizations.Any(l => l.LocaleId == locale))
                        {
                            portal.CorrespondingMedia.LinkLocalizations.Add(new MediaLocalization
                            {
                                LocaleId = locale,
                                MediaLink = link
                            });
                        }
                    }

                    // Thumbnail media per-locale link
                    if (portal.ThumbnailMedia != null && !reader.IsDBNull(o_thumb_media_link))
                    {
                        var link = reader.GetString(o_thumb_media_link);
                        if (!portal.ThumbnailMedia.LinkLocalizations.Any(l => l.LocaleId == locale))
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

            return portals.Values.ToList();
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
            ON ml1.media_id  = p.corresponding_media_id
           AND ml1.locale_id = i.locale_id
        
        LEFT JOIN media_localization ml2 
            ON ml2.media_id  = p.thumbnail_media_id
           AND ml2.locale_id = i.locale_id
        
        WHERE p.id = @PortalId
          AND p.space_id = @SpaceId
        ORDER BY i.locale_id;";

            await using var conn = await _db.OpenConnectionAsync();
            await using var cmd = _db.CreateCommand(conn, sql);
            cmd.Parameters.Add(_db.CreateParameter("@PortalId", portalId));
            cmd.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));

            await using var reader = await cmd.ExecuteReaderAsync();

            PortalModel? portal = null;

            bool ordReady = false;
            int o_portal_id = -1, o_space_id = -1, o_booth_id = -1, o_portal_type_id = -1, o_event_id = -1, o_external_link = -1, o_text_key = -1;
            int o_corr_media_id = -1, o_corr_media_type_id = -1;
            int o_thumb_media_id = -1, o_thumb_media_type_id = -1;
            int o_locale_id = -1, o_localized_text_value = -1, o_corresponding_media_link = -1, o_thumbnail_media_link = -1;

            while (await reader.ReadAsync())
            {
                if (!ordReady)
                {
                    o_portal_id = reader.GetOrdinal("portal_id");
                    o_space_id = reader.GetOrdinal("space_id");
                    o_booth_id = reader.GetOrdinal("booth_id");
                    o_portal_type_id = reader.GetOrdinal("portal_type_id");
                    o_event_id = reader.GetOrdinal("event_id");
                    o_external_link = reader.GetOrdinal("external_link");
                    o_text_key = reader.GetOrdinal("text_key");

                    o_corr_media_id = reader.GetOrdinal("corr_media_id");
                    o_corr_media_type_id = reader.GetOrdinal("corr_media_type_id");

                    o_thumb_media_id = reader.GetOrdinal("thumb_media_id");
                    o_thumb_media_type_id = reader.GetOrdinal("thumb_media_type_id");

                    o_locale_id = reader.GetOrdinal("locale_id");
                    o_localized_text_value = reader.GetOrdinal("localized_text_value");
                    o_corresponding_media_link = reader.GetOrdinal("corresponding_media_link");
                    o_thumbnail_media_link = reader.GetOrdinal("thumbnail_media_link");
                    ordReady = true;
                }

                portal ??= new PortalModel
                {
                    PortalId = reader.GetInt32(o_portal_id),
                    SpaceId = reader.GetInt32(o_space_id),
                    BoothId = reader.IsDBNull(o_booth_id) ? (int?)null : reader.GetInt32(o_booth_id),
                    PortalTypeId = reader.IsDBNull(o_portal_type_id) ? (int?)null : reader.GetInt32(o_portal_type_id),
                    EventId = reader.IsDBNull(o_event_id) ? (int?)null : reader.GetInt32(o_event_id),
                    ExternalLink = reader.IsDBNull(o_external_link) ? null : reader.GetString(o_external_link),

                    LocalizedPairs = new LocalizedPairs
                    {
                        Key = reader.IsDBNull(o_text_key) ? string.Empty : reader.GetString(o_text_key),
                        Values = new List<LocalizedValue>()
                    },

                    CorrespondingMedia = reader.IsDBNull(o_corr_media_id)
                        ? null
                        : new MediaData
                        {
                            Id = reader.GetString(o_corr_media_id),
                            MediaTypeId = reader.IsDBNull(o_corr_media_type_id) ? 0 : reader.GetInt32(o_corr_media_type_id),
                            LinkLocalizations = new List<MediaLocalization>()
                        },

                    ThumbnailMedia = reader.IsDBNull(o_thumb_media_id)
                        ? null
                        : new MediaData
                        {
                            Id = reader.GetString(o_thumb_media_id),
                            MediaTypeId = reader.IsDBNull(o_thumb_media_type_id) ? 0 : reader.GetInt32(o_thumb_media_type_id),
                            LinkLocalizations = new List<MediaLocalization>()
                        }
                };

                if (!reader.IsDBNull(o_locale_id))
                {
                    var locale = reader.GetString(o_locale_id);

                    // Portal text localization
                    if (!reader.IsDBNull(o_localized_text_value))
                    {
                        var textValue = reader.GetString(o_localized_text_value);
                        if (!portal.LocalizedPairs.Values.Any(v => v.LocaleId == locale))
                        {
                            portal.LocalizedPairs.Values.Add(new LocalizedValue
                            {
                                LocaleId = locale,
                                Value = textValue
                            });
                        }
                    }

                    // Corresponding media link per-locale
                    if (portal.CorrespondingMedia != null && !reader.IsDBNull(o_corresponding_media_link))
                    {
                        var link = reader.GetString(o_corresponding_media_link);
                        if (!portal.CorrespondingMedia.LinkLocalizations.Any(l => l.LocaleId == locale))
                        {
                            portal.CorrespondingMedia.LinkLocalizations.Add(new MediaLocalization
                            {
                                LocaleId = locale,
                                MediaLink = link
                            });
                        }
                    }

                    // Thumbnail media link per-locale
                    if (portal.ThumbnailMedia != null && !reader.IsDBNull(o_thumbnail_media_link))
                    {
                        var link = reader.GetString(o_thumbnail_media_link);
                        if (!portal.ThumbnailMedia.LinkLocalizations.Any(l => l.LocaleId == locale))
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

            await using var conn = await _db.OpenConnectionAsync();
            await using var tx = await conn.BeginTransactionAsync();

            try
            {
                // 1) Load supported locales for this space
                var supportedLocales = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                const string fetchLocales = @"
            SELECT locale_id
              FROM supported_languages
             WHERE space_id = @SpaceId;";

                await using (var cmd = _db.CreateCommand(conn, fetchLocales))
                {
                    cmd.Transaction = tx;
                    cmd.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));
                    await using var rdr = await cmd.ExecuteReaderAsync();
                    while (await rdr.ReadAsync())
                    {
                        if (!rdr.IsDBNull(0))
                            supportedLocales.Add(rdr.GetString(0));
                    }
                }

                // 2) Optionally insert corresponding media
                string? corrMediaId = null;
                if (portalDto.CorrespondingMedia != null)
                {
                    corrMediaId = await FilterAndInsertMediaAsync(
                        conn, tx, spaceId,
                        portalDto.CorrespondingMedia,
                        supportedLocales,
                        InsertMediaAsync  // provider-agnostic overload: (DbConnection, DbTransaction, int, MediaCreateDto, HashSet<string>?) -> Task<string>
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

                // 4) Decide text_field_key (avoid NOT NULL errors)
                var textKey = !string.IsNullOrWhiteSpace(portalDto.LocalizedPairs?.Key)
                    ? portalDto.LocalizedPairs!.Key
                    : $"portal_text_{Guid.NewGuid():N}";

                // 5) Insert portal row (include text_field_key)
                const string insertPortal = @"
            INSERT INTO portal
              (space_id, booth_id, portal_type_id, event_id,
               corresponding_media_id, thumbnail_media_id,
               text_field_key, external_link)
            VALUES
              (@SpaceId, @BoothId, @PortalTypeId, @EventId,
               @CorrId, @ThumbId,
               @TextKey, @ExternalLink);";

                await using (var ins = _db.CreateCommand(conn, insertPortal))
                {
                    ins.Transaction = tx;
                    ins.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));
                    ins.Parameters.Add(_db.CreateParameter("@BoothId", (object?)portalDto.BoothId ?? DBNull.Value));
                    ins.Parameters.Add(_db.CreateParameter("@PortalTypeId", (object?)portalDto.PortalTypeId ?? DBNull.Value));
                    ins.Parameters.Add(_db.CreateParameter("@EventId", (object?)portalDto.EventId ?? DBNull.Value));
                    ins.Parameters.Add(_db.CreateParameter("@CorrId", (object?)corrMediaId ?? DBNull.Value));
                    ins.Parameters.Add(_db.CreateParameter("@ThumbId", (object?)thumbMediaId ?? DBNull.Value));
                    ins.Parameters.Add(_db.CreateParameter("@TextKey", textKey));
                    ins.Parameters.Add(_db.CreateParameter("@ExternalLink", (object?)portalDto.ExternalLink ?? DBNull.Value));
                    await ins.ExecuteNonQueryAsync();
                }

                // 6) Get new portal id
                int newPortalId;
                await using (var idCmd = _db.CreateCommand(conn, "SELECT LAST_INSERT_ID();"))
                {
                    idCmd.Transaction = tx;
                    newPortalId = Convert.ToInt32(await idCmd.ExecuteScalarAsync());
                }

                // 7) Insert i18n for the portal text (supported locales only)
                if (portalDto.LocalizedPairs?.Values != null)
                {
                    const string insertI18n = @"
                INSERT INTO i18n (`key`, locale_id, value, space_id)
                VALUES (@TextKey, @LocaleId, @Value, @SpaceId);";

                    foreach (var loc in portalDto.LocalizedPairs.Values)
                    {
                        if (string.IsNullOrWhiteSpace(loc.LocaleId) || !supportedLocales.Contains(loc.LocaleId))
                            continue;

                        await using var cmdI = _db.CreateCommand(conn, insertI18n);
                        cmdI.Transaction = tx;
                        cmdI.Parameters.Add(_db.CreateParameter("@TextKey", textKey));
                        cmdI.Parameters.Add(_db.CreateParameter("@LocaleId", loc.LocaleId));
                        cmdI.Parameters.Add(_db.CreateParameter("@Value", loc.Value ?? string.Empty));
                        cmdI.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));
                        await cmdI.ExecuteNonQueryAsync();
                    }
                }

                await tx.CommitAsync();

                // 8) Reload and return
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
            await using var conn = await _db.OpenConnectionAsync();
            await using var tx = await conn.BeginTransactionAsync();

            try
            {
                // 1) Get existing text_key (text_field_key)
                string textKey;
                {
                    const string fetch = @"
                SELECT text_field_key
                FROM portal
                WHERE id = @PortalId
                  AND space_id = @SpaceId;";

                    await using var c = _db.CreateCommand(conn, fetch);
                    c.Transaction = tx;
                    c.Parameters.Add(_db.CreateParameter("@PortalId", portalId));
                    c.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));

                    var o = await c.ExecuteScalarAsync();
                    if (o == null || o == DBNull.Value)
                    {
                        await tx.RollbackAsync();
                        return null;
                    }

                    textKey = Convert.ToString(o)!;
                }

                // 2) Update external_link
                const string updLink = @"
            UPDATE portal
               SET external_link = @ExternalLink
             WHERE id = @PortalId
               AND space_id = @SpaceId;";

                await using (var cmdL = _db.CreateCommand(conn, updLink))
                {
                    cmdL.Transaction = tx;
                    cmdL.Parameters.Add(_db.CreateParameter("@ExternalLink", (object?)dto.ExternalLink ?? DBNull.Value));
                    cmdL.Parameters.Add(_db.CreateParameter("@PortalId", portalId));
                    cmdL.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));
                    await cmdL.ExecuteNonQueryAsync();
                }

                // 3) Upsert i18n for textKey using LocalizedPairs.Values
                const string updI18n = @"
            UPDATE i18n
               SET value = @Value
             WHERE `key` = @TextKey
               AND locale_id = @LocaleId
               AND space_id  = @SpaceId;";
                const string insI18n = @"
            INSERT INTO i18n (`key`, locale_id, value, space_id)
            VALUES (@TextKey, @LocaleId, @Value, @SpaceId);";

                if (dto.LocalizedPairs?.Values != null)
                {
                    foreach (var loc in dto.LocalizedPairs.Values)
                    {
                        var localeId = loc.LocaleId;
                        var value = loc.Value ?? string.Empty;

                        await using var cu = _db.CreateCommand(conn, updI18n);
                        cu.Transaction = tx;
                        cu.Parameters.Add(_db.CreateParameter("@TextKey", textKey));
                        cu.Parameters.Add(_db.CreateParameter("@LocaleId", localeId));
                        cu.Parameters.Add(_db.CreateParameter("@Value", value));
                        cu.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));

                        if (await cu.ExecuteNonQueryAsync() == 0)
                        {
                            await using var ci = _db.CreateCommand(conn, insI18n);
                            ci.Transaction = tx;
                            ci.Parameters.Add(_db.CreateParameter("@TextKey", textKey));
                            ci.Parameters.Add(_db.CreateParameter("@LocaleId", localeId));
                            ci.Parameters.Add(_db.CreateParameter("@Value", value));
                            ci.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));
                            await ci.ExecuteNonQueryAsync();
                        }
                    }
                }

                // 4) Helper to upsert one media block (links only, like your original)
                async Task UpsertMedia(MediaUpdateDto mDto, string columnIdName)
                {
                    var mediaId = string.IsNullOrWhiteSpace(mDto.Id)
                        ? Guid.NewGuid().ToString()
                        : mDto.Id;

                    // a) Update portal set {columnIdName} = @MediaId
                    var updPortalMedia = $@"
                UPDATE portal
                   SET {columnIdName} = @MediaId
                 WHERE id = @PortalId AND space_id = @SpaceId;";

                    await using (var cm = _db.CreateCommand(conn, updPortalMedia))
                    {
                        cm.Transaction = tx;
                        cm.Parameters.Add(_db.CreateParameter("@MediaId", mediaId));
                        cm.Parameters.Add(_db.CreateParameter("@PortalId", portalId));
                        cm.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));

                        if (await cm.ExecuteNonQueryAsync() == 0)
                            throw new InvalidOperationException("Portal not found");
                    }

                    // b) Upsert media row
                    const string updMedia = @"
                UPDATE media
                   SET media_type_id = @MediaTypeId,
                       text_key      = @TextKey,
                       description_key = @DescKey
                 WHERE id = @MediaId AND space_id = @SpaceId;";
                    const string insMedia = @"
                INSERT INTO media (id, space_id, media_type_id, text_key, description_key)
                VALUES (@MediaId, @SpaceId, @MediaTypeId, @TextKey, @DescKey);";

                    await using (var cm2 = _db.CreateCommand(conn, updMedia))
                    {
                        cm2.Transaction = tx;
                        cm2.Parameters.Add(_db.CreateParameter("@MediaId", mediaId));
                        cm2.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));
                        cm2.Parameters.Add(_db.CreateParameter("@MediaTypeId", mDto.MediaTypeId));
                        cm2.Parameters.Add(_db.CreateParameter("@TextKey", (object?)mDto.TextKey ?? DBNull.Value));
                        cm2.Parameters.Add(_db.CreateParameter("@DescKey", (object?)mDto.DescriptionKey ?? DBNull.Value));

                        if (await cm2.ExecuteNonQueryAsync() == 0)
                        {
                            await using var ci2 = _db.CreateCommand(conn, insMedia);
                            ci2.Transaction = tx;
                            ci2.Parameters.Add(_db.CreateParameter("@MediaId", mediaId));
                            ci2.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));
                            ci2.Parameters.Add(_db.CreateParameter("@MediaTypeId", mDto.MediaTypeId));
                            ci2.Parameters.Add(_db.CreateParameter("@TextKey", (object?)mDto.TextKey ?? DBNull.Value));
                            ci2.Parameters.Add(_db.CreateParameter("@DescKey", (object?)mDto.DescriptionKey ?? DBNull.Value));
                            await ci2.ExecuteNonQueryAsync();
                        }
                    }

                    // c) Upsert media_localization (link per-locale)
                    const string updLoc = @"
                UPDATE media_localization
                   SET media_link = @MediaLink
                 WHERE media_id = @MediaId AND locale_id = @LocaleId;";
                    const string insLoc = @"
                INSERT INTO media_localization (id, media_id, locale_id, media_link)
                VALUES (@Id, @MediaId, @LocaleId, @MediaLink);";

                    if (mDto.LinkLocalizations != null)
                    {
                        foreach (var loc in mDto.LinkLocalizations)
                        {
                            await using var cl = _db.CreateCommand(conn, updLoc);
                            cl.Transaction = tx;
                            cl.Parameters.Add(_db.CreateParameter("@MediaId", mediaId));
                            cl.Parameters.Add(_db.CreateParameter("@LocaleId", loc.LocaleId));
                            cl.Parameters.Add(_db.CreateParameter("@MediaLink", loc.MediaLink ?? string.Empty));

                            if (await cl.ExecuteNonQueryAsync() == 0)
                            {
                                await using var ci = _db.CreateCommand(conn, insLoc);
                                ci.Transaction = tx;
                                ci.Parameters.Add(_db.CreateParameter("@Id", Guid.NewGuid().ToString()));
                                ci.Parameters.Add(_db.CreateParameter("@MediaId", mediaId));
                                ci.Parameters.Add(_db.CreateParameter("@LocaleId", loc.LocaleId));
                                ci.Parameters.Add(_db.CreateParameter("@MediaLink", loc.MediaLink ?? string.Empty));
                                await ci.ExecuteNonQueryAsync();
                            }
                        }
                    }
                }

                // 5) Apply to corresponding/thumbnail media if provided
                if (dto.CorrespondingMedia != null)
                    await UpsertMedia(dto.CorrespondingMedia, "corresponding_media_id");
                if (dto.ThumbnailMedia != null)
                    await UpsertMedia(dto.ThumbnailMedia, "thumbnail_media_id");

                await tx.CommitAsync();

                // 6) Reload
                return await GetPortalByIdAsync(spaceId, portalId);
            }
            catch
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
            await using var conn = await _db.OpenConnectionAsync();
            await using var tx = await conn.BeginTransactionAsync();

            try
            {
                // 1) Fetch text_field_key and verify portal belongs to booth+space
                const string fetch = @"
           SELECT text_field_key
           FROM portal
           WHERE id = @PortalId
             AND space_id = @SpaceId
             AND booth_id = @BoothId;";

                string textKey;
                await using (var c = _db.CreateCommand(conn, fetch))
                {
                    c.Transaction = tx;
                    c.Parameters.Add(_db.CreateParameter("@PortalId", portalId));
                    c.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));
                    c.Parameters.Add(_db.CreateParameter("@BoothId", boothId));

                    var o = await c.ExecuteScalarAsync();
                    if (o == null || o == DBNull.Value)
                    {
                        await tx.RollbackAsync();
                        return null; // not found or booth mismatch
                    }
                    textKey = Convert.ToString(o)!;
                }
                // 2. Update portal type
                const string updType = @"
                    UPDATE portal
                    SET portal_type_id = @PortalTypeId
                    WHERE id = @PortalId
                      AND space_id = @SpaceId
                      AND booth_id = @BoothId;";

                await using (var cmdL = _db.CreateCommand(conn, updType))
                {
                    cmdL.Transaction = tx;
                    cmdL.Parameters.Add(_db.CreateParameter("@PortalTypeId", (object?)dto.PortalTypeId ?? DBNull.Value));
                    cmdL.Parameters.Add(_db.CreateParameter("@PortalId", portalId));
                    cmdL.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));
                    cmdL.Parameters.Add(_db.CreateParameter("@BoothId", boothId));
                    await cmdL.ExecuteNonQueryAsync();
                }
                // 2) Update external_link
                const string updLink = @"
            UPDATE portal
               SET external_link = @ExternalLink
             WHERE id = @PortalId
               AND space_id = @SpaceId
               AND booth_id = @BoothId;";

                await using (var cmdL = _db.CreateCommand(conn, updLink))
                {
                    cmdL.Transaction = tx;
                    cmdL.Parameters.Add(_db.CreateParameter("@ExternalLink", (object?)dto.ExternalLink ?? DBNull.Value));
                    cmdL.Parameters.Add(_db.CreateParameter("@PortalId", portalId));
                    cmdL.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));
                    cmdL.Parameters.Add(_db.CreateParameter("@BoothId", boothId));
                    await cmdL.ExecuteNonQueryAsync();
                }

                // 3) Upsert i18n for the portal text key
                const string updI18n = @"
            UPDATE i18n
               SET value = @Value
             WHERE `key` = @TextKey
               AND locale_id = @LocaleId
               AND space_id  = @SpaceId;";
                const string insI18n = @"
            INSERT INTO i18n (`key`, locale_id, value, space_id)
            VALUES (@TextKey, @LocaleId, @Value, @SpaceId);";

                if (dto.LocalizedPairs?.Values != null)
                {
                    foreach (var loc in dto.LocalizedPairs.Values)
                    {
                        var localeId = loc.LocaleId;
                        var value = loc.Value ?? string.Empty;

                        await using var cu = _db.CreateCommand(conn, updI18n);
                        cu.Transaction = tx;
                        cu.Parameters.Add(_db.CreateParameter("@TextKey", textKey));
                        cu.Parameters.Add(_db.CreateParameter("@LocaleId", localeId));
                        cu.Parameters.Add(_db.CreateParameter("@Value", value));
                        cu.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));

                        if (await cu.ExecuteNonQueryAsync() == 0)
                        {
                            await using var ci = _db.CreateCommand(conn, insI18n);
                            ci.Transaction = tx;
                            ci.Parameters.Add(_db.CreateParameter("@TextKey", textKey));
                            ci.Parameters.Add(_db.CreateParameter("@LocaleId", localeId));
                            ci.Parameters.Add(_db.CreateParameter("@Value", value));
                            ci.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));
                            await ci.ExecuteNonQueryAsync();
                        }
                    }
                }

                // 4) Helper: upsert a media block and link it on the portal row
                async Task UpsertMedia(MediaUpdateDto mDto, string columnIdName)
                {
                    var mediaId = string.IsNullOrWhiteSpace(mDto.Id)
                        ? Guid.NewGuid().ToString()
                        : mDto.Id;

                    // a) Link media to portal (booth- and space-scoped)
                    var updPortalMedia = $@"
                UPDATE portal
                   SET {columnIdName} = @MediaId
                 WHERE id = @PortalId AND space_id = @SpaceId AND booth_id = @BoothId;";

                    await using (var cm = _db.CreateCommand(conn, updPortalMedia))
                    {
                        cm.Transaction = tx;
                        cm.Parameters.Add(_db.CreateParameter("@MediaId", mediaId));
                        cm.Parameters.Add(_db.CreateParameter("@PortalId", portalId));
                        cm.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));
                        cm.Parameters.Add(_db.CreateParameter("@BoothId", boothId));

                        if (await cm.ExecuteNonQueryAsync() == 0)
                            throw new InvalidOperationException("Portal not found");
                    }

                    // b) Upsert media row (space-scoped)
                    const string updMedia = @"
                UPDATE media
                   SET media_type_id = @MediaTypeId,
                       text_key      = @TextKey,
                       description_key = @DescKey
                 WHERE id = @MediaId AND space_id = @SpaceId;";
                    const string insMedia = @"
                INSERT INTO media (id, space_id, media_type_id, text_key, description_key)
                VALUES (@MediaId, @SpaceId, @MediaTypeId, @TextKey, @DescKey);";

                    await using (var cm2 = _db.CreateCommand(conn, updMedia))
                    {
                        cm2.Transaction = tx;
                        cm2.Parameters.Add(_db.CreateParameter("@MediaId", mediaId));
                        cm2.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));
                        cm2.Parameters.Add(_db.CreateParameter("@MediaTypeId", mDto.MediaTypeId));
                        cm2.Parameters.Add(_db.CreateParameter("@TextKey", (object?)mDto.TextKey ?? DBNull.Value));
                        cm2.Parameters.Add(_db.CreateParameter("@DescKey", (object?)mDto.DescriptionKey ?? DBNull.Value));

                        if (await cm2.ExecuteNonQueryAsync() == 0)
                        {
                            await using var ci2 = _db.CreateCommand(conn, insMedia);
                            ci2.Transaction = tx;
                            ci2.Parameters.Add(_db.CreateParameter("@MediaId", mediaId));
                            ci2.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));
                            ci2.Parameters.Add(_db.CreateParameter("@MediaTypeId", mDto.MediaTypeId));
                            ci2.Parameters.Add(_db.CreateParameter("@TextKey", (object?)mDto.TextKey ?? DBNull.Value));
                            ci2.Parameters.Add(_db.CreateParameter("@DescKey", (object?)mDto.DescriptionKey ?? DBNull.Value));
                            await ci2.ExecuteNonQueryAsync();
                        }
                    }

                    // c) Upsert media_localization per locale
                    const string updLoc = @"
                UPDATE media_localization
                   SET media_link = @MediaLink
                 WHERE media_id = @MediaId AND locale_id = @LocaleId;";
                    const string insLoc = @"
                INSERT INTO media_localization (id, media_id, locale_id, media_link)
                VALUES (@Id, @MediaId, @LocaleId, @MediaLink);";

                    if (mDto.LinkLocalizations != null)
                    {
                        foreach (var loc in mDto.LinkLocalizations)
                        {
                            await using var cl = _db.CreateCommand(conn, updLoc);
                            cl.Transaction = tx;
                            cl.Parameters.Add(_db.CreateParameter("@MediaId", mediaId));
                            cl.Parameters.Add(_db.CreateParameter("@LocaleId", loc.LocaleId));
                            cl.Parameters.Add(_db.CreateParameter("@MediaLink", loc.MediaLink ?? string.Empty));

                            if (await cl.ExecuteNonQueryAsync() == 0)
                            {
                                await using var ci = _db.CreateCommand(conn, insLoc);
                                ci.Transaction = tx;
                                ci.Parameters.Add(_db.CreateParameter("@Id", Guid.NewGuid().ToString()));
                                ci.Parameters.Add(_db.CreateParameter("@MediaId", mediaId));
                                ci.Parameters.Add(_db.CreateParameter("@LocaleId", loc.LocaleId));
                                ci.Parameters.Add(_db.CreateParameter("@MediaLink", loc.MediaLink ?? string.Empty));
                                await ci.ExecuteNonQueryAsync();
                            }
                        }
                    }
                }

                // 5) Apply to corresponding/thumbnail media if provided
                if (dto.CorrespondingMedia != null)
                    await UpsertMedia(dto.CorrespondingMedia, "corresponding_media_id");
                if (dto.ThumbnailMedia != null)
                    await UpsertMedia(dto.ThumbnailMedia, "thumbnail_media_id");

                await tx.CommitAsync();

                // 6) Reload and return
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
            await using var conn = await _db.OpenConnectionAsync();
            await using var tx = await conn.BeginTransactionAsync();

            try
            {
                // 1) Fetch portal row to get keys and media IDs
                const string fetchSql = @"
            SELECT text_field_key, corresponding_media_id, thumbnail_media_id
            FROM portal
            WHERE id = @PortalId
              AND space_id = @SpaceId;";

                string textKey;
                string? corrId;
                string? thumbId;

                await using (var fetchCmd = _db.CreateCommand(conn, fetchSql))
                {
                    fetchCmd.Transaction = tx;
                    fetchCmd.Parameters.Add(_db.CreateParameter("@PortalId", portalId));
                    fetchCmd.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));

                    await using var reader = await fetchCmd.ExecuteReaderAsync();
                    if (!await reader.ReadAsync())
                    {
                        await tx.RollbackAsync();
                        return false; // Not found
                    }

                    var o_text_key = reader.GetOrdinal("text_field_key");
                    var o_corr_id = reader.GetOrdinal("corresponding_media_id");
                    var o_thumb_id = reader.GetOrdinal("thumbnail_media_id");

                    textKey = reader.IsDBNull(o_text_key) ? string.Empty : reader.GetString(o_text_key);
                    corrId = reader.IsDBNull(o_corr_id) ? null : reader.GetString(o_corr_id);
                    thumbId = reader.IsDBNull(o_thumb_id) ? null : reader.GetString(o_thumb_id);
                }

                // 2) Delete i18n rows for this portal’s key (space-scoped)
                const string delI18n = @"
            DELETE FROM i18n
            WHERE `key` = @TextKey
              AND space_id = @SpaceId;";

                await using (var cmdI18n = _db.CreateCommand(conn, delI18n))
                {
                    cmdI18n.Transaction = tx;
                    cmdI18n.Parameters.Add(_db.CreateParameter("@TextKey", textKey));
                    cmdI18n.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));
                    await cmdI18n.ExecuteNonQueryAsync();
                }

                // 3) Delete portal row itself
                const string delPortal = @"
            DELETE FROM portal
            WHERE id = @PortalId
              AND space_id = @SpaceId;";

                await using (var cmdPortal = _db.CreateCommand(conn, delPortal))
                {
                    cmdPortal.Transaction = tx;
                    cmdPortal.Parameters.Add(_db.CreateParameter("@PortalId", portalId));
                    cmdPortal.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));
                    await cmdPortal.ExecuteNonQueryAsync();
                }

                // 4) Delete media_localization and media for corresponding_media_id
                if (!string.IsNullOrEmpty(corrId))
                {
                    const string delLoc1 = @"DELETE FROM media_localization WHERE media_id = @MediaId;";
                    await using (var cmdLoc = _db.CreateCommand(conn, delLoc1))
                    {
                        cmdLoc.Transaction = tx;
                        cmdLoc.Parameters.Add(_db.CreateParameter("@MediaId", corrId!));
                        await cmdLoc.ExecuteNonQueryAsync();
                    }

                    const string delMed1 = @"DELETE FROM media WHERE id = @MediaId;";
                    await using (var cmdMed = _db.CreateCommand(conn, delMed1))
                    {
                        cmdMed.Transaction = tx;
                        cmdMed.Parameters.Add(_db.CreateParameter("@MediaId", corrId!));
                        await cmdMed.ExecuteNonQueryAsync();
                    }
                }

                // 5) Delete media_localization and media for thumbnail_media_id
                if (!string.IsNullOrEmpty(thumbId))
                {
                    const string delLoc2 = @"DELETE FROM media_localization WHERE media_id = @MediaId;";
                    await using (var cmdLoc = _db.CreateCommand(conn, delLoc2))
                    {
                        cmdLoc.Transaction = tx;
                        cmdLoc.Parameters.Add(_db.CreateParameter("@MediaId", thumbId!));
                        await cmdLoc.ExecuteNonQueryAsync();
                    }

                    const string delMed2 = @"DELETE FROM media WHERE id = @MediaId;";
                    await using (var cmdMed = _db.CreateCommand(conn, delMed2))
                    {
                        cmdMed.Transaction = tx;
                        cmdMed.Parameters.Add(_db.CreateParameter("@MediaId", thumbId!));
                        await cmdMed.ExecuteNonQueryAsync();
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


        /// <summary>
        /// Deletes a portal by its ID, booth, and space, ensuring the booth association matches, and removes all dependent localizations and media records.
        /// </summary>
        /// <param name="spaceId">The ID of the space containing the portal.</param>
        /// <param name="boothId">The ID of the booth the portal belongs to.</param>
        /// <param name="portalId">The ID of the portal to delete.</param>
        /// <returns>True if the portal and all associated records were deleted; false if not found or booth association mismatch.</returns>
        public async Task<bool> DeletePortalAsync(int spaceId, int boothId, int portalId)
        {
            await using var conn = await _db.OpenConnectionAsync();
            await using var tx = await conn.BeginTransactionAsync();

            try
            {
                // 1) Fetch portal row to get keys and media IDs, and confirm booth match
                const string fetchSql = @"
            SELECT text_field_key, corresponding_media_id, thumbnail_media_id
            FROM portal
            WHERE id = @PortalId
              AND space_id = @SpaceId
              AND booth_id = @BoothId;";

                string textKey;
                string? corrId;
                string? thumbId;

                await using (var fetchCmd = _db.CreateCommand(conn, fetchSql))
                {
                    fetchCmd.Transaction = tx;
                    fetchCmd.Parameters.Add(_db.CreateParameter("@PortalId", portalId));
                    fetchCmd.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));
                    fetchCmd.Parameters.Add(_db.CreateParameter("@BoothId", boothId));

                    await using var reader = await fetchCmd.ExecuteReaderAsync();
                    if (!await reader.ReadAsync())
                    {
                        await tx.RollbackAsync();
                        return false; // Not found or booth mismatch
                    }

                    var o_text = reader.GetOrdinal("text_field_key");
                    var o_corr = reader.GetOrdinal("corresponding_media_id");
                    var o_thumb = reader.GetOrdinal("thumbnail_media_id");

                    textKey = reader.IsDBNull(o_text) ? string.Empty : reader.GetString(o_text);
                    corrId = reader.IsDBNull(o_corr) ? null : reader.GetString(o_corr);
                    thumbId = reader.IsDBNull(o_thumb) ? null : reader.GetString(o_thumb);
                }

                // 2) Delete i18n rows for this portal’s key (space-scoped)
                const string delI18n = @"
            DELETE FROM i18n
            WHERE `key` = @TextKey
              AND space_id = @SpaceId;";

                await using (var cmdI18n = _db.CreateCommand(conn, delI18n))
                {
                    cmdI18n.Transaction = tx;
                    cmdI18n.Parameters.Add(_db.CreateParameter("@TextKey", textKey));
                    cmdI18n.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));
                    await cmdI18n.ExecuteNonQueryAsync();
                }

                // 3) Delete portal row itself (booth-scoped)
                const string delPortal = @"
            DELETE FROM portal
            WHERE id = @PortalId
              AND space_id = @SpaceId
              AND booth_id = @BoothId;";

                await using (var cmdPortal = _db.CreateCommand(conn, delPortal))
                {
                    cmdPortal.Transaction = tx;
                    cmdPortal.Parameters.Add(_db.CreateParameter("@PortalId", portalId));
                    cmdPortal.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));
                    cmdPortal.Parameters.Add(_db.CreateParameter("@BoothId", boothId));
                    await cmdPortal.ExecuteNonQueryAsync();
                }

                // 4) Delete media_localization and media for corresponding_media_id
                if (!string.IsNullOrEmpty(corrId))
                {
                    const string delLoc1 = @"DELETE FROM media_localization WHERE media_id = @MediaId;";
                    await using (var cmdLoc = _db.CreateCommand(conn, delLoc1))
                    {
                        cmdLoc.Transaction = tx;
                        cmdLoc.Parameters.Add(_db.CreateParameter("@MediaId", corrId!));
                        await cmdLoc.ExecuteNonQueryAsync();
                    }

                    const string delMed1 = @"DELETE FROM media WHERE id = @MediaId;";
                    await using (var cmdMed = _db.CreateCommand(conn, delMed1))
                    {
                        cmdMed.Transaction = tx;
                        cmdMed.Parameters.Add(_db.CreateParameter("@MediaId", corrId!));
                        await cmdMed.ExecuteNonQueryAsync();
                    }
                }

                // 5) Delete media_localization and media for thumbnail_media_id
                if (!string.IsNullOrEmpty(thumbId))
                {
                    const string delLoc2 = @"DELETE FROM media_localization WHERE media_id = @MediaId;";
                    await using (var cmdLoc = _db.CreateCommand(conn, delLoc2))
                    {
                        cmdLoc.Transaction = tx;
                        cmdLoc.Parameters.Add(_db.CreateParameter("@MediaId", thumbId!));
                        await cmdLoc.ExecuteNonQueryAsync();
                    }

                    const string delMed2 = @"DELETE FROM media WHERE id = @MediaId;";
                    await using (var cmdMed = _db.CreateCommand(conn, delMed2))
                    {
                        cmdMed.Transaction = tx;
                        cmdMed.Parameters.Add(_db.CreateParameter("@MediaId", thumbId!));
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
        DbConnection conn,
        DbTransaction tx,
        int spaceId,
        MediaCreateDto dto,
        HashSet<string>? supportedLocales)
        {
            if (dto == null) throw new ArgumentNullException(nameof(dto));

            // 1) Get UUID for new media row
            string mediaId;
            await using (var uuidCmd = _db.CreateCommand(conn, "SELECT UUID();"))
            {
                uuidCmd.Transaction = tx;
                var o = await uuidCmd.ExecuteScalarAsync();
                mediaId = Convert.ToString(o)!;
            }

            // 2) Insert media row
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

            // Helper to check locale support (if a set was provided)
            bool IsSupported(string locale) =>
                supportedLocales == null || supportedLocales.Count == 0 || supportedLocales.Contains(locale);

            // 3) Insert i18n text values
            if (!string.IsNullOrWhiteSpace(dto.TextKey) && dto.TextLocalizations != null)
            {
                const string insI18n = @"
        INSERT INTO i18n (`key`, locale_id, value, space_id)
        VALUES (@Key, @LocaleId, @Value, @SpaceId);";

                foreach (var loc in dto.TextLocalizations)
                {
                    if (string.IsNullOrWhiteSpace(loc.LocaleId) || !IsSupported(loc.LocaleId)) continue;

                    await using var cmdI = _db.CreateCommand(conn, insI18n);
                    cmdI.Transaction = tx;
                    cmdI.Parameters.Add(_db.CreateParameter("@Key", dto.TextKey));
                    cmdI.Parameters.Add(_db.CreateParameter("@LocaleId", loc.LocaleId));
                    cmdI.Parameters.Add(_db.CreateParameter("@Value", loc.Value ?? string.Empty));
                    cmdI.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));   // <— add this
                    await cmdI.ExecuteNonQueryAsync();
                }
            }

            // 4) Insert i18n description values
            if (!string.IsNullOrWhiteSpace(dto.DescriptionKey) && dto.DescriptionLocalizations != null)
            {
                const string insI18n = @"
        INSERT INTO i18n (`key`, locale_id, value, space_id)
        VALUES (@Key, @LocaleId, @Value, @SpaceId);";

                foreach (var loc in dto.DescriptionLocalizations)
                {
                    if (string.IsNullOrWhiteSpace(loc.LocaleId) || !IsSupported(loc.LocaleId)) continue;

                    await using var cmdI = _db.CreateCommand(conn, insI18n);
                    cmdI.Transaction = tx;
                    cmdI.Parameters.Add(_db.CreateParameter("@Key", dto.DescriptionKey));
                    cmdI.Parameters.Add(_db.CreateParameter("@LocaleId", loc.LocaleId));
                    cmdI.Parameters.Add(_db.CreateParameter("@Value", loc.Value ?? string.Empty));
                    cmdI.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));   // <— add this
                    await cmdI.ExecuteNonQueryAsync();
                }
            }


            // 5) Insert media links (media_localization)
            if (dto.LinkLocalizations != null)
            {
                const string insLoc = @"
            INSERT INTO media_localization (media_id, locale_id, media_link)
            VALUES (@MediaId, @LocaleId, @MediaLink);";

                foreach (var loc in dto.LinkLocalizations)
                {
                    if (string.IsNullOrWhiteSpace(loc.LocaleId) || !IsSupported(loc.LocaleId)) continue;

                    await using var cmdLoc = _db.CreateCommand(conn, insLoc);
                    cmdLoc.Transaction = tx;
                    cmdLoc.Parameters.Add(_db.CreateParameter("@MediaId", mediaId));
                    cmdLoc.Parameters.Add(_db.CreateParameter("@LocaleId", loc.LocaleId));
                    cmdLoc.Parameters.Add(_db.CreateParameter("@MediaLink", loc.MediaLink ?? string.Empty));
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
        DbConnection conn,
        DbTransaction tx,
        int spaceId,
        MediaCreateDto? mediaDto,
        HashSet<string> supportedLocales,
        Func<DbConnection, DbTransaction, int, MediaCreateDto, HashSet<string>?, Task<string>> insertFunc)
        {
            if (mediaDto == null)
                return null;

            // Filter LinkLocalizations (List<MediaLocalization>)
            if (mediaDto.LinkLocalizations != null)
            {
                mediaDto.LinkLocalizations = mediaDto.LinkLocalizations
                    .Where(x => !string.IsNullOrWhiteSpace(x.LocaleId) && supportedLocales.Contains(x.LocaleId))
                    .ToList();
            }

            // Filter TextLocalizations (List<LocalizedValue>)
            if (mediaDto.TextLocalizations != null)
            {
                mediaDto.TextLocalizations = mediaDto.TextLocalizations
                    .Where(x => !string.IsNullOrWhiteSpace(x.LocaleId) && supportedLocales.Contains(x.LocaleId))
                    .ToList();
            }

            // Filter DescriptionLocalizations (List<LocalizedValue>)
            if (mediaDto.DescriptionLocalizations != null)
            {
                mediaDto.DescriptionLocalizations = mediaDto.DescriptionLocalizations
                    .Where(x => !string.IsNullOrWhiteSpace(x.LocaleId) && supportedLocales.Contains(x.LocaleId))
                    .ToList();
            }

            // Only insert if there are any link localizations after filtering
            if (mediaDto.LinkLocalizations != null && mediaDto.LinkLocalizations.Count > 0)
            {
                // insertFunc is your provider-agnostic InsertMediaAsync(conn, tx, spaceId, dto, supportedLocales)
                return await insertFunc(conn, tx, spaceId, mediaDto, supportedLocales);
            }

            return null;
        }


        #endregion
    }
}
