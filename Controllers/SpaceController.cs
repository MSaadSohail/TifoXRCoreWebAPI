// <copyright file="SpaceController.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Syed Hussain</author>
// <date>08/12/2025</date>
// <summary>Controller to handle space routes</summary>

using GMS.TifoXRCoreWebAPI.Middleware.Errors;
using GMS.TifoXRCoreWebAPI.Models;
using GMS.TifoXRCoreWebAPI.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace GMS.TifoXRCoreWebAPI.Controllers
{
    [Route("api/space")]
    [ApiController]
    public class SpaceController : ControllerBase
    {
        private readonly ISpaceRepository _spaceRepo;
        public SpaceController(ISpaceRepository spaceRepo)
            => _spaceRepo = spaceRepo;

        /// <summary>
        /// GET /api/space/{id}
        /// Returns a space by id.
        /// </summary>
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(SpaceData), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<SpaceData>> GetSpaceById(int id)
        {
            if (id <= 0)
                throw ErrorService.Exception(
                    ErrorType.Argument,
                    nameof(GetSpaceById),
                    ErrorMessages.Validation.PositiveIntRequired,
                    paramName: nameof(id),
                    parameters: new { id });

            var space = await _spaceRepo.GetSpaceByIdAsync(id);

            if (space is null)
                throw ErrorService.Exception(
                    ErrorType.NotFound,
                    nameof(GetSpaceById),
                    ErrorMessages.Http.NotFound,
                    parameters: new { id });

            return Ok(space);
        }

        /// <summary>
        /// POST /api/space
        /// Creates a new space.
        /// </summary>
        [HttpPost("")]
        [ProducesResponseType(typeof(SpaceData), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<SpaceData>> CreateSpace([FromBody] Space dto)
        {
            if (dto is null)
                throw ErrorService.Exception(
                    ErrorType.ArgumentNull,
                    nameof(CreateSpace),
                    ErrorMessages.Validation.MissingParameter,
                    paramName: nameof(dto));

            if (dto.LocalizedDescription is null || dto.LocalizedDescription.Values is null || !dto.LocalizedDescription.Values.Any())
                throw ErrorService.Exception(
                    ErrorType.Argument,
                    nameof(CreateSpace),
                    ErrorMessages.Validation.EmptyCollection,
                    paramName: nameof(dto.LocalizedDescription),
                    parameters: new { field = "LocalizedDescription.Values" });


            var created = await _spaceRepo.CreateSpaceAsync(dto);

            if (created is null)
                throw ErrorService.Exception(
                    ErrorType.InvalidOperation,
                    nameof(CreateSpace),
                    ErrorMessages.Http.Conflict,
                    parameters: new { dto });

            return CreatedAtAction(nameof(GetSpaceById), new { id = created.Id }, created);
        }

        /// <summary>
        /// PUT /api/space/{id}
        /// Updates an existing space by id.
        /// </summary>
        [HttpPut("{id:int}")]
        [ProducesResponseType(typeof(SpaceData), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<SpaceData>> UpdateSpaceById(int id, [FromBody] Space spaceDto)
        {
            if (id <= 0)
                throw ErrorService.Exception(
                    ErrorType.Argument,
                    nameof(UpdateSpaceById),
                    ErrorMessages.Validation.PositiveIntRequired,
                    paramName: nameof(id),
                    parameters: new { id });

            if (spaceDto is null)
                throw ErrorService.Exception(
                    ErrorType.ArgumentNull,
                    nameof(UpdateSpaceById),
                    ErrorMessages.Validation.MissingParameter,
                    paramName: nameof(spaceDto),
                    parameters: new { id });

            if (spaceDto.LocalizedDescription is null ||
                spaceDto.LocalizedDescription.Values is null ||
                !spaceDto.LocalizedDescription.Values.Any())
                throw ErrorService.Exception(
                    ErrorType.Argument,
                    nameof(UpdateSpaceById),
                    ErrorMessages.Validation.EmptyCollection,
                    paramName: nameof(spaceDto.LocalizedDescription),
                    parameters: new { id, field = "LocalizedDescription.Values" });

            var updated = await _spaceRepo.UpdateSpaceAsync(id, spaceDto);

            if (updated is null)
                throw ErrorService.Exception(
                    ErrorType.NotFound,
                    nameof(UpdateSpaceById),
                    ErrorMessages.Http.NotFound,
                    parameters: new { id });

            return Ok(updated);
        }


    }
}
