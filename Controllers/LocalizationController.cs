// <copyright file="LocalizationController.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Saad Sohail</author>
// <date>07/31/2025</date>
// <summary>Controller to handle localization routes</summary>

using GMS.TifoXRCoreWebAPI.Middleware;
using GMS.TifoXRCoreWebAPI.Middleware.Errors;
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
                throw ErrorService.Log(
                    ErrorType.Argument,
                    nameof(GetAllBySpace),
                    ErrorMessages.Validation.PositiveIntRequired,
                    paramName: nameof(spaceId),
                    parameters: new { spaceId });

            var result = await _localizationRepo.GetAllLocalizationsBySpaceAsync(spaceId);

            if (result is null || result.Count == 0)
                throw ErrorService.Log(
                    ErrorType.NotFound,
                    nameof(GetAllBySpace),
                    ErrorMessages.Http.NotFound,
                    parameters: new { spaceId });

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
                throw ErrorService.Log(
                    ErrorType.Argument,
                    nameof(GetLocalization),
                    ErrorMessages.Validation.PositiveIntRequired,
                    paramName: nameof(spaceId),
                    parameters: new { spaceId, key });

            if (string.IsNullOrWhiteSpace(key))
                throw ErrorService.Log(
                    ErrorType.Argument,
                    nameof(GetLocalization),
                    ErrorMessages.Validation.InvalidFormat,
                    paramName: nameof(key),
                    parameters: new { spaceId });

            var result = await _localizationRepo.GetLocalizationByKeyAsync(spaceId, key);

            if (result is null)
                throw ErrorService.Log(
                    ErrorType.NotFound,
                    nameof(GetLocalization),
                    ErrorMessages.Http.NotFound,
                    parameters: new { spaceId, key });

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
                throw ErrorService.Log(
                    ErrorType.Argument,
                    nameof(CreateLocalization),
                    ErrorMessages.Validation.PositiveIntRequired,
                    paramName: nameof(spaceId),
                    parameters: new { spaceId });

            if (dto is null)
                throw ErrorService.Log(
                    ErrorType.ArgumentNull,
                    nameof(CreateLocalization),
                    ErrorMessages.Validation.MissingParameter,
                    paramName: nameof(dto),
                    parameters: new { spaceId });

            if (string.IsNullOrWhiteSpace(dto.Key))
                throw ErrorService.Log(
                    ErrorType.Argument,
                    nameof(CreateLocalization),
                    ErrorMessages.Validation.InvalidFormat,
                    paramName: nameof(dto.Key),
                    parameters: new { spaceId });

            if (dto.Values is null || !dto.Values.Any())
                throw ErrorService.Log(
                    ErrorType.Argument,
                    nameof(CreateLocalization),
                    ErrorMessages.Validation.EmptyCollection,
                    paramName: nameof(dto.Values),
                    parameters: new { spaceId, dto.Key });

            var created = await _localizationRepo.CreateLocalizationAsync(spaceId, dto);

            if (created is null)
                throw ErrorService.Log(
                    ErrorType.InvalidOperation,
                    nameof(CreateLocalization),
                    ErrorMessages.Http.Conflict,
                    parameters: new { spaceId, dto.Key });

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
                throw ErrorService.Log(
                    ErrorType.Argument,
                    nameof(UpdateLocalization),
                    ErrorMessages.Validation.PositiveIntRequired,
                    paramName: nameof(spaceId),
                    parameters: new { spaceId, key });

            if (string.IsNullOrWhiteSpace(key))
                throw ErrorService.Log(
                     ErrorType.Argument,
                     nameof(UpdateLocalization),
                     ErrorMessages.Validation.InvalidFormat,
                     paramName: nameof(key),
                     parameters: new { spaceId });

            if (dto is null)
                throw ErrorService.Log(
                    ErrorType.ArgumentNull,
                    nameof(UpdateLocalization),
                    ErrorMessages.Validation.MissingParameter,
                    paramName: nameof(dto),
                    parameters: new { spaceId, key });

            if (dto.Values is null || !dto.Values.Any())
                throw ErrorService.Log(
                    ErrorType.Argument,
                    nameof(UpdateLocalization),
                    ErrorMessages.Validation.EmptyCollection,
                    paramName: nameof(dto.Values),
                    parameters: new { spaceId, key });

            var updated = await _localizationRepo.UpdateLocalizationAsync(spaceId, key, dto);

            if (updated is null)
                throw ErrorService.Log(
                    ErrorType.NotFound,
                    nameof(UpdateLocalization),
                    ErrorMessages.Http.NotFound,
                    parameters: new { spaceId, key });

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
                throw ErrorService.Log(
                    ErrorType.Argument,
                    nameof(DeleteLocalization),
                    ErrorMessages.Validation.PositiveIntRequired,
                    paramName: nameof(spaceId),
                    parameters: new { spaceId, key });

            if (string.IsNullOrWhiteSpace(key))
                throw ErrorService.Log(
                    ErrorType.Argument,
                    nameof(DeleteLocalization),
                    ErrorMessages.Validation.InvalidFormat,
                    paramName: nameof(key),
                    parameters: new { spaceId });

            var ok = await _localizationRepo.DeleteLocalizationAsync(spaceId, key);

            if (!ok)
                throw ErrorService.Log(
                    ErrorType.NotFound,
                    nameof(DeleteLocalization),
                    ErrorMessages.Http.NotFound,
                    parameters: new { spaceId, key });

            return NoContent();
        }

        #endregion
    }
}
