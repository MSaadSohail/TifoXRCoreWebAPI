// <copyright file="PortalController.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Saad Sohail</author>
// <date>07/23/2025</date>
// <summary>Controller to handle portal routes</summary>

using Microsoft.AspNetCore.Mvc;
using TifoXRCoreWebAPI.Models;
using TifoXRCoreWebAPI.Repositories.Interfaces;

namespace TifoXRCoreWebAPI.Controllers
{
    [Route("api/space/{spaceId}/portal")]
    [ApiController]
    public class PortalController(IPortalRepository portalRepository) : ControllerBase
    {
        private readonly IPortalRepository _portalRepository = portalRepository;

        /// <summary>
        /// Gets all portals for the specified space.
        /// </summary>
        /// <param name="spaceId">The ID of the space.</param>
        /// <returns>A list of portals in the given space.</returns>
        [HttpGet]
        [ProducesResponseType(typeof(List<PortalModel>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<List<PortalModel>>> GetPortalsBySpace(
            [FromRoute] int spaceId
        )
        {
            try
            {
                var portals = await _portalRepository.GetPortalsBySpaceAsync(spaceId);

                if (portals == null || portals.Count == 0)
                    return NotFound();

                return Ok(portals);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Gets all portals for the specified booth in a space.
        /// </summary>
        /// <param name="spaceId">The ID of the space.</param>
        /// <param name="boothId">The ID of the booth.</param>
        /// <returns>A list of portals under the booth in the space.</returns>
        [HttpGet]
        [Route("/api/space/{spaceId}/booth/{boothId}/portals")]
        [ProducesResponseType(typeof(List<PortalModel>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<List<PortalModel>>> GetPortalsByBooth(
            [FromRoute] int spaceId,
            [FromRoute] int boothId
        )
        {
            try
            {
                var portals = await _portalRepository.GetPortalsByBoothAsync(spaceId, boothId);
                
                if (portals == null || portals.Count == 0)
                    return NotFound();
                
                return Ok(portals);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Gets a specific portal by its ID within a space.
        /// </summary>
        /// <param name="spaceId">The ID of the space.</param>
        /// <param name="portalId">The ID of the portal.</param>
        /// <returns>The portal data if found.</returns>
        [HttpGet("{portalId}")]
        [ProducesResponseType(typeof(PortalModel), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<PortalModel>> GetPortalById(
            [FromRoute] int spaceId,
            [FromRoute] int portalId)
        {
            try
            {
                var portal = await _portalRepository.GetPortalByIdAsync(spaceId, portalId);
                
                if (portal == null) return NotFound();
                
                return Ok(portal);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
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
            try
            {
                if (portalDto == null) return BadRequest();
                
                var created = await _portalRepository.CreatePortalAsync(spaceId, portalDto);
                
                return CreatedAtAction(nameof(GetPortalById),
                    new { spaceId, portalId = created.PortalId }, created);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
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
            try
            {
                if (portalDto == null) return BadRequest();
                
                var updated = await _portalRepository.UpdatePortalAsync(spaceId, portalId, portalDto);
                
                if (updated == null) return NotFound();
                
                return Ok(updated);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Updates a portal belonging to a specific booth within a space.
        /// </summary>
        /// <param name="spaceId">The ID of the space.</param>
        /// <param name="boothId">The ID of the booth.</param>
        /// <param name="portalId">The ID of the portal.</param>
        /// <param name="dto">The portal update data.</param>
        /// <returns>The updated portal data.</returns>
        [HttpPut]
        [Route("/api/space/{spaceId}/booth/{boothId}/portal/{portalId}")]
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
            try
            {
                if (dto == null) return BadRequest();
                
                var updated = await _portalRepository.UpdatePortalAsync(spaceId, boothId, portalId, dto);
                
                if (updated == null) return NotFound();
                
                return Ok(updated);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Deletes a portal from the specified space.
        /// </summary>
        /// <param name="spaceId">The ID of the space.</param>
        /// <param name="portalId">The ID of the portal.</param>
        /// <returns>No content on success.</returns>
        [HttpDelete("{portalId}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeletePortal(
            [FromRoute] int spaceId,
            [FromRoute] int portalId)
        {
            try
            {
                var deleted = await _portalRepository.DeletePortalAsync(spaceId, portalId);
                
                if (!deleted)
                    return NotFound();
                
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Deletes a portal associated with a specific booth within a space.
        /// </summary>
        /// <param name="spaceId">The ID of the space.</param>
        /// <param name="boothId">The ID of the booth.</param>
        /// <param name="portalId">The ID of the portal.</param>
        /// <returns>No content on success.</returns>
        [HttpDelete]
        [Route("/api/space/{spaceId}/booth/{boothId}/portal/{portalId}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeletePortal(
            [FromRoute] int spaceId,
            [FromRoute] int boothId,
            [FromRoute] int portalId
        )
        {
            try
            {
                var deleted = await _portalRepository.DeletePortalAsync(spaceId, boothId, portalId);
                
                if (!deleted)
                    return NotFound();
                
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}
