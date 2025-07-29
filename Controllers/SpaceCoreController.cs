// <copyright file="SpaceCoreController.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Syed Hussain</author>
// <date>07/28/2025</date>
// <summary>Controller to handle space routes</summary>
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using GMS.TifoXRCoreWebAPI.Models;
using GMS.TifoXRCoreWebAPI.Repositories.Interfaces;

namespace GMS.TifoXRCoreWebAPI.Controllers
{
    [Route("api/space")]
    [ApiController]
    public class SpaceCoreController : ControllerBase
    {
        private readonly ISpaceRepository _spaceRepo;
        public SpaceCoreController(ISpaceRepository spaceRepo)
            => _spaceRepo = spaceRepo;

        [HttpGet("{id}")]
        [ProducesResponseType(typeof(SpaceData), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<SpaceData>> GetSpaceById(int id)
        {
            var space = await _spaceRepo.GetSpaceByIdAsync(id);
            if (space == null) return NotFound();
            return Ok(space);
        }

        [HttpPost("")]
        [ProducesResponseType(typeof(SpaceData), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<SpaceData>> CreateSpace([FromBody] Space dto)
        {
            if (dto == null || dto.LocalizedDescription == null)
                return BadRequest();

            try
            {
                var created = await _spaceRepo.CreateSpaceAsync(dto);
                return CreatedAtAction(nameof(GetSpaceById), new { id = created.Id }, created);
            }
            catch (MySqlException ex)
            {
                // foreign key violation, etc.
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPut("{id}")]
        [ProducesResponseType(typeof(SpaceData), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<SpaceData>> UpdateSpaceById(int id, [FromBody] Space spaceDto)
        {
            if (spaceDto == null || spaceDto.LocalizedDescription == null)
                return BadRequest();

            try
            {
                var updated = await _spaceRepo.UpdateSpaceAsync(id, spaceDto);
                if (updated == null)
                    return NotFound();

                return Ok(updated);
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
