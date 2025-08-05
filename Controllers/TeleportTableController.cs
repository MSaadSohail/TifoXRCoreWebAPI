// <copyright file="TeleportController.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Saad Sohail</author>
// <date>07/23/2025</date>
// <summary>Controller to handle teleportable routes</summary>

using GMS.TifoXRCoreWebAPI.Models;
using GMS.TifoXRCoreWebAPI.Repositories.Interfaces;
using Microsoft.AspNetCore.Mvc;
using TifoXRCoreWebAPI.Middleware;

namespace GMS.TifoXRCoreWebAPI.Controllers
{
    [Route("api/space")]
    [ApiController]
    public class TeleportTableController(ITeleportTableRepository teleportRepository, IHostEnvironment env) : ControllerBase
    {
        private readonly ITeleportTableRepository _teleportRepository = teleportRepository;
        private readonly IHostEnvironment _env = env;

        #region GET

        /// <summary>
        /// GET /api/space/{spaceId}/teleport_tables
        /// Returns all teleport tables in a space, with their localized names and buttons.
        /// </summary>
        [HttpGet("{spaceId}/teleport_table")]
        [ProducesResponseType(typeof(TeleportTableData), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<TeleportTableData>> GetTeleportTablesBySpace([FromRoute] int spaceId)
        {
            if (spaceId <= 0)
                // Argument validation (Bad Request)
                throw new ArgumentException(GlobalException.FormatExceptionMessage(
                    "spaceId must be a positive integer.",
                    nameof(GetTeleportTablesBySpace),
                    new { spaceId })
                );

            var table = await _teleportRepository.GetTeleportTableBySpaceAsync(spaceId);

            // Not found (404)
            return table == null
                ? throw new KeyNotFoundException(GlobalException.FormatExceptionMessage(
                    $"Teleport table not found.",
                    nameof(GetTeleportTablesBySpace),
                    new { spaceId }))
                : (ActionResult<TeleportTableData>)Ok(table);
        }

        #endregion

        #region POST

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
            if (spaceId <= 0)
                throw new ArgumentException(GlobalException.FormatExceptionMessage(
                    "spaceId must be a positive integer.",
                    nameof(CreateTeleportTable),
                    new { spaceId }
                ));

            ArgumentNullException.ThrowIfNull(dto);

            var created = await _teleportRepository.CreateTeleportTableAsync(spaceId, dto);

            return created == null
                ? throw new InvalidOperationException(GlobalException.FormatExceptionMessage(
                    "Creation failed.",
                    nameof(CreateTeleportTable),
                    new { spaceId, dto }
                ))
                : (ActionResult<TeleportTableData>)CreatedAtAction(
                    nameof(GetTeleportTablesBySpace),               // Name of your GET action
                    new { spaceId, tableId = created.Id },          // Route parameters for GET action
                    created                                         // The response body
                );
        }

        #endregion

        #region PUT

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
            if (spaceId <= 0)
                throw new ArgumentException($"spaceId: {spaceId} must be a positive integer.", nameof(spaceId));
            if (tableId <= 0)
                throw new ArgumentException("tableId must be a positive integer.", nameof(tableId));
            if (dto == null)
                throw new ArgumentNullException(nameof(dto));
            if (dto.Buttons == null || !dto.Buttons.Any())
                throw new ArgumentException("Buttons cannot be empty.", nameof(dto.Buttons));
            if (dto.Buttons.Any(b => !b.Id.HasValue || b.Id <= 0))
                throw new ArgumentException("Each button must have a valid (positive) Id.", nameof(dto.Buttons));
            if (dto.LocalizedPairs == null || dto.LocalizedPairs.Values == null || !dto.LocalizedPairs.Values.Any())
                throw new ArgumentException("LocalizedPairs.Values cannot be empty.", nameof(dto.LocalizedPairs.Values));

            var updated = await _teleportRepository.UpdateTeleportTableAsync(spaceId, tableId, dto);

            return updated == null 
                ? throw new KeyNotFoundException("Teleport table not found.") 
                : (ActionResult<TeleportTableData>)Ok(updated);
        }

        #endregion

        #region DELETE

        /// <summary>
        /// Deletes a specific teleport table by spaceId and tableId.
        /// </summary>
        [HttpDelete("{spaceId}/teleport_table/{tableId}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteTeleportTable(int spaceId, int tableId)
        {
            if (spaceId <= 0)
                throw new ArgumentException("spaceId must be a positive integer.", nameof(spaceId));
            if (tableId <= 0)
                throw new ArgumentException("tableId must be a positive integer.", nameof(tableId));

            var ok = await _teleportRepository.DeleteTeleportTableAsync(spaceId, tableId);

            if (!ok)
                throw new KeyNotFoundException("Teleport table not found.");

            return NoContent();
        }

        /// <summary>
        /// Deletes all teleport tables in the specified space.
        /// </summary>
        [HttpDelete("{spaceId}/teleport_tables")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteTeleportTablesBySpace(int spaceId)
        {
            if (spaceId <= 0)
                throw new ArgumentException("spaceId must be a positive integer.", nameof(spaceId));

            var count = await _teleportRepository.DeleteTeleportTablesBySpaceAsync(spaceId);
            
            return Ok(new { deleted = count });
        }

        /// <summary>
        /// Deletes a specific button from a teleport table by spaceId, tableId, and buttonId.
        /// </summary>
        [HttpDelete("{spaceId}/teleport_table/{tableId}/button/{buttonId}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteTeleportTableButton(int spaceId, int tableId, int buttonId)
        {
            if (spaceId <= 0)
                throw new ArgumentException("spaceId must be a positive integer.", nameof(spaceId));
            if (tableId <= 0)
                throw new ArgumentException("tableId must be a positive integer.", nameof(tableId));
            if (buttonId <= 0)
                throw new ArgumentException("buttonId must be a positive integer.", nameof(buttonId));

            var ok = await _teleportRepository.DeleteTeleportTableButtonAsync(buttonId, tableId);
            
            if (!ok)
                throw new KeyNotFoundException("Teleport table button not found.");
            
            return NoContent();
        }

        #endregion
    }
}
