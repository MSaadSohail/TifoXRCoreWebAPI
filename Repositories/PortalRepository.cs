// <copyright file="PortalRepository.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Saad Sohail</author>
// <date>07/23/2025</date>
// <summary>Class to handle portal SQL side</summary>

//
using GMS.TifoXRCoreWebAPI.Models;
using GMS.TifoXRCoreWebAPI.Models.Common;
using GMS.TifoXRCoreWebAPI.Utilities.Infrastructure;
//
using System.Data;
using System.Data.Common;

namespace GMS.TifoXRCoreWebAPI.Repositories
{
    public class PortalRepository(IDbProvider db) : IPortalRepository
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
            p.text_field_key,

            sl.locale_id,
            i.[value]                     AS localized_text_value,

            cm.media_type_id              AS corr_media_type_id,
            tm.media_type_id              AS thumb_media_type_id,

            ml1.media_link                AS corr_media_link,
            ml2.media_link                AS thumb_media_link
        FROM portal p
        LEFT JOIN supported_languages sl
            ON sl.space_id = p.space_id
        LEFT JOIN i18n i
            ON i.[key] = p.text_field_key
           AND i.space_id = p.space_id
           AND i.locale_id = sl.locale_id
        LEFT JOIN media cm
            ON cm.id = p.corresponding_media_id
           AND cm.space_id = p.space_id
        LEFT JOIN media tm
            ON tm.id = p.thumbnail_media_id
           AND tm.space_id = p.space_id
        LEFT JOIN media_localization ml1
            ON ml1.media_id = p.corresponding_media_id
           AND ml1.locale_id = sl.locale_id
        LEFT JOIN media_localization ml2
            ON ml2.media_id = p.thumbnail_media_id
           AND ml2.locale_id = sl.locale_id
        WHERE p.space_id = @SpaceId
        ORDER BY p.id, sl.locale_id;";

            await using var conn = await _db.OpenConnectionAsync();
            await using var cmd = _db.CreateCommand(conn, sql);
            cmd.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));

            var map = new Dictionary<int, PortalModel>();

            await using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                var portalId = reader.GetInt32(reader.GetOrdinal("portal_id"));

                if (!map.TryGetValue(portalId, out var portal))
                {
                    portal = new PortalModel
                    {
                        Id = portalId,
                        SpaceId = reader.GetInt32(reader.GetOrdinal("space_id")),
                        BoothId = reader.IsDBNull("booth_id") ? null : reader.GetInt32("booth_id"),
                        PortalTypeId = reader.IsDBNull("portal_type_id") ? null : reader.GetInt32("portal_type_id"),
                        EventId = reader.IsDBNull("event_id") ? null : reader.GetInt32("event_id"),
                        ExternalLink = reader.IsDBNull("external_link") ? null : reader.GetString("external_link"),
                        LocalizedPairs = new LocalizedPairs
                        {
                            Key = reader.GetString(reader.GetOrdinal("text_field_key")),
                            Values = new List<LocalizedValue>()
                        },
                        CorrespondingMedia = BuildMediaData(reader, "corr_media_id", "corr_media_type_id")!,
                        ThumbnailMedia = BuildMediaData(reader,"thumb_media_id","thumb_media_type_id")
                    };

                    map[portalId] = portal;
                }

                if (reader.IsDBNull("locale_id"))
                    continue;

                var locale = reader.GetString("locale_id");

                if (!reader.IsDBNull("localized_text_value") &&
                    !portal.LocalizedPairs.Values.Any(v => v.LocaleId == locale))
                {
                    portal.LocalizedPairs.Values.Add(new LocalizedValue
                    {
                        LocaleId = locale,
                        Value = reader.GetString("localized_text_value")
                    });
                }

                if (!reader.IsDBNull("corr_media_link") &&
                    !portal.CorrespondingMedia.LinkLocalizations.Any(l => l.LocaleId == locale))
                {
                    portal.CorrespondingMedia.LinkLocalizations.Add(new MediaLocalization
                    {
                        LocaleId = locale,
                        MediaLink = reader.GetString("corr_media_link")
                    });
                }

                if (!reader.IsDBNull("thumb_media_link") &&
                    !portal.ThumbnailMedia.LinkLocalizations.Any(l => l.LocaleId == locale))
                {
                    portal.ThumbnailMedia.LinkLocalizations.Add(new MediaLocalization
                    {
                        LocaleId = locale,
                        MediaLink = reader.GetString("thumb_media_link")
                    });
                }
            }

            return map.Values.ToList();
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
    i.[value]                 AS localized_text_value,

    ml1.media_link            AS corr_media_link,
    ml2.media_link            AS thumb_media_link

FROM portal p

LEFT JOIN supported_languages sl
    ON sl.space_id = p.space_id

LEFT JOIN i18n i 
    ON i.[key]      = p.text_field_key
   AND i.space_id   = p.space_id
   AND i.locale_id  = sl.locale_id
   AND i.is_deleted = 0

LEFT JOIN media cm
    ON cm.id        = p.corresponding_media_id
   AND cm.space_id  = p.space_id
   AND cm.is_deleted = 0

LEFT JOIN media tm
    ON tm.id        = p.thumbnail_media_id
   AND tm.space_id  = p.space_id
   AND tm.is_deleted = 0

LEFT JOIN media_localization ml1 
    ON ml1.media_id  = p.corresponding_media_id
   AND ml1.locale_id = sl.locale_id
   AND ml1.is_deleted = 0

LEFT JOIN media_localization ml2 
    ON ml2.media_id  = p.thumbnail_media_id
   AND ml2.locale_id = sl.locale_id
   AND ml2.is_deleted = 0

WHERE p.space_id = @SpaceId
  AND p.booth_id = @BoothId
  AND p.is_deleted = 0
ORDER BY p.id, sl.locale_id;
";

            await using var conn = await _db.OpenConnectionAsync();
            await using var cmd = _db.CreateCommand(conn, sql);

            cmd.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));
            cmd.Parameters.Add(_db.CreateParameter("@BoothId", boothId));

            var map = new Dictionary<int, PortalModel>();

            await using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                var portalId = reader.GetInt32(reader.GetOrdinal("portal_id"));

                if (!map.TryGetValue(portalId, out var portal))
                {
                    portal = new PortalModel
                    {
                        Id = portalId,
                        SpaceId = reader.GetInt32(reader.GetOrdinal("space_id")),
                        BoothId = reader.GetInt32(reader.GetOrdinal("booth_id")),
                        PortalTypeId = reader.IsDBNull(reader.GetOrdinal("portal_type_id"))
                            ? null
                            : reader.GetInt32(reader.GetOrdinal("portal_type_id")),
                        EventId = reader.IsDBNull(reader.GetOrdinal("event_id"))
                            ? null
                            : reader.GetInt32(reader.GetOrdinal("event_id")),
                        ExternalLink = reader.IsDBNull(reader.GetOrdinal("external_link"))
                            ? null
                            : reader.GetString(reader.GetOrdinal("external_link")),

                        LocalizedPairs = new LocalizedPairs
                        {
                            Key = reader.GetString(reader.GetOrdinal("text_key")),
                            Values = new List<LocalizedValue>()
                        },

                        CorrespondingMedia = BuildMediaData(reader, "corr_media_id", "corr_media_type_id")!,
                        ThumbnailMedia = BuildMediaData(reader, "thumb_media_id", "thumb_media_type_id")
                    };

                    map[portalId] = portal;
                }

                if (reader.IsDBNull(reader.GetOrdinal("locale_id")))
                    continue;

                var locale = reader.GetString(reader.GetOrdinal("locale_id"));

                // --- Portal text localization ---
                if (!reader.IsDBNull(reader.GetOrdinal("localized_text_value")) &&
                    !portal.LocalizedPairs.Values.Any(v => v.LocaleId == locale))
                {
                    portal.LocalizedPairs.Values.Add(new LocalizedValue
                    {
                        LocaleId = locale,
                        Value = reader.GetString(reader.GetOrdinal("localized_text_value"))
                    });
                }

                // --- Corresponding media localization ---
                if (portal.CorrespondingMedia != null &&
                    !reader.IsDBNull(reader.GetOrdinal("corr_media_link")) &&
                    !portal.CorrespondingMedia.LinkLocalizations.Any(l => l.LocaleId == locale))
                {
                    portal.CorrespondingMedia.LinkLocalizations.Add(new MediaLocalization
                    {
                        LocaleId = locale,
                        MediaLink = reader.GetString(reader.GetOrdinal("corr_media_link"))
                    });
                }

                // --- Thumbnail media localization ---
                if (portal.ThumbnailMedia != null &&
                    !reader.IsDBNull(reader.GetOrdinal("thumb_media_link")) &&
                    !portal.ThumbnailMedia.LinkLocalizations.Any(l => l.LocaleId == locale))
                {
                    portal.ThumbnailMedia.LinkLocalizations.Add(new MediaLocalization
                    {
                        LocaleId = locale,
                        MediaLink = reader.GetString(reader.GetOrdinal("thumb_media_link"))
                    });
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
    i.locale_id                   AS locale_id,
    i.[value]                     AS localized_text_value,

    -- Media localizations
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
    ON i.[key] = p.text_field_key
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

            while (await reader.ReadAsync())
            {
                if (portal == null)
                {
                    portal = new PortalModel
                    {
                        Id = reader.GetInt32(reader.GetOrdinal("portal_id")),
                        SpaceId = reader.GetInt32(reader.GetOrdinal("space_id")),
                        BoothId = reader.IsDBNull(reader.GetOrdinal("booth_id"))
                            ? null
                            : reader.GetInt32(reader.GetOrdinal("booth_id")),
                        PortalTypeId = reader.IsDBNull(reader.GetOrdinal("portal_type_id"))
                            ? null
                            : reader.GetInt32(reader.GetOrdinal("portal_type_id")),
                        EventId = reader.IsDBNull(reader.GetOrdinal("event_id"))
                            ? null
                            : reader.GetInt32(reader.GetOrdinal("event_id")),
                        ExternalLink = reader.IsDBNull(reader.GetOrdinal("external_link"))
                            ? null
                            : reader.GetString(reader.GetOrdinal("external_link")),

                        LocalizedPairs = new LocalizedPairs
                        {
                            Key = reader.GetString(reader.GetOrdinal("text_key")),
                            Values = new List<LocalizedValue>()
                        },

                        CorrespondingMedia = BuildMediaData(reader, "corr_media_id", "corr_media_type_id")!,
                        ThumbnailMedia = BuildMediaData(reader, "thumb_media_id", "thumb_media_type_id")!
                    };
                }

                if (reader.IsDBNull(reader.GetOrdinal("locale_id")))
                    continue;

                var locale = reader.GetString(reader.GetOrdinal("locale_id"));

                // --- Portal text localization ---
                if (!reader.IsDBNull(reader.GetOrdinal("localized_text_value")) &&
                    !portal.LocalizedPairs.Values.Any(v => v.LocaleId == locale))
                {
                    portal.LocalizedPairs.Values.Add(new LocalizedValue
                    {
                        LocaleId = locale,
                        Value = reader.GetString(reader.GetOrdinal("localized_text_value"))
                    });
                }

                // --- Corresponding media localization ---
                if (portal.CorrespondingMedia != null &&
                    !reader.IsDBNull(reader.GetOrdinal("corresponding_media_link")) &&
                    !portal.CorrespondingMedia.LinkLocalizations.Any(l => l.LocaleId == locale))
                {
                    portal.CorrespondingMedia.LinkLocalizations.Add(new MediaLocalization
                    {
                        LocaleId = locale,
                        MediaLink = reader.GetString(reader.GetOrdinal("corresponding_media_link"))
                    });
                }

                // --- Thumbnail media localization ---
                if (portal.ThumbnailMedia != null &&
                    !reader.IsDBNull(reader.GetOrdinal("thumbnail_media_link")) &&
                    !portal.ThumbnailMedia.LinkLocalizations.Any(l => l.LocaleId == locale))
                {
                    portal.ThumbnailMedia.LinkLocalizations.Add(new MediaLocalization
                    {
                        LocaleId = locale,
                        MediaLink = reader.GetString(reader.GetOrdinal("thumbnail_media_link"))
                    });
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
                // 1️⃣ Load supported locales for this space
                var supportedLocales = new HashSet<string>();

                const string fetchLocalesSql = @"
SELECT locale_id
FROM supported_languages
WHERE space_id = @SpaceId;";

                await using (var cmd = _db.CreateCommand(conn, fetchLocalesSql))
                {
                    cmd.Transaction = tx;
                    cmd.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));

                    await using var rdr = await cmd.ExecuteReaderAsync();
                    while (await rdr.ReadAsync())
                    {
                        supportedLocales.Add(rdr.GetString(rdr.GetOrdinal("locale_id")));
                    }
                }

                // 2️⃣ Optionally insert corresponding media
                int? corrMediaId = null;
                if (portalDto.CorrespondingMedia != null)
                {
                    corrMediaId = await FilterAndInsertMediaAsync(
                        conn,
                        tx,
                        spaceId,
                        portalDto.CorrespondingMedia,
                        supportedLocales,
                        InsertMediaAsync
                    );
                }

                // 3️⃣ Optionally insert thumbnail media
                int? thumbMediaId = null;
                if (portalDto.ThumbnailMedia != null)
                {
                    thumbMediaId = await FilterAndInsertMediaAsync(
                        conn,
                        tx,
                        spaceId,
                        portalDto.ThumbnailMedia,
                        supportedLocales,
                        InsertMediaAsync
                    );
                }

                // 4️⃣ Insert portal (IDENTITY id)
                const string insertPortalSql = @"
INSERT INTO portal
    (space_id, booth_id, portal_type_id, event_id,
     corresponding_media_id, thumbnail_media_id,
     external_link)
VALUES
    (@SpaceId, @BoothId, @PortalTypeId, @EventId,
     @CorrId, @ThumbId, @ExternalLink);

SELECT CAST(SCOPE_IDENTITY() AS INT);";

                int newPortalId;

                await using (var cmd = _db.CreateCommand(conn, insertPortalSql))
                {
                    cmd.Transaction = tx;

                    cmd.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));
                    cmd.Parameters.Add(_db.CreateParameter("@BoothId", portalDto.BoothId));
                    cmd.Parameters.Add(_db.CreateParameter("@PortalTypeId", portalDto.PortalTypeId));
                    cmd.Parameters.Add(_db.CreateParameter("@EventId", portalDto.EventId));
                    cmd.Parameters.Add(_db.CreateParameter("@CorrId", (object?)corrMediaId ?? DBNull.Value));
                    cmd.Parameters.Add(_db.CreateParameter("@ThumbId", (object?)thumbMediaId ?? DBNull.Value));
                    cmd.Parameters.Add(_db.CreateParameter(
                        "@ExternalLink",
                        (object?)portalDto.ExternalLink ?? DBNull.Value
                    ));

                    newPortalId = (int)await cmd.ExecuteScalarAsync();
                }

                // 5️⃣ Fetch generated text_field_key
                const string fetchTextKeySql = @"
SELECT text_field_key
FROM portal
WHERE id = @PortalId;";

                string textFieldKey;

                await using (var cmd = _db.CreateCommand(conn, fetchTextKeySql))
                {
                    cmd.Transaction = tx;
                    cmd.Parameters.Add(_db.CreateParameter("@PortalId", newPortalId));

                    textFieldKey = (string)await cmd.ExecuteScalarAsync();
                }

                // 6️⃣ Insert i18n values (SQL Server syntax)
                const string insertI18nSql = @"
INSERT INTO i18n ([key], locale_id, [value], space_id)
VALUES (@TextKey, @LocaleId, @Value, @SpaceId);";

                if (portalDto.LocalizedPairs?.Values != null)
                {
                    foreach (var loc in portalDto.LocalizedPairs.Values
                                 .Where(x => supportedLocales.Contains(x.LocaleId)))
                    {
                        await using var cmdI = _db.CreateCommand(conn, insertI18nSql);
                        cmdI.Transaction = tx;

                        cmdI.Parameters.Add(_db.CreateParameter("@TextKey", textFieldKey));
                        cmdI.Parameters.Add(_db.CreateParameter("@LocaleId", loc.LocaleId));
                        cmdI.Parameters.Add(_db.CreateParameter("@Value", loc.Value));
                        cmdI.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));

                        await cmdI.ExecuteNonQueryAsync();
                    }
                }

                await tx.CommitAsync();

                // 7️⃣ Reload and return full portal
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
                // 1️⃣ Fetch existing text_field_key
                string textKey;

                const string fetchTextKeySql = @"
SELECT text_field_key
FROM portal
WHERE id = @PortalId
  AND space_id = @SpaceId;";

                await using (var cmd = _db.CreateCommand(conn, fetchTextKeySql))
                {
                    cmd.Transaction = tx;
                    cmd.Parameters.Add(_db.CreateParameter("@PortalId", portalId));
                    cmd.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));

                    var result = await cmd.ExecuteScalarAsync();
                    if (result == null)
                        return null;

                    textKey = (string)result;
                }

                // 2️⃣ Update external_link
                const string updatePortalLinkSql = @"
UPDATE portal
SET external_link = @ExternalLink
WHERE id = @PortalId
  AND space_id = @SpaceId;";

                await using (var cmd = _db.CreateCommand(conn, updatePortalLinkSql))
                {
                    cmd.Transaction = tx;
                    cmd.Parameters.Add(_db.CreateParameter("@ExternalLink", (object?)dto.ExternalLink ?? DBNull.Value));
                    cmd.Parameters.Add(_db.CreateParameter("@PortalId", portalId));
                    cmd.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));

                    await cmd.ExecuteNonQueryAsync();
                }

                // 3️⃣ Upsert i18n values
                const string updateI18nSql = @"
UPDATE i18n
SET [value] = @Value
WHERE [key] = @TextKey
  AND locale_id = @LocaleId
  AND space_id = @SpaceId;";

                const string insertI18nSql = @"
INSERT INTO i18n ([key], locale_id, [value], space_id)
VALUES (@TextKey, @LocaleId, @Value, @SpaceId);";

                if (dto.LocalizedPairs?.Values != null)
                {
                    foreach (var loc in dto.LocalizedPairs.Values)
                    {
                        await using var cmdUpd = _db.CreateCommand(conn, updateI18nSql);
                        cmdUpd.Transaction = tx;

                        cmdUpd.Parameters.Add(_db.CreateParameter("@TextKey", textKey));
                        cmdUpd.Parameters.Add(_db.CreateParameter("@LocaleId", loc.LocaleId));
                        cmdUpd.Parameters.Add(_db.CreateParameter("@Value", loc.Value));
                        cmdUpd.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));

                        if (await cmdUpd.ExecuteNonQueryAsync() == 0)
                        {
                            await using var cmdIns = _db.CreateCommand(conn, insertI18nSql);
                            cmdIns.Transaction = tx;

                            cmdIns.Parameters.Add(_db.CreateParameter("@TextKey", textKey));
                            cmdIns.Parameters.Add(_db.CreateParameter("@LocaleId", loc.LocaleId));
                            cmdIns.Parameters.Add(_db.CreateParameter("@Value", loc.Value));
                            cmdIns.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));

                            await cmdIns.ExecuteNonQueryAsync();
                        }
                    }
                }

                // 4️⃣ Local helper: upsert media + localizations
                async Task UpsertMediaAsync(MediaUpdateDto mDto, string portalColumn)
                {
                    var mediaId = mDto.Id;

                    // a) Update portal FK
                    var updatePortalMediaSql = $@"
UPDATE portal
SET {portalColumn} = @MediaId
WHERE id = @PortalId
  AND space_id = @SpaceId;";

                    await using (var cmd = _db.CreateCommand(conn, updatePortalMediaSql))
                    {
                        cmd.Transaction = tx;
                        cmd.Parameters.Add(_db.CreateParameter("@MediaId", mediaId));
                        cmd.Parameters.Add(_db.CreateParameter("@PortalId", portalId));
                        cmd.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));

                        if (await cmd.ExecuteNonQueryAsync() == 0)
                            throw new InvalidOperationException("Portal not found");
                    }

                    // b) Update media metadata
                    const string updateMediaSql = @"
UPDATE media
SET media_type_id = @MediaTypeId,
    text_key = @TextKey,
    description_key = @DescKey
WHERE id = @MediaId
  AND space_id = @SpaceId;";

                    await using (var cmd = _db.CreateCommand(conn, updateMediaSql))
                    {
                        cmd.Transaction = tx;
                        cmd.Parameters.Add(_db.CreateParameter("@MediaId", mediaId));
                        cmd.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));
                        cmd.Parameters.Add(_db.CreateParameter("@MediaTypeId", mDto.MediaTypeId));
                        cmd.Parameters.Add(_db.CreateParameter("@TextKey", (object?)mDto.TextKey ?? DBNull.Value));
                        cmd.Parameters.Add(_db.CreateParameter("@DescKey", (object?)mDto.DescriptionKey ?? DBNull.Value));

                        await cmd.ExecuteNonQueryAsync();
                    }

                    // c) Upsert media_localization
                    const string updateLocSql = @"
UPDATE media_localization
SET media_link = @MediaLink
WHERE media_id = @MediaId
  AND locale_id = @LocaleId;";

                    const string insertLocSql = @"
INSERT INTO media_localization (media_id, locale_id, media_link)
VALUES (@MediaId, @LocaleId, @MediaLink);";

                    if (mDto.LinkLocalizations != null)
                    {
                        foreach (var loc in mDto.LinkLocalizations)
                        {
                            await using var cmdUpd = _db.CreateCommand(conn, updateLocSql);
                            cmdUpd.Transaction = tx;

                            cmdUpd.Parameters.Add(_db.CreateParameter("@MediaId", mediaId));
                            cmdUpd.Parameters.Add(_db.CreateParameter("@LocaleId", loc.LocaleId));
                            cmdUpd.Parameters.Add(_db.CreateParameter("@MediaLink", loc.MediaLink));

                            if (await cmdUpd.ExecuteNonQueryAsync() == 0)
                            {
                                await using var cmdIns = _db.CreateCommand(conn, insertLocSql);
                                cmdIns.Transaction = tx;

                                cmdIns.Parameters.Add(_db.CreateParameter("@MediaId", mediaId));
                                cmdIns.Parameters.Add(_db.CreateParameter("@LocaleId", loc.LocaleId));
                                cmdIns.Parameters.Add(_db.CreateParameter("@MediaLink", loc.MediaLink));

                                await cmdIns.ExecuteNonQueryAsync();
                            }
                        }
                    }
                }

                // 5️⃣ Apply updates
                if (dto.CorrespondingMedia != null)
                    await UpsertMediaAsync(dto.CorrespondingMedia, "corresponding_media_id");

                if (dto.ThumbnailMedia != null)
                    await UpsertMediaAsync(dto.ThumbnailMedia, "thumbnail_media_id");

                await tx.CommitAsync();

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
        public async Task<PortalModel?> UpdatePortalAsync(
            int spaceId,
            int boothId,
            int portalId,
            PortalUpdateDto dto)
        {
            await using var conn = await _db.OpenConnectionAsync();
            await using var tx = await conn.BeginTransactionAsync();

            try
            {
                // 1️⃣ Fetch text_field_key and validate booth ownership
                const string fetchTextKeySql = @"
SELECT text_field_key
FROM portal
WHERE id = @PortalId
  AND space_id = @SpaceId
  AND booth_id = @BoothId;";

                string textKey;

                await using (var cmd = _db.CreateCommand(conn, fetchTextKeySql))
                {
                    cmd.Transaction = tx;
                    cmd.Parameters.Add(_db.CreateParameter("@PortalId", portalId));
                    cmd.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));
                    cmd.Parameters.Add(_db.CreateParameter("@BoothId", boothId));

                    var result = await cmd.ExecuteScalarAsync();
                    if (result == null)
                        return null;

                    textKey = (string)result;
                }

                // 2️⃣ Update external_link
                const string updatePortalSql = @"
UPDATE portal
SET external_link = @ExternalLink
WHERE id = @PortalId
  AND space_id = @SpaceId
  AND booth_id = @BoothId;";

                await using (var cmd = _db.CreateCommand(conn, updatePortalSql))
                {
                    cmd.Transaction = tx;
                    cmd.Parameters.Add(_db.CreateParameter("@ExternalLink", (object?)dto.ExternalLink ?? DBNull.Value));
                    cmd.Parameters.Add(_db.CreateParameter("@PortalId", portalId));
                    cmd.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));
                    cmd.Parameters.Add(_db.CreateParameter("@BoothId", boothId));

                    await cmd.ExecuteNonQueryAsync();
                }

                // 3️⃣ Upsert i18n values
                const string updateI18nSql = @"
UPDATE i18n
SET [value] = @Value
WHERE [key] = @TextKey
  AND locale_id = @LocaleId
  AND space_id = @SpaceId;";

                const string insertI18nSql = @"
INSERT INTO i18n ([key], locale_id, [value], space_id)
VALUES (@TextKey, @LocaleId, @Value, @SpaceId);";

                if (dto.LocalizedPairs?.Values != null)
                {
                    foreach (var loc in dto.LocalizedPairs.Values)
                    {
                        await using var cmdUpd = _db.CreateCommand(conn, updateI18nSql);
                        cmdUpd.Transaction = tx;

                        cmdUpd.Parameters.Add(_db.CreateParameter("@TextKey", textKey));
                        cmdUpd.Parameters.Add(_db.CreateParameter("@LocaleId", loc.LocaleId));
                        cmdUpd.Parameters.Add(_db.CreateParameter("@Value", loc.Value));
                        cmdUpd.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));

                        if (await cmdUpd.ExecuteNonQueryAsync() == 0)
                        {
                            await using var cmdIns = _db.CreateCommand(conn, insertI18nSql);
                            cmdIns.Transaction = tx;

                            cmdIns.Parameters.Add(_db.CreateParameter("@TextKey", textKey));
                            cmdIns.Parameters.Add(_db.CreateParameter("@LocaleId", loc.LocaleId));
                            cmdIns.Parameters.Add(_db.CreateParameter("@Value", loc.Value));
                            cmdIns.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));

                            await cmdIns.ExecuteNonQueryAsync();
                        }
                    }
                }

                // 4️⃣ Local helper: update media + localizations
                async Task UpsertMediaAsync(MediaUpdateDto mDto, string portalColumn)
                {
                    var mediaId = mDto.Id;

                    // a) Update portal FK
                    var updatePortalMediaSql = $@"
UPDATE portal
SET {portalColumn} = @MediaId
WHERE id = @PortalId
  AND space_id = @SpaceId
  AND booth_id = @BoothId;";

                    await using (var cmd = _db.CreateCommand(conn, updatePortalMediaSql))
                    {
                        cmd.Transaction = tx;
                        cmd.Parameters.Add(_db.CreateParameter("@MediaId", mediaId));
                        cmd.Parameters.Add(_db.CreateParameter("@PortalId", portalId));
                        cmd.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));
                        cmd.Parameters.Add(_db.CreateParameter("@BoothId", boothId));

                        if (await cmd.ExecuteNonQueryAsync() == 0)
                            throw new InvalidOperationException("Portal not found");
                    }

                    // b) Update media metadata
                    const string updateMediaSql = @"
UPDATE media
SET media_type_id   = @MediaTypeId,
    text_key        = @TextKey,
    description_key = @DescKey
WHERE id = @MediaId
  AND space_id = @SpaceId;";

                    await using (var cmd = _db.CreateCommand(conn, updateMediaSql))
                    {
                        cmd.Transaction = tx;
                        cmd.Parameters.Add(_db.CreateParameter("@MediaId", mediaId));
                        cmd.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));
                        cmd.Parameters.Add(_db.CreateParameter("@MediaTypeId", mDto.MediaTypeId));
                        cmd.Parameters.Add(_db.CreateParameter("@TextKey", (object?)mDto.TextKey ?? DBNull.Value));
                        cmd.Parameters.Add(_db.CreateParameter("@DescKey", (object?)mDto.DescriptionKey ?? DBNull.Value));

                        await cmd.ExecuteNonQueryAsync();
                    }

                    // c) Upsert media_localization
                    const string updateLocSql = @"
UPDATE media_localization
SET media_link = @MediaLink
WHERE media_id = @MediaId
  AND locale_id = @LocaleId;";

                    const string insertLocSql = @"
INSERT INTO media_localization (media_id, locale_id, media_link)
VALUES (@MediaId, @LocaleId, @MediaLink);";

                    if (mDto.LinkLocalizations != null)
                    {
                        foreach (var loc in mDto.LinkLocalizations)
                        {
                            await using var cmdUpd = _db.CreateCommand(conn, updateLocSql);
                            cmdUpd.Transaction = tx;

                            cmdUpd.Parameters.Add(_db.CreateParameter("@MediaId", mediaId));
                            cmdUpd.Parameters.Add(_db.CreateParameter("@LocaleId", loc.LocaleId));
                            cmdUpd.Parameters.Add(_db.CreateParameter("@MediaLink", loc.MediaLink));

                            if (await cmdUpd.ExecuteNonQueryAsync() == 0)
                            {
                                await using var cmdIns = _db.CreateCommand(conn, insertLocSql);
                                cmdIns.Transaction = tx;

                                cmdIns.Parameters.Add(_db.CreateParameter("@MediaId", mediaId));
                                cmdIns.Parameters.Add(_db.CreateParameter("@LocaleId", loc.LocaleId));
                                cmdIns.Parameters.Add(_db.CreateParameter("@MediaLink", loc.MediaLink));

                                await cmdIns.ExecuteNonQueryAsync();
                            }
                        }
                    }
                }

                // 5️⃣ Apply media updates
                if (dto.CorrespondingMedia != null)
                    await UpsertMediaAsync(dto.CorrespondingMedia, "corresponding_media_id");

                if (dto.ThumbnailMedia != null)
                    await UpsertMediaAsync(dto.ThumbnailMedia, "thumbnail_media_id");

                await tx.CommitAsync();

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
                // 1️⃣ Fetch portal-linked keys and media IDs (only if not already deleted)
                const string fetchSql = @"
SELECT
    text_field_key,
    corresponding_media_id,
    thumbnail_media_id
FROM portal
WHERE id = @PortalId
  AND space_id = @SpaceId
  AND is_deleted = 0;";

                string textKey;
                int? corrMediaId;
                int? thumbMediaId;

                await using (var cmd = _db.CreateCommand(conn, fetchSql))
                {
                    cmd.Transaction = tx;
                    cmd.Parameters.Add(_db.CreateParameter("@PortalId", portalId));
                    cmd.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));

                    await using var reader = await cmd.ExecuteReaderAsync();
                    if (!await reader.ReadAsync())
                        return false;

                    textKey = reader.GetString(reader.GetOrdinal("text_field_key"));
                    corrMediaId = reader.IsDBNull(reader.GetOrdinal("corresponding_media_id"))
                        ? null
                        : reader.GetInt32(reader.GetOrdinal("corresponding_media_id"));
                    thumbMediaId = reader.IsDBNull(reader.GetOrdinal("thumbnail_media_id"))
                        ? null
                        : reader.GetInt32(reader.GetOrdinal("thumbnail_media_id"));
                }

                // 2️⃣ Soft-delete i18n rows for portal text
                const string softDeleteI18nSql = @"
UPDATE i18n
SET is_deleted = 1
WHERE [key] = @TextKey
  AND space_id = @SpaceId
  AND is_deleted = 0;";

                await using (var cmd = _db.CreateCommand(conn, softDeleteI18nSql))
                {
                    cmd.Transaction = tx;
                    cmd.Parameters.Add(_db.CreateParameter("@TextKey", textKey));
                    cmd.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));
                    await cmd.ExecuteNonQueryAsync();
                }

                // 3️⃣ Soft-delete portal
                const string softDeletePortalSql = @"
UPDATE portal
SET is_deleted = 1
WHERE id = @PortalId
  AND space_id = @SpaceId
  AND is_deleted = 0;";

                await using (var cmd = _db.CreateCommand(conn, softDeletePortalSql))
                {
                    cmd.Transaction = tx;
                    cmd.Parameters.Add(_db.CreateParameter("@PortalId", portalId));
                    cmd.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));
                    await cmd.ExecuteNonQueryAsync();
                }

                // 4️⃣ Soft-delete corresponding media and its localizations
                if (corrMediaId.HasValue)
                {
                    const string softDeleteMediaLocSql = @"
UPDATE media_localization
SET is_deleted = 1
WHERE media_id = @MediaId
  AND is_deleted = 0;";

                    await using (var cmd = _db.CreateCommand(conn, softDeleteMediaLocSql))
                    {
                        cmd.Transaction = tx;
                        cmd.Parameters.Add(_db.CreateParameter("@MediaId", corrMediaId.Value));
                        await cmd.ExecuteNonQueryAsync();
                    }

                    const string softDeleteMediaSql = @"
UPDATE media
SET is_deleted = 1
WHERE id = @MediaId
  AND space_id = @SpaceId
  AND is_deleted = 0;";

                    await using (var cmd = _db.CreateCommand(conn, softDeleteMediaSql))
                    {
                        cmd.Transaction = tx;
                        cmd.Parameters.Add(_db.CreateParameter("@MediaId", corrMediaId.Value));
                        cmd.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));
                        await cmd.ExecuteNonQueryAsync();
                    }
                }

                // 5️⃣ Soft-delete thumbnail media and its localizations
                if (thumbMediaId.HasValue)
                {
                    const string softDeleteMediaLocSql = @"
UPDATE media_localization
SET is_deleted = 1
WHERE media_id = @MediaId
  AND is_deleted = 0;";

                    await using (var cmd = _db.CreateCommand(conn, softDeleteMediaLocSql))
                    {
                        cmd.Transaction = tx;
                        cmd.Parameters.Add(_db.CreateParameter("@MediaId", thumbMediaId.Value));
                        await cmd.ExecuteNonQueryAsync();
                    }

                    const string softDeleteMediaSql = @"
UPDATE media
SET is_deleted = 1
WHERE id = @MediaId
  AND space_id = @SpaceId
  AND is_deleted = 0;";

                    await using (var cmd = _db.CreateCommand(conn, softDeleteMediaSql))
                    {
                        cmd.Transaction = tx;
                        cmd.Parameters.Add(_db.CreateParameter("@MediaId", thumbMediaId.Value));
                        cmd.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));
                        await cmd.ExecuteNonQueryAsync();
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
                // 1️⃣ Fetch portal-linked keys and media IDs (confirm booth + not deleted)
                const string fetchSql = @"
SELECT
    text_field_key,
    corresponding_media_id,
    thumbnail_media_id
FROM portal
WHERE id = @PortalId
  AND space_id = @SpaceId
  AND booth_id = @BoothId
  AND is_deleted = 0;";

                string textKey;
                int? corrMediaId;
                int? thumbMediaId;

                await using (var cmd = _db.CreateCommand(conn, fetchSql))
                {
                    cmd.Transaction = tx;
                    cmd.Parameters.Add(_db.CreateParameter("@PortalId", portalId));
                    cmd.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));
                    cmd.Parameters.Add(_db.CreateParameter("@BoothId", boothId));

                    await using var reader = await cmd.ExecuteReaderAsync();
                    if (!await reader.ReadAsync())
                        return false;

                    textKey = reader.GetString(reader.GetOrdinal("text_field_key"));
                    corrMediaId = reader.IsDBNull(reader.GetOrdinal("corresponding_media_id"))
                        ? null
                        : reader.GetInt32(reader.GetOrdinal("corresponding_media_id"));
                    thumbMediaId = reader.IsDBNull(reader.GetOrdinal("thumbnail_media_id"))
                        ? null
                        : reader.GetInt32(reader.GetOrdinal("thumbnail_media_id"));
                }

                // 2️⃣ Soft-delete i18n rows
                const string softDeleteI18nSql = @"
UPDATE i18n
SET is_deleted = 1
WHERE [key] = @TextKey
  AND space_id = @SpaceId
  AND is_deleted = 0;";

                await using (var cmd = _db.CreateCommand(conn, softDeleteI18nSql))
                {
                    cmd.Transaction = tx;
                    cmd.Parameters.Add(_db.CreateParameter("@TextKey", textKey));
                    cmd.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));
                    await cmd.ExecuteNonQueryAsync();
                }

                // 3️⃣ Soft-delete portal
                const string softDeletePortalSql = @"
UPDATE portal
SET is_deleted = 1
WHERE id = @PortalId
  AND space_id = @SpaceId
  AND booth_id = @BoothId
  AND is_deleted = 0;";

                await using (var cmd = _db.CreateCommand(conn, softDeletePortalSql))
                {
                    cmd.Transaction = tx;
                    cmd.Parameters.Add(_db.CreateParameter("@PortalId", portalId));
                    cmd.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));
                    cmd.Parameters.Add(_db.CreateParameter("@BoothId", boothId));
                    await cmd.ExecuteNonQueryAsync();
                }

                // 4️⃣ Soft-delete corresponding media + localizations
                if (corrMediaId.HasValue)
                {
                    const string softDeleteMediaLocSql = @"
UPDATE media_localization
SET is_deleted = 1
WHERE media_id = @MediaId
  AND is_deleted = 0;";

                    await using (var cmd = _db.CreateCommand(conn, softDeleteMediaLocSql))
                    {
                        cmd.Transaction = tx;
                        cmd.Parameters.Add(_db.CreateParameter("@MediaId", corrMediaId.Value));
                        await cmd.ExecuteNonQueryAsync();
                    }

                    const string softDeleteMediaSql = @"
UPDATE media
SET is_deleted = 1
WHERE id = @MediaId
  AND space_id = @SpaceId
  AND is_deleted = 0;";

                    await using (var cmd = _db.CreateCommand(conn, softDeleteMediaSql))
                    {
                        cmd.Transaction = tx;
                        cmd.Parameters.Add(_db.CreateParameter("@MediaId", corrMediaId.Value));
                        cmd.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));
                        await cmd.ExecuteNonQueryAsync();
                    }
                }

                // 5️⃣ Soft-delete thumbnail media + localizations
                if (thumbMediaId.HasValue)
                {
                    const string softDeleteMediaLocSql = @"
UPDATE media_localization
SET is_deleted = 1
WHERE media_id = @MediaId
  AND is_deleted = 0;";

                    await using (var cmd = _db.CreateCommand(conn, softDeleteMediaLocSql))
                    {
                        cmd.Transaction = tx;
                        cmd.Parameters.Add(_db.CreateParameter("@MediaId", thumbMediaId.Value));
                        await cmd.ExecuteNonQueryAsync();
                    }

                    const string softDeleteMediaSql = @"
UPDATE media
SET is_deleted = 1
WHERE id = @MediaId
  AND space_id = @SpaceId
  AND is_deleted = 0;";

                    await using (var cmd = _db.CreateCommand(conn, softDeleteMediaSql))
                    {
                        cmd.Transaction = tx;
                        cmd.Parameters.Add(_db.CreateParameter("@MediaId", thumbMediaId.Value));
                        cmd.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));
                        await cmd.ExecuteNonQueryAsync();
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
        private async Task<int> InsertMediaAsync(
            DbConnection conn,
            DbTransaction tx,
            int spaceId,
            MediaCreateDto dto
        )
        {
            // 1️⃣ Insert media row and fetch IDENTITY
            const string insMedia = @"
INSERT INTO media (space_id, media_type_id, text_key, description_key)
VALUES (@SpaceId, @MediaTypeId, @TextKey, @DescKey);

SELECT CAST(SCOPE_IDENTITY() AS INT);";

            int mediaId;

            await using (var cmd = _db.CreateCommand(conn, insMedia))
            {
                cmd.Transaction = tx;
                cmd.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));
                cmd.Parameters.Add(_db.CreateParameter("@MediaTypeId", dto.MediaTypeId));
                cmd.Parameters.Add(_db.CreateParameter("@TextKey", (object?)dto.TextKey ?? DBNull.Value));
                cmd.Parameters.Add(_db.CreateParameter("@DescKey", (object?)dto.DescriptionKey ?? DBNull.Value));

                mediaId = (int)(await cmd.ExecuteScalarAsync())!;
            }

            // 2️⃣ Insert i18n text localizations
            if (dto.TextKey != null && dto.TextLocalizations != null)
            {
                const string insI18n = @"
INSERT INTO i18n ([key], locale_id, [value], space_id)
VALUES (@Key, @LocaleId, @Value, @SpaceId);";

                foreach (var loc in dto.TextLocalizations)
                {
                    await using var cmd = _db.CreateCommand(conn, insI18n);
                    cmd.Transaction = tx;
                    cmd.Parameters.Add(_db.CreateParameter("@Key", dto.TextKey));
                    cmd.Parameters.Add(_db.CreateParameter("@LocaleId", loc.LocaleId));
                    cmd.Parameters.Add(_db.CreateParameter("@Value", loc.Value ?? string.Empty));
                    cmd.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));
                    await cmd.ExecuteNonQueryAsync();
                }
            }

            // 3️⃣ Insert i18n description localizations
            if (dto.DescriptionKey != null && dto.DescriptionLocalizations != null)
            {
                const string insI18n = @"
INSERT INTO i18n ([key], locale_id, [value], space_id)
VALUES (@Key, @LocaleId, @Value, @SpaceId);";

                foreach (var loc in dto.DescriptionLocalizations)
                {
                    await using var cmd = _db.CreateCommand(conn, insI18n);
                    cmd.Transaction = tx;
                    cmd.Parameters.Add(_db.CreateParameter("@Key", dto.DescriptionKey));
                    cmd.Parameters.Add(_db.CreateParameter("@LocaleId", loc.LocaleId));
                    cmd.Parameters.Add(_db.CreateParameter("@Value", loc.Value ?? string.Empty));
                    cmd.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));
                    await cmd.ExecuteNonQueryAsync();
                }
            }

            // 4️⃣ Insert media localizations
            if (dto.LinkLocalizations != null)
            {
                const string insLoc = @"
INSERT INTO media_localization (media_id, locale_id, media_link)
VALUES (@MediaId, @LocaleId, @MediaLink);";

                foreach (var loc in dto.LinkLocalizations)
                {
                    await using var cmd = _db.CreateCommand(conn, insLoc);
                    cmd.Transaction = tx;
                    cmd.Parameters.Add(_db.CreateParameter("@MediaId", mediaId));
                    cmd.Parameters.Add(_db.CreateParameter("@LocaleId", loc.LocaleId));
                    cmd.Parameters.Add(_db.CreateParameter("@MediaLink", loc.MediaLink));
                    await cmd.ExecuteNonQueryAsync();
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
        private static async Task<int?> FilterAndInsertMediaAsync(
            DbConnection conn,
            DbTransaction tx,
            int spaceId,
            MediaCreateDto? mediaDto,
            HashSet<string> supportedLocales,
            Func<DbConnection, DbTransaction, int, MediaCreateDto, Task<int>> insertFunc
        )
        {
            if (mediaDto == null)
                return null;

            // Filter LinkLocalizations
            if (mediaDto.LinkLocalizations != null)
            {
                mediaDto.LinkLocalizations = mediaDto.LinkLocalizations
                    .Where(x => supportedLocales.Contains(x.LocaleId))
                    .ToList();
            }

            // Filter TextLocalizations
            if (mediaDto.TextLocalizations != null)
            {
                mediaDto.TextLocalizations = mediaDto.TextLocalizations
                    .Where(x => supportedLocales.Contains(x.LocaleId))
                    .ToList();
            }

            // Filter DescriptionLocalizations
            if (mediaDto.DescriptionLocalizations != null)
            {
                mediaDto.DescriptionLocalizations = [.. mediaDto.DescriptionLocalizations.Where(x => supportedLocales.Contains(x.LocaleId))];
            }

            // Only insert if there is at least one media localization
            if (mediaDto.LinkLocalizations != null && mediaDto.LinkLocalizations.Count > 0)
                return await insertFunc(conn, tx, spaceId, mediaDto);

            return null;
        }

        private static MediaData? BuildMediaData(DbDataReader reader, string idColumn, string mediaTypeColumn)
        {
            int idOrdinal = reader.GetOrdinal(idColumn);

            if (reader.IsDBNull(idOrdinal))
                return null;

            return new MediaData
            {
                Id = reader.GetInt64(idOrdinal),
                MediaTypeId = reader.IsDBNull(reader.GetOrdinal(mediaTypeColumn))
                    ? 0
                    : reader.GetInt32(reader.GetOrdinal(mediaTypeColumn)),
                LinkLocalizations = new List<MediaLocalization>()
            };
        }

        #endregion
    }
}
