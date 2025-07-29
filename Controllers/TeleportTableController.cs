// <copyright file="TeleportController.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Saad Sohail</author>
// <date>07/23/2025</date>
// <summary>Controller to handle teleport able routes</summary>

using Microsoft.AspNetCore.Mvc;
using GMS.TifoXRCoreWebAPI.Models;
using GMS.TifoXRCoreWebAPI.Repositories.Interfaces;

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
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new { error = ex.Message }
                );
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

            if (dto.LocalizedName == null)
                return BadRequest("LocalizedName is required");

            if (dto.Buttons == null)
                return BadRequest("Buttons are required");

            try
            {
                var updated = await _teleportRepository.UpdateTeleportTableAsync(spaceId, tableId, dto);
                if (updated == null)
                    return NotFound();

                return Ok(updated);
            }
            catch (Exception ex)
            {
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new { error = ex.Message }
                );
            }
        }
    }
}
