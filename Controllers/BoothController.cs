// <copyright file="BoothController.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Syed Hussain</author>
// <date>07/28/2025</date>
// <summary>Controller to handle booth routes</summary>

using Microsoft.AspNetCore.Mvc;

using GMS.TifoXRCoreWebAPI.Models;
using GMS.TifoXRCoreWebAPI.Repositories.Interfaces;

namespace GMS.TifoXRCoreWebAPI.Controllers
{
    [Route("api/space")]
    [ApiController]

    //For Booths
    public class BoothController(IBoothRepository boothRepository) : ControllerBase
    {
        private readonly IBoothRepository _boothRepository = boothRepository;

        [HttpGet("{spaceId}/booths")]
        public async Task<ActionResult<List<BoothModel>>> GetAllBoothsBySpace([FromRoute] int spaceId)
        {
            try
            {
                var booths = await _boothRepository.GetAllBoothsBySpaceAsync(spaceId);
                if (booths == null || booths.Count == 0)
                    return NotFound();
                return Ok(booths);
            }
            catch (Exception ex)
            {
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new { error = ex.Message }
                );
            }
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
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new { error = ex.Message }
                );
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
            catch (Exception ex)
            {
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new { error = ex.Message }
                );
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
