// <copyright file="PortalController.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Saad Sohail</author>
// <date>07/23/2025</date>
// <summary>Controller to handle portal routes</summary>

using Microsoft.AspNetCore.Mvc;
//
using GMS.TifoXRCoreWebAPI.Models;
using GMS.TifoXRCoreWebAPI.Repositories;
using GMS.TifoXRCoreWebAPI.Middleware.Errors;

namespace GMS.TifoXRCoreWebAPI.Controllers
{
    [Route("api/space/{spaceId}/portal")]
    [ApiController]
    public class PortalController(IPortalRepository portalRepository) : ControllerBase
    {
        private readonly IPortalRepository _portalRepository = portalRepository;

        #region GET

        /// <summary>
        /// GET /api/space/{spaceId}/portal
        /// Gets all portals for the specified space.
        /// </summary>
        /// <param name="spaceId">The ID of the space.</param>
        /// <returns>A list of portals in the given space.</returns>
        [HttpGet]
        [ProducesResponseType(typeof(List<PortalModel>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<List<PortalModel>>> GetPortalsBySpace([FromRoute] int spaceId)
        {
            if (spaceId <= 0)
                throw ErrorService.Exception(
                    ErrorType.Argument,
                    nameof(GetPortalsBySpace),
                    ErrorMessages.Validation.PositiveIntRequired,
                    paramName: nameof(spaceId),
                    parameters: new { spaceId });

            var portals = await _portalRepository.GetPortalsBySpaceAsync(spaceId);

            if (portals == null || portals.Count == 0)
                throw ErrorService.Exception(
                    ErrorType.NotFound,
                    nameof(GetPortalsBySpace),
                    ErrorMessages.Http.NotFound,
                    parameters: new { spaceId });

            return Ok(portals);
        }

        /// <summary>
        /// GET /api/space/{spaceId}/booth/{boothId}/portals
        /// Gets all portals for the specified booth in a space.
        /// </summary>
        /// <param name="spaceId">The ID of the space.</param>
        /// <param name="boothId">The ID of the booth.</param>
        /// <returns>A list of portals under the booth in the space.</returns>
        [HttpGet("~/api/space/{spaceId}/booth/{boothId}/portals")]
        [ProducesResponseType(typeof(List<PortalModel>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<List<PortalModel>>> GetPortalsByBooth(
            [FromRoute] int spaceId,
            [FromRoute] int boothId
        )
        {
            if (spaceId <= 0)
                throw ErrorService.Exception(
                    ErrorType.Argument,
                    nameof(GetPortalsByBooth),
                    ErrorMessages.Validation.PositiveIntRequired,
                    paramName: nameof(spaceId),
                    parameters: new { spaceId, boothId });

            if (boothId <= 0)
                throw ErrorService.Exception(
                    ErrorType.Argument,
                    nameof(GetPortalsByBooth),
                    ErrorMessages.Validation.PositiveIntRequired,
                    paramName: nameof(boothId),
                    parameters: new { spaceId, boothId });

            var portals = await _portalRepository.GetPortalsByBoothAsync(spaceId, boothId);

            if (portals == null || portals.Count == 0)
                throw ErrorService.Exception(
                    ErrorType.NotFound,
                    nameof(GetPortalsByBooth),
                    ErrorMessages.Http.NotFound,
                    parameters: new { spaceId, boothId });

            return Ok(portals);
        }

        /// <summary>
        /// GET /api/space/{spaceId}/portal/{portalId}
        /// Gets a specific portal by its ID within a space.
        /// </summary>
        /// <param name="spaceId">The ID of the space.</param>
        /// <param name="portalId">The ID of the portal.</param>
        /// <returns>The portal data if found.</returns>
        [HttpGet("{portalId}")]
        [ProducesResponseType(typeof(PortalModel), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<PortalModel>> GetPortalById(
            [FromRoute] int spaceId,
            [FromRoute] int portalId
        )
        {
            if (spaceId <= 0)
                throw ErrorService.Exception(
                    ErrorType.Argument,
                    nameof(GetPortalById),
                    ErrorMessages.Validation.PositiveIntRequired,
                    paramName: nameof(spaceId),
                    parameters: new { spaceId, portalId });

            if (portalId <= 0)
                throw ErrorService.Exception(
                    ErrorType.Argument,
                    nameof(GetPortalById),
                    ErrorMessages.Validation.PositiveIntRequired,
                    paramName: nameof(portalId),
                    parameters: new { spaceId, portalId });

            var portal = await _portalRepository.GetPortalByIdAsync(spaceId, portalId);

            if (portal is null)
                throw ErrorService.Exception(
                    ErrorType.NotFound,
                    nameof(GetPortalById),
                    ErrorMessages.Http.NotFound,
                    parameters: new { spaceId, portalId });

            return Ok(portal);
        }

        #endregion

        #region POST

        /// <summary>
        /// POST /api/space/{spaceId}/portal
        /// Creates a new portal in the specified space.
        /// </summary>
        /// <param name="spaceId">The ID of the space.</param>
        /// <param name="portalDto">The portal creation data.</param>
        /// <returns>The created portal data.</returns>
        [HttpPost("portal")]
        [ProducesResponseType(typeof(PortalModel), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<PortalModel>> CreatePortal(
            [FromRoute] int spaceId,
            [FromBody] PortalCreateDto portalDto
        )
        {
            if (spaceId <= 0)
                throw ErrorService.Exception(
                    ErrorType.Argument,
                    nameof(CreatePortal),
                    ErrorMessages.Validation.PositiveIntRequired,
                    paramName: nameof(spaceId),
                    parameters: new { spaceId });

            if (portalDto is null)
                throw ErrorService.Exception(
                    ErrorType.ArgumentNull,
                    nameof(CreatePortal),
                    ErrorMessages.Validation.MissingParameter,
                    paramName: nameof(portalDto),
                    parameters: new { spaceId });

            if (portalDto.LocalizedPairs == null || portalDto.LocalizedPairs.Values == null || portalDto.LocalizedPairs.Values.Count == 0)
                throw ErrorService.Exception(
                    ErrorType.Argument,
                    nameof(CreatePortal),
                    ErrorMessages.Validation.EmptyCollection,
                    paramName: nameof(portalDto.LocalizedPairs),
                    parameters: new { spaceId, field = "LocalizedPairs.Values" });

            var created = await _portalRepository.CreatePortalAsync(spaceId, portalDto);

            if (created is null)
                throw ErrorService.Exception(
                    ErrorType.InvalidOperation,
                    nameof(CreatePortal),
                    ErrorMessages.Http.Conflict,
                    parameters: new { spaceId });
            return CreatedAtAction(
                nameof(GetPortalById),
                new { spaceId, portalId = created.PortalId },
                created);
        }

        #endregion

        #region PUT

        /// <summary>
        /// PUT /api/space/{spaceId}/portal/{portalId}
        /// Updates an existing portal in the specified space.
        /// </summary>
        /// <param name="spaceId">The ID of the space.</param>
        /// <param name="portalId">The ID of the portal.</param>
        /// <param name="portalDto">The portal update data.</param>
        /// <returns>The updated portal data.</returns>
        [HttpPut("{portalId}")]
        [ProducesResponseType(typeof(PortalModel), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<PortalModel>> UpdatePortal(
            [FromRoute] int spaceId,
            [FromRoute] int portalId,
            [FromBody] PortalUpdateDto portalDto)
        {
            if (spaceId <= 0)
                throw ErrorService.Exception(
                    ErrorType.Argument,
                    nameof(UpdatePortal),
                    ErrorMessages.Validation.PositiveIntRequired,
                    paramName: nameof(spaceId),
                    parameters: new { spaceId, portalId });

            if (portalId <= 0)
                throw ErrorService.Exception(
                    ErrorType.Argument,
                    nameof(UpdatePortal),
                    ErrorMessages.Validation.PositiveIntRequired,
                    paramName: nameof(portalId),
                    parameters: new { spaceId, portalId });

            if (portalDto is null)
                throw ErrorService.Exception(
                    ErrorType.ArgumentNull,
                    nameof(UpdatePortal),
                    ErrorMessages.Validation.MissingParameter,
                    paramName: nameof(portalDto),
                    parameters: new { spaceId, portalId });

            if (portalDto.LocalizedPairs == null)
                throw ErrorService.Exception(
                    ErrorType.Argument,
                    nameof(UpdatePortal),
                    ErrorMessages.Validation.MissingParameter,
                    paramName: nameof(portalDto.LocalizedPairs),
                    parameters: new { spaceId, portalId });

            if (string.IsNullOrWhiteSpace(portalDto.LocalizedPairs.Key))
                throw ErrorService.Exception(
                    ErrorType.Argument,
                    nameof(UpdatePortal),
                    ErrorMessages.Validation.InvalidFormat,
                    paramName: nameof(portalDto.LocalizedPairs.Key),
                    parameters: new { spaceId, portalId });

            if (portalDto.LocalizedPairs.Values == null || !portalDto.LocalizedPairs.Values.Any())
                throw ErrorService.Exception(
                    ErrorType.Argument,
                    nameof(UpdatePortal),
                    ErrorMessages.Validation.EmptyCollection,
                    paramName: nameof(portalDto.LocalizedPairs),
                    parameters: new { spaceId, portalId, field = "LocalizedPairs.Values" });

            var updated = await _portalRepository.UpdatePortalAsync(spaceId, portalId, portalDto);

            if (updated == null)
                throw ErrorService.Exception(
                    ErrorType.NotFound,
                    nameof(UpdatePortal),
                    ErrorMessages.Http.NotFound,
                    parameters: new { spaceId, portalId });

            return Ok(updated);
        }

        /// <summary>
        /// PUT /api/space/{spaceId}/booth/{boothId}/portal/{portalId}
        /// Updates a portal belonging to a specific booth within a space.
        /// </summary>
        /// <param name="spaceId">The ID of the space.</param>
        /// <param name="boothId">The ID of the booth.</param>
        /// <param name="portalId">The ID of the portal.</param>
        /// <param name="dto">The portal update data.</param>
        /// <returns>The updated portal data.</returns>
        [HttpPut("~/api/space/{spaceId}/booth/{boothId}/portal/{portalId}")]
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
            if (spaceId <= 0)
                throw ErrorService.Exception(
                    ErrorType.Argument,
                    nameof(UpdatePortalData),
                    ErrorMessages.Validation.PositiveIntRequired,
                    paramName: nameof(spaceId),
                    parameters: new { spaceId, boothId, portalId });

            if (boothId <= 0)
                throw ErrorService.Exception(
                    ErrorType.Argument,
                    nameof(UpdatePortalData),
                    ErrorMessages.Validation.PositiveIntRequired,
                    paramName: nameof(boothId),
                    parameters: new { spaceId, boothId, portalId });

            if (portalId <= 0)
                throw ErrorService.Exception(
                    ErrorType.Argument,
                    nameof(UpdatePortalData),
                    ErrorMessages.Validation.PositiveIntRequired,
                    paramName: nameof(portalId),
                    parameters: new { spaceId, boothId, portalId });

            if (dto is null)
                throw ErrorService.Exception(
                    ErrorType.ArgumentNull,
                    nameof(UpdatePortalData),
                    ErrorMessages.Validation.MissingParameter,
                    paramName: nameof(dto),
                    parameters: new { spaceId, boothId, portalId });

            if (dto.LocalizedPairs == null)
                throw ErrorService.Exception(
                    ErrorType.Argument,
                    nameof(UpdatePortalData),
                    ErrorMessages.Validation.MissingParameter,
                    paramName: nameof(dto.LocalizedPairs),
                    parameters: new { spaceId, boothId, portalId });


            if (string.IsNullOrWhiteSpace(dto.LocalizedPairs.Key))
                throw ErrorService.Exception(
                    ErrorType.Argument,
                    nameof(UpdatePortalData),
                    ErrorMessages.Validation.InvalidFormat,
                    paramName: nameof(dto.LocalizedPairs.Key),
                    parameters: new { spaceId, boothId, portalId });

            if (dto.LocalizedPairs.Values == null || !dto.LocalizedPairs.Values.Any())
                throw ErrorService.Exception(
                    ErrorType.Argument,
                    nameof(UpdatePortalData),
                    ErrorMessages.Validation.EmptyCollection,
                    paramName: nameof(dto.LocalizedPairs),
                    parameters: new { spaceId, boothId, portalId, field = "LocalizedPairs.Values" });

            var updated = await _portalRepository.UpdatePortalAsync(spaceId, boothId, portalId, dto);

            if (updated == null)
                throw ErrorService.Exception(
                    ErrorType.NotFound,
                    nameof(UpdatePortalData),
                    ErrorMessages.Http.NotFound,
                    parameters: new { spaceId, boothId, portalId });

            return Ok(updated);
        }

        #endregion

        #region DELETE

        /// <summary>
        /// DELETE /api/space/{spaceId}/portal/{portalId}
        /// Deletes a portal from the specified space.
        /// </summary>
        /// <param name="spaceId">The ID of the space.</param>
        /// <param name="portalId">The ID of the portal.</param>
        /// <returns>No content on success.</returns>
        [HttpDelete("{portalId}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeletePortal(
            [FromRoute] int spaceId,
            [FromRoute] int portalId)
        {
            if (spaceId <= 0)
                throw ErrorService.Exception(
                    ErrorType.Argument,
                    nameof(DeletePortal),
                    ErrorMessages.Validation.PositiveIntRequired,
                    paramName: nameof(spaceId),
                    parameters: new { spaceId, portalId });

            if (portalId <= 0)
                throw ErrorService.Exception(
                    ErrorType.Argument,
                    nameof(DeletePortal),
                    ErrorMessages.Validation.PositiveIntRequired,
                    paramName: nameof(portalId),
                    parameters: new { spaceId, portalId });

            var deleted = await _portalRepository.DeletePortalAsync(spaceId, portalId);

            if (!deleted)
                throw ErrorService.Exception(
                    ErrorType.NotFound,
                    nameof(DeletePortal),
                    ErrorMessages.Http.NotFound,
                    parameters: new { spaceId, portalId });

            return Ok(new { message = "Portal deleted successfully." });
        }

        /// <summary>
        /// DELETE /api/space/{spaceId}/booth/{boothId}/portal/{portalId}
        /// Deletes a portal associated with a specific booth within a space.
        /// </summary>
        /// <param name="spaceId">The ID of the space.</param>
        /// <param name="boothId">The ID of the booth.</param>
        /// <param name="portalId">The ID of the portal.</param>
        /// <returns>No content on success.</returns>
        [HttpDelete("~/api/space/{spaceId}/booth/{boothId}/portal/{portalId}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeletePortalForBooth(
            [FromRoute] int spaceId,
            [FromRoute] int boothId,
            [FromRoute] int portalId
        )
        {
            if (spaceId <= 0)
                throw ErrorService.Exception(
                    ErrorType.Argument,
                    nameof(DeletePortalForBooth),
                    ErrorMessages.Validation.PositiveIntRequired,
                    paramName: nameof(spaceId),
                    parameters: new { spaceId, boothId, portalId });

            if (boothId <= 0)
                throw ErrorService.Exception(
                    ErrorType.Argument,
                    nameof(DeletePortalForBooth),
                    ErrorMessages.Validation.PositiveIntRequired,
                    paramName: nameof(boothId),
                    parameters: new { spaceId, boothId, portalId });

            if (portalId <= 0)
                throw ErrorService.Exception(
                    ErrorType.Argument,
                    nameof(DeletePortalForBooth),
                    ErrorMessages.Validation.PositiveIntRequired,
                    paramName: nameof(portalId),
                    parameters: new { spaceId, boothId, portalId });

            var deleted = await _portalRepository.DeletePortalAsync(spaceId, boothId, portalId);

            if (!deleted)
                throw ErrorService.Exception(
                    ErrorType.NotFound,
                    nameof(DeletePortalForBooth),
                    ErrorMessages.Http.NotFound,
                    parameters: new { spaceId, boothId, portalId });

            return Ok(new { message = "Portal deleted successfully." });
        }

        #endregion
    }
}
