// <copyright file="TeleportController.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Saad Sohail</author>
// <date>07/23/2025</date>
// <summary>Controller to handle teleportable routes</summary>

using GMS.TifoXRCoreWebAPI.Models;
using GMS.TifoXRCoreWebAPI.Repositories.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace GMS.TifoXRCoreWebAPI.Controllers
{
    [Route("api/space")]
    [ApiController]
    public class TeleportTableController : ControllerBase
    {
        private readonly ITeleportTableRepository _teleportRepository;
        public TeleportTableController(ITeleportTableRepository teleportRepository)
        {
            _teleportRepository = teleportRepository;
        }

        /// <summary>
        /// GET /api/space/{spaceId}/teleport_tables
        /// Returns all teleport tables in a space, with their localized names and buttons.
        /// </summary>
        [HttpGet("{spaceId}/teleport_tables")]
        [ProducesResponseType(typeof(TeleportTableData), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<TeleportTableData>> GetTeleportTablesBySpace(
            [FromRoute] int spaceId)
        {
            try
            {
                var table = await _teleportRepository.GetTeleportTableBySpaceAsync(spaceId);
                if (table == null)
                    return NotFound();
                return Ok(table);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// POST /api/space/{spaceId}/teleport_table
        /// Creates a new teleport table, including its name and buttons with localized values.
        /// </summary>
        [HttpPost("{spaceId}/teleport_table")]
        [ProducesResponseType(typeof(TeleportTableData), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<TeleportTableData>> CreateTeleportTable(
            [FromRoute] int spaceId,
            [FromBody] TeleportTableCreateDto dto)
        {
            if (dto == null)
                return BadRequest();

            try
            {
                var created = await _teleportRepository.CreateTeleportTableAsync(spaceId, dto);
                if (created == null)
                    return StatusCode(500, new { error = "Creation failed." });

                return Created($"/api/space/{spaceId}/teleport_table/{created.Id}", created);
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
            Console.WriteLine($"spaceId: {spaceId}, tableId: {tableId}");

            if (dto == null)
                return BadRequest();

            try
            {
                var updated = await _teleportRepository.UpdateTeleportTableAsync(spaceId, tableId, dto);
                if (updated == null)
                    return NotFound();

                return Ok(updated);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpDelete("{spaceId}/teleport_table/{tableId}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteTeleportTable(
            [FromRoute] int spaceId,
            [FromRoute] int tableId)
        {
            try
            {
                var ok = await _teleportRepository.DeleteTeleportTableAsync(spaceId, tableId);
                if (!ok) return NotFound();
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpDelete("{spaceId}/teleport_tables")]
        [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteTeleportTablesBySpace(
            [FromRoute] int spaceId)
        {
            try
            {
                var count = await _teleportRepository.DeleteTeleportTablesBySpaceAsync(spaceId);
                return Ok(new { deleted = count });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpDelete("{spaceId}/teleport_table/{tableId}/button/{buttonId}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteTeleportTableButton(
            [FromRoute] int spaceId,
            [FromRoute] int tableId,
            [FromRoute] int buttonId)
        {
            try
            {
                var ok = await _teleportRepository.DeleteTeleportTableButtonAsync(buttonId, tableId);
                if (!ok) return NotFound();
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

    }
}
