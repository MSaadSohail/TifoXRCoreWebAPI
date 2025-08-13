// <copyright file="LocalizationController.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Saad Sohail</author>
// <date>07/31/2025</date>
// <summary>Controller to handle localization routes</summary>

using GMS.TifoXRCoreWebAPI.Middleware;
using GMS.TifoXRCoreWebAPI.Middleware.Exceptions;
using GMS.TifoXRCoreWebAPI.Models.Common;
using GMS.TifoXRCoreWebAPI.Repositories.Interfaces;
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

        #region GET
        /// <summary>
        /// GET /api/localization/space/{spaceId}
        /// Returns all i18n keys and their locale-value pairs for the space.
        /// </summary>
        [HttpGet("{spaceId:int}/localizations")]
        [ProducesResponseType(typeof(List<LocalizedPairs>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<List<LocalizedPairs>>> GetAllBySpace([FromRoute] int spaceId)
        {
            if (spaceId <= 0)
                throw new ArgumentException(
                    GlobalException.FormatExceptionMessage(
                        "spaceId must be a positive integer.",
                        nameof(GetAllBySpace),
                        new { spaceId }
                    ),
                    nameof(spaceId)
                );

            var result = await _localizationRepo.GetAllLocalizationsBySpaceAsync(spaceId);

            if (result is null || result.Count == 0)
                throw new ResourceNotFoundException(
                    GlobalException.FormatExceptionMessage(
                        "No localizations found for the specified space.",
                        nameof(GetAllBySpace),
                        new { spaceId }
                    )
                );

            return Ok(result);
        }


        /// <summary>
        /// GET /api/space/{spaceId}/localizations/{key}
        /// Fetches all localizations (all supported locales) for a given key in a space.
        /// </summary>
        [HttpGet("{spaceId:int}/localizations/{key}")]
        [ProducesResponseType(typeof(LocalizedPairs), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<LocalizedPairs>> GetLocalization(
        [FromRoute] int spaceId,
        [FromRoute] string key)
        {
            if (spaceId <= 0)
                throw new ArgumentException(
                    GlobalException.FormatExceptionMessage(
                        "spaceId must be a positive integer.",
                        nameof(GetLocalization),
                        new { spaceId, key }
                    ),
                    nameof(spaceId)
                );

            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException(
                    GlobalException.FormatExceptionMessage(
                        "key cannot be null, empty, or whitespace.",
                        nameof(GetLocalization),
                        new { spaceId }
                    ),
                    nameof(key)
                );

            var result = await _localizationRepo.GetLocalizationByKeyAsync(spaceId, key);

            if (result is null)
                throw new ResourceNotFoundException(
                    GlobalException.FormatExceptionMessage(
                        "Localization not found for the specified key.",
                        nameof(GetLocalization),
                        new { spaceId, key }
                    )
                );

            return Ok(result);
        }

        #endregion

        #region POST

        /// <summary>
        /// POST /api/space/{spaceId}/localizations
        /// Creates a new localization key with multiple locale values.
        /// </summary>
        [HttpPost("{spaceId:int}/localizations")]
        [ProducesResponseType(typeof(LocalizedPairs), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<LocalizedPairs>> CreateLocalization(
        [FromRoute] int spaceId,
        [FromBody] LocalizedPairs dto)
        {
            if (spaceId <= 0)
                throw new ArgumentException(
                    GlobalException.FormatExceptionMessage(
                        "spaceId must be a positive integer.",
                        nameof(CreateLocalization),
                        new { spaceId }
                    ),
                    nameof(spaceId)
                );

            if (dto is null)
                throw new ArgumentNullException(
                    nameof(dto),
                    GlobalException.FormatExceptionMessage(
                        "DTO cannot be null.",
                        nameof(CreateLocalization),
                        new { spaceId }
                    )
                );

            if (string.IsNullOrWhiteSpace(dto.Key))
                throw new ArgumentException(
                    GlobalException.FormatExceptionMessage(
                        "Key cannot be null, empty, or whitespace.",
                        nameof(CreateLocalization),
                        new { spaceId }
                    ),
                    nameof(dto.Key)
                );

            if (dto.Values is null || !dto.Values.Any())
                throw new ArgumentException(
                    GlobalException.FormatExceptionMessage(
                        "Values collection cannot be null or empty.",
                        nameof(CreateLocalization),
                        new { spaceId, dto.Key }
                    ),
                    nameof(dto.Values)
                );

            var created = await _localizationRepo.CreateLocalizationAsync(spaceId, dto);

            if (created is null)
                throw new InvalidOperationException(
                    GlobalException.FormatExceptionMessage(
                        "Creation failed.",
                        nameof(CreateLocalization),
                        new { spaceId, dto.Key }
                    )
                );

            return CreatedAtAction(
                nameof(GetLocalization),
                new { spaceId, key = created.Key },
                created
            );
        }


        #endregion

        #region PUT

        /// <summary>
        /// PUT /api/space/{spaceId}/localizations/{key}
        /// Updates all localizations for a given key in a space.
        /// </summary>
        [HttpPut("{spaceId:int}/localizations/{key}")]
        [ProducesResponseType(typeof(LocalizedPairs), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<LocalizedPairs>> UpdateLocalization(
        [FromRoute] int spaceId,
        [FromRoute] string key,
        [FromBody] LocalizedPairs dto)
        {
            if (spaceId <= 0)
                throw new ArgumentException(
                    GlobalException.FormatExceptionMessage(
                        "spaceId must be a positive integer.",
                        nameof(UpdateLocalization),
                        new { spaceId, key }
                    ),
                    nameof(spaceId)
                );

            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException(
                    GlobalException.FormatExceptionMessage(
                        "key cannot be null, empty, or whitespace.",
                        nameof(UpdateLocalization),
                        new { spaceId }
                    ),
                    nameof(key)
                );

            if (dto is null)
                throw new ArgumentNullException(
                    nameof(dto),
                    GlobalException.FormatExceptionMessage(
                        "DTO cannot be null.",
                        nameof(UpdateLocalization),
                        new { spaceId, key }
                    )
                );

            if (dto.Values is null || !dto.Values.Any())
                throw new ArgumentException(
                    GlobalException.FormatExceptionMessage(
                        "Values collection cannot be null or empty.",
                        nameof(UpdateLocalization),
                        new { spaceId, key }
                    ),
                    nameof(dto.Values)
                );

            var updated = await _localizationRepo.UpdateLocalizationAsync(spaceId, key, dto);

            if (updated is null)
                throw new ResourceNotFoundException(
                    GlobalException.FormatExceptionMessage(
                        "Localization not found for the specified key.",
                        nameof(UpdateLocalization),
                        new { spaceId, key }
                    )
                );

            return Ok(updated);
        }


        #endregion

        #region DELETE

        /// <summary>
        /// DELETE /api/space/{spaceId}/localizations/{key}
        /// Deletes all localizations for a key in a space.
        /// </summary>
        [HttpDelete("{spaceId:int}/localizations/{key}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteLocalization(
        [FromRoute] int spaceId,
        [FromRoute] string key)
        {
            if (spaceId <= 0)
                throw new ArgumentException(
                    GlobalException.FormatExceptionMessage(
                        "spaceId must be a positive integer.",
                        nameof(DeleteLocalization),
                        new { spaceId, key }
                    ),
                    nameof(spaceId)
                );

            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException(
                    GlobalException.FormatExceptionMessage(
                        "key cannot be null, empty, or whitespace.",
                        nameof(DeleteLocalization),
                        new { spaceId }
                    ),
                    nameof(key)
                );

            var ok = await _localizationRepo.DeleteLocalizationAsync(spaceId, key);

            if (!ok)
                throw new ResourceNotFoundException(
                    GlobalException.FormatExceptionMessage(
                        "Localization not found for the specified key.",
                        nameof(DeleteLocalization),
                        new { spaceId, key }
                    )
                );

            return NoContent();
        }


        #endregion
    }
}
