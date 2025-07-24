
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using System.Data;
using TifoXRCoreWebAPI.Models;
using TifoXRCoreWebAPI.Models.Common;
using TifoXRCoreWebAPI.Repositories;
using TifoXRCoreWebAPI.Repositories.Interfaces;

namespace TifoXRCoreWebAPI.Controllers
{
    [Route("api/space")]
    [ApiController]

    

    //For Portals
    public partial class SpaceController : ControllerBase
    {
        private readonly string _connectionString;

        //public SpaceController(IConfiguration configuration)
        //    => _connectionString = configuration.GetConnectionString("DefaultConnection");


        private readonly IPortalRepository _portalRepository;

        public SpaceController(IPortalRepository portalRepository)
        {
            _portalRepository = portalRepository;
        }

        [HttpGet("{spaceId}/portals")]
        public async Task<ActionResult<List<PortalData>>> GetPortalsBySpace(
            [FromRoute] int spaceId
        )
        {
            var portals = await _portalRepository.GetPortalsBySpaceAsync(spaceId);
            if (portals == null || portals.Count == 0)
                return NotFound();
            return Ok(portals);
        }

        [HttpGet("{spaceId}/booth/{boothId}/portals")]
        public async Task<ActionResult<List<PortalData>>> GetPortalsByBooth(
            [FromRoute] int spaceId, 
            [FromRoute] int boothId
        )
        {
            var portals = await _portalRepository.GetPortalsByBoothAsync(spaceId, boothId);
            if (portals == null || portals.Count == 0)
                return NotFound();
            return Ok(portals);
        }

        [HttpGet("{spaceId}/portal/{portalId}")]
        public async Task<ActionResult<PortalData>> GetPortalById(
            [FromRoute] int spaceId, 
            [FromRoute] int portalId)
        {
            var portal = await _portalRepository.GetPortalByIdAsync(spaceId, portalId);
            if (portal == null) return NotFound();
            return Ok(portal);
        }

        [HttpPost("{spaceId}/portal")]
        public async Task<ActionResult<PortalData>> CreatePortal(
            [FromRoute] int spaceId, 
            [FromBody] PortalCreateDto portalDto
        )
        {
            if (portalDto == null) return BadRequest();
            var created = await _portalRepository.CreatePortalAsync(spaceId, portalDto);
            return CreatedAtAction(nameof(GetPortalById), 
                new { spaceId, portalId = created.PortalId }, created);
        }

        [HttpPut("{spaceId}/portal/{portalId}")]
        public async Task<ActionResult<PortalData>> UpdatePortal(
            [FromRoute] int spaceId, 
            [FromRoute] int portalId, 
            [FromBody] PortalUpdateDto portalDto)
        {
            if (portalDto == null) return BadRequest();
            
            var updated = await _portalRepository.UpdatePortalAsync(spaceId, portalId, portalDto);
            
            if(updated == null) return NotFound();
            
            return Ok(updated);
        }

        [HttpPut("{spaceId}/booth/{boothId}/portal/{portalId}")]
        public async Task<ActionResult<PortalResponse>> UpdatePortalData(
            [FromRoute] int spaceId,
            [FromRoute] int boothId,
            [FromRoute] int portalId,
            [FromBody] PortalUpdateDto dto
        )
        {
            if (dto == null) return BadRequest();

            var updated = await _portalRepository.UpdatePortalAsync(spaceId, boothId, portalId, dto);

            if (updated == null) return NotFound();

            return Ok(updated);
        }

        [HttpDelete("{spaceId}/portal/{portalId}")]
        public async Task<IActionResult> DeletePortal(
            [FromRoute] int spaceId, 
            [FromRoute] int portalId)
        {
            var deleted = await _portalRepository.DeletePortalAsync(spaceId, portalId);
            if (!deleted)
                return NotFound();
            return NoContent();
        }

        [HttpDelete("{spaceId}/booth/{boothId}/portal/{portalId}")]
        public async Task<IActionResult> DeletePortal(
            [FromRoute] int spaceId,
            [FromRoute] int boothId,
            [FromRoute] int portalId
        )
        {
            var deleted = await _portalRepository.DeletePortalAsync(spaceId, boothId, portalId);
            if (!deleted)
                return NotFound(); // Could be 404 for not found or mismatched booth
            return NoContent();
        }

    }

    //For Booths
    public partial class BoothController : ControllerBase
    {

        private readonly IBoothRepository _boothRepository;

        public BoothController( IBoothRepository boothRepository)
        {
            _boothRepository = boothRepository;
        }

        [HttpGet("{spaceId}/booths")]
        public async Task<ActionResult<List<BoothModel>>> GetAllBoothsBySpace([FromRoute] int spaceId)
        {
            var booths = await _boothRepository.GetAllBoothsBySpaceAsync(spaceId);
            if (booths == null || booths.Count == 0)
                return NotFound();

            return Ok(booths);
        }


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

            try
            {
                var updated = await _boothRepository.UpdateBoothAsync(spaceId, boothId, dto);
                if (updated == null) return NotFound();
                return Ok(new Response { Booth = updated });
            }
            catch (Exception ex)
            {
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

            try
            {
                var createdBooth = await _boothRepository.CreateBoothAsync(spaceId, boothDto);
                return CreatedAtAction(
                    nameof(GetAllBoothsBySpace),
                    new { spaceId },
                    new BoothWrapper { booth = createdBooth }
                );
            }
            catch (MySqlException ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }


        /// <summary>
        /// DELETE /api/space/{spaceId}/booth/{boothId}
        /// Deletes a booth and all its dependent data:
        ///  • Booth’s own i18n entries
        ///  • All portals under that booth, including each portal’s:
        ///      – i18n entries
        ///      – corresponding & thumbnail media_localization rows
        ///      – corresponding & thumbnail media rows
        ///  • Finally the booth record itself
        /// </summary>
        [HttpDelete("{spaceId}/booth/{boothId}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteBoothCascade([FromRoute] int spaceId, [FromRoute] int boothId)
        {
            try
            {
                var success = await _boothRepository.DeleteBoothCascadeAsync(spaceId, boothId);
                return success ? NoContent() : NotFound();
            }
            catch (MySqlException ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }


        

        //For Teleport Tables
        //public partial class SpaceController : ControllerBase
        //{

        //    /// <summary>
        //    /// GET /api/space/{spaceId}/teleport_tables
        //    /// Returns all teleport tables in a space, with their localized names and buttons.
        //    /// </summary>
        //    [HttpGet("{spaceId}/teleport_tables")]
        //    [ProducesResponseType(typeof(List<TeleportTableData>), StatusCodes.Status200OK)]
        //    [ProducesResponseType(StatusCodes.Status404NotFound)]
        //    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        //    public async Task<ActionResult<List<TeleportTableData>>> GetTeleportTablesBySpace(
        //        [FromRoute] int spaceId)
        //    {
        //        try
        //        {
        //            const string tableSql = @"
        //                SELECT
        //                    t.id,
        //                    t.space_id,
        //                    t.is_active,
        //                    t.name_key,
        //                    i.locale_id,
        //                    i.value
        //                FROM teleport_table AS t
        //                LEFT JOIN i18n AS i
        //                  ON i.`key`     = t.name_key
        //                 AND i.space_id  = t.space_id
        //                WHERE t.space_id = @SpaceId
        //                ORDER BY t.id, i.locale_id;
        //            ";

        //            await using var conn = new MySqlConnection(_connectionString);
        //            await conn.OpenAsync();

        //            var tableMap = new Dictionary<int, TeleportTableData>();
        //            // Scope the table reader so it's disposed before button queries
        //            {
        //                await using var tblCmd = new MySqlCommand(tableSql, conn);
        //                tblCmd.Parameters.AddWithValue("@SpaceId", spaceId);

        //                await using var tblReader = await tblCmd.ExecuteReaderAsync();
        //                while (await tblReader.ReadAsync())
        //                {
        //                    var id = tblReader.GetInt32("id");
        //                    if (!tableMap.TryGetValue(id, out var table))
        //                    {
        //                        table = new TeleportTableData
        //                        {
        //                            Id = id,
        //                            SpaceId = tblReader.GetInt32("space_id"),
        //                            IsActive = tblReader.GetBoolean("is_active"),
        //                            LocalizedName = new LocalizedName
        //                            {
        //                                Key = tblReader.GetString("name_key"),
        //                                Values = new List<LocalizedValue>()
        //                            },
        //                            Buttons = new List<ButtonData>()
        //                        };
        //                        tableMap[id] = table;
        //                    }

        //                    if (!tblReader.IsDBNull("locale_id"))
        //                    {
        //                        table.LocalizedName.Values.Add(new LocalizedValue
        //                        {
        //                            LocaleId = tblReader.GetString("locale_id"),
        //                            Value = tblReader.GetString("value")
        //                        });
        //                    }
        //                }
        //                // tblReader disposed here
        //            }

        //            // Now fetch buttons for each table
        //            const string btnSql = @"
        //                SELECT
        //                    b.id,
        //                    b.text_key,
        //                    i.locale_id,
        //                    i.value
        //                FROM teleport_table_button AS b
        //                LEFT JOIN i18n AS i
        //                  ON i.`key`    = b.text_key
        //                 AND i.space_id = @SpaceId
        //                WHERE b.table_id = @TableId
        //                ORDER BY b.id, i.locale_id;
        //            ";

        //            foreach (var table in tableMap.Values)
        //            {
        //                var btnMap = new Dictionary<int, ButtonData>();

        //                await using var btnCmd = new MySqlCommand(btnSql, conn);
        //                btnCmd.Parameters.AddWithValue("@SpaceId", spaceId);
        //                btnCmd.Parameters.AddWithValue("@TableId", table.Id);

        //                await using var btnReader = await btnCmd.ExecuteReaderAsync();
        //                while (await btnReader.ReadAsync())
        //                {
        //                    var btnId = btnReader.GetInt32("id");
        //                    if (!btnMap.TryGetValue(btnId, out var btn))
        //                    {
        //                        btn = new ButtonData
        //                        {
        //                            Id = btnId,
        //                            LocalizedName = new LocalizedName
        //                            {
        //                                Key = btnReader.GetString("text_key"),
        //                                Values = new List<LocalizedValue>()
        //                            }
        //                        };
        //                        btnMap[btnId] = btn;
        //                    }

        //                    if (!btnReader.IsDBNull("locale_id"))
        //                    {
        //                        btn.LocalizedName.Values.Add(new LocalizedValue
        //                        {
        //                            LocaleId = btnReader.GetString("locale_id"),
        //                            Value = btnReader.GetString("value")
        //                        });
        //                    }
        //                }

        //                table.Buttons = new List<ButtonData>(btnMap.Values);
        //            }

        //            var result = new List<TeleportTableData>(tableMap.Values);
        //            if (result.Count == 0)
        //                return NotFound();

        //            return Ok(result);
        //        }
        //        catch (Exception ex)
        //        {
        //            return StatusCode(500, new { error = ex.Message });
        //        }
        //    }

        //    // In Controllers/SpaceController.cs
        //    [HttpPut("{spaceId}/teleport_table/{tableId}")]
        //    public async Task<ActionResult<TeleportTableData>> UpdateTeleportTableById(
        //        [FromRoute] int spaceId,
        //        [FromRoute] int tableId,
        //        [FromBody] TeleportTableUpdateDto dto
        //    )
        //    {
        //        if (dto == null) return BadRequest();

        //        await using var conn = new MySqlConnection(_connectionString);
        //        await conn.OpenAsync();
        //        await using var tx = await conn.BeginTransactionAsync();
        //        try
        //        {
        //            // … existing upsert of teleport_table + i18n for table name …

        //            // 3) Upsert buttons if provided
        //            if (dto.Buttons != null)
        //            {
        //                const string updBtnSql = @"
        //                    UPDATE teleport_table_button
        //                       SET text_key = @TextKey
        //                     WHERE id = @BtnId
        //                       AND table_id = @TableId;
        //                ";
        //                const string insBtnSql = @"
        //                    INSERT INTO teleport_table_button (table_id, text_key)
        //                    VALUES (@TableId, @TextKey);
        //                ";

        //                const string updI18n = @"
        //                    UPDATE i18n
        //                       SET value = @Value
        //                     WHERE `key`     = @TextKey
        //                       AND locale_id = @LocaleId
        //                       AND space_id  = @SpaceId;
        //                ";

        //                const string insI18n = @"
        //                    INSERT INTO i18n (`key`, locale_id, value, space_id)
        //                    VALUES (@TextKey, @LocaleId, @Value, @SpaceId);
        //                ";

        //                foreach (var btn in dto.Buttons)
        //                {
        //                    // 3a) upsert button row
        //                    int btnId;
        //                    if (btn.Id.HasValue)
        //                    {
        //                        await using var cmdU = new MySqlCommand(updBtnSql, conn, tx);
        //                        cmdU.Parameters.AddWithValue("@TextKey", btn.LocalizedName.Key);
        //                        cmdU.Parameters.AddWithValue("@BtnId", btn.Id.Value);
        //                        cmdU.Parameters.AddWithValue("@TableId", tableId);

        //                        var updatedData = await cmdU.ExecuteNonQueryAsync();
        //                        if (updatedData == 0)
        //                            return NotFound($"Button {btn.Id.Value} not found on table {tableId}");

        //                        btnId = btn.Id.Value;
        //                    }
        //                    else
        //                    {
        //                        await using var cmdI = new MySqlCommand(insBtnSql, conn, tx);
        //                        cmdI.Parameters.AddWithValue("@TableId", tableId);
        //                        cmdI.Parameters.AddWithValue("@TextKey", btn.LocalizedName.Key);
        //                        await cmdI.ExecuteNonQueryAsync();
        //                        btnId = Convert.ToInt32(cmdI.LastInsertedId);
        //                    }

        //                    // 3b) upsert i18n entries for this button’s text_key
        //                    foreach (var loc in btn.LocalizedName.Values)
        //                    {
        //                        await using var cu = new MySqlCommand(updI18n, conn, tx);
        //                        cu.Parameters.AddWithValue("@TextKey", btn.LocalizedName.Key);
        //                        cu.Parameters.AddWithValue("@LocaleId", loc.LocaleId);
        //                        cu.Parameters.AddWithValue("@Value", loc.Value);
        //                        cu.Parameters.AddWithValue("@SpaceId", spaceId);
        //                        if (await cu.ExecuteNonQueryAsync() == 0)
        //                        {
        //                            await using var ci = new MySqlCommand(insI18n, conn, tx);
        //                            ci.Parameters.AddWithValue("@TextKey", btn.LocalizedName.Key);
        //                            ci.Parameters.AddWithValue("@LocaleId", loc.LocaleId);
        //                            ci.Parameters.AddWithValue("@Value", loc.Value);
        //                            ci.Parameters.AddWithValue("@SpaceId", spaceId);
        //                            await ci.ExecuteNonQueryAsync();
        //                        }
        //                    }
        //                }
        //            }

        //            await tx.CommitAsync();

        //            // 4) reload and return the updated table (with buttons)
        //            var updated = await LoadTeleportTableById(conn, spaceId, tableId);
        //            return Ok(updated);
        //        }
        //        catch (MySqlException ex)
        //        {
        //            await tx.RollbackAsync();
        //            return StatusCode(500, new { error = ex.Message });
        //        }
        //    }

        //    // Helper to load a single teleport table with its localizations
        //    private static async Task<TeleportTableData> LoadTeleportTableById(
        //        MySqlConnection conn,
        //        int spaceId,
        //        int tableId
        //    )
        //    {
        //        const string sql = @"
        //            SELECT
        //                t.id,
        //                t.space_id,
        //                t.is_active,
        //                t.name_key,
        //                i.locale_id,
        //                i.value
        //            FROM teleport_table AS t
        //            LEFT JOIN i18n AS i
        //              ON i.`key`    = t.name_key
        //             AND i.space_id = t.space_id
        //            WHERE t.id = @TableId
        //              AND t.space_id = @SpaceId
        //            ORDER BY i.locale_id;
        //        ";

        //        await using var cmd = new MySqlCommand(sql, conn);
        //        cmd.Parameters.AddWithValue("@TableId", tableId);
        //        cmd.Parameters.AddWithValue("@SpaceId", spaceId);

        //        var map = new Dictionary<int, TeleportTableData>();
        //        await using var reader = await cmd.ExecuteReaderAsync();
        //        TeleportTableData table = null!;
        //        while (await reader.ReadAsync())
        //        {
        //            if (table == null)
        //            {
        //                table = new TeleportTableData
        //                {
        //                    Id = reader.GetInt32("id"),
        //                    SpaceId = reader.GetInt32("space_id"),
        //                    IsActive = reader.GetBoolean("is_active"),
        //                    LocalizedName = new LocalizedName
        //                    {
        //                        Key = reader.GetString("name_key"),
        //                        Values = new List<LocalizedValue>()
        //                    }
        //                };
        //            }

        //            if (!reader.IsDBNull("locale_id"))
        //            {
        //                table.LocalizedName.Values.Add(new LocalizedValue
        //                {
        //                    LocaleId = reader.GetString("locale_id"),
        //                    Value = reader.GetString("value")
        //                });
        //            }
        //        }

        //        return table;
        //    }

        //    [HttpGet("{id}")]
        //    [ProducesResponseType(typeof(SpaceData), StatusCodes.Status200OK)]
        //    [ProducesResponseType(StatusCodes.Status404NotFound)]
        //    public async Task<ActionResult<SpaceData>> GetSpaceById(int id)
        //    {
        //        const string sql = @"
        //    SELECT 
        //        s.id,
        //        s.platform_type_id,
        //        s.entity_id,
        //        s.sku,
        //        s.link,
        //        s.is_published,
        //        s.is_live,
        //        s.description_key,
        //        s.creation_time,
        //        s.modified_time,
        //        s.modified_by,
        //        i.locale_id,
        //        i.value
        //    FROM space s
        //    LEFT JOIN i18n i 
        //        ON i.`key` = s.description_key 
        //        AND i.space_id = s.id
        //    WHERE s.id = @Id
        //    ORDER BY i.locale_id;
        //";

        //        await using var conn = new MySqlConnection(_connectionString);
        //        await conn.OpenAsync();
        //        await using var cmd = new MySqlCommand(sql, conn);
        //        cmd.Parameters.AddWithValue("@Id", id);

        //        await using var reader = await cmd.ExecuteReaderAsync();
        //        SpaceData space = null;
        //        var descValues = new List<LocalizedValue>();

        //        while (await reader.ReadAsync())
        //        {
        //            if (space == null)
        //            {
        //                space = new SpaceData
        //                {
        //                    Id = reader.GetInt32("id"),
        //                    PlatformTypeId = reader.GetInt32("platform_type_id"),
        //                    EntityId = reader.GetInt32("entity_id"),
        //                    Sku = reader.IsDBNull(reader.GetOrdinal("sku")) ? null : reader.GetString("sku"),
        //                    Link = reader.IsDBNull(reader.GetOrdinal("link")) ? null : reader.GetString("link"),
        //                    IsPublished = reader.GetBoolean("is_published"),
        //                    IsLive = reader.GetBoolean("is_live"),
        //                    CreationTime = reader.GetDateTime("creation_time"),
        //                    ModifiedTime = reader.GetDateTime("modified_time"),
        //                    ModifiedBy = reader.GetString("modified_by"),
        //                    LocalizedDescription = new LocalizedName
        //                    {
        //                        Key = reader.GetString("description_key"),
        //                        Values = descValues
        //                    }
        //                };
        //            }

        //            if (!reader.IsDBNull(reader.GetOrdinal("locale_id")))
        //            {
        //                descValues.Add(new LocalizedValue
        //                {
        //                    LocaleId = reader.GetString("locale_id"),
        //                    Value = reader.IsDBNull(reader.GetOrdinal("value")) ? null : reader.GetString("value")
        //                });
        //            }
        //        }

        //        return space == null ? NotFound() : Ok(space);
        //    }

        //    [HttpPost("")]
        //    [ProducesResponseType(typeof(SpaceData), StatusCodes.Status201Created)]
        //    [ProducesResponseType(StatusCodes.Status400BadRequest)]
        //    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        //    public async Task<ActionResult<SpaceData>> CreateSpace([FromBody] Space spaceDto)
        //    {
        //        if (spaceDto == null || spaceDto.LocalizedDescription == null)
        //            return BadRequest();

        //        await using var conn = new MySqlConnection(_connectionString);
        //        await conn.OpenAsync();
        //        await using var tx = await conn.BeginTransactionAsync();

        //        try
        //        {
        //            const string insertSql = @"
        //        INSERT INTO space (
        //            platform_type_id, entity_id, sku, link,
        //            is_published, is_live, description_key,
        //            creation_time, modified_time, modified_by
        //        )
        //        VALUES (
        //            @PlatformTypeId, @EntityId, @Sku, @Link,
        //            @IsPublished, @IsLive, @DescriptionKey,
        //            @CreationTime, @ModifiedTime, @ModifiedBy
        //        );
        //        SELECT LAST_INSERT_ID();";

        //            int newId;
        //            await using (var cmd = new MySqlCommand(insertSql, conn, tx))
        //            {
        //                cmd.Parameters.AddWithValue("@PlatformTypeId", spaceDto.PlatformTypeId);
        //                cmd.Parameters.AddWithValue("@EntityId", spaceDto.EntityId);
        //                cmd.Parameters.AddWithValue("@Sku", (object?)spaceDto.Sku ?? DBNull.Value);
        //                cmd.Parameters.AddWithValue("@Link", (object?)spaceDto.Link ?? DBNull.Value);
        //                cmd.Parameters.AddWithValue("@IsPublished", spaceDto.IsPublished);
        //                cmd.Parameters.AddWithValue("@IsLive", spaceDto.IsLive);
        //                cmd.Parameters.AddWithValue("@DescriptionKey", spaceDto.LocalizedDescription.Key);
        //                cmd.Parameters.AddWithValue("@CreationTime", DateTime.UtcNow);
        //                cmd.Parameters.AddWithValue("@ModifiedTime", DateTime.UtcNow);
        //                cmd.Parameters.AddWithValue("@ModifiedBy", spaceDto.ModifiedBy ?? "system");

        //                newId = Convert.ToInt32(await cmd.ExecuteScalarAsync());
        //            }

        //            const string insertI18nSql = @"
        //        INSERT INTO i18n (`key`, locale_id, value, space_id)
        //        VALUES (@Key, @LocaleId, @Value, @SpaceId);";

        //            foreach (var loc in spaceDto.LocalizedDescription.Values)
        //            {
        //                await using var cmdI18n = new MySqlCommand(insertI18nSql, conn, tx);
        //                cmdI18n.Parameters.AddWithValue("@Key", spaceDto.LocalizedDescription.Key);
        //                cmdI18n.Parameters.AddWithValue("@LocaleId", loc.LocaleId);
        //                cmdI18n.Parameters.AddWithValue("@Value", (object?)loc.Value ?? DBNull.Value);
        //                cmdI18n.Parameters.AddWithValue("@SpaceId", newId);
        //                await cmdI18n.ExecuteNonQueryAsync();
        //            }

        //            await tx.CommitAsync();

        //            var created = await LoadSpaceById(conn, newId);
        //            return CreatedAtAction(nameof(GetSpaceById), new { id = newId }, created);
        //        }
        //        catch (MySqlException ex)
        //        {
        //            await tx.RollbackAsync();
        //            return StatusCode(500, new { error = ex.Message });
        //        }
        //    }

        /*private static async Task<SpaceData> LoadSpaceById(MySqlConnection conn, int spaceId)
        {
            const string sql = @"
        SELECT 
            s.id,
            s.platform_type_id,
            s.entity_id,
            s.sku,
            s.link,
            s.is_published,
            s.is_live,
            s.description_key,
            s.creation_time,
            s.modified_time,
            s.modified_by,
            i.locale_id,
            i.value
        FROM space s
        LEFT JOIN i18n i
            ON i.`key` = s.description_key AND i.space_id = s.id
        WHERE s.id = @SpaceId
        ORDER BY i.locale_id;
    ";

            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@SpaceId", spaceId);

            await using var reader = await cmd.ExecuteReaderAsync();
            SpaceData space = null;
            var localizedDescriptions = new List<LocalizedValue>();

            while (await reader.ReadAsync())
            {
                if (space == null)
                {
                    space = new SpaceData
                    {
                        Id = reader.GetInt32("id"),
                        PlatformTypeId = reader.GetInt32("platform_type_id"),
                        EntityId = reader.GetInt32("entity_id"),
                        Sku = reader.IsDBNull(reader.GetOrdinal("sku")) ? null : reader.GetString("sku"),
                        Link = reader.IsDBNull(reader.GetOrdinal("link")) ? null : reader.GetString("link"),
                        IsPublished = reader.GetBoolean("is_published"),
                        IsLive = reader.GetBoolean("is_live"),
                        CreationTime = reader.GetDateTime("creation_time"),
                        ModifiedTime = reader.GetDateTime("modified_time"),
                        ModifiedBy = reader.GetString("modified_by"),
                        LocalizedDescription = new LocalizedName
                        {
                            Key = reader.GetString("description_key"),
                            Values = localizedDescriptions
                        }
                    };
                }

                if (!reader.IsDBNull(reader.GetOrdinal("locale_id")))
                {
                    localizedDescriptions.Add(new LocalizedValue
                    {
                        LocaleId = reader.GetString("locale_id"),
                        Value = reader.IsDBNull(reader.GetOrdinal("value")) ? null : reader.GetString("value")
                    });
                }
            }

            return space;
        }*/




    }
}
