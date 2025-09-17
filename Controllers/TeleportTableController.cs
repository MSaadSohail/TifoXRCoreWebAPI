// <copyright file="TeleportController.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Saad Sohail</author>
// <date>07/23/2025</date>
// <summary>Controller to handle teleportable routes</summary>

using Microsoft.AspNetCore.Mvc;
using GMS.TifoXRCoreWebAPI.Models;
using GMS.TifoXRCoreWebAPI.Repositories;
using GMS.TifoXRCoreWebAPI.Middleware.Errors;

namespace GMS.TifoXRCoreWebAPI.Controllers
{
    [Route("api/space")]
    [ApiController]
    public class TeleportTableController(ITeleportTableRepository teleportRepository) : ControllerBase
    {
        private readonly ITeleportTableRepository _teleportRepository = teleportRepository;

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
                throw ErrorService.Exception(
                    ErrorType.Argument,
                    nameof(GetTeleportTablesBySpace),
                    ErrorMessages.Validation.PositiveIntRequired,
                    paramName: nameof(spaceId),
                    parameters: new { spaceId });

            var table = await _teleportRepository.GetTeleportTableBySpaceAsync(spaceId);

            // Not found (404)
            if (table is null)
                throw ErrorService.Exception(
                    ErrorType.NotFound,
                    nameof(GetTeleportTablesBySpace),
                    ErrorMessages.Http.NotFound,
                    parameters: new { spaceId });

            return Ok(table);
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
                throw ErrorService.Exception(
                    ErrorType.Argument,
                    nameof(CreateTeleportTable),
                    ErrorMessages.Validation.PositiveIntRequired,
                    paramName: nameof(spaceId),
                    parameters: new { spaceId });

            if (dto is null)
                throw ErrorService.Exception(
                    ErrorType.ArgumentNull,
                    nameof(CreateTeleportTable),
                    ErrorMessages.Validation.MissingParameter,
                    paramName: nameof(dto),
                    parameters: new { spaceId });

            var created = await _teleportRepository.CreateTeleportTableAsync(spaceId, dto);

            if (created is null)
                throw ErrorService.Exception(
                    ErrorType.InvalidOperation,
                    nameof(CreateTeleportTable),
                    ErrorMessages.Http.Conflict,
                    parameters: new { spaceId });

            return CreatedAtAction(
                nameof(GetTeleportTablesBySpace),
                new { spaceId, tableId = created.Id },
                created);
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
                throw ErrorService.Exception(
                    ErrorType.Argument,
                    nameof(UpdateTeleportTableById),
                    ErrorMessages.Validation.PositiveIntRequired,
                    paramName: nameof(spaceId),
                    parameters: new { spaceId, tableId });

            if (tableId <= 0)
                throw ErrorService.Exception(
                    ErrorType.Argument,
                    nameof(UpdateTeleportTableById),
                    ErrorMessages.Validation.PositiveIntRequired,
                    paramName: nameof(tableId),
                    parameters: new { spaceId, tableId });

            if (dto is null)
                throw ErrorService.Exception(
                    ErrorType.ArgumentNull,
                    nameof(UpdateTeleportTableById),
                    ErrorMessages.Validation.MissingParameter,
                    paramName: nameof(dto),
                    parameters: new { spaceId, tableId });

            if (dto.Buttons is null || !dto.Buttons.Any())
                throw ErrorService.Exception(
                    ErrorType.Argument,
                    nameof(UpdateTeleportTableById),
                    ErrorMessages.Validation.EmptyCollection,
                    paramName: nameof(dto.Buttons),
                    parameters: new { spaceId, tableId });

            if (dto.Buttons.Any(b => !b.Id.HasValue || b.Id <= 0))
                throw ErrorService.Exception(
                    ErrorType.Argument,
                    nameof(UpdateTeleportTableById),
                    ErrorMessages.Validation.MustHaveValidIds,
                    paramName: nameof(dto.Buttons),
                    parameters: new { spaceId, tableId, ids = dto.Buttons.Select(b => b.Id) });

            if (dto.LocalizedPairs?.Values is null || !dto.LocalizedPairs.Values.Any())
                throw ErrorService.Exception(
                    ErrorType.Argument,
                    nameof(UpdateTeleportTableById),
                    ErrorMessages.Validation.EmptyCollection,
                    paramName: nameof(dto.LocalizedPairs),
                    parameters: new { spaceId, tableId });

            var updated = await _teleportRepository.UpdateTeleportTableAsync(spaceId, tableId, dto);

            if (updated is null)
                throw ErrorService.Exception(
                    ErrorType.NotFound,
                    nameof(UpdateTeleportTableById),
                    ErrorMessages.Http.NotFound,
                    parameters: new { spaceId, tableId });

            return Ok(updated);
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
                throw ErrorService.Exception(
                    ErrorType.Argument,
                    nameof(DeleteTeleportTable),
                    ErrorMessages.Validation.PositiveIntRequired,
                    paramName: nameof(spaceId),
                    parameters: new { spaceId, tableId });

            if (tableId <= 0)
                throw ErrorService.Exception(
                    ErrorType.Argument,
                    nameof(DeleteTeleportTable),
                    ErrorMessages.Validation.PositiveIntRequired,
                    paramName: nameof(tableId),
                    parameters: new { spaceId, tableId });

            var ok = await _teleportRepository.DeleteTeleportTableAsync(spaceId, tableId);

            if (!ok)
                throw ErrorService.Exception(
                    ErrorType.NotFound,
                    nameof(DeleteTeleportTable),
                    ErrorMessages.Http.NotFound,
                    parameters: new { spaceId, tableId });

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
                throw ErrorService.Exception(
                    ErrorType.Argument,
                    nameof(DeleteTeleportTablesBySpace),
                    ErrorMessages.Validation.PositiveIntRequired,
                    paramName: nameof(spaceId),
                    parameters: new { spaceId });

            var count = await _teleportRepository.DeleteTeleportTablesBySpaceAsync(spaceId);

            return Ok(new
            {
                message = $"Teleport Tables Records Deleted: {count}."
            });
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
                throw ErrorService.Exception(
                    ErrorType.Argument,
                    nameof(DeleteTeleportTableButton),
                    ErrorMessages.Validation.PositiveIntRequired,
                    paramName: nameof(spaceId),
                    parameters: new { spaceId, tableId, buttonId });

            if (tableId <= 0)
                throw ErrorService.Exception(
                    ErrorType.Argument,
                    nameof(DeleteTeleportTableButton),
                    ErrorMessages.Validation.PositiveIntRequired,
                    paramName: nameof(tableId),
                    parameters: new { spaceId, tableId, buttonId });

            if (buttonId <= 0)
                throw ErrorService.Exception(
                    ErrorType.Argument,
                    nameof(DeleteTeleportTableButton),
                    ErrorMessages.Validation.PositiveIntRequired,
                    paramName: nameof(buttonId),
                    parameters: new { spaceId, tableId, buttonId });

            var ok = await _teleportRepository.DeleteTeleportTableButtonAsync(buttonId, tableId);

            if (!ok)
                throw ErrorService.Exception(
                    ErrorType.NotFound,
                    nameof(DeleteTeleportTableButton),
                    ErrorMessages.Http.NotFound,
                    parameters: new { spaceId, tableId, buttonId });

            return Ok(new { message = "Teleport Table Button deleted successfully." });
        }

        #endregion
    }
}
