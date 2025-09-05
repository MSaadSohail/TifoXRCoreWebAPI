// <copyright file="PersonalityController.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Syed Hussain</author>
// <date>07/23/2025</date>
// <summary>Personality HTTP methods</summary>
using GMS.TifoXRCoreWebAPI.Middleware;
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

        [HttpGet("api/personality/{id:int}")]
        [ProducesResponseType(typeof(PersonalityData), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<PersonalityData>> GetPersonalityById(int id)
        {
            if (id <= 0)
                throw new ArgumentException(
                    GlobalException.FormatExceptionMessage(
                        "id must be a positive integer.",
                        nameof(GetPersonalityById),
                        new { id }
                    ),
                    nameof(id)
                );

            var personality = await _personalityRepository.GetPersonalityByIdAsync(id);

            if (personality is null)
                throw new ResourceNotFoundException(
                    GlobalException.FormatExceptionMessage(
                        "Personality not found.",
                        nameof(GetPersonalityById),
                        new { id }
                    )
                );

            return Ok(personality);
        }


        [HttpPost("api/personality")]
        [ProducesResponseType(typeof(PersonalityData), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<PersonalityData>> CreatePersonality([FromBody] PersonalityCreateDto dto)
        {
            if (dto is null)
                throw new ArgumentNullException(
                    nameof(dto),
                    GlobalException.FormatExceptionMessage(
                        "DTO cannot be null.",
                        nameof(CreatePersonality)
                    )
                );

            // Optional: add specific field checks if required (e.g., Name, Bio) and throw ArgumentException

            var created = await _personalityRepository.CreatePersonalityAsync(dto);

            if (created is null)
                throw new InvalidOperationException(
                    GlobalException.FormatExceptionMessage(
                        "Creation failed.",
                        nameof(CreatePersonality),
                        new { dto }
                    )
                );

            return CreatedAtAction(nameof(GetPersonalityById), new { id = created.Id }, created);
        }

        // PUT: api/personality/{id}
        [HttpPut("api/personality/{id}")]
        public async Task<IActionResult> UpdatePersonality(int id, [FromBody] PersonalityUpdateDto dto)
        {
            if (dto is null)
                throw new ArgumentNullException(
                    nameof(dto),
                    GlobalException.FormatExceptionMessage(
                        "DTO cannot be null.",
                        nameof(UpdatePersonality),
                        new { id }
                    )
                );

            if (!ModelState.IsValid)
                throw new ArgumentException(
                    GlobalException.FormatExceptionMessage(
                        "Model validation failed.",
                        nameof(UpdatePersonality),
                        new { id, errors = ModelState }
                    )
                );

            var updated = await _personalityRepository.UpdatePersonalityAsync(id, dto);

            if (updated is null)
                throw new ResourceNotFoundException(
                    GlobalException.FormatExceptionMessage(
                        $"Personality with ID {id} not found.",
                        nameof(UpdatePersonality),
                        new { id }
                    )
                );

            return Ok(updated);
        }

        // DELETE: {id}
        [HttpDelete("api/personality/{id:int}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Delete(int id)
        {
            var ok = await _personalityRepository.DeletePersonalityAsync(id);

            if (!ok)
                throw new ResourceNotFoundException(
                    GlobalException.FormatExceptionMessage(
                        $"Personality with ID {id} not found.",
                        nameof(Delete),
                        new { id }
                    )
                );

            return NoContent();
        }


    }

}
