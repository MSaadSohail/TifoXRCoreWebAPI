using GMS.TifoXRCoreWebAPI.Models.Common;
using Microsoft.AspNetCore.Mvc;

namespace GMS.TifoXRCoreWebAPI.Controllers
{
    [Route("api/space")]
    [ApiController]
    public class LocalizationController : ControllerBase
    {
        private readonly ILocalizationRepository _localizationRepo;
        public LocalizationController(ILocalizationRepository localizationRepo)
        {
            _localizationRepo = localizationRepo;
        }

        /// <summary>
        /// GET /api/localization/space/{spaceId}
        /// Returns all i18n keys and their locale-value pairs for the space.
        /// </summary>
        [HttpGet("{spaceId}/localizations")]
        [ProducesResponseType(typeof(List<LocalizedPairs>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<LocalizedPairs>>> GetAllBySpace([FromRoute] int spaceId)
        {
            var result = await _localizationRepo.GetAllLocalizationsBySpaceAsync(spaceId);
            return Ok(result);
        }

        /// <summary>
        /// GET /api/space/{spaceId}/localizations/{key}
        /// Fetches all localizations (all supported locales) for a given key in a space.
        /// </summary>
        [HttpGet("{spaceId}/localizations/{key}")]
        [ProducesResponseType(typeof(LocalizedPairs), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<LocalizedPairs>> GetLocalization(
            [FromRoute] int spaceId,
            [FromRoute] string key)
        {
            try
            {
                var result = await _localizationRepo.GetLocalizationByKeyAsync(spaceId, key);
                if (result == null)
                    return NotFound();
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// POST /api/space/{spaceId}/localizations
        /// Creates a new localization key with multiple locale values.
        /// </summary>
        [HttpPost("{spaceId}/localizations")]
        [ProducesResponseType(typeof(LocalizedPairs), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<LocalizedPairs>> CreateLocalization(
            [FromRoute] int spaceId,
            [FromBody] LocalizedPairs dto)
        {
            if (dto == null || string.IsNullOrEmpty(dto.Key) || dto.Values == null)
                return BadRequest();

            try
            {
                var created = await _localizationRepo.CreateLocalizationAsync(spaceId, dto);
                return Created($"/api/space/{spaceId}/localizations/{created.Key}", created);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// PUT /api/space/{spaceId}/localizations/{key}
        /// Updates all localizations for a given key in a space.
        /// </summary>
        [HttpPut("{spaceId}/localizations/{key}")]
        [ProducesResponseType(typeof(LocalizedPairs), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<LocalizedPairs>> UpdateLocalization(
            [FromRoute] int spaceId,
            [FromRoute] string key,
            [FromBody] LocalizedPairs dto)
        {
            if (dto == null || string.IsNullOrEmpty(key) || dto.Values == null)
                return BadRequest();

            try
            {
                var updated = await _localizationRepo.UpdateLocalizationAsync(spaceId, key, dto);
                if (updated == null)
                    return NotFound();
                return Ok(updated);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// DELETE /api/space/{spaceId}/localizations/{key}
        /// Deletes all localizations for a key in a space.
        /// </summary>
        [HttpDelete("{spaceId}/localizations/{key}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteLocalization(
            [FromRoute] int spaceId,
            [FromRoute] string key)
        {
            try
            {
                var ok = await _localizationRepo.DeleteLocalizationAsync(spaceId, key);
                if (!ok) return NotFound();
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}
