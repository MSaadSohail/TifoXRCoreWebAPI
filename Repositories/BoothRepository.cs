// <copyright file="BoothRepository.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Syed Hussain</author>
// <date>08/18/2025</date>
// <summary>Class to handle booth SQL side</summary>

using System.Data;
using System.Data.Common;
//
using GMS.TifoXRCoreWebAPI.Models;
using GMS.TifoXRCoreWebAPI.Models.Common;
using GMS.TifoXRCoreWebAPI.Utilities.Infrastructure;

namespace GMS.TifoXRCoreWebAPI.Repositories
{
    public class BoothRepository(IConfiguration configuration, IDbProvider db) : IBoothRepository
    {
        private readonly IDbProvider _db = db;

        public async Task<List<BoothModel>> GetAllBoothsBySpaceAsync(int spaceId)
        {
            const string sql = @"
            SELECT
                b.id,
                b.space_id,
                b.name_key,
                b.map_spot_id,
                ms.x,
                ms.y,
                ms.z,
                i.locale_id,
                i.value
            FROM booth b
            INNER JOIN i18n i
                ON i.`key` = b.name_key
               AND i.space_id = b.space_id
            LEFT JOIN map_spot ms
                ON b.map_spot_id = ms.id
            WHERE b.space_id = @SpaceId
            ORDER BY b.id, i.locale_id;";

            await using var conn = await _db.OpenConnectionAsync();
            await using var cmd = _db.CreateCommand(conn, sql);
            cmd.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));

            var booths = new Dictionary<int, BoothModel>();

            await using (var reader = await cmd.ExecuteReaderAsync())
            {
                bool ordReady = false;
                int o_id = -1, o_space_id = -1, o_name_key = -1, o_map_spot_id = -1;
                int o_x = -1, o_y = -1, o_z = -1, o_locale_id = -1, o_value = -1;

                while (await reader.ReadAsync())
                {
                    if (!ordReady)
                    {
                        o_id = reader.GetOrdinal("id");
                        o_space_id = reader.GetOrdinal("space_id");
                        o_name_key = reader.GetOrdinal("name_key");
                        o_map_spot_id = reader.GetOrdinal("map_spot_id");
                        o_x = reader.GetOrdinal("x");
                        o_y = reader.GetOrdinal("y");
                        o_z = reader.GetOrdinal("z");
                        o_locale_id = reader.GetOrdinal("locale_id");
                        o_value = reader.GetOrdinal("value");
                        ordReady = true;
                    }

                    var id = reader.GetInt32(o_id);

                    if (!booths.TryGetValue(id, out var booth))
                    {
                        var hasCoords = !reader.IsDBNull(o_x); // x/y/z are null when no map_spot row
                        booth = new BoothModel
                        {
                            Id = id,
                            SpaceId = reader.GetInt32(o_space_id),
                            MapSpotId = reader.IsDBNull(o_map_spot_id) ? default : reader.GetInt32(o_map_spot_id),
                            LocalizedPairs = new LocalizedPairs
                            {
                                Key = reader.GetString(o_name_key),
                                Values = new List<LocalizedValue>()
                            },
                            MediaItems = new List<MediaData>()
                        };

                        booths[id] = booth;
                    }

                    var locId = reader.GetString(o_locale_id);
                    var val = reader.GetString(o_value);

                    // De-dupe per locale just in case
                    if (!booth.LocalizedPairs.Values.Any(v => v.LocaleId == locId))
                    {
                        booth.LocalizedPairs.Values.Add(new LocalizedValue
                        {
                            LocaleId = locId,
                            Value = val
                        });
                    }
                }
            }

            if (booths.Count > 0)
            {
                await LoadBoothMediaAsync(conn, booths, spaceId);
            }

            return booths.Values.ToList();
        }

        private async Task LoadBoothMediaAsync(DbConnection conn, IDictionary<int, BoothModel> booths, int spaceId)
        {
            if (booths is null || booths.Count == 0)
            {
                return;
            }

            var boothIds = booths.Keys.ToList();
            var paramNames = boothIds.Select((_, i) => $"@B{i}").ToList();

            var sql = $@"
            SELECT
                bm.booth_id,
                bm.media_id,
                m.media_type_id,
                m.text_key,
                m.description_key,
                ml.locale_id,
                ml.media_link
            FROM booth_media bm
            INNER JOIN media m
                ON bm.media_id = m.id
            LEFT JOIN media_localization ml
                ON ml.media_id = m.id
            WHERE bm.booth_id IN ({string.Join(", ", paramNames)})
              AND m.space_id = @SpaceId
            ORDER BY bm.booth_id, bm.media_id, ml.locale_id;";

            await using var cmd = _db.CreateCommand(conn, sql);
            for (int i = 0; i < boothIds.Count; i++)
            {
                cmd.Parameters.Add(_db.CreateParameter(paramNames[i], boothIds[i]));
            }

            cmd.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));

            await using var reader = await cmd.ExecuteReaderAsync();

            var mediaCache = new Dictionary<(int BoothId, string MediaId), MediaData>();

            bool ordReady = false;
            int oBooth = -1, oMedia = -1, oType = -1, oText = -1, oDesc = -1, oLocale = -1, oLink = -1;

            while (await reader.ReadAsync())
            {
                if (!ordReady)
                {
                    oBooth = reader.GetOrdinal("booth_id");
                    oMedia = reader.GetOrdinal("media_id");
                    oType = reader.GetOrdinal("media_type_id");
                    oText = reader.GetOrdinal("text_key");
                    oDesc = reader.GetOrdinal("description_key");
                    oLocale = reader.GetOrdinal("locale_id");
                    oLink = reader.GetOrdinal("media_link");
                    ordReady = true;
                }

                var boothId = reader.GetInt32(oBooth);
                if (!booths.TryGetValue(boothId, out var booth))
                {
                    continue;
                }

                var mediaId = reader.GetString(oMedia);
                var key = (boothId, mediaId);

                if (!mediaCache.TryGetValue(key, out var media))
                {
                    media = new MediaData
                    {
                        Id = mediaId,
                        MediaTypeId = reader.GetInt32(oType),
                        TextKey = reader.IsDBNull(oText) ? null : reader.GetString(oText),
                        DescriptionKey = reader.IsDBNull(oDesc) ? null : reader.GetString(oDesc),
                        LinkLocalizations = new List<MediaLocalization>()
                    };

                    mediaCache[key] = media;
                    booth.MediaItems.Add(media);
                }

                if (!reader.IsDBNull(oLocale) && !reader.IsDBNull(oLink))
                {
                    var locale = reader.GetString(oLocale);

                    if (!media.LinkLocalizations.Any(l => string.Equals(l.LocaleId, locale, StringComparison.OrdinalIgnoreCase)))
                    {
                        media.LinkLocalizations.Add(new MediaLocalization
                        {
                            LocaleId = locale,
                            MediaLink = reader.GetString(oLink)
                        });
                    }
                }
            }
        }

        private async Task SyncBoothMediaAsync(
            DbConnection conn,
            DbTransaction tx,
            int spaceId,
            int boothId,
             IReadOnlyList<BoothMediaUpdateDto> mediaItems)
        {
            var normalizedItems = mediaItems ?? Array.Empty<BoothMediaUpdateDto>();

            if (normalizedItems.Count == 0)
                return;

            var existing = new List<(string MediaId, string? TextKey, string? DescriptionKey)>();

            const string fetchSql = @"
            SELECT bm.media_id, m.text_key, m.description_key
              FROM booth_media bm
              INNER JOIN media m ON bm.media_id = m.id
             WHERE bm.booth_id = @BoothId
             ORDER BY bm.media_id;";

            await using (var fetchCmd = _db.CreateCommand(conn, fetchSql))
            {
                fetchCmd.Transaction = tx;
                fetchCmd.Parameters.Add(_db.CreateParameter("@BoothId", boothId));

                await using var reader = await fetchCmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var mediaId = reader.GetString(0);
                    var textKey = reader.IsDBNull(1) ? null : reader.GetString(1);
                    var descKey = reader.IsDBNull(2) ? null : reader.GetString(2);
                    existing.Add((
                        reader.GetString(0),
                        reader.IsDBNull(1) ? null : reader.GetString(1),
                        reader.IsDBNull(2) ? null : reader.GetString(2)));
                }
            }

            if (existing.Count == 0)
                throw new InvalidOperationException("No media exists for this booth to update.");

            if (normalizedItems.Count > existing.Count)
                throw new InvalidOperationException("Cannot add new media via booth update. Use the media creation endpoint instead.");

            var usedIndexes = new HashSet<int>();

            foreach (var dto in normalizedItems)
            {
                if (dto.LinkLocalizations == null || dto.LinkLocalizations.Count == 0)
                {
                    continue;
                }

                var index = FindMatchingMediaIndex(dto, existing, usedIndexes);
                if (index < 0)
                    throw new InvalidOperationException("Unable to match media update request to existing booth media.");

                usedIndexes.Add(index);
                var mediaMeta = existing[index];

                const string updateSql = @"
                UPDATE media
                   SET media_type_id = @MediaTypeId,
                       text_key = @TextKey,
                       description_key = @DescKey
                 WHERE id = @MediaId
                   AND space_id = @SpaceId;";

                await using (var cmd = _db.CreateCommand(conn, updateSql))
                {
                    cmd.Transaction = tx;
                    cmd.Parameters.Add(_db.CreateParameter("@MediaTypeId", dto.MediaTypeId));
                    cmd.Parameters.Add(_db.CreateParameter("@TextKey", (object?)dto.TextKey ?? DBNull.Value));
                    cmd.Parameters.Add(_db.CreateParameter("@DescKey", (object?)dto.DescriptionKey ?? DBNull.Value));
                    cmd.Parameters.Add(_db.CreateParameter("@MediaId", mediaMeta.MediaId));
                    cmd.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));
                    await cmd.ExecuteNonQueryAsync();
                }

                await ReplaceMediaLocalizationsAsync(conn, tx, mediaMeta.MediaId, dto.LinkLocalizations);
            }
        }

        static int FindMatchingMediaIndex(
                BoothMediaUpdateDto dto,
                List<(string MediaId, string? TextKey, string? DescriptionKey)> existing,
                HashSet<int> usedIndexes)
        {
            bool MatchEntry(int idx, Func<(string MediaId, string? TextKey, string? DescriptionKey), bool> predicate)
                => !usedIndexes.Contains(idx) && predicate(existing[idx]);

            string? textKey = dto.TextKey;
            string? descKey = dto.DescriptionKey;

            // 1) Match on both text & description keys when provided.
            if (!string.IsNullOrWhiteSpace(textKey) || !string.IsNullOrWhiteSpace(descKey))
            {
                for (int i = 0; i < existing.Count; i++)
                {
                    if (MatchEntry(i, e =>
                        (string.Equals(e.TextKey, textKey, StringComparison.OrdinalIgnoreCase) || string.IsNullOrWhiteSpace(textKey)) &&
                        (string.Equals(e.DescriptionKey, descKey, StringComparison.OrdinalIgnoreCase) || string.IsNullOrWhiteSpace(descKey))))
                    {
                        return i;
                    }
                }
            }

            // 2) Match on text key alone.
            if (!string.IsNullOrWhiteSpace(textKey))
            {
                for (int i = 0; i < existing.Count; i++)
                {
                    if (MatchEntry(i, e => string.Equals(e.TextKey, textKey, StringComparison.OrdinalIgnoreCase)))
                    {
                        return i;
                    }
                }
            }

            // 3) Match on description key alone.
            if (!string.IsNullOrWhiteSpace(descKey))
            {
                for (int i = 0; i < existing.Count; i++)
                {
                    if (MatchEntry(i, e => string.Equals(e.DescriptionKey, descKey, StringComparison.OrdinalIgnoreCase)))
                    {
                        return i;
                    }
                }
            }

            // 4) Fallback to the first unused entry (preserve order).
            for (var i = 0; i < existing.Count; i++)
            {
                if (!usedIndexes.Contains(i))
                    return i;
            }

            return -1;
        }

        private async Task<string> InsertMediaAsync(
            DbConnection conn,
            DbTransaction tx,
            int spaceId,
            MediaCreateDto dto)
        {
            string mediaId;
            await using (var uuidCmd = _db.CreateCommand(conn, "SELECT UUID();"))
            {
                uuidCmd.Transaction = tx;
                var result = await uuidCmd.ExecuteScalarAsync();
                mediaId = Convert.ToString(result);
                if (string.IsNullOrWhiteSpace(mediaId))
                {
                    mediaId = Guid.NewGuid().ToString("N");
                }
            }

            const string insertMediaSql = @"
            INSERT INTO media (id, space_id, media_type_id, text_key, description_key)
            VALUES (@Id, @SpaceId, @MediaTypeId, @TextKey, @DescKey);";

            await using (var cmd = _db.CreateCommand(conn, insertMediaSql))
            {
                cmd.Transaction = tx;
                cmd.Parameters.Add(_db.CreateParameter("@Id", mediaId));
                cmd.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));
                cmd.Parameters.Add(_db.CreateParameter("@MediaTypeId", dto.MediaTypeId));
                cmd.Parameters.Add(_db.CreateParameter("@TextKey", (object?)dto.TextKey ?? DBNull.Value));
                cmd.Parameters.Add(_db.CreateParameter("@DescKey", (object?)dto.DescriptionKey ?? DBNull.Value));

                await cmd.ExecuteNonQueryAsync();
            }

            if (dto.TextKey != null && dto.TextLocalizations != null)
            {
                const string insTextSql = @"
INSERT INTO i18n (`key`, locale_id, value, space_id)
VALUES (@Key, @LocaleId, @Value, @SpaceId);";

                foreach (var loc in dto.TextLocalizations)
                {
                    await using var cmd = _db.CreateCommand(conn, insTextSql);
                    cmd.Transaction = tx;
                    cmd.Parameters.Add(_db.CreateParameter("@Key", dto.TextKey));
                    cmd.Parameters.Add(_db.CreateParameter("@LocaleId", loc.LocaleId));
                    cmd.Parameters.Add(_db.CreateParameter("@Value", loc.Value ?? string.Empty));
                    cmd.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));
                    await cmd.ExecuteNonQueryAsync();
                }
            }

            if (dto.DescriptionKey != null && dto.DescriptionLocalizations != null)
            {
                const string insDescSql = @"
INSERT INTO i18n (`key`, locale_id, value, space_id)
VALUES (@Key, @LocaleId, @Value, @SpaceId);";

                foreach (var loc in dto.DescriptionLocalizations)
                {
                    await using var cmd = _db.CreateCommand(conn, insDescSql);
                    cmd.Transaction = tx;
                    cmd.Parameters.Add(_db.CreateParameter("@Key", dto.DescriptionKey));
                    cmd.Parameters.Add(_db.CreateParameter("@LocaleId", loc.LocaleId));
                    cmd.Parameters.Add(_db.CreateParameter("@Value", loc.Value ?? string.Empty));
                    cmd.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));
                    await cmd.ExecuteNonQueryAsync();
                }
            }

            if (dto.LinkLocalizations != null)
            {
                const string insLocSql = @"
INSERT INTO media_localization (media_id, locale_id, media_link)
VALUES (@MediaId, @LocaleId, @MediaLink);";

                foreach (var loc in dto.LinkLocalizations)
                {
                    await using var cmdLoc = _db.CreateCommand(conn, insLocSql);
                    cmdLoc.Transaction = tx;
                    cmdLoc.Parameters.Add(_db.CreateParameter("@MediaId", mediaId));
                    cmdLoc.Parameters.Add(_db.CreateParameter("@LocaleId", loc.LocaleId));
                    cmdLoc.Parameters.Add(_db.CreateParameter("@MediaLink", loc.MediaLink));
                    await cmdLoc.ExecuteNonQueryAsync();
                }
            }

            return mediaId;
        }

        private async Task InsertBoothMediaAsync(DbConnection conn, DbTransaction tx, int boothId, string mediaId)
        {
            const string deleteSql = @"
            DELETE FROM booth_media
             WHERE booth_id = @BoothId AND media_id = @MediaId;";

            await using (var delCmd = _db.CreateCommand(conn, deleteSql))
            {
                delCmd.Transaction = tx;
                delCmd.Parameters.Add(_db.CreateParameter("@BoothId", boothId));
                delCmd.Parameters.Add(_db.CreateParameter("@MediaId", mediaId));
                await delCmd.ExecuteNonQueryAsync();
            }

            const string insertSql = @"
            INSERT INTO booth_media (booth_id, media_id)
            VALUES (@BoothId, @MediaId);";

            await using var insCmd = _db.CreateCommand(conn, insertSql);
            insCmd.Transaction = tx;
            insCmd.Parameters.Add(_db.CreateParameter("@BoothId", boothId));
            insCmd.Parameters.Add(_db.CreateParameter("@MediaId", mediaId));
            await insCmd.ExecuteNonQueryAsync();
        }

        private async Task ReplaceMediaLocalizationsAsync(
            DbConnection conn,
            DbTransaction tx,
            string mediaId,
            IEnumerable<MediaLocalization> localizations)
        {
            const string deleteSql = @"
            DELETE FROM media_localization
             WHERE media_id = @MediaId;";

            await using (var delCmd = _db.CreateCommand(conn, deleteSql))
            {
                delCmd.Transaction = tx;
                delCmd.Parameters.Add(_db.CreateParameter("@MediaId", mediaId));
                await delCmd.ExecuteNonQueryAsync();
            }

            const string insertSql = @"
            INSERT INTO media_localization (media_id, locale_id, media_link)
            VALUES (@MediaId, @LocaleId, @MediaLink);";

            foreach (var loc in localizations)
            {
                await using var insCmd = _db.CreateCommand(conn, insertSql);
                insCmd.Transaction = tx;
                insCmd.Parameters.Add(_db.CreateParameter("@MediaId", mediaId));
                insCmd.Parameters.Add(_db.CreateParameter("@LocaleId", loc.LocaleId));
                insCmd.Parameters.Add(_db.CreateParameter("@MediaLink", loc.MediaLink));
                await insCmd.ExecuteNonQueryAsync();
            }
        }

        private async Task DeleteMediaCascadeAsync(
            DbConnection conn,
            DbTransaction tx,
            int boothId,
            string mediaId,
            string? textKey,
            string? descriptionKey,
            int spaceId)
        {
            const string deleteLocSql = @"
            DELETE FROM media_localization
             WHERE media_id = @MediaId;";

            await using (var delLoc = _db.CreateCommand(conn, deleteLocSql))
            {
                delLoc.Transaction = tx;
                delLoc.Parameters.Add(_db.CreateParameter("@MediaId", mediaId));
                await delLoc.ExecuteNonQueryAsync();
            }

            const string deleteMapSql = @"
            DELETE FROM booth_media
             WHERE booth_id = @BoothId AND media_id = @MediaId;";

            await using (var delMap = _db.CreateCommand(conn, deleteMapSql))
            {
                delMap.Transaction = tx;
                delMap.Parameters.Add(_db.CreateParameter("@BoothId", boothId));
                delMap.Parameters.Add(_db.CreateParameter("@MediaId", mediaId));
                await delMap.ExecuteNonQueryAsync();
            }

            const string deleteMediaSql = @"
            DELETE FROM media
             WHERE id = @MediaId;";

            await using (var delMedia = _db.CreateCommand(conn, deleteMediaSql))
            {
                delMedia.Transaction = tx;
                delMedia.Parameters.Add(_db.CreateParameter("@MediaId", mediaId));
                await delMedia.ExecuteNonQueryAsync();
            }

            if (!string.IsNullOrWhiteSpace(textKey))
            {
                const string deleteTextSql = @"
            DELETE FROM i18n
             WHERE `key` = @Key AND space_id = @SpaceId;";

                await using var delText = _db.CreateCommand(conn, deleteTextSql);
                delText.Transaction = tx;
                delText.Parameters.Add(_db.CreateParameter("@Key", textKey));
                delText.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));
                await delText.ExecuteNonQueryAsync();
            }

            if (!string.IsNullOrWhiteSpace(descriptionKey))
            {
                const string deleteDescSql = @"
            DELETE FROM i18n
             WHERE `key` = @Key AND space_id = @SpaceId;";

                await using var delDesc = _db.CreateCommand(conn, deleteDescSql);
                delDesc.Transaction = tx;
                delDesc.Parameters.Add(_db.CreateParameter("@Key", descriptionKey));
                delDesc.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));
                await delDesc.ExecuteNonQueryAsync();
            }
        }

        private static List<MediaCreateDto> NormalizeMediaCreates(List<MediaCreateDto>? mediaItems, HashSet<string> supportedLocales)
        {
            var result = new List<MediaCreateDto>();
            if (mediaItems == null)
                return result;

            foreach (var item in mediaItems)
            {
                if (item == null)
                    continue;

                var links = NormalizeLocalizations(item.LinkLocalizations, supportedLocales);
                if (links.Count == 0)
                    continue;

                result.Add(new MediaCreateDto
                {
                    MediaTypeId = item.MediaTypeId,
                    TextKey = item.TextKey,
                    DescriptionKey = item.DescriptionKey,
                    LinkLocalizations = links,
                    TextLocalizations = NormalizeLocalizedValues(item.TextLocalizations, supportedLocales),
                    DescriptionLocalizations = NormalizeLocalizedValues(item.DescriptionLocalizations, supportedLocales)
                });
            }

            return result;
        }

        private static List<BoothMediaUpdateDto> NormalizeBoothMediaUpdates(List<BoothMediaUpdateDto>? mediaItems, HashSet<string> supportedLocales)
        {
            var result = new List<BoothMediaUpdateDto>();

            if (mediaItems == null)
                return result;

            foreach (var item in mediaItems)
            {
                if (item == null)
                    continue;

                var links = NormalizeLocalizations(item.LinkLocalizations, supportedLocales);
                if (links.Count == 0)
                    continue;

                result.Add(new BoothMediaUpdateDto
                {
                    MediaTypeId = item.MediaTypeId,
                    TextKey = item.TextKey,
                    DescriptionKey = item.DescriptionKey,
                    LinkLocalizations = links
                });
            }

            return result;
        }

        private static List<MediaLocalization> NormalizeLocalizations(IEnumerable<MediaLocalization>? source, HashSet<string> supportedLocales)
        {
            var result = new List<MediaLocalization>();
            if (source == null)
                return result;

            foreach (var loc in source)
            {
                if (loc == null)
                    continue;

                if (string.IsNullOrWhiteSpace(loc.LocaleId))
                    continue;

                if (!supportedLocales.Contains(loc.LocaleId))
                    continue;

                if (result.Any(l => string.Equals(l.LocaleId, loc.LocaleId, StringComparison.OrdinalIgnoreCase)))
                    continue;

                result.Add(new MediaLocalization
                {
                    LocaleId = loc.LocaleId,
                    MediaLink = loc.MediaLink
                });
            }

            return result;
        }

        private static List<LocalizedValue>? NormalizeLocalizedValues(IEnumerable<LocalizedValue>? source, HashSet<string> supportedLocales)
        {
            if (source == null)
                return null;

            var result = new List<LocalizedValue>();

            foreach (var loc in source)
            {
                if (loc == null)
                    continue;

                if (string.IsNullOrWhiteSpace(loc.LocaleId))
                    continue;

                if (!supportedLocales.Contains(loc.LocaleId))
                    continue;

                if (result.Any(l => string.Equals(l.LocaleId, loc.LocaleId, StringComparison.OrdinalIgnoreCase)))
                    continue;

                result.Add(new LocalizedValue
                {
                    LocaleId = loc.LocaleId,
                    Value = loc.Value
                });
            }

            return result;
        }

        public async Task<BoothModel?> UpdateBoothAsync(int spaceId, int boothId, BoothUpdateDto dto)
        {
            await using var conn = await _db.OpenConnectionAsync();
            await using var tx = await conn.BeginTransactionAsync();

            try
            {
                // 0) Fetch immutable booth data (name_key) and ensure booth exists
                const string fetchBoothKeySql = @"SELECT name_key FROM booth WHERE id = @BoothId AND space_id = @SpaceId;";
                string? boothNameKey;

                await using (var fetchCmd = _db.CreateCommand(conn, fetchBoothKeySql))
                {
                    fetchCmd.Transaction = tx;
                    fetchCmd.Parameters.Add(_db.CreateParameter("@BoothId", boothId));
                    fetchCmd.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));

                    var result = await fetchCmd.ExecuteScalarAsync();
                    if (result is null || result == DBNull.Value)
                    {
                        await tx.RollbackAsync();
                        return null;
                    }

                    boothNameKey = Convert.ToString(result);
                }

                if (string.IsNullOrWhiteSpace(boothNameKey))
                {
                    await tx.RollbackAsync();
                    return null;
                }

                // 1) Validate map_spot exists
                const string validateSpotSql = @"SELECT COUNT(1) FROM map_spot WHERE id = @MapSpotId;";
                await using (var cmd = _db.CreateCommand(conn, validateSpotSql))
                {
                    cmd.Transaction = tx;
                    cmd.Parameters.Add(_db.CreateParameter("@MapSpotId", dto.MapSpotId));
                    var exists = Convert.ToInt32(await cmd.ExecuteScalarAsync()) > 0;
                    if (!exists)
                        throw new InvalidOperationException($"MapSpot with ID {dto.MapSpotId} does not exist.");
                }

                // 2) Update booth row
                const string updBooth = @"
                UPDATE booth
                   SET name_key = @NameKey,
                       map_spot_id = @MapSpotId
                 WHERE id = @BoothId
                   AND space_id = @SpaceId;";
                int affected;
                await using (var cmd = _db.CreateCommand(conn, updBooth))
                {
                    cmd.Transaction = tx;
                    cmd.Parameters.Add(_db.CreateParameter("@NameKey", boothNameKey));
                    cmd.Parameters.Add(_db.CreateParameter("@BoothId", boothId));
                    cmd.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));
                    cmd.Parameters.Add(_db.CreateParameter("@MapSpotId", dto.MapSpotId));
                    affected = await cmd.ExecuteNonQueryAsync();
                }
                if (affected == 0)
                {
                    await tx.RollbackAsync();
                    return null; // not found (or unchanged under MySQL's "0 rows affected" semantics)
                }

                // 3) Validate locales (skip if none provided)
                var allLocales = dto.LocalizedPairs?.Values?
                    .Select(v => v.LocaleId)
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .ToList() ?? new List<string>();

                if (dto.MediaItems != null)
                {
                    foreach (var locale in dto.MediaItems
                        .Where(m => m?.LinkLocalizations != null)
                        .SelectMany(m => m.LinkLocalizations!)
                        .Select(l => l.LocaleId)
                        .Where(s => !string.IsNullOrWhiteSpace(s)))
                    {
                        allLocales.Add(locale);
                    }
                }

                allLocales = allLocales
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                var supported = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                if (allLocales.Count > 0)
                {
                    const string checkSupportedLangSqlBase = @"
                    SELECT locale_id
                      FROM supported_languages
                     WHERE locale_id IN ({0}) AND space_id = @SpaceId;";

                    var paramNames = allLocales.Select((_, i) => $"@loc{i}").ToList();
                    var dynamicQuery = string.Format(checkSupportedLangSqlBase, string.Join(", ", paramNames));

                    await using (var checkCmd = _db.CreateCommand(conn, dynamicQuery))
                    {
                        checkCmd.Transaction = tx;
                        for (int i = 0; i < allLocales.Count; i++)
                            checkCmd.Parameters.Add(_db.CreateParameter(paramNames[i], allLocales[i]));
                        checkCmd.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));

                        await using var rdr = await checkCmd.ExecuteReaderAsync();
                        while (await rdr.ReadAsync())
                            supported.Add(rdr.IsDBNull(0) ? "" : rdr.GetString(0));
                    }

                    var unsupported = allLocales.Where(l => !supported.Contains(l)).ToList();
                    if (unsupported.Count > 0)
                        throw new InvalidOperationException($"Unsupported locales: {string.Join(", ", unsupported)}");
                }

                var normalizedMedia = NormalizeBoothMediaUpdates(dto.MediaItems, supported);

                // 4) Upsert i18n for booth name_key
                const string updI18n = @"
                UPDATE i18n
                   SET value = @Value
                 WHERE `key` = @NameKey AND locale_id = @LocaleId AND space_id = @SpaceId;";
                const string insI18n = @"
                INSERT INTO i18n (`key`, locale_id, value, space_id)
                VALUES (@NameKey, @LocaleId, @Value, @SpaceId);";

                if (dto.LocalizedPairs?.Values != null)
                {
                    foreach (var loc in dto.LocalizedPairs.Values)
                    {
                        await using var cmdUp = _db.CreateCommand(conn, updI18n);
                        cmdUp.Transaction = tx;
                        cmdUp.Parameters.Add(_db.CreateParameter("@NameKey", boothNameKey));
                        cmdUp.Parameters.Add(_db.CreateParameter("@LocaleId", loc.LocaleId));
                        cmdUp.Parameters.Add(_db.CreateParameter("@Value", loc.Value ?? string.Empty));
                        cmdUp.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));

                        if (await cmdUp.ExecuteNonQueryAsync() == 0)
                        {
                            await using var cmdIn = _db.CreateCommand(conn, insI18n);
                            cmdIn.Transaction = tx;
                            cmdIn.Parameters.Add(_db.CreateParameter("@NameKey", boothNameKey));
                            cmdIn.Parameters.Add(_db.CreateParameter("@LocaleId", loc.LocaleId));
                            cmdIn.Parameters.Add(_db.CreateParameter("@Value", loc.Value ?? string.Empty));
                            cmdIn.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));
                            await cmdIn.ExecuteNonQueryAsync();
                        }
                    }
                }

                await SyncBoothMediaAsync(conn, tx, spaceId, boothId, normalizedMedia);

                await tx.CommitAsync();

                // 5) Reload and return the updated booth
                return await LoadBoothByIdAsync(conn, spaceId, boothId);
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        public async Task<MediaData?> AddMediaToBoothAsync(int spaceId, int boothId, MediaCreateDto dto)
        {
            if (dto is null)
                throw new ArgumentNullException(nameof(dto));

            await using var conn = await _db.OpenConnectionAsync();
            await using var tx = await conn.BeginTransactionAsync();

            try
            {
                const string boothExistsSql = @"SELECT COUNT(1) FROM booth WHERE id = @BoothId AND space_id = @SpaceId;";

                await using (var existsCmd = _db.CreateCommand(conn, boothExistsSql))
                {
                    existsCmd.Transaction = tx;
                    existsCmd.Parameters.Add(_db.CreateParameter("@BoothId", boothId));
                    existsCmd.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));

                    var exists = Convert.ToInt32(await existsCmd.ExecuteScalarAsync()) > 0;
                    if (!exists)
                    {
                        await tx.RollbackAsync();
                        return null;
                    }
                }

                var allLocales = new List<string>();

                void AddLocales(IEnumerable<LocalizedValue>? values)
                {
                    if (values == null)
                        return;

                    foreach (var val in values.Where(v => !string.IsNullOrWhiteSpace(v?.LocaleId)))
                        allLocales.Add(val!.LocaleId);
                }

                if (dto.LinkLocalizations != null)
                {
                    foreach (var loc in dto.LinkLocalizations.Where(l => !string.IsNullOrWhiteSpace(l?.LocaleId)))
                        allLocales.Add(loc!.LocaleId);
                }

                AddLocales(dto.TextLocalizations);
                AddLocales(dto.DescriptionLocalizations);

                allLocales = allLocales
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .Select(s => s!)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                var supported = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                if (allLocales.Count > 0)
                {
                    const string checkSupportedSqlBase = @"
                    SELECT locale_id
                      FROM supported_languages
                     WHERE locale_id IN ({0})
                       AND space_id = @SpaceId;";

                    var paramNames = allLocales.Select((_, i) => $"@loc{i}").ToList();
                    var query = string.Format(checkSupportedSqlBase, string.Join(", ", paramNames));

                    await using (var checkCmd = _db.CreateCommand(conn, query))
                    {
                        checkCmd.Transaction = tx;
                        for (int i = 0; i < allLocales.Count; i++)
                            checkCmd.Parameters.Add(_db.CreateParameter(paramNames[i], allLocales[i]));
                        checkCmd.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));

                        await using var rdr = await checkCmd.ExecuteReaderAsync();
                        while (await rdr.ReadAsync())
                            supported.Add(rdr.IsDBNull(0) ? string.Empty : rdr.GetString(0));
                    }
                }

                var normalizedList = NormalizeMediaCreates(new List<MediaCreateDto> { dto }, supported);
                if (normalizedList.Count == 0)
                    throw new InvalidOperationException("Media link localizations are required to add booth media.");

                var normalized = normalizedList[0];

                var mediaId = await InsertMediaAsync(conn, tx, spaceId, normalized);
                await InsertBoothMediaAsync(conn, tx, boothId, mediaId);

                await tx.CommitAsync();

                return new MediaData
                {
                    Id = mediaId,
                    MediaTypeId = normalized.MediaTypeId,
                    TextKey = normalized.TextKey,
                    DescriptionKey = normalized.DescriptionKey,
                    LinkLocalizations = normalized.LinkLocalizations?
                        .Select(l => new MediaLocalization
                        {
                            LocaleId = l.LocaleId,
                            MediaLink = l.MediaLink
                        })
                        .ToList() ?? new List<MediaLocalization>()
                };
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        public async Task<bool> DeleteBoothMediaAsync(int spaceId, int boothId, string mediaId)
        {
            if (string.IsNullOrWhiteSpace(mediaId))
                throw new ArgumentException("Media ID is required.", nameof(mediaId));

            await using var conn = await _db.OpenConnectionAsync();
            await using var tx = await conn.BeginTransactionAsync();

            try
            {
                const string lookupSql = @"
                SELECT m.text_key, m.description_key
                  FROM booth_media bm
                  INNER JOIN booth b ON bm.booth_id = b.id
                  LEFT JOIN media m ON m.id = bm.media_id
                 WHERE b.space_id = @SpaceId
                   AND b.id = @BoothId
                   AND bm.media_id = @MediaId;";

                string? textKey = null;
                string? descriptionKey = null;

                await using (var lookupCmd = _db.CreateCommand(conn, lookupSql))
                {
                    lookupCmd.Transaction = tx;
                    lookupCmd.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));
                    lookupCmd.Parameters.Add(_db.CreateParameter("@BoothId", boothId));
                    lookupCmd.Parameters.Add(_db.CreateParameter("@MediaId", mediaId));

                    await using var reader = await lookupCmd.ExecuteReaderAsync();
                    if (!await reader.ReadAsync())
                    {
                        await tx.RollbackAsync();
                        return false;
                    }

                    if (!reader.IsDBNull(0))
                        textKey = reader.GetString(0);
                    if (!reader.IsDBNull(1))
                        descriptionKey = reader.GetString(1);
                }

                await DeleteMediaCascadeAsync(conn, tx, boothId, mediaId, textKey, descriptionKey, spaceId);

                await tx.CommitAsync();
                return true;
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        private async Task<BoothModel?> LoadBoothByIdAsync(DbConnection conn, int spaceId, int boothId)
        {
            const string sql = @"
            SELECT
                b.id,
                b.space_id,
                b.name_key,
                b.map_spot_id,
                ms.x,
                ms.y,
                ms.z,
                i.locale_id,
                i.value
            FROM booth b
            INNER JOIN i18n i
                ON i.`key` = b.name_key
               AND i.space_id = b.space_id
            LEFT JOIN map_spot ms
                ON b.map_spot_id = ms.id
            WHERE b.space_id = @SpaceId AND b.id = @BoothId
            ORDER BY i.locale_id;";

            await using var cmd = _db.CreateCommand(conn, sql);
            cmd.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));
            cmd.Parameters.Add(_db.CreateParameter("@BoothId", boothId));

            BoothModel? booth = null;
            var values = new List<LocalizedValue>();

            await using (var reader = await cmd.ExecuteReaderAsync())
            {
                bool ordReady = false;
                int o_id = -1, o_space_id = -1, o_name_key = -1, o_map_spot_id = -1;
                int o_x = -1, o_y = -1, o_z = -1, o_locale_id = -1, o_value = -1;

                while (await reader.ReadAsync())
                {
                    if (!ordReady)
                    {
                        o_id = reader.GetOrdinal("id");
                        o_space_id = reader.GetOrdinal("space_id");
                        o_name_key = reader.GetOrdinal("name_key");
                        o_map_spot_id = reader.GetOrdinal("map_spot_id");
                        o_x = reader.GetOrdinal("x");
                        o_y = reader.GetOrdinal("y");
                        o_z = reader.GetOrdinal("z");
                        o_locale_id = reader.GetOrdinal("locale_id");
                        o_value = reader.GetOrdinal("value");
                        ordReady = true;
                    }

                    booth ??= new BoothModel
                    {
                        Id = reader.GetInt32(o_id),
                        SpaceId = reader.GetInt32(o_space_id),
                        MapSpotId = reader.IsDBNull(o_map_spot_id) ? 0 : reader.GetInt32(o_map_spot_id),
                        //MapSpot = reader.IsDBNull(o_x) ? null : new MapSpotModel
                        //{
                        //    X = reader.IsDBNull(o_x) ? 0 : reader.GetDecimal(o_x),
                        //    Y = reader.IsDBNull(o_y) ? 0 : reader.GetDecimal(o_y),
                        //    Z = reader.IsDBNull(o_z) ? 0 : reader.GetDecimal(o_z),
                        //},
                        LocalizedPairs = new LocalizedPairs
                        {
                            Key = reader.GetString(o_name_key),
                            Values = values
                        },
                        MediaItems = new List<MediaData>()
                    };

                    // accumulate locales
                    var locId = reader.GetString(o_locale_id);
                    if (!values.Any(v => v.LocaleId == locId))
                    {
                        values.Add(new LocalizedValue
                        {
                            LocaleId = locId,
                            Value = reader.GetString(o_value)
                        });
                    }
                }
            }

            if (booth is not null)
            {
                var map = new Dictionary<int, BoothModel>
                {
                    [booth.Id] = booth
                };

                await LoadBoothMediaAsync(conn, map, spaceId);
            }

            return booth;
        }

        public async Task<BoothModel> CreateBoothAsync(int spaceId, BoothCreateDto boothDto)
        {
            await using var conn = await _db.OpenConnectionAsync();
            await using var tx = await conn.BeginTransactionAsync();

            try
            {
                // 1) Validate that the map_spot exists and load its coords
                const string validateMapSpotSql = @"SELECT x, y, z FROM map_spot WHERE id = @MapSpotId;";
                MapSpotModel? mapSpot = null;

                await using (var cmd = _db.CreateCommand(conn, validateMapSpotSql))
                {
                    cmd.Transaction = tx;
                    cmd.Parameters.Add(_db.CreateParameter("@MapSpotId", boothDto.MapSpotId));

                    await using var reader = await cmd.ExecuteReaderAsync();
                    if (!await reader.ReadAsync())
                        throw new InvalidOperationException($"MapSpot with ID {boothDto.MapSpotId} does not exist.");

                    var oX = reader.GetOrdinal("x");
                    var oY = reader.GetOrdinal("y");
                    var oZ = reader.GetOrdinal("z");

                    mapSpot = new MapSpotModel
                    {
                        X = reader.IsDBNull(oX) ? 0 : reader.GetDecimal(oX),
                        Y = reader.IsDBNull(oY) ? 0 : reader.GetDecimal(oY),
                        Z = reader.IsDBNull(oZ) ? 0 : reader.GetDecimal(oZ)
                    };
                }

                // 2) Insert booth (references existing map_spot)
                const string insertBoothSql = @"
INSERT INTO booth (space_id, name_key, map_spot_id)
VALUES (@SpaceId, @NameKey, @MapSpotId);";

                await using (var cmd = _db.CreateCommand(conn, insertBoothSql))
                {
                    cmd.Transaction = tx;
                    cmd.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));
                    cmd.Parameters.Add(_db.CreateParameter("@NameKey", boothDto.LocalizedPairs.Key));
                    cmd.Parameters.Add(_db.CreateParameter("@MapSpotId", boothDto.MapSpotId));
                    await cmd.ExecuteNonQueryAsync();
                }

                int newBoothId;
                await using (var idCmd = _db.CreateCommand(conn, "SELECT LAST_INSERT_ID();"))
                {
                    idCmd.Transaction = tx;
                    newBoothId = Convert.ToInt32(await idCmd.ExecuteScalarAsync());
                }

                // 3) Insert i18n for supported locales only (batch validate)
                var insertedValues = new List<LocalizedValue>();
                var values = boothDto.LocalizedPairs?.Values ?? new List<LocalizedValue>();
                var allLocales = values
                    .Select(v => v.LocaleId)
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .ToList();

                if (boothDto.MediaItems != null)
                {
                    foreach (var locale in boothDto.MediaItems
                        .Where(m => m?.LinkLocalizations != null)
                        .SelectMany(m => m.LinkLocalizations!)
                        .Select(l => l.LocaleId)
                        .Where(s => !string.IsNullOrWhiteSpace(s)))
                    {
                        allLocales.Add(locale);
                    }
                }

                allLocales = allLocales
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                var supported = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                if (allLocales.Count > 0)
                {
                    var paramNames = allLocales.Select((_, i) => $"@loc{i}").ToList();
                    var checkSupportedLangSql = $@"
                    SELECT locale_id
                      FROM supported_languages
                     WHERE locale_id IN ({string.Join(", ", paramNames)})
                       AND space_id = @SpaceId;";

                    await using (var checkCmd = _db.CreateCommand(conn, checkSupportedLangSql))
                    {
                        checkCmd.Transaction = tx;
                        for (int i = 0; i < allLocales.Count; i++)
                            checkCmd.Parameters.Add(_db.CreateParameter(paramNames[i], allLocales[i]));
                        checkCmd.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));

                        await using var rdr = await checkCmd.ExecuteReaderAsync();
                        while (await rdr.ReadAsync())
                            supported.Add(rdr.IsDBNull(0) ? "" : rdr.GetString(0));
                    }
                }

                const string insertI18nSql = @"
INSERT INTO i18n (`key`, locale_id, value, space_id)
VALUES (@NameKey, @LocaleId, @Value, @SpaceId);";

                foreach (var val in values)
                {
                    if (!supported.Contains(val.LocaleId)) continue;

                    await using var cmdI18n = _db.CreateCommand(conn, insertI18nSql);
                    cmdI18n.Transaction = tx;
                    cmdI18n.Parameters.Add(_db.CreateParameter("@NameKey", boothDto.LocalizedPairs.Key));
                    cmdI18n.Parameters.Add(_db.CreateParameter("@LocaleId", val.LocaleId));
                    cmdI18n.Parameters.Add(_db.CreateParameter("@Value", val.Value ?? string.Empty));
                    cmdI18n.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));
                    await cmdI18n.ExecuteNonQueryAsync();

                        insertedValues.Add(new LocalizedValue { LocaleId = val.LocaleId, Value = val.Value });
                }

                var normalizedMedia = NormalizeMediaCreates(boothDto.MediaItems, supported);
                var insertedMedia = new List<MediaData>();

                foreach (var mediaDto in normalizedMedia)
                {
                    var mediaId = await InsertMediaAsync(conn, tx, spaceId, mediaDto);
                    await InsertBoothMediaAsync(conn, tx, newBoothId, mediaId);

                    insertedMedia.Add(new MediaData
                    {
                        Id = mediaId,
                        MediaTypeId = mediaDto.MediaTypeId,
                        TextKey = mediaDto.TextKey,
                        DescriptionKey = mediaDto.DescriptionKey,
                        LinkLocalizations = mediaDto.LinkLocalizations?
                            .Select(l => new MediaLocalization
                            {
                                LocaleId = l.LocaleId,
                                MediaLink = l.MediaLink
                            })
                            .ToList() ?? new List<MediaLocalization>()
                    });
                }

                await tx.CommitAsync();

                return new BoothModel
                {
                    Id = newBoothId,
                    SpaceId = spaceId,
                    MapSpotId = boothDto.MapSpotId,
                   //MapSpot = mapSpot!,
                    LocalizedPairs = new LocalizedPairs
                    {
                        Key = boothDto.LocalizedPairs.Key,
                        Values = insertedValues
                    },
                    MediaItems = insertedMedia
                };
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        public async Task<bool> DeleteBoothCascadeAsync(int spaceId, int boothId)
        {
            await using var conn = await _db.OpenConnectionAsync();
            await using var tx = await conn.BeginTransactionAsync();

            try
            {
                // 1) Fetch booth name_key
                const string fetchBooth = @"
            SELECT b.name_key
            FROM booth b
            WHERE b.id = @BoothId AND b.space_id = @SpaceId;";
                string? boothKey;

                await using (var cmd = _db.CreateCommand(conn, fetchBooth))
                {
                    cmd.Transaction = tx;
                    cmd.Parameters.Add(_db.CreateParameter("@BoothId", boothId));
                    cmd.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));
                    var o = await cmd.ExecuteScalarAsync();
                    if (o == null || o == DBNull.Value)
                    {
                        await tx.RollbackAsync();
                        return false;
                    }
                    boothKey = Convert.ToString(o);
                }

                var boothMedia = new List<(string MediaId, string? TextKey, string? DescriptionKey)>();
                const string fetchBoothMediaSql = @"
            SELECT bm.media_id, m.text_key, m.description_key
              FROM booth_media bm
              INNER JOIN media m ON bm.media_id = m.id
             WHERE bm.booth_id = @BoothId;";

                await using (var mediaCmd = _db.CreateCommand(conn, fetchBoothMediaSql))
                {
                    mediaCmd.Transaction = tx;
                    mediaCmd.Parameters.Add(_db.CreateParameter("@BoothId", boothId));

                    await using var mediaReader = await mediaCmd.ExecuteReaderAsync();
                    while (await mediaReader.ReadAsync())
                    {
                        boothMedia.Add((
                            mediaReader.GetString(0),
                            mediaReader.IsDBNull(1) ? null : mediaReader.GetString(1),
                            mediaReader.IsDBNull(2) ? null : mediaReader.GetString(2)
                        ));
                    }
                }

                // 2) Fetch portals for this booth
                const string fetchPortals = @"
            SELECT p.id, p.text_field_key, p.corresponding_media_id, p.thumbnail_media_id
            FROM portal p
            WHERE p.booth_id = @BoothId AND p.space_id = @SpaceId;";

                var portals = new List<(int Id, string Key, string? C, string? T)>();
                await using (var cmd = _db.CreateCommand(conn, fetchPortals))
                {
                    cmd.Transaction = tx;
                    cmd.Parameters.Add(_db.CreateParameter("@BoothId", boothId));
                    cmd.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));

                    await using var r = await cmd.ExecuteReaderAsync();

                    int o_id = -1, o_key = -1, o_c = -1, o_t = -1;
                    bool ordReady = false;

                    while (await r.ReadAsync())
                    {
                        if (!ordReady)
                        {
                            o_id = r.GetOrdinal("id");
                            o_key = r.GetOrdinal("text_field_key");
                            o_c = r.GetOrdinal("corresponding_media_id");
                            o_t = r.GetOrdinal("thumbnail_media_id");
                            ordReady = true;
                        }

                        portals.Add((
                            r.GetInt32(o_id),
                            r.GetString(o_key),
                            r.IsDBNull(o_c) ? null : r.GetString(o_c),
                            r.IsDBNull(o_t) ? null : r.GetString(o_t)
                        ));
                    }
                }

                // 3) Delete each portal's i18n and the portal row
                foreach (var (pid, key, _, _) in portals)
                {
                    const string delI18nSql = @"DELETE FROM i18n   WHERE `key` = @K AND space_id = @S;";
                    const string delPortalSql = @"DELETE FROM portal WHERE id = @P AND space_id = @S;";

                    await using (var delI18n = _db.CreateCommand(conn, delI18nSql))
                    {
                        delI18n.Transaction = tx;
                        delI18n.Parameters.Add(_db.CreateParameter("@K", key));
                        delI18n.Parameters.Add(_db.CreateParameter("@S", spaceId));
                        await delI18n.ExecuteNonQueryAsync();
                    }

                    await using (var delPortal = _db.CreateCommand(conn, delPortalSql))
                    {
                        delPortal.Transaction = tx;
                        delPortal.Parameters.Add(_db.CreateParameter("@P", pid));
                        delPortal.Parameters.Add(_db.CreateParameter("@S", spaceId));
                        await delPortal.ExecuteNonQueryAsync();
                    }
                }

                // 4) Delete media_localization & media for corresponding/thumbnail media
                const string delMediaLocSql = @"DELETE FROM media_localization WHERE media_id = @M;";
                const string delMediaSql = @"DELETE FROM media               WHERE id       = @M;";

                foreach (var (_, _, corr, thumb) in portals)
                {
                    if (!string.IsNullOrWhiteSpace(corr))
                    {
                        await using (var cmd = _db.CreateCommand(conn, delMediaLocSql))
                        {
                            cmd.Transaction = tx;
                            cmd.Parameters.Add(_db.CreateParameter("@M", corr!));
                            await cmd.ExecuteNonQueryAsync();
                        }
                        await using (var cmd = _db.CreateCommand(conn, delMediaSql))
                        {
                            cmd.Transaction = tx;
                            cmd.Parameters.Add(_db.CreateParameter("@M", corr!));
                            await cmd.ExecuteNonQueryAsync();
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(thumb))
                    {
                        await using (var cmd = _db.CreateCommand(conn, delMediaLocSql))
                        {
                            cmd.Transaction = tx;
                            cmd.Parameters.Add(_db.CreateParameter("@M", thumb!));
                            await cmd.ExecuteNonQueryAsync();
                        }
                        await using (var cmd = _db.CreateCommand(conn, delMediaSql))
                        {
                            cmd.Transaction = tx;
                            cmd.Parameters.Add(_db.CreateParameter("@M", thumb!));
                            await cmd.ExecuteNonQueryAsync();
                        }
                    }
                }

                foreach (var media in boothMedia)
                {
                    await DeleteMediaCascadeAsync(conn, tx, boothId, media.MediaId, media.TextKey, media.DescriptionKey, spaceId);
                }

                // 5) Delete booth's i18n & booth
                const string delBi18nSql = @"DELETE FROM i18n  WHERE `key` = @K AND space_id = @S;";
                const string delBoothSql = @"DELETE FROM booth WHERE id = @B AND space_id = @S;";

                await using (var delBi18n = _db.CreateCommand(conn, delBi18nSql))
                {
                    delBi18n.Transaction = tx;
                    delBi18n.Parameters.Add(_db.CreateParameter("@K", boothKey!));
                    delBi18n.Parameters.Add(_db.CreateParameter("@S", spaceId));
                    await delBi18n.ExecuteNonQueryAsync();
                }

                await using (var delBooth = _db.CreateCommand(conn, delBoothSql))
                {
                    delBooth.Transaction = tx;
                    delBooth.Parameters.Add(_db.CreateParameter("@B", boothId));
                    delBooth.Parameters.Add(_db.CreateParameter("@S", spaceId));
                    await delBooth.ExecuteNonQueryAsync();
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
