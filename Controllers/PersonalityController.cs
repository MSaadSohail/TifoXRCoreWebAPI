// <copyright file="PersonalityController.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Urvashi Dhingra</author>
// <date>09/05/2025</date>
// <summary>Personality HTTP methods</summary>

using GMS.TifoXRCoreWebAPI.Middleware;
using GMS.TifoXRCoreWebAPI.Middleware.Errors;
using GMS.TifoXRCoreWebAPI.Middleware.Exceptions;
using GMS.TifoXRCoreWebAPI.Models;
using GMS.TifoXRCoreWebAPI.Models.Common;
using GMS.TifoXRCoreWebAPI.Repositories;
using GMS.TifoXRCoreWebAPI.Repositories.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace GMS.TifoXRCoreWebAPI.Controllers
{
    [ApiController]
    public class PersonalityController : ControllerBase
    {
        private readonly IPersonalityRepository _personalityRepository;

        public PersonalityController(IPersonalityRepository repository)
        {
            _personalityRepository = repository;
        }

        /// <summary>
        /// GET /api/personality/{id}
        /// Returns a personality by id.
        /// </summary>
        [HttpGet("api/personality/{id:int}")]
        [ProducesResponseType(typeof(PersonalityData), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<PersonalityData>> GetPersonalityById(int id)
        {
            if (id <= 0)
                throw ErrorService.Log(
                    ErrorType.Argument,
                    nameof(GetPersonalityById),
                    ErrorMessages.Validation.PositiveIntRequired,
                    paramName: nameof(id),
                    parameters: new { id });

            var personality = await _personalityRepository.GetPersonalityByIdAsync(id);

            if (personality is null)
                throw ErrorService.Log(
                    ErrorType.NotFound,
                    nameof(GetPersonalityById),
                    ErrorMessages.Http.NotFound,
                    parameters: new { id });

            return Ok(personality);
        }


        [HttpPost("api/personality")]
        [ProducesResponseType(typeof(PersonalityData), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<PersonalityData>> CreatePersonality([FromBody] PersonalityCreateDto dto)
        {
            if (dto is null)
                throw ErrorService.Log(
                    ErrorType.ArgumentNull,
                    nameof(CreatePersonality),
                    ErrorMessages.Validation.MissingParameter,
                    paramName: nameof(dto));

            // Optional: add specific field checks if required (e.g., Name, Bio) and throw ArgumentException

            var created = await _personalityRepository.CreatePersonalityAsync(dto);

            if (created is null)
                throw ErrorService.Log(
                    ErrorType.InvalidOperation,
                    nameof(CreatePersonality),
                    ErrorMessages.Http.Conflict,
                    parameters: new { dto });

            return CreatedAtAction(nameof(GetPersonalityById), new { id = created.Id }, created);
        }


    }

}
