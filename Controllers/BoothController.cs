// <copyright file="BoothController.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Syed Hussain</author>
// <date>08/08/2025</date>
// <summary>Controller to handle booth routes</summary>


using GMS.TifoXRCoreWebAPI.Models;
using GMS.TifoXRCoreWebAPI.Repositories.Interfaces;
using Microsoft.AspNetCore.Mvc;
using GMS.TifoXRCoreWebAPI.Middleware;
using GMS.TifoXRCoreWebAPI.Middleware.Exceptions;

namespace GMS.TifoXRCoreWebAPI.Controllers
{
    [Route("api/space")]
    [ApiController]

    //For Booths
    public class BoothController(IBoothRepository boothRepository) : ControllerBase
    {
        private readonly IBoothRepository _boothRepository = boothRepository;

        /// <summary>
        /// GET /api/space/{spaceId}/booths
        /// Returns all booths for a space.
        /// </summary>
        [HttpGet("{spaceId}/booths")]
        [ProducesResponseType(typeof(List<BoothModel>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<List<BoothModel>>> GetAllBoothsBySpace(
            [FromRoute] int spaceId)
        {
            if (spaceId <= 0)
                throw new ArgumentException(
                    GlobalException.FormatExceptionMessage(
                        "spaceId must be a positive integer.",
                        nameof(GetAllBoothsBySpace),
                        new { spaceId }
                    ),
                    nameof(spaceId)
                );

            var booths = await _boothRepository.GetAllBoothsBySpaceAsync(spaceId);

            if (booths == null || booths.Count == 0)
                throw new ResourceNotFoundException(
                    GlobalException.FormatExceptionMessage(
                        "No booths found for the specified space.",
                        nameof(GetAllBoothsBySpace),
                        new { spaceId }
                    )
                );

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
        [FromBody] BoothUpdateDto dto)
        {
            if (spaceId <= 0)
                throw new ArgumentException(
                    GlobalException.FormatExceptionMessage(
                        "spaceId must be a positive integer.",
                        nameof(UpdateBooth),
                        new { spaceId, boothId }
                    ),
                    nameof(spaceId)
                );

            if (boothId <= 0)
                throw new ArgumentException(
                    GlobalException.FormatExceptionMessage(
                        "boothId must be a positive integer.",
                        nameof(UpdateBooth),
                        new { spaceId, boothId }
                    ),
                    nameof(boothId)
                );

            if (dto is null)
                throw new ArgumentNullException(
                    nameof(dto),
                    GlobalException.FormatExceptionMessage(
                        "DTO cannot be null.",
                        nameof(UpdateBooth),
                        new { spaceId, boothId }
                    )
                );

            var updated = await _boothRepository.UpdateBoothAsync(spaceId, boothId, dto);

            if (updated is null)
                throw new ResourceNotFoundException(
                    GlobalException.FormatExceptionMessage(
                        "Booth not found.",
                        nameof(UpdateBooth),
                        new { spaceId, boothId }
                    )
                );

            return Ok(new Response { Booth = updated });
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
            [FromBody] BoothCreateDto boothDto)
        {
            if (spaceId <= 0)
                throw new ArgumentException(
                    GlobalException.FormatExceptionMessage(
                        "spaceId must be a positive integer.",
                        nameof(CreateBooth),
                        new { spaceId }
                    ),
                    nameof(spaceId)
                );

            if (boothDto is null)
                throw new ArgumentNullException(
                    nameof(boothDto),
                    GlobalException.FormatExceptionMessage(
                        "DTO cannot be null.",
                        nameof(CreateBooth),
                        new { spaceId }
                    )
                );

            var createdBooth = await _boothRepository.CreateBoothAsync(spaceId, boothDto);

            if (createdBooth is null)
                throw new InvalidOperationException(
                    GlobalException.FormatExceptionMessage(
                        "Creation failed.",
                        nameof(CreateBooth),
                        new { spaceId }
                    )
                );

            // If you have GET-by-id, prefer CreatedAtAction(nameof(GetBoothById), new { spaceId, boothId = createdBooth.Id }, ...)
            return CreatedAtAction(
                nameof(GetAllBoothsBySpace),
                new { spaceId },
                new BoothWrapper { booth = createdBooth }
            );
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
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteBoothCascade([FromRoute] int spaceId, [FromRoute] int boothId)
        {
            if (spaceId <= 0)
                throw new ArgumentException(
                    GlobalException.FormatExceptionMessage(
                        "spaceId must be a positive integer.",
                        nameof(DeleteBoothCascade),
                        new { spaceId, boothId }
                    ),
                    nameof(spaceId)
                );

            if (boothId <= 0)
                throw new ArgumentException(
                    GlobalException.FormatExceptionMessage(
                        "boothId must be a positive integer.",
                        nameof(DeleteBoothCascade),
                        new { spaceId, boothId }
                    ),
                    nameof(boothId)
                );

            var success = await _boothRepository.DeleteBoothCascadeAsync(spaceId, boothId);

            if (!success)
                throw new ResourceNotFoundException(
                    GlobalException.FormatExceptionMessage(
                        "Booth not found.",
                        nameof(DeleteBoothCascade),
                        new { spaceId, boothId }
                    )
                );

            return NoContent();
        }

    }
}
