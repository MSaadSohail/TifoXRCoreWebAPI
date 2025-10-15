// Controllers/CharitiesController.cs
using GMS.TifoXRCoreWebAPI.Models;
using GMS.TifoXRCoreWebAPI.Repositories.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace GMS.TifoXRCoreWebAPI.Controllers
{
    [ApiController]
    [Route("api/space/{spaceId:int}/charities")]
    public sealed class CharitiesController : ControllerBase
    {
        private readonly ICharityRepository _repo;
        public CharitiesController(ICharityRepository repo) => _repo = repo;

        [HttpPost]
        public async Task<IActionResult> Create(int spaceId, [FromBody] CharityCreateDto dto)
        {
            if (dto is null) return BadRequest("Body is required.");
            if (dto.SpaceId == 0) dto = dto with { SpaceId = spaceId };
            if (dto.SpaceId != spaceId) return BadRequest("spaceId mismatch between route and body.");

            var created = await _repo.CreateCharityAsync(dto);
            return CreatedAtAction(nameof(GetById), new { spaceId, id = created.Id }, created);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int spaceId, int id)
        {
            var item = await _repo.GetCharityByIdAsync(id, spaceId);
            return item is null ? NotFound() : Ok(item);
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(int spaceId)
        {
            var items = await _repo.GetCharitiesBySpaceAsync(spaceId);
            return Ok(items);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int spaceId, int id, [FromBody] CharityUpdateDto dto)
        {
            if (dto is null) return BadRequest("Body is required.");
            var updated = await _repo.UpdateCharityAsync(spaceId, id, dto);
            return Ok(updated);
        }

        // Controllers/CharitiesController.cs  (add this action)
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int spaceId, int id)
        {
            var ok = await _repo.DeleteCharityAsync(spaceId, id);
            return ok ? NoContent() : NotFound();
        }


    }
}
