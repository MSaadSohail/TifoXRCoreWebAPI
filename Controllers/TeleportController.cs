using Microsoft.AspNetCore.Mvc;
using TifoXRCoreWebAPI.Models;
using TifoXRCoreWebAPI.Repositories.Interfaces;


namespace TifoXRCoreWebAPI.Controllers
{
    [Route("api/space")]
    [ApiController]
    public class TeleportController : ControllerBase
    {
        private readonly ITeleportRepository _teleportRepository;
        public TeleportController(ITeleportRepository teleportRepository)
        {
            _teleportRepository = teleportRepository;
        }

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
                var tables = await _teleportRepository.GetTeleportTablesBySpaceAsync(spaceId);
                if (tables == null || tables.Count == 0)
                    return NotFound();
                return Ok(tables);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }


        /// <summary>
        /// PUT /api/space/{spaceId}/teleport_table/{tableId}
        /// Updates a teleport table, including its name and buttons with localized values.
        /// </summary>
        [HttpPut("{spaceId}/teleport_table/{tableId}")]
        [ProducesResponseType(typeof(TeleportTableData), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<TeleportTableData>> UpdateTeleportTableById(
           [FromRoute] int spaceId,
           [FromRoute] int tableId,
           [FromBody] TeleportTableUpdateDto dto)
        {
            if (dto == null)
                return BadRequest();

            try
            {
                var updated = await _teleportRepository.UpdateTeleportTableAsync(spaceId, tableId, dto);
                if (updated == null)
                    return NotFound();

                return Ok(updated);
            }
            catch (MySqlConnector.MySqlException ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}
