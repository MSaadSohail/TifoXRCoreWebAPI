
//using TifoXR3DBackend.Models;
//using Microsoft.AspNetCore.Mvc;
//using MySqlConnector;

//namespace TifoXR3DBackend.Controllers
//{
//    [Route("api/[controller]")]
//    [ApiController]
//    public class PortalController : ControllerBase
//    {
//        private readonly string _connectionString;

//        public PortalController(IConfiguration configuration)
//        {
//            _connectionString = configuration.GetConnectionString("DefaultConnection");
//        }

//        // GET /api/portal/123
//        [HttpGet("{portal_id}")]
//        public async Task<ActionResult<PortalModel>> GetPortalDataByID(
//            [FromRoute(Name = "portal_id")] int portalId
//        )
//        {
//            const string sql = @"
//SELECT 
//p.id                    AS portal_id,
//p.space_id,
//p.booth_id,
//p.portal_type_id,
//p.event_id,
//p.corresponding_media_id,
//p.thumbnail_media_id,
//p.text_field_key        AS i18n_key,
//p.external_link,
//i.locale_id                AS locale_id,
//i.value                 AS localized_text_value,
//ml1.media_link          AS corresponding_media_link,
//ml2.media_link          AS thumbnail_media_link
//FROM portal p
//LEFT JOIN i18n i 
//ON i.`key`    = p.text_field_key
//AND i.space_id = p.space_id
//LEFT JOIN media_localization ml1 
//ON ml1.media_id  = p.corresponding_media_id
//AND ml1.locale_id = i.locale_id
//LEFT JOIN media_localization ml2 
//ON ml2.media_id  = p.thumbnail_media_id
//AND ml2.locale_id = ml1.locale_id
//WHERE p.id = @PortalId
//ORDER BY i.locale_id;
//";

//            await using var conn = new MySqlConnection(_connectionString);
//            await conn.OpenAsync();

//            await using var cmd = new MySqlCommand(sql, conn);
//            cmd.Parameters.AddWithValue("@PortalId", portalId);

//            await using var reader = await cmd.ExecuteReaderAsync();
//            PortalModel portal = null;

//            while (await reader.ReadAsync())
//            {
//                if (portal == null)
//                {
//                    portal = new PortalModel
//                    {
//                        PortalId = reader.GetInt32("portal_id"),
//                        SpaceId = reader.GetInt32("space_id"),
//                        BoothId = reader.IsDBNull(reader.GetOrdinal("booth_id"))
//                                                    ? (int?)null
//                                                    : reader.GetInt32("booth_id"),
//                        PortalTypeId = reader.IsDBNull(reader.GetOrdinal("portal_type_id"))
//                                                    ? (int?)null
//                                                    : reader.GetInt32("portal_type_id"),
//                        EventId = reader.IsDBNull(reader.GetOrdinal("event_id"))
//                                                    ? (int?)null
//                                                    : reader.GetInt32("event_id"),
//                        CorrespondingMediaId = reader.IsDBNull(reader.GetOrdinal("corresponding_media_id"))
//                                                    ? null
//                                                    : reader.GetString("corresponding_media_id"),
//                        ThumbnailMediaId = reader.IsDBNull(reader.GetOrdinal("thumbnail_media_id"))
//                                                    ? null
//                                                    : reader.GetString("thumbnail_media_id"),
//                        TextFieldKey = reader.GetString("i18n_key"),
//                        ExternalLink = reader.IsDBNull(reader.GetOrdinal("external_link"))
//                                                    ? null
//                                                    : reader.GetString("external_link"),
//                        Localizations = new List<PortalLocalization>()
//                    };
//                }

//                portal.Localizations.Add(new PortalLocalization
//                {
//                    LocaleId = reader.GetString("locale_id"),
//                    LocalizedTextValue = reader.IsDBNull(reader.GetOrdinal("localized_text_value"))
//                                                    ? null
//                                                    : reader.GetString("localized_text_value"),
//                    CorrespondingMediaLink = reader.IsDBNull(reader.GetOrdinal("corresponding_media_link"))
//                                                    ? null
//                                                    : reader.GetString("corresponding_media_link"),
//                    ThumbnailMediaLink = reader.IsDBNull(reader.GetOrdinal("thumbnail_media_link"))
//                                                    ? null
//                                                    : reader.GetString("thumbnail_media_link")
//                });
//            }

//            if (portal == null)
//                return NotFound();

//            return Ok(portal);
//        }
//        [HttpPost]
//        [ProducesResponseType(typeof(PortalModel), StatusCodes.Status201Created)]
//        [ProducesResponseType(StatusCodes.Status400BadRequest)]
//        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
//        public async Task<ActionResult<PortalModel>> CreatePortal([FromBody] PortalCreateDto create)
//        {
//            if (create == null)
//                return BadRequest();

//            await using var conn = new MySqlConnection(_connectionString);
//            await conn.OpenAsync();
//            await using var tx = await conn.BeginTransactionAsync();

//            try
//            {
//                // 1) Insert portal record
//                const string insertPortalSql = @"
//                INSERT INTO portal
//                    (space_id, booth_id, portal_type_id, event_id,
//                        corresponding_media_id, thumbnail_media_id,
//                        text_field_key, external_link)
//                VALUES
//                    (@spaceId, @boothId, @portalTypeId, @eventId,
//                        @correspondingMediaId, @thumbnailMediaId,
//                        @textFieldKey, @externalLink);
//                SELECT LAST_INSERT_ID();
//            ";

//                int newPortalId;
//                await using (var cmd = new MySqlCommand(insertPortalSql, conn, tx))
//                {
//                    cmd.Parameters.AddWithValue("@spaceId", create.SpaceId);
//                    cmd.Parameters.AddWithValue("@boothId", (object?)create.BoothId ?? DBNull.Value);
//                    cmd.Parameters.AddWithValue("@portalTypeId", (object?)create.PortalTypeId ?? DBNull.Value);
//                    cmd.Parameters.AddWithValue("@eventId", (object?)create.EventId ?? DBNull.Value);
//                    cmd.Parameters.AddWithValue("@correspondingMediaId", (object?)create.CorrespondingMediaId ?? DBNull.Value);
//                    cmd.Parameters.AddWithValue("@thumbnailMediaId", (object?)create.ThumbnailMediaId ?? DBNull.Value);
//                    cmd.Parameters.AddWithValue("@textFieldKey", create.TextFieldKey);
//                    cmd.Parameters.AddWithValue("@externalLink", (object?)create.ExternalLink ?? DBNull.Value);
//                    newPortalId = Convert.ToInt32(await cmd.ExecuteScalarAsync());
//                }

//                // 2) Insert i18n entries for each locale
//                const string insertI18nSql = @"
//                INSERT INTO i18n (`key`, locale, value, space_id)
//                VALUES (@textFieldKey, @locale, @textValue, @spaceId);
//            ";
//                foreach (var loc in create.Localizations)
//                {
//                    await using var cmdI18n = new MySqlCommand(insertI18nSql, conn, tx);
//                    cmdI18n.Parameters.AddWithValue("@textFieldKey", create.TextFieldKey);
//                    cmdI18n.Parameters.AddWithValue("@locale", loc.LocaleId);
//                    cmdI18n.Parameters.AddWithValue("@textValue", loc.LocalizedTextValue);
//                    cmdI18n.Parameters.AddWithValue("@spaceId", create.SpaceId);
//                    await cmdI18n.ExecuteNonQueryAsync();
//                }

//                // 3) Insert media_localization for corresponding and thumbnail media
//                const string insertMediaSql = @"
//                INSERT INTO media_localization (media_id, locale_id, media_link)
//                VALUES (@mediaId, @localeId, @mediaLink);
//            ";
//                foreach (var loc in create.Localizations)
//                {
//                    // corresponding media
//                    await using (var cmdMedia1 = new MySqlCommand(insertMediaSql, conn, tx))
//                    {
//                        cmdMedia1.Parameters.AddWithValue("@mediaId", create.CorrespondingMediaId);
//                        cmdMedia1.Parameters.AddWithValue("@localeId", loc.LocaleId);
//                        cmdMedia1.Parameters.AddWithValue("@mediaLink", loc.CorrespondingMediaLink);
//                        await cmdMedia1.ExecuteNonQueryAsync();
//                    }
//                    // thumbnail media
//                    await using (var cmdMedia2 = new MySqlCommand(insertMediaSql, conn, tx))
//                    {
//                        cmdMedia2.Parameters.AddWithValue("@mediaId", create.ThumbnailMediaId);
//                        cmdMedia2.Parameters.AddWithValue("@localeId", loc.LocaleId);
//                        cmdMedia2.Parameters.AddWithValue("@mediaLink", loc.ThumbnailMediaLink);
//                        await cmdMedia2.ExecuteNonQueryAsync();
//                    }
//                }

//                await tx.CommitAsync();

//                // 4) Reload and return the created portal
//                var created = await LoadPortalById(conn, newPortalId);
//                return CreatedAtAction(nameof(GetPortalDataByID),
//                                        new { portal_id = newPortalId },
//                                        created);
//            }
//            catch
//            {
//                await tx.RollbackAsync();
//                return StatusCode(500);
//            }
//        }

//        // Assume this GET action exists for CreatedAtAction
            

//        // Helper to re-fetch a portal and its localizations
//        private static async Task<PortalModel> LoadPortalById(MySqlConnection conn, int portalId)
//        {
//            const string sql = @"
//            SELECT 
//                p.id                    AS portal_id,
//                p.space_id,
//                p.booth_id,
//                p.portal_type_id,
//                p.event_id,
//                p.corresponding_media_id,
//                p.thumbnail_media_id,
//                p.text_field_key        AS i18n_key,
//                p.external_link,
//                i.locale                AS locale,
//                i.value                 AS localized_text_value,
//                ml1.media_link          AS corresponding_media_link,
//                ml2.media_link          AS thumbnail_media_link
//            FROM portal p
//            LEFT JOIN i18n i 
//                ON i.`key`    = p.text_field_key
//                AND i.space_id = p.space_id
//            LEFT JOIN media_localization ml1 
//                ON ml1.media_id  = p.corresponding_media_id
//                AND ml1.locale_id = i.locale_id
//            LEFT JOIN media_localization ml2 
//                ON ml2.media_id  = p.thumbnail_media_id
//                AND ml2.locale_id = ml1.locale_id
//            WHERE p.id = @PortalId
//            ORDER BY i.locale;
//        ";

//            await using var cmd = new MySqlCommand(sql, conn);
//            cmd.Parameters.AddWithValue("@PortalId", portalId);

//            await using var reader = await cmd.ExecuteReaderAsync();
//            PortalModel portal = null;
//            while (await reader.ReadAsync())
//            {
//                if (portal == null)
//                {
//                    portal = new PortalModel
//                    {
//                        PortalId = reader.GetInt32("portal_id"),
//                        SpaceId = reader.GetInt32("space_id"),
//                        BoothId = reader.IsDBNull(reader.GetOrdinal("booth_id"))
//                                                    ? (int?)null
//                                                    : reader.GetInt32("booth_id"),
//                        PortalTypeId = reader.IsDBNull(reader.GetOrdinal("portal_type_id"))
//                                                    ? (int?)null
//                                                    : reader.GetInt32("portal_type_id"),
//                        EventId = reader.IsDBNull(reader.GetOrdinal("event_id"))
//                                                    ? (int?)null
//                                                    : reader.GetInt32("event_id"),
//                        CorrespondingMediaId = reader.IsDBNull(reader.GetOrdinal("corresponding_media_id"))
//                                                    ? null
//                                                    : reader.GetString("corresponding_media_id"),
//                        ThumbnailMediaId = reader.IsDBNull(reader.GetOrdinal("thumbnail_media_id"))
//                                                    ? null
//                                                    : reader.GetString("thumbnail_media_id"),
//                        TextFieldKey = reader.GetString("i18n_key"),
//                        ExternalLink = reader.IsDBNull(reader.GetOrdinal("external_link"))
//                                                    ? null
//                                                    : reader.GetString("external_link"),
//                        Localizations = new List<PortalLocalization>()
//                    };
//                }
//                portal.Localizations.Add(new PortalLocalization
//                {
//                    LocaleId = reader.GetString("locale"),
//                    LocalizedTextValue = reader.IsDBNull(reader.GetOrdinal("localized_text_value"))
//                                                    ? null
//                                                    : reader.GetString("localized_text_value"),
//                    CorrespondingMediaLink = reader.IsDBNull(reader.GetOrdinal("corresponding_media_link"))
//                                                    ? null
//                                                    : reader.GetString("corresponding_media_link"),
//                    ThumbnailMediaLink = reader.IsDBNull(reader.GetOrdinal("thumbnail_media_link"))
//                                                    ? null
//                                                    : reader.GetString("thumbnail_media_link")
//                });
//            }
//            return portal;
//        }

//    }

//    public class PortalCreateDto
//    {
//        public int SpaceId { get; set; }
//        public int? BoothId { get; set; }
//        public int? PortalTypeId { get; set; }
//        public int? EventId { get; set; }
//        public string CorrespondingMediaId { get; set; }
//        public string ThumbnailMediaId { get; set; }
//        public string TextFieldKey { get; set; }
//        public string ExternalLink { get; set; }
//        public List<PortalCreateLocalization> Localizations { get; set; }
//    }

//    public class PortalCreateLocalization
//    {
//        public string LocaleId { get; set; }
//        public string LocalizedTextValue { get; set; }
//        public string CorrespondingMediaLink { get; set; }
//        public string ThumbnailMediaLink { get; set; }
//    }

//}

