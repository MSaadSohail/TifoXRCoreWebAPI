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
using GMS.TifoXRCoreWebAPI.Middleware;
using GMS.TifoXRCoreWebAPI.Middleware.Exceptions;

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
                throw new ArgumentException(
                    GlobalException.FormatExceptionMessage(
                        "spaceId must be a positive integer.",
                        nameof(GetPortalsBySpace),
                        new { spaceId }
                    ),
                    nameof(spaceId)
                );

            var portals = await _portalRepository.GetPortalsBySpaceAsync(spaceId);

            // Not found (404) – same pattern used in TeleportTableController
            if (portals == null || portals.Count == 0)
                throw new ResourceNotFoundException(
                    GlobalException.FormatExceptionMessage(
                        "Portals not found.",
                        nameof(GetPortalsBySpace),
                        new { spaceId }
                    )
                );

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
                throw new ArgumentException(
                    GlobalException.FormatExceptionMessage(
                        "spaceId must be a positive integer.",
                        nameof(GetPortalsByBooth),
                        new { spaceId, boothId }
                    ),
                    nameof(spaceId)
                );

            if (boothId <= 0)
                throw new ArgumentException(
                    GlobalException.FormatExceptionMessage(
                        "boothId must be a positive integer.",
                        nameof(GetPortalsByBooth),
                        new { spaceId, boothId }
                    ),
                    nameof(boothId)
                );

            var portals = await _portalRepository.GetPortalsByBoothAsync(spaceId, boothId);

            if (portals == null || portals.Count == 0)
                throw new ResourceNotFoundException(
                    GlobalException.FormatExceptionMessage(
                        "Portals not found for the specified booth.",
                        nameof(GetPortalsByBooth),
                        new { spaceId, boothId }
                    )
                );

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
                throw new ArgumentException(
                    GlobalException.FormatExceptionMessage(
                        "spaceId must be a positive integer.",
                        nameof(GetPortalById),
                        new { spaceId, portalId }
                    ),
                    nameof(spaceId)
                );

            if (portalId <= 0)
                throw new ArgumentException(
                    GlobalException.FormatExceptionMessage(
                        "portalId must be a positive integer.",
                        nameof(GetPortalById),
                        new { spaceId, portalId }
                    ),
                    nameof(portalId)
                );

            var portal = await _portalRepository.GetPortalByIdAsync(spaceId, portalId);

            return portal == null
                ? throw new ResourceNotFoundException(
                    GlobalException.FormatExceptionMessage(
                        "Portal not found.",
                        nameof(GetPortalById),
                        new { spaceId, portalId }
                    )
                )
                : Ok(portal);
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
                throw new ArgumentException(GlobalException.FormatExceptionMessage(
                    "spaceId must be a positive integer.",
                    nameof(CreatePortal),
                    new { spaceId }
                ));

            ArgumentNullException.ThrowIfNull(portalDto);

            if (portalDto.LocalizedPairs == null || portalDto.LocalizedPairs.Values == null || portalDto.LocalizedPairs.Values.Count == 0)
                throw new ArgumentException(GlobalException.FormatExceptionMessage(
                    "LocalizedPairs.Values cannot be empty.",
                    nameof(CreatePortal),
                    new { spaceId }
                ), nameof(portalDto.LocalizedPairs));

            var created = await _portalRepository.CreatePortalAsync(spaceId, portalDto);

            return created == null
                ? throw new InvalidOperationException(GlobalException.FormatExceptionMessage(
                    "Portal creation failed.",
                    nameof(CreatePortal),
                    new { spaceId, portalDto }
                ))
                : (ActionResult<PortalModel>)CreatedAtAction(
                    nameof(GetPortalById),
                    new { spaceId, portalId = created.PortalId },
                    created
                );
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
                throw new ArgumentException(
                    GlobalException.FormatExceptionMessage(
                        "spaceId must be a positive integer.",
                        nameof(UpdatePortal),
                        new { spaceId, portalId }
                    ),
                    nameof(spaceId)
                );

            if (portalId <= 0)
                throw new ArgumentException(
                    GlobalException.FormatExceptionMessage(
                        "portalId must be a positive integer.",
                        nameof(UpdatePortal),
                        new { spaceId, portalId }
                    ),
                    nameof(portalId)
                );

            ArgumentNullException.ThrowIfNull(portalDto);

            if (portalDto.LocalizedPairs == null)
                throw new ArgumentException(
                    GlobalException.FormatExceptionMessage(
                        "LocalizedPairs cannot be null.",
                        nameof(UpdatePortal),
                        new { spaceId, portalId }
                    ),
                    nameof(portalDto.LocalizedPairs)
                );

            if (string.IsNullOrWhiteSpace(portalDto.LocalizedPairs.Key))
                throw new ArgumentException(
                    GlobalException.FormatExceptionMessage(
                        "LocalizedPairs.Key cannot be null or whitespace.",
                        nameof(UpdatePortal),
                        new { spaceId, portalId }
                    ),
                    nameof(portalDto.LocalizedPairs.Key)
                );

            if (portalDto.LocalizedPairs.Values == null || !portalDto.LocalizedPairs.Values.Any())
                throw new ArgumentException(
                    GlobalException.FormatExceptionMessage(
                        "LocalizedPairs.Values cannot be empty.",
                        nameof(UpdatePortal),
                        new { spaceId, portalId }
                    ),
                    nameof(portalDto.LocalizedPairs)
                );

            var updated = await _portalRepository.UpdatePortalAsync(spaceId, portalId, portalDto);

            if (updated == null)
                throw new ResourceNotFoundException(
                    GlobalException.FormatExceptionMessage(
                        "Portal not found.",
                        nameof(UpdatePortal),
                        new { spaceId, portalId }
                    )
                );

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
                throw new ArgumentException(
                    GlobalException.FormatExceptionMessage(
                        "spaceId must be a positive integer.",
                        nameof(UpdatePortalData),
                        new { spaceId, boothId, portalId }
                    ),
                    nameof(spaceId)
                );

            if (boothId <= 0)
                throw new ArgumentException(
                    GlobalException.FormatExceptionMessage(
                        "boothId must be a positive integer.",
                        nameof(UpdatePortalData),
                        new { spaceId, boothId, portalId }
                    ),
                    nameof(boothId)
                );

            if (portalId <= 0)
                throw new ArgumentException(
                    GlobalException.FormatExceptionMessage(
                        "portalId must be a positive integer.",
                        nameof(UpdatePortalData),
                        new { spaceId, boothId, portalId }
                    ),
                    nameof(portalId)
                );

            ArgumentNullException.ThrowIfNull(dto);

            if (dto.LocalizedPairs == null)
                throw new ArgumentException(
                    GlobalException.FormatExceptionMessage(
                        "LocalizedPairs cannot be null.",
                        nameof(UpdatePortalData),
                        new { spaceId, boothId, portalId }
                    ),
                    nameof(dto.LocalizedPairs)
                );

            if (string.IsNullOrWhiteSpace(dto.LocalizedPairs.Key))
                throw new ArgumentException(
                    GlobalException.FormatExceptionMessage(
                        "LocalizedPairs.Key cannot be null or whitespace.",
                        nameof(UpdatePortalData),
                        new { spaceId, boothId, portalId }
                    ),
                    nameof(dto.LocalizedPairs.Key)
                );

            if (dto.LocalizedPairs.Values == null || !dto.LocalizedPairs.Values.Any())
                throw new ArgumentException(
                    GlobalException.FormatExceptionMessage(
                        "LocalizedPairs.Values cannot be empty.",
                        nameof(UpdatePortalData),
                        new { spaceId, boothId, portalId }
                    ),
                    nameof(dto.LocalizedPairs)
                );

            var updated = await _portalRepository.UpdatePortalAsync(spaceId, boothId, portalId, dto);

            if (updated == null)
                throw new ResourceNotFoundException(
                    GlobalException.FormatExceptionMessage(
                        "Portal not found for the specified booth.",
                        nameof(UpdatePortalData),
                        new { spaceId, boothId, portalId }
                    )
                );

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
                throw new ArgumentException(
                    GlobalException.FormatExceptionMessage(
                        "spaceId must be a positive integer.",
                        nameof(DeletePortal),
                        new { spaceId, portalId }
                    ),
                    nameof(spaceId)
                );

            if (portalId <= 0)
                throw new ArgumentException(
                    GlobalException.FormatExceptionMessage(
                        "portalId must be a positive integer.",
                        nameof(DeletePortal),
                        new { spaceId, portalId }
                    ),
                    nameof(portalId)
                );

            var deleted = await _portalRepository.DeletePortalAsync(spaceId, portalId);

            if (!deleted)
                throw new ResourceNotFoundException(
                    GlobalException.FormatExceptionMessage(
                        "Portal not found.",
                        nameof(DeletePortal),
                        new { spaceId, portalId }
                    )
                );

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
                throw new ArgumentException(
                    GlobalException.FormatExceptionMessage(
                        "spaceId must be a positive integer.",
                        nameof(DeletePortalForBooth),
                        new { spaceId, boothId, portalId }
                    ),
                    nameof(spaceId)
                );

            if (boothId <= 0)
                throw new ArgumentException(
                    GlobalException.FormatExceptionMessage(
                        "boothId must be a positive integer.",
                        nameof(DeletePortalForBooth),
                        new { spaceId, boothId, portalId }
                    ),
                    nameof(boothId)
                );

            if (portalId <= 0)
                throw new ArgumentException(
                    GlobalException.FormatExceptionMessage(
                        "portalId must be a positive integer.",
                        nameof(DeletePortalForBooth),
                        new { spaceId, boothId, portalId }
                    ),
                    nameof(portalId)
                );

            var deleted = await _portalRepository.DeletePortalAsync(spaceId, boothId, portalId);

            if (!deleted)
                throw new ResourceNotFoundException(
                    GlobalException.FormatExceptionMessage(
                        "Portal not found for the specified booth.",
                        nameof(DeletePortalForBooth),
                        new { spaceId, boothId, portalId }
                    )
                );

            return Ok(new { message = "Portal deleted successfully." });
        }

        #endregion
    }
}
