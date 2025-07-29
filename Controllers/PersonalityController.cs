// <copyright file="PersonalityController.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Syed Hussain</author>
// <date>07/23/2025</date>
// <summary>Personality HTTP methods</summary>
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using System.Data;
using GMS.TifoXRCoreWebAPI.Models;
using GMS.TifoXRCoreWebAPI.Models.Common;
using GMS.TifoXRCoreWebAPI.Repositories;
using GMS.TifoXRCoreWebAPI.Repositories.Interfaces;

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

        [HttpGet("api/personality/{id}")]
        [ProducesResponseType(typeof(PersonalityData), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<PersonalityData>> GetPersonalityById(int id)
        {
            var personality = await _personalityRepository.GetPersonalityByIdAsync(id);
            return personality == null ? NotFound() : Ok(personality);
        }

        [HttpPost("api/personality")]
        [ProducesResponseType(typeof(PersonalityData), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<PersonalityData>> CreatePersonality([FromBody] PersonalityCreateDto dto)
        {
            if (dto == null)
                return BadRequest("Invalid request body.");

            try
            {
                var created = await _personalityRepository.CreatePersonalityAsync(dto);
                return CreatedAtAction(nameof(GetPersonalityById), new { id = created.Id }, created);
            }
            catch (MySqlException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        


    }

}
