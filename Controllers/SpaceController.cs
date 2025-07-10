
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using System.Data;
using TifoXRWebApi.Models;
using TifoXRWebApi.Models.Common;

namespace TifoXRWebApi.Controllers
{
    [Route("api/space")]
    [ApiController]

    //For Portals
    public partial class SpaceController : ControllerBase
    {
        private readonly string _connectionString;

        public SpaceController(IConfiguration configuration)
            => _connectionString = configuration.GetConnectionString("DefaultConnection");

        /// <summary>
        /// GET /api/space/{spaceId}/portals
        /// Returns all portals in a space,
        /// including localized name and media localizations.
        /// </summary>
        [HttpGet("{spaceId}/portals")]
        public async Task<ActionResult<List<PortalData>>> GetPortalsBySpace([FromRoute] int spaceId)
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
                    p.text_field_key              AS i18n_key,

                    ml1.locale_id                 AS locale_id,
                    i.value                       AS localized_text_value,
                    ml1.media_link                AS corresponding_media_link,
                    ml2.media_link                AS thumbnail_media_link

                FROM portal p
                LEFT JOIN i18n i 
                  ON p.text_field_key = i.`key`
                 AND i.space_id     = p.space_id
                LEFT JOIN media_localization ml1 
                  ON ml1.media_id   = p.corresponding_media_id
                 AND ml1.locale_id  = i.locale_id
                LEFT JOIN media_localization ml2 
                  ON ml2.media_id   = p.thumbnail_media_id
                 AND ml2.locale_id  = ml1.locale_id
                WHERE p.space_id = @SpaceId
                ORDER BY p.id, locale_id;
            ";

            await using var conn = new MySqlConnection(_connectionString);
            await conn.OpenAsync();
            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@SpaceId", spaceId);

            var map = new Dictionary<int, PortalData>();
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var id = reader.GetInt32("portal_id");
                if (!map.TryGetValue(id, out var portal))
                {
                    portal = new PortalData
                    {
                        PortalId = id,
                        SpaceId = reader.GetInt32("space_id"),
                        BoothId = reader.IsDBNull("booth_id") ? null : reader.GetInt32("booth_id"),
                        PortalTypeId = reader.IsDBNull("portal_type_id") ? null : reader.GetInt32("portal_type_id"),
                        EventId = reader.IsDBNull("event_id") ? null : reader.GetInt32("event_id"),
                        ExternalLink = reader.IsDBNull("external_link") ? null : reader.GetString("external_link"),

                        TextFieldKey = new LocalizedName
                        {
                            Key = reader.GetString("i18n_key"),
                            Values = new List<LocalizedValue>()
                        },

                        CorrespondingMedia = reader.IsDBNull("corr_media_id")
                            ? null
                            : new MediaData
                            {
                                Id = reader.GetString("corr_media_id"),
                                Localizations = new List<MediaLocalization>()
                            },

                        ThumbnailMedia = reader.IsDBNull("thumb_media_id")
                            ? null
                            : new MediaData
                            {
                                Id = reader.GetString("thumb_media_id"),
                                Localizations = new List<MediaLocalization>()
                            }
                    };
                    map[id] = portal;
                }

                // add localized name entry
                if (!reader.IsDBNull("locale_id"))
                {
                    portal.TextFieldKey.Values.Add(new LocalizedValue
                    {
                        LocaleId = reader.GetString("locale_id"),
                        Value = reader.IsDBNull("localized_text_value")
                                   ? null!
                                   : reader.GetString("localized_text_value")
                    });

                    // add corresponding media entry
                    if (portal.CorrespondingMedia != null)
                    {
                        portal.CorrespondingMedia.Localizations.Add(new MediaLocalization
                        {
                            LocaleId = reader.GetString("locale_id"),
                            MediaLink = reader.IsDBNull("corresponding_media_link")
                                        ? null!
                                        : reader.GetString("corresponding_media_link")
                        });
                    }

                    // add thumbnail media entry
                    if (portal.ThumbnailMedia != null)
                    {
                        portal.ThumbnailMedia.Localizations.Add(new MediaLocalization
                        {
                            LocaleId = reader.GetString("locale_id"),
                            MediaLink = reader.IsDBNull("thumbnail_media_link")
                                        ? null!
                                        : reader.GetString("thumbnail_media_link")
                        });
                    }
                }
            }

            var list = new List<PortalData>(map.Values);
            if (list.Count == 0)
                return NotFound();
            return Ok(list);
        }

        /// <summary>
        /// GET /api/space/{spaceId}/booth/{boothId}/portals
        /// Returns all portals under a specific booth in a space,
        /// including localized name and media localizations.
        /// </summary>
        [HttpGet("{spaceId}/booth/{boothId}/portals")]
        [ProducesResponseType(typeof(List<PortalData>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<List<PortalData>>> GetPortalsByBooth(
            [FromRoute] int spaceId,
            [FromRoute] int boothId)
        {
            try
            {
                const string sql = @"
                    SELECT
                        p.id                            AS portal_id,
                        p.space_id,
                        p.booth_id,
                        p.portal_type_id,
                        p.event_id,
                        CONCAT(p.corresponding_media_id,'') AS corr_media_id,
                        CONCAT(p.thumbnail_media_id,'')     AS thumb_media_id,
                        CONCAT(p.text_field_key,'')         AS text_key,
                        p.external_link,

                        i.locale_id                    AS locale_id,
                        i.value                        AS localized_text_value,

                        ml1.media_link                 AS corr_media_link,
                        ml2.media_link                 AS thumb_media_link

                    FROM portal p
                    LEFT JOIN i18n i 
                      ON i.`key`    = p.text_field_key
                     AND i.space_id = p.space_id
                    LEFT JOIN media_localization ml1 
                      ON ml1.media_id  = p.corresponding_media_id
                     AND ml1.locale_id = i.locale_id
                    LEFT JOIN media_localization ml2 
                      ON ml2.media_id  = p.thumbnail_media_id
                     AND ml2.locale_id = ml1.locale_id

                    WHERE p.space_id = @SpaceId
                      AND p.booth_id = @BoothId
                    ORDER BY p.id, i.locale_id;
                ";

                await using var conn = new MySqlConnection(_connectionString);
                await conn.OpenAsync();
                await using var cmd = new MySqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@SpaceId", spaceId);
                cmd.Parameters.AddWithValue("@BoothId", boothId);

                var map = new Dictionary<int, PortalData>();
                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var id = reader.GetInt32("portal_id");
                    if (!map.TryGetValue(id, out var portal))
                    {
                        portal = new PortalData
                        {
                            PortalId = id,
                            SpaceId = reader.GetInt32("space_id"),
                            BoothId = reader.GetInt32("booth_id"),
                            PortalTypeId = reader.IsDBNull("portal_type_id") ? null : reader.GetInt32("portal_type_id"),
                            EventId = reader.IsDBNull("event_id") ? null : reader.GetInt32("event_id"),
                            ExternalLink = reader.IsDBNull("external_link") ? null : reader.GetString("external_link"),

                            TextFieldKey = new LocalizedName
                            {
                                Key = reader.GetString("text_key"),
                                Values = new List<LocalizedValue>()
                            },

                            CorrespondingMedia = string.IsNullOrEmpty(reader.GetString("corr_media_id"))
                                ? null
                                : new MediaData
                                {
                                    Id = reader.GetString("corr_media_id"),
                                    Localizations = new List<MediaLocalization>()
                                },

                            ThumbnailMedia = string.IsNullOrEmpty(reader.GetString("thumb_media_id"))
                                ? null
                                : new MediaData
                                {
                                    Id = reader.GetString("thumb_media_id"),
                                    Localizations = new List<MediaLocalization>()
                                }
                        };
                        map[id] = portal;
                    }

                    if (!reader.IsDBNull("locale_id"))
                    {
                        var locale = reader.GetString("locale_id");
                        portal.TextFieldKey.Values.Add(new LocalizedValue
                        {
                            LocaleId = locale,
                            Value = reader.GetString("localized_text_value")
                        });

                        if (portal.CorrespondingMedia != null)
                        {
                            portal.CorrespondingMedia.Localizations.Add(new MediaLocalization
                            {
                                LocaleId = locale,
                                MediaLink = reader.GetString("corr_media_link")
                            });
                        }

                        if (portal.ThumbnailMedia != null)
                        {
                            portal.ThumbnailMedia.Localizations.Add(new MediaLocalization
                            {
                                LocaleId = locale,
                                MediaLink = reader.GetString("thumb_media_link")
                            });
                        }
                    }
                }

                var result = new List<PortalData>(map.Values);
                if (result.Count == 0)
                    return NotFound();

                return Ok(result);
            }
            catch (Exception ex)
            {
                // Optionally log ex here
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// PUT /api/space/{spaceId}/portal/{portalId}
        /// Updates a single portal in a space,
        /// including localized name and media localizations.
        /// </summary>
        [HttpPut("{spaceId}/portal/{portalId}")]
        [ProducesResponseType(typeof(PortalResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<PortalResponse>> UpdatePortalData(
            [FromRoute] int spaceId,
            [FromRoute] int portalId,
            [FromBody] PortalUpdateDto dto
        )
        {
            if (dto == null) return BadRequest();

            await using var conn = new MySqlConnection(_connectionString);
            await conn.OpenAsync();
            await using var tx = await conn.BeginTransactionAsync();
            try
            {
                // get existing text_key
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
                    if (o == null) return NotFound();
                    textKey = o.ToString()!;
                }

                // upsert i18n for textKey
                const string updI18n = @"
                    UPDATE i18n SET value=@Value
                    WHERE `key`=@TextKey AND locale_id=@LocaleId AND space_id=@SpaceId;
                ";

                const string insI18n = @"
                    INSERT INTO i18n (`key`,locale_id,value,space_id)
                    VALUES(@TextKey,@LocaleId,@Value,@SpaceId);
                ";

                foreach (var loc in dto.LocalizedName.Values)
                {
                    await using var cu = new MySqlCommand(updI18n, conn, tx);
                    cu.Parameters.AddWithValue("@TextKey", textKey);
                    cu.Parameters.AddWithValue("@LocaleId", loc.LocaleId);
                    cu.Parameters.AddWithValue("@Value", loc.Value);
                    cu.Parameters.AddWithValue("@SpaceId", spaceId);
                    if (await cu.ExecuteNonQueryAsync() == 0)
                    {
                        await using var ci = new MySqlCommand(insI18n, conn, tx);
                        ci.Parameters.AddWithValue("@TextKey", textKey);
                        ci.Parameters.AddWithValue("@LocaleId", loc.LocaleId);
                        ci.Parameters.AddWithValue("@Value", loc.Value);
                        ci.Parameters.AddWithValue("@SpaceId", spaceId);
                        await ci.ExecuteNonQueryAsync();
                    }
                }

                // helper to upsert one media block
                async Task UpsertMedia(MediaUpdateDto mDto, string columnIdName)
                {
                    // determine media_id param name depends on column (either p.corresponding_media_id or p.thumbnail_media_id)
                    var mediaId = string.IsNullOrWhiteSpace(mDto.Id)
                                  ? Guid.NewGuid().ToString()
                                  : mDto.Id;

                    // 1) update portal set column = @MediaId
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

                    // 2) upsert media table
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

                    // 3) upsert localizations
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

                // apply to correspondingMedia & thumbnailMedia if provided
                if (dto.CorrespondingMedia != null)
                    await UpsertMedia(dto.CorrespondingMedia, "corresponding_media_id");
                if (dto.ThumbnailMedia != null)
                    await UpsertMedia(dto.ThumbnailMedia, "thumbnail_media_id");

                await tx.CommitAsync();

                var updated = await LoadPortalById(conn, spaceId, portalId);
                return Ok(new PortalResponse { Portal = updated });
            }
            catch (MySqlException ex)
            {
                await tx.RollbackAsync();
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// PUT /api/space/{spaceId}/booth/{boothId}/portal/{portalId}
        /// Updates a single portal under a specific booth in a space,
        /// including localized name and media localizations.
        /// </summary>
        [HttpPut("{spaceId}/booth/{boothId}/portal/{portalId}")]
        [ProducesResponseType(typeof(PortalResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<PortalResponse>> UpdatePortalData(
            [FromRoute] int spaceId,
            [FromRoute] int boothId,
            [FromRoute] int portalId,
            [FromBody] PortalUpdateDto dto
        )
        {
            if (dto == null) return BadRequest();

            await using var conn = new MySqlConnection(_connectionString);
            await conn.OpenAsync();
            await using var tx = await conn.BeginTransactionAsync();

            try
            {
                // 1) fetch the existing text_field_key and validate space/booth
                string textKey;
                {
                    const string fetchKey = @"
                        SELECT text_field_key
                        FROM portal
                        WHERE id = @PortalId
                        AND space_id = @SpaceId
                        AND booth_id = @BoothId;
                    ";

                    await using var cmd = new MySqlCommand(fetchKey, conn, tx);
                    cmd.Parameters.AddWithValue("@PortalId", portalId);
                    cmd.Parameters.AddWithValue("@SpaceId", spaceId);
                    cmd.Parameters.AddWithValue("@BoothId", boothId);
                    var o = await cmd.ExecuteScalarAsync();
                    if (o == null) return NotFound();
                    textKey = o.ToString()!;
                }

                // 2) upsert i18n rows for the portal’s text
                const string updI18n = @"
                    UPDATE i18n
                    SET value = @Value
                    WHERE `key`     = @TextKey
                    AND locale_id = @LocaleId
                    AND space_id  = @SpaceId;
                ";

                const string insI18n = @"
                    INSERT INTO i18n (`key`, locale_id, value, space_id)
                    VALUES (@TextKey, @LocaleId, @Value, @SpaceId);
                ";

                foreach (var loc in dto.LocalizedName.Values)
                {
                    await using var cu = new MySqlCommand(updI18n, conn, tx);
                    cu.Parameters.AddWithValue("@TextKey", textKey);
                    cu.Parameters.AddWithValue("@LocaleId", loc.LocaleId);
                    cu.Parameters.AddWithValue("@Value", loc.Value);
                    cu.Parameters.AddWithValue("@SpaceId", spaceId);
                    if (await cu.ExecuteNonQueryAsync() == 0)
                    {
                        await using var ci = new MySqlCommand(insI18n, conn, tx);
                        ci.Parameters.AddWithValue("@TextKey", textKey);
                        ci.Parameters.AddWithValue("@LocaleId", loc.LocaleId);
                        ci.Parameters.AddWithValue("@Value", loc.Value);
                        ci.Parameters.AddWithValue("@SpaceId", spaceId);
                        await ci.ExecuteNonQueryAsync();
                    }
                }

                // 3) helper to upsert a single media slot
                async Task UpsertMediaSlot(MediaUpdateDto mDto, string portalColumn)
                {
                    var mediaId = string.IsNullOrWhiteSpace(mDto.Id)
                                  ? Guid.NewGuid().ToString()
                                  : mDto.Id;

                    // update portal record to point at this media
                    var updPortal = $@"
                        UPDATE portal
                        SET {portalColumn} = @MediaId
                        WHERE id = @PortalId
                        AND space_id = @SpaceId
                        AND booth_id = @BoothId;
                    ";

                    await using (var pCmd = new MySqlCommand(updPortal, conn, tx))
                    {
                        pCmd.Parameters.AddWithValue("@MediaId", mediaId);
                        pCmd.Parameters.AddWithValue("@PortalId", portalId);
                        pCmd.Parameters.AddWithValue("@SpaceId", spaceId);
                        pCmd.Parameters.AddWithValue("@BoothId", boothId);
                        if (await pCmd.ExecuteNonQueryAsync() == 0)
                            throw new InvalidOperationException("Portal not found");
                    }

                    // upsert media row
                    const string updMedia = @"
                        UPDATE media
                        SET media_type_id  = @MediaTypeId,
                            text_key       = @TextKey,
                            description_key= @DescKey
                        WHERE id = @MediaId
                        AND space_id = @SpaceId;
                    ";

                    const string insMedia = @"
                        INSERT INTO media
                            (id, space_id, media_type_id, text_key, description_key)
                        VALUES
                            (@MediaId, @SpaceId, @MediaTypeId, @TextKey, @DescKey);
                    ";

                    await using (var mCmd = new MySqlCommand(updMedia, conn, tx))
                    {
                        mCmd.Parameters.AddWithValue("@MediaId", mediaId);
                        mCmd.Parameters.AddWithValue("@SpaceId", spaceId);
                        mCmd.Parameters.AddWithValue("@MediaTypeId", mDto.MediaTypeId);
                        mCmd.Parameters.AddWithValue("@TextKey", mDto.TextKey ?? "");
                        mCmd.Parameters.AddWithValue("@DescKey", mDto.DescriptionKey ?? "");
                        if (await mCmd.ExecuteNonQueryAsync() == 0)
                        {
                            await using var mi = new MySqlCommand(insMedia, conn, tx);
                            mi.Parameters.AddWithValue("@MediaId", mediaId);
                            mi.Parameters.AddWithValue("@SpaceId", spaceId);
                            mi.Parameters.AddWithValue("@MediaTypeId", mDto.MediaTypeId);
                            mi.Parameters.AddWithValue("@TextKey", mDto.TextKey ?? "");
                            mi.Parameters.AddWithValue("@DescKey", mDto.DescriptionKey ?? "");
                            await mi.ExecuteNonQueryAsync();
                        }
                    }

                    // upsert localizations
                    const string updLoc = @"
                        UPDATE media_localization
                        SET media_link = @Link
                        WHERE media_id = @MediaId
                        AND locale_id = @LocaleId;
                    ";

                    const string insLoc = @"
                        INSERT INTO media_localization
                            (id, media_id, locale_id, media_link)
                        VALUES
                            (@Id, @MediaId, @LocaleId, @Link);
                    ";

                    foreach (var ml in mDto.Localizations)
                    {
                        await using var lCmd = new MySqlCommand(updLoc, conn, tx);
                        lCmd.Parameters.AddWithValue("@MediaId", mediaId);
                        lCmd.Parameters.AddWithValue("@LocaleId", ml.LocaleId);
                        lCmd.Parameters.AddWithValue("@Link", ml.MediaLink);
                        if (await lCmd.ExecuteNonQueryAsync() == 0)
                        {
                            await using var li = new MySqlCommand(insLoc, conn, tx);
                            li.Parameters.AddWithValue("@Id", Guid.NewGuid().ToString());
                            li.Parameters.AddWithValue("@MediaId", mediaId);
                            li.Parameters.AddWithValue("@LocaleId", ml.LocaleId);
                            li.Parameters.AddWithValue("@Link", ml.MediaLink);
                            await li.ExecuteNonQueryAsync();
                        }
                    }
                }

                // 4) upsert the two media slots if provided
                if (dto.CorrespondingMedia != null)
                    await UpsertMediaSlot(dto.CorrespondingMedia, "corresponding_media_id");
                if (dto.ThumbnailMedia != null)
                    await UpsertMediaSlot(dto.ThumbnailMedia, "thumbnail_media_id");

                await tx.CommitAsync();

                // 5) reload & return
                var updated = await LoadPortalById(conn, spaceId, portalId);
                return Ok(new PortalResponse { Portal = updated });
            }
            catch (MySqlException ex)
            {
                await tx.RollbackAsync();
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// POST /api/space/{spaceId}/portal
        /// Creates a new portal under the given space.
        /// </summary>
        [HttpPost("{spaceId}/portal")]
        [ProducesResponseType(typeof(PortalData), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<PortalData>> CreatePortal(
            [FromRoute] int spaceId,
            [FromBody] PortalCreateDto portalDto
        )
        {
            if (portalDto == null)
                return BadRequest();

            await using var conn = new MySqlConnection(_connectionString);
            await conn.OpenAsync();
            await using var tx = await conn.BeginTransactionAsync();

            try
            {
                // 1) Optionally insert corresponding media first
                string? corrMediaId = null;
                if (portalDto.CorrespondingMedia != null)
                {
                    corrMediaId = await InsertOrUpdateMediaAsync(conn, tx, spaceId, portalDto.CorrespondingMedia);
                }

                // 2) Optionally insert thumbnail media
                string? thumbMediaId = null;
                if (portalDto.ThumbnailMedia != null)
                {
                    thumbMediaId = await InsertOrUpdateMediaAsync(conn, tx, spaceId, portalDto.ThumbnailMedia);
                }

                // 3) Insert portal record
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

                // 4) Insert i18n entries for the portal’s text_key
                const string insertI18n = @"
                    INSERT INTO i18n (`key`, locale_id, value, space_id)
                    VALUES (@TextKey, @LocaleId, @Value, @SpaceId);
                ";
                foreach (var loc in portalDto.LocalizedName.Values)
                {
                    await using var cmdI = new MySqlCommand(insertI18n, conn, tx);
                    cmdI.Parameters.AddWithValue("@TextKey", portalDto.LocalizedName.Key);
                    cmdI.Parameters.AddWithValue("@LocaleId", loc.LocaleId);
                    cmdI.Parameters.AddWithValue("@Value", loc.Value);
                    cmdI.Parameters.AddWithValue("@SpaceId", spaceId);
                    await cmdI.ExecuteNonQueryAsync();
                }

                await tx.CommitAsync();

                // 5) Reload full portal and return
                var created = await LoadPortalById(conn, spaceId, newPortalId);
                return CreatedAtAction(
                    nameof(GetPortalsBySpace),
                    new { spaceId = spaceId},
                    created
                );
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// DELETE /api/space/{spaceId}/portal/{portalId}
        /// Deletes a portal in the given space by id.
        /// </summary>
        [HttpDelete("{spaceId}/portal/{portalId}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeletePortal(
            [FromRoute] int spaceId,
            [FromRoute] int portalId
        )
        {
            await using var conn = new MySqlConnection(_connectionString);
            await conn.OpenAsync();
            await using var tx = await conn.BeginTransactionAsync();
            try
            {
                // 1) Fetch portal row to get keys and media IDs
                const string fetchSql = @"
                    SELECT text_field_key,
                           corresponding_media_id,
                           thumbnail_media_id
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
                        return NotFound();
                    textKey = reader.GetString("text_field_key");
                    corrId = reader.IsDBNull("corresponding_media_id")
                               ? null
                               : reader.GetString("corresponding_media_id");
                    thumbId = reader.IsDBNull("thumbnail_media_id")
                               ? null
                               : reader.GetString("thumbnail_media_id");
                }

                // 2) Delete i18n rows for this portal’s key
                const string delI18n = @"
                    DELETE FROM i18n
                    WHERE `key`     = @TextKey
                       AND space_id  = @SpaceId;
                ";

                await using (var cmdI18n = new MySqlCommand(delI18n, conn, tx))
                {
                    cmdI18n.Parameters.AddWithValue("@TextKey", textKey);
                    cmdI18n.Parameters.AddWithValue("@SpaceId", spaceId);
                    await cmdI18n.ExecuteNonQueryAsync();
                }

                // 3) Delete portal row
                const string delPortal = @"
                    DELETE FROM portal
                    WHERE id       = @PortalId
                      AND space_id = @SpaceId;
                ";

                await using (var cmdPortal = new MySqlCommand(delPortal, conn, tx))
                {
                    cmdPortal.Parameters.AddWithValue("@PortalId", portalId);
                    cmdPortal.Parameters.AddWithValue("@SpaceId", spaceId);
                    await cmdPortal.ExecuteNonQueryAsync();
                }

                // 4) Delete media_localization and media for corresponding_media_id
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

                // 5) Delete media_localization and media for thumbnail_media_id
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
                return NoContent();
            }
            catch (MySqlException ex)
            {
                await tx.RollbackAsync();
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // Helper to insert or update a media row + its localizations, returns media_id
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

        private static async Task<PortalData> LoadPortalById(
            MySqlConnection conn,
            int spaceId,
            int portalId
        )
        {
            const string sql = @"
                SELECT 
                    p.id                         AS portal_id,
                    p.space_id,
                    p.booth_id,
                    p.portal_type_id,
                    p.event_id,
                    p.external_link,

                    p.corresponding_media_id     AS corr_media_id,
                    p.thumbnail_media_id         AS thumb_media_id,
                    p.text_field_key             AS text_key,

                    ml1.locale_id                AS locale_id,
                    i.value                      AS localized_text_value,
                    ml1.media_link               AS corresponding_media_link,
                    ml2.media_link               AS thumbnail_media_link

                FROM portal p
                LEFT JOIN i18n i 
                  ON p.text_field_key = i.`key` 
                 AND i.space_id     = p.space_id
                LEFT JOIN media_localization ml1 
                  ON ml1.media_id   = p.corresponding_media_id 
                 AND ml1.locale_id  = i.locale_id
                LEFT JOIN media_localization ml2 
                  ON ml2.media_id   = p.thumbnail_media_id 
                 AND ml2.locale_id  = ml1.locale_id
                WHERE p.id       = @PortalId
                  AND p.space_id = @SpaceId
                ORDER BY locale_id;
            ";

            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@PortalId", portalId);
            cmd.Parameters.AddWithValue("@SpaceId", spaceId);

            await using var reader = await cmd.ExecuteReaderAsync();

            PortalData portal = null!;

            while (await reader.ReadAsync())
            {
                portal ??= new PortalData
                {
                    PortalId = reader.GetInt32("portal_id"),
                    SpaceId = reader.GetInt32("space_id"),
                    BoothId = reader.IsDBNull("booth_id") ? null : reader.GetInt32("booth_id"),
                    PortalTypeId = reader.IsDBNull("portal_type_id") ? null : reader.GetInt32("portal_type_id"),
                    EventId = reader.IsDBNull("event_id") ? null : reader.GetInt32("event_id"),
                    ExternalLink = reader.IsDBNull("external_link") ? null : reader.GetString("external_link"),

                    TextFieldKey = new LocalizedName
                    {
                        Key = reader.GetString("text_key"),
                        Values = new List<LocalizedValue>()
                    },

                    CorrespondingMedia = reader.IsDBNull("corr_media_id")
                            ? null
                            : new MediaData
                            {
                                Id = reader.GetString("corr_media_id"),
                                Localizations = new List<MediaLocalization>()
                            },

                    ThumbnailMedia = reader.IsDBNull("thumb_media_id")
                            ? null
                            : new MediaData
                            {
                                Id = reader.GetString("thumb_media_id"),
                                Localizations = new List<MediaLocalization>()
                            }
                };

                // for this locale:
                if (!reader.IsDBNull("locale_id"))
                {
                    var locale = reader.GetString("locale_id");

                    // add text value
                    portal.TextFieldKey.Values.Add(new LocalizedValue
                    {
                        LocaleId = locale,
                        Value = reader.IsDBNull("localized_text_value")
                                   ? null!
                                   : reader.GetString("localized_text_value")
                    });

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
    }

    //For Booths
    public partial class SpaceController : ControllerBase
    {

        // GET /api/space/{spaceId}/booths
        [HttpGet("{spaceId}/booths")]
        public async Task<ActionResult<List<BoothModel>>> GetAllBoothsBySpace([FromRoute] int spaceId)
        {
            const string sql = @"
                SELECT
                    b.id,
                    b.space_id,
                    b.name_key,
                    i.locale_id,
                    i.value
                FROM booth b
                INNER JOIN i18n i
                  ON i.`key` = b.name_key
                 AND i.space_id = b.space_id
                WHERE b.space_id = @SpaceId
                ORDER BY b.id, i.locale_id;
            ";

            await using var conn = new MySqlConnection(_connectionString);
            await conn.OpenAsync();
            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@SpaceId", spaceId);

            var dict = new Dictionary<int, BoothModel>();
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var id = reader.GetInt32("id");
                if (!dict.TryGetValue(id, out var booth))
                {
                    booth = new BoothModel
                    {
                        Id = id,
                        SpaceId = reader.GetInt32("space_id"),
                        LocalizedName = new LocalizedName
                        {
                            Key = reader.GetString("name_key"),
                            Values = new List<LocalizedValue>()
                        }
                    };
                    dict[id] = booth;
                }

                booth.LocalizedName.Values.Add(new LocalizedValue
                {
                    LocaleId = reader.GetString("locale_id"),
                    Value = reader.GetString("value")
                });
            }

            var list = new List<BoothModel>(dict.Values);
            if (list.Count == 0) return NotFound();
            return Ok(list);
        }
    
        // PUT /api/space/1/booth/2
        [HttpPut("{spaceId}/booth/{boothId}")]
        [ProducesResponseType(typeof(Response), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<Response>> UpdateBooth(
            [FromRoute] int spaceId,
            [FromRoute] int boothId,
            [FromBody] BoothUpdateDto dto
        )
        {
            if (dto == null) return BadRequest();

            await using var conn = new MySqlConnection(_connectionString);
            await conn.OpenAsync();
            await using var tx = await conn.BeginTransactionAsync();

            try
            {
                const string updBooth = @"
                    UPDATE booth
                       SET name_key = @NameKey
                     WHERE id = @BoothId
                       AND space_id = @SpaceId;
                ";
                await using (var cmd = new MySqlCommand(updBooth, conn, tx))
                {
                    cmd.Parameters.AddWithValue("@NameKey", dto.LocalizedName.Key);
                    cmd.Parameters.AddWithValue("@BoothId", boothId);
                    cmd.Parameters.AddWithValue("@SpaceId", spaceId);
                    if (await cmd.ExecuteNonQueryAsync() == 0) return NotFound();
                }

                const string updI18n = @"
                    UPDATE i18n
                       SET value = @Value
                     WHERE `key`     = @NameKey
                       AND locale_id = @LocaleId
                       AND space_id  = @SpaceId;
                ";
                const string insI18n = @"
                    INSERT INTO i18n (`key`, locale_id, value, space_id)
                    VALUES (@NameKey, @LocaleId, @Value, @SpaceId);
                ";

                foreach (var loc in dto.LocalizedName.Values)
                {
                    await using var cmdUp = new MySqlCommand(updI18n, conn, tx);
                    cmdUp.Parameters.AddWithValue("@NameKey", dto.LocalizedName.Key);
                    cmdUp.Parameters.AddWithValue("@LocaleId", loc.LocaleId);
                    cmdUp.Parameters.AddWithValue("@Value", loc.Value);
                    cmdUp.Parameters.AddWithValue("@SpaceId", spaceId);

                    if (await cmdUp.ExecuteNonQueryAsync() == 0)
                    {
                        await using var cmdIn = new MySqlCommand(insI18n, conn, tx);
                        cmdIn.Parameters.AddWithValue("@NameKey", dto.LocalizedName.Key);
                        cmdIn.Parameters.AddWithValue("@LocaleId", loc.LocaleId);
                        cmdIn.Parameters.AddWithValue("@Value", loc.Value);
                        cmdIn.Parameters.AddWithValue("@SpaceId", spaceId);
                        await cmdIn.ExecuteNonQueryAsync();
                    }
                }

                await tx.CommitAsync();
                var updated = await LoadBoothById(conn, spaceId, boothId);
                return Ok(new Response { Booth = updated });
            }
            catch (MySqlException ex)
            {
                await tx.RollbackAsync();
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// POST /api/space/{spaceId}/booth
        /// Creates a new booth in the given space with its localized name.
        /// </summary>
        [HttpPost("{spaceId}/booth")]
        [ProducesResponseType(typeof(BoothWrapper), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<BoothWrapper>> CreateBooth(
            [FromRoute] int spaceId,
            [FromBody] BoothCreateDto boothDto
        )
        {
            if (boothDto == null)
                return BadRequest();

            await using var conn = new MySqlConnection(_connectionString);
            await conn.OpenAsync();
            await using var tx = await conn.BeginTransactionAsync();

            try
            {
                // 1) Insert into booth
                const string insertBoothSql = @"
                    INSERT INTO booth (space_id, name_key)
                         VALUES (@SpaceId, @NameKey);
                ";
                int newId;
                await using (var cmd = new MySqlCommand(insertBoothSql, conn, tx))
                {
                    cmd.Parameters.AddWithValue("@SpaceId", spaceId);
                    cmd.Parameters.AddWithValue("@NameKey", boothDto.LocalizedName.Key);
                    await cmd.ExecuteNonQueryAsync();
                    newId = Convert.ToInt32(cmd.LastInsertedId);
                }

                // 2) Insert i18n entries for this name_key
                const string insertI18nSql = @"
                    INSERT INTO i18n (`key`, locale_id, value, space_id)
                         VALUES (@NameKey, @LocaleId, @Value, @SpaceId);
                ";
                foreach (var val in boothDto.LocalizedName.Values)
                {
                    await using var cmdI18n = new MySqlCommand(insertI18nSql, conn, tx);
                    cmdI18n.Parameters.AddWithValue("@NameKey", boothDto.LocalizedName.Key);
                    cmdI18n.Parameters.AddWithValue("@LocaleId", val.LocaleId);
                    cmdI18n.Parameters.AddWithValue("@Value", val.Value);
                    cmdI18n.Parameters.AddWithValue("@SpaceId", spaceId);
                    await cmdI18n.ExecuteNonQueryAsync();
                }

                await tx.CommitAsync();

                // 3) Build response model
                var createdBooth = new BoothModel
                {
                    Id = newId,
                    SpaceId = spaceId,
                    LocalizedName = new LocalizedName
                    {
                        Key = boothDto.LocalizedName.Key,
                        Values = new List<LocalizedValue>(boothDto.LocalizedName.Values)
                    }
                };

                var wrapper = new BoothWrapper { booth = createdBooth };
                return CreatedAtAction(
                    nameof(GetAllBoothsBySpace),
                    new { spaceId, },
                    wrapper
                );
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                return StatusCode(500, new { error = ex.Message });
            }
        }

        private static async Task<BoothModel> LoadBoothById(MySqlConnection conn, int spaceId, int boothId)
        {
            const string sql = @"
                SELECT b.id, b.space_id, b.name_key, i.locale_id, i.value
                FROM booth b
                LEFT JOIN i18n i
                  ON i.`key`    = b.name_key
                 AND i.space_id = b.space_id
                WHERE b.id = @BoothId
                  AND b.space_id = @SpaceId
                ORDER BY i.locale_id;
            ";
            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@BoothId", boothId);
            cmd.Parameters.AddWithValue("@SpaceId", spaceId);

            await using var reader = await cmd.ExecuteReaderAsync();

            BoothModel? booth = null;

            while (await reader.ReadAsync())
            {
                if (booth == null)
                {
                    booth = new BoothModel
                    {
                        Id = reader.GetInt32("id"),
                        SpaceId = reader.GetInt32("space_id"),
                        LocalizedName = new LocalizedName
                        {
                            Key = reader.GetString("name_key"),
                            Values = new List<LocalizedValue>()
                        }
                    };
                }
                booth.LocalizedName.Values.Add(new LocalizedValue
                {
                    LocaleId = reader.GetString("locale_id"),
                    Value = reader.GetString("value")
                });
            }

            return booth;
        }

    }

    //For Teleport Tables
    public partial class SpaceController : ControllerBase
    {

        /// <summary>
        /// GET /api/space/{spaceId}/teleport_tables
        /// Returns all teleport tables in a space, with their localized names and buttons.
        /// </summary>
        [HttpGet("{spaceId}/teleport_tables")]
        [ProducesResponseType(typeof(List<TeleportTableData>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<List<TeleportTableData>>> GetTeleportTablesBySpace(
            [FromRoute] int spaceId)
        {
            try
            {
                const string tableSql = @"
                    SELECT
                        t.id,
                        t.space_id,
                        t.is_active,
                        t.name_key,
                        i.locale_id,
                        i.value
                    FROM teleport_table AS t
                    LEFT JOIN i18n AS i
                      ON i.`key`     = t.name_key
                     AND i.space_id  = t.space_id
                    WHERE t.space_id = @SpaceId
                    ORDER BY t.id, i.locale_id;
                ";

                await using var conn = new MySqlConnection(_connectionString);
                await conn.OpenAsync();

                var tableMap = new Dictionary<int, TeleportTableData>();
                // Scope the table reader so it's disposed before button queries
                {
                    await using var tblCmd = new MySqlCommand(tableSql, conn);
                    tblCmd.Parameters.AddWithValue("@SpaceId", spaceId);

                    await using var tblReader = await tblCmd.ExecuteReaderAsync();
                    while (await tblReader.ReadAsync())
                    {
                        var id = tblReader.GetInt32("id");
                        if (!tableMap.TryGetValue(id, out var table))
                        {
                            table = new TeleportTableData
                            {
                                Id = id,
                                SpaceId = tblReader.GetInt32("space_id"),
                                IsActive = tblReader.GetBoolean("is_active"),
                                LocalizedName = new LocalizedName
                                {
                                    Key = tblReader.GetString("name_key"),
                                    Values = new List<LocalizedValue>()
                                },
                                Buttons = new List<ButtonData>()
                            };
                            tableMap[id] = table;
                        }

                        if (!tblReader.IsDBNull("locale_id"))
                        {
                            table.LocalizedName.Values.Add(new LocalizedValue
                            {
                                LocaleId = tblReader.GetString("locale_id"),
                                Value = tblReader.GetString("value")
                            });
                        }
                    }
                    // tblReader disposed here
                }

                // Now fetch buttons for each table
                const string btnSql = @"
                    SELECT
                        b.id,
                        b.text_key,
                        i.locale_id,
                        i.value
                    FROM teleport_table_button AS b
                    LEFT JOIN i18n AS i
                      ON i.`key`    = b.text_key
                     AND i.space_id = @SpaceId
                    WHERE b.table_id = @TableId
                    ORDER BY b.id, i.locale_id;
                ";

                foreach (var table in tableMap.Values)
                {
                    var btnMap = new Dictionary<int, ButtonData>();

                    await using var btnCmd = new MySqlCommand(btnSql, conn);
                    btnCmd.Parameters.AddWithValue("@SpaceId", spaceId);
                    btnCmd.Parameters.AddWithValue("@TableId", table.Id);

                    await using var btnReader = await btnCmd.ExecuteReaderAsync();
                    while (await btnReader.ReadAsync())
                    {
                        var btnId = btnReader.GetInt32("id");
                        if (!btnMap.TryGetValue(btnId, out var btn))
                        {
                            btn = new ButtonData
                            {
                                Id = btnId,
                                LocalizedName = new LocalizedName
                                {
                                    Key = btnReader.GetString("text_key"),
                                    Values = new List<LocalizedValue>()
                                }
                            };
                            btnMap[btnId] = btn;
                        }

                        if (!btnReader.IsDBNull("locale_id"))
                        {
                            btn.LocalizedName.Values.Add(new LocalizedValue
                            {
                                LocaleId = btnReader.GetString("locale_id"),
                                Value = btnReader.GetString("value")
                            });
                        }
                    }

                    table.Buttons = new List<ButtonData>(btnMap.Values);
                }

                var result = new List<TeleportTableData>(tableMap.Values);
                if (result.Count == 0)
                    return NotFound();

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // In Controllers/SpaceController.cs
        [HttpPut("{spaceId}/teleport_table/{tableId}")]
        public async Task<ActionResult<TeleportTableData>> UpdateTeleportTableById(
            [FromRoute] int spaceId,
            [FromRoute] int tableId,
            [FromBody] TeleportTableUpdateDto dto
        )
        {
            if (dto == null) return BadRequest();

            await using var conn = new MySqlConnection(_connectionString);
            await conn.OpenAsync();
            await using var tx = await conn.BeginTransactionAsync();
            try
            {
                // … existing upsert of teleport_table + i18n for table name …

                // 3) Upsert buttons if provided
                if (dto.Buttons != null)
                {
                    const string updBtnSql = @"
                        UPDATE teleport_table_button
                           SET text_key = @TextKey
                         WHERE id = @BtnId
                           AND table_id = @TableId;
                    ";
                    const string insBtnSql = @"
                        INSERT INTO teleport_table_button (table_id, text_key)
                        VALUES (@TableId, @TextKey);
                    ";

                    const string updI18n = @"
                        UPDATE i18n
                           SET value = @Value
                         WHERE `key`     = @TextKey
                           AND locale_id = @LocaleId
                           AND space_id  = @SpaceId;
                    ";

                    const string insI18n = @"
                        INSERT INTO i18n (`key`, locale_id, value, space_id)
                        VALUES (@TextKey, @LocaleId, @Value, @SpaceId);
                    ";

                    foreach (var btn in dto.Buttons)
                    {
                        // 3a) upsert button row
                        int btnId;
                        if (btn.Id.HasValue)
                        {
                            await using var cmdU = new MySqlCommand(updBtnSql, conn, tx);
                            cmdU.Parameters.AddWithValue("@TextKey", btn.LocalizedName.Key);
                            cmdU.Parameters.AddWithValue("@BtnId", btn.Id.Value);
                            cmdU.Parameters.AddWithValue("@TableId", tableId);

                            var updatedData = await cmdU.ExecuteNonQueryAsync();
                            if (updatedData == 0)
                                return NotFound($"Button {btn.Id.Value} not found on table {tableId}");

                            btnId = btn.Id.Value;
                        }
                        else
                        {
                            await using var cmdI = new MySqlCommand(insBtnSql, conn, tx);
                            cmdI.Parameters.AddWithValue("@TableId", tableId);
                            cmdI.Parameters.AddWithValue("@TextKey", btn.LocalizedName.Key);
                            await cmdI.ExecuteNonQueryAsync();
                            btnId = Convert.ToInt32(cmdI.LastInsertedId);
                        }

                        // 3b) upsert i18n entries for this button’s text_key
                        foreach (var loc in btn.LocalizedName.Values)
                        {
                            await using var cu = new MySqlCommand(updI18n, conn, tx);
                            cu.Parameters.AddWithValue("@TextKey", btn.LocalizedName.Key);
                            cu.Parameters.AddWithValue("@LocaleId", loc.LocaleId);
                            cu.Parameters.AddWithValue("@Value", loc.Value);
                            cu.Parameters.AddWithValue("@SpaceId", spaceId);
                            if (await cu.ExecuteNonQueryAsync() == 0)
                            {
                                await using var ci = new MySqlCommand(insI18n, conn, tx);
                                ci.Parameters.AddWithValue("@TextKey", btn.LocalizedName.Key);
                                ci.Parameters.AddWithValue("@LocaleId", loc.LocaleId);
                                ci.Parameters.AddWithValue("@Value", loc.Value);
                                ci.Parameters.AddWithValue("@SpaceId", spaceId);
                                await ci.ExecuteNonQueryAsync();
                            }
                        }
                    }
                }

                await tx.CommitAsync();

                // 4) reload and return the updated table (with buttons)
                var updated = await LoadTeleportTableById(conn, spaceId, tableId);
                return Ok(updated);
            }
            catch (MySqlException ex)
            {
                await tx.RollbackAsync();
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // Helper to load a single teleport table with its localizations
        private static async Task<TeleportTableData> LoadTeleportTableById(
            MySqlConnection conn,
            int spaceId,
            int tableId
        )
        {
            const string sql = @"
                SELECT
                    t.id,
                    t.space_id,
                    t.is_active,
                    t.name_key,
                    i.locale_id,
                    i.value
                FROM teleport_table AS t
                LEFT JOIN i18n AS i
                  ON i.`key`    = t.name_key
                 AND i.space_id = t.space_id
                WHERE t.id = @TableId
                  AND t.space_id = @SpaceId
                ORDER BY i.locale_id;
            ";

            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@TableId", tableId);
            cmd.Parameters.AddWithValue("@SpaceId", spaceId);

            var map = new Dictionary<int, TeleportTableData>();
            await using var reader = await cmd.ExecuteReaderAsync();
            TeleportTableData table = null!;
            while (await reader.ReadAsync())
            {
                if (table == null)
                {
                    table = new TeleportTableData
                    {
                        Id = reader.GetInt32("id"),
                        SpaceId = reader.GetInt32("space_id"),
                        IsActive = reader.GetBoolean("is_active"),
                        LocalizedName = new LocalizedName
                        {
                            Key = reader.GetString("name_key"),
                            Values = new List<LocalizedValue>()
                        }
                    };
                }

                if (!reader.IsDBNull("locale_id"))
                {
                    table.LocalizedName.Values.Add(new LocalizedValue
                    {
                        LocaleId = reader.GetString("locale_id"),
                        Value = reader.GetString("value")
                    });
                }
            }

            return table;
        }
    }
}
