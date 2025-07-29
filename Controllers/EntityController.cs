// <copyright file="EntityController.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Syed Hussain</author>
// <date>07/28/2025</date>
// <summary>Controller to handle Entity APIs</summary>
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using GMS.TifoXRCoreWebAPI.Models;
using GMS.TifoXRCoreWebAPI.Repositories.Interfaces;

namespace GMS.TifoXRCoreWebAPI.Controllers
{
    [ApiController]
    [Route("api/entity")]
    public class EntityController : ControllerBase
    {
        private readonly IEntityRepository _repo;
        private readonly ILogger<EntityController> _logger;

        public EntityController(IEntityRepository repo,
                                ILogger<EntityController> logger)
        {
            _repo = repo;
            _logger = logger;
        }

        // GET

        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(EntityData), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<EntityData>> Get(int id)
        {
            try
            {
                var entity = await _repo.GetByIdAsync(id);
                return entity is null ? NotFound() : Ok(entity);
            }
            catch (MySqlException ex)
            {
                _logger.LogError(ex, "Error fetching entity {EntityId}", id);
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // POST

        [HttpPost]
        [ProducesResponseType(typeof(EntityData), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<EntityData>> Create([FromBody] Entity dto)
        {
            if (dto?.LocalizedName == null)
                return BadRequest();

            try
            {
                var created = await _repo.CreateAsync(dto);
                return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
            }
            catch (MySqlException ex)
            {
                _logger.LogError(ex, "Error creating entity");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // PUT

        [HttpPut("{id:int}")]
        [ProducesResponseType(typeof(EntityData), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<EntityData>> Update(int id, [FromBody] Entity dto)
        {
            if (dto?.LocalizedName == null)
                return BadRequest();

            try
            {
                var updated = await _repo.UpdateAsync(id, dto);

                // repository returns null when the id didn’t match any row
                return updated is null ? NotFound() : Ok(updated);
            }
            catch (MySqlException ex)
            {
                _logger.LogError(ex, "Error updating entity {EntityId}", id);
                return StatusCode(500, new { error = ex.Message });
            }
        }

    }
}