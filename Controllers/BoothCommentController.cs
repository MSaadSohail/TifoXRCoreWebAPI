// Controllers/BoothCommentsController.cs
using GMS.TifoXRCoreWebAPI.Models;
using GMS.TifoXRCoreWebAPI.Repositories.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Data;

namespace GMS.TifoXRCoreWebAPI.Controllers
{
    [ApiController]
    [Route("api/space/{spaceId:int}/booth-comments")]
    public sealed class BoothCommentController : ControllerBase
    {
        private readonly IBoothCommentRepository _repo;

        public BoothCommentController(IBoothCommentRepository repo)
        {
            _repo = repo;
        }

        /// <summary>
        /// Publishes a predefined comment for a user in the given space (idempotent).
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Create(int spaceId, [FromBody] BoothCommentCreateDto dto)
        {
            if (dto is null) return BadRequest("Body is required.");

            var result = await _repo.CreateBoothCommentAsync(spaceId, dto);
            return Ok(result);
        }

        /// <summary>
        /// Lists booth_comment rows for a space. Pass ?includeInactive=true to include inactive rows.
        /// Each item includes localized values for its predefined comment in this space.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAll(int spaceId, [FromQuery] bool includeInactive = false)
        {
            var items = await _repo.GetBoothCommentsBySpaceAsync(spaceId, includeInactive);
            return Ok(items);
        }

        /// <summary>
        /// Update a booth_comment by id; you may change predefinedCommentId and/or isActive.
        /// </summary>
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int spaceId, int id, [FromBody] BoothCommentUpdateDto dto)
        {
            if (dto is null) return BadRequest("Body is required.");

            try
            {
                var updated = await _repo.UpdateBoothCommentAsync(spaceId, id, dto);
                if (updated is null) return NotFound();
                return Ok(updated);
            }
            catch (DuplicateNameException ex)
            {
                return Conflict(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
