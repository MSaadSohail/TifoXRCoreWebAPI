using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using TifoXRCoreWebAPI.Models;
using TifoXRCoreWebAPI.Repositories.Interfaces;

namespace TifoXRCoreWebAPI.Controllers
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
    }
}
