// <copyright file="SpaceController.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Syed Hussain</author>
// <date>08/12/2025</date>
// <summary>Controller to handle space routes</summary>

using Microsoft.AspNetCore.Mvc;
//
using GMS.TifoXRCoreWebAPI.Middleware;
using GMS.TifoXRCoreWebAPI.Middleware.Exceptions;
using GMS.TifoXRCoreWebAPI.Models;
using GMS.TifoXRCoreWebAPI.Repositories.Interfaces;

namespace GMS.TifoXRCoreWebAPI.Controllers
{
    [Route("api/space")]
    [ApiController]
    public class SpaceController : ControllerBase
    {
        private readonly ISpaceRepository _spaceRepo;
        public SpaceController(ISpaceRepository spaceRepo)
            => _spaceRepo = spaceRepo;

        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(SpaceData), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<SpaceData>> GetSpaceById(int id)
        {
            if (id <= 0)
                throw new ArgumentException(
                    GlobalException.FormatExceptionMessage(
                        "id must be a positive integer.",
                        nameof(GetSpaceById),
                        new { id }
                    ),
                    nameof(id)
                );

            var space = await _spaceRepo.GetSpaceByIdAsync(id);

            if (space is null)
                throw new ResourceNotFoundException(
                    GlobalException.FormatExceptionMessage(
                        "Space not found.",
                        nameof(GetSpaceById),
                        new { id }
                    )
                );

            return Ok(space);
        }


        [HttpPost("")]
        [ProducesResponseType(typeof(SpaceData), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<SpaceData>> CreateSpace([FromBody] Space dto)
        {
            if (dto is null)
                throw new ArgumentNullException(
                    nameof(dto),
                    GlobalException.FormatExceptionMessage(
                        "DTO cannot be null.",
                        nameof(CreateSpace)
                    )
                );

            if (dto.LocalizedDescription is null || dto.LocalizedDescription.Values is null || !dto.LocalizedDescription.Values.Any())
                throw new ArgumentException(
                    GlobalException.FormatExceptionMessage(
                        "LocalizedDescription.Values cannot be null or empty.",
                        nameof(CreateSpace)
                    ),
                    nameof(dto.LocalizedDescription)
                );

            var created = await _spaceRepo.CreateSpaceAsync(dto);

            if (created is null)
                throw new InvalidOperationException(
                    GlobalException.FormatExceptionMessage(
                        "Creation failed.",
                        nameof(CreateSpace)
                    )
                );

            return CreatedAtAction(nameof(GetSpaceById), new { id = created.Id }, created);
        }


        [HttpPut("{id:int}")]
        [ProducesResponseType(typeof(SpaceData), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<SpaceData>> UpdateSpaceById(int id, [FromBody] Space spaceDto)
        {
            if (id <= 0)
                throw new ArgumentException(
                    GlobalException.FormatExceptionMessage(
                        "id must be a positive integer.",
                        nameof(UpdateSpaceById),
                        new { id }
                    ),
                    nameof(id)
                );

            if (spaceDto is null)
                throw new ArgumentNullException(
                    nameof(spaceDto),
                    GlobalException.FormatExceptionMessage(
                        "DTO cannot be null.",
                        nameof(UpdateSpaceById),
                        new { id }
                    )
                );

            if (spaceDto.LocalizedDescription is null ||
                spaceDto.LocalizedDescription.Values is null ||
                !spaceDto.LocalizedDescription.Values.Any())
                throw new ArgumentException(
                    GlobalException.FormatExceptionMessage(
                        "LocalizedDescription.Values cannot be null or empty.",
                        nameof(UpdateSpaceById),
                        new { id }
                    ),
                    nameof(spaceDto.LocalizedDescription)
                );

            var updated = await _spaceRepo.UpdateSpaceAsync(id, spaceDto);

            if (updated is null)
                throw new ResourceNotFoundException(
                    GlobalException.FormatExceptionMessage(
                        "Space not found.",
                        nameof(UpdateSpaceById),
                        new { id }
                    )
                );

            return Ok(updated);
        }


    }
}
