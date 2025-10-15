// Controllers/PredefinedCommentsController.cs
using GMS.TifoXRCoreWebAPI.Models;
using GMS.TifoXRCoreWebAPI.Repositories.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace GMS.TifoXRCoreWebAPI.Controllers
{
    [ApiController]
    [Route("api/space/{spaceId:int}/predefined-comments")]
    public sealed class PredefinedCommentController : ControllerBase
    {
        private readonly IPredefinedCommentRepository _repo;

        public PredefinedCommentController(IPredefinedCommentRepository repo)
        {
            _repo = repo;
        }

        /// <summary>
        /// Returns predefined comments for this space with all localized values.
        /// Query: ?includeInactive=true to include inactive ones.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Get(int spaceId, [FromQuery] bool includeInactive = false)
        {
            var items = await _repo.GetPredefinedCommentsBySpaceAsync(spaceId, includeInactive);
            return Ok(items);
        }

        [HttpPost]
        public async Task<IActionResult> Create(int spaceId, [FromBody] PredefinedCommentCreateDto dto)
        {
            if (dto is null) return BadRequest("Body is required.");

            var created = await _repo.CreatePredefinedCommentAsync(spaceId, dto);
            // Keeping 200 OK for consistency; switch to 201 Created if preferred.
            return Ok(created);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int spaceId, int id, [FromBody] PredefinedCommentUpdateDto dto)
        {
            if (dto is null) return BadRequest("Body is required.");

            var updated = await _repo.UpdatePredefinedCommentByIdAsync(spaceId, id, dto);
            if (updated is null) return NotFound();

            return Ok(updated);
        }

        /// <summary>
        /// Soft delete by default (is_active = FALSE). 
        /// Use ?hard=true for hard delete (only if not referenced by booth_comment).
        /// Optionally remove space-specific i18n with ?deleteI18nForSpace=true (soft delete only).
        /// </summary>
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(
            int spaceId,
            int id,
            [FromQuery] bool hard = false,
            [FromQuery] bool deleteI18nForSpace = false)
        {
            var result = await _repo.DeletePredefinedCommentAsync(spaceId, id, hard, deleteI18nForSpace);

            if (result.NotFound) return NotFound();

            if (result.ConflictInUse)
                return Conflict(new { message = "Cannot hard-delete: comment is referenced by one or more booth_comment rows. Use soft delete instead." });

            // 204 No Content on success (soft or hard)
            return NoContent();
        }
    }
}
