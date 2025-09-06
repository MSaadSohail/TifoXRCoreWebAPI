// <copyright file="BoothController.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Urvashi Dhingra</author>
// <date>09/05/2025</date>
// <summary>Controller to handle booth routes</summary>

using Microsoft.AspNetCore.Mvc;
//
using Serilog;
//
using GMS.TifoXRCoreWebAPI.Middleware.Errors;
using GMS.TifoXRCoreWebAPI.Middleware;
using GMS.TifoXRCoreWebAPI.Middleware.Exceptions;
using GMS.TifoXRCoreWebAPI.Models;
using GMS.TifoXRCoreWebAPI.Repositories.Interfaces;
using GMS.TifoXRCoreWebAPI.Utilities.Logger.Interface;

namespace GMS.TifoXRCoreWebAPI.Controllers
{
    [Route("api/space")]
    [ApiController]

    //For Booths
    public class BoothController(IBoothRepository boothRepository) : ControllerBase
    {
        private readonly IBoothRepository _boothRepository = boothRepository;

        #region GET

        /// <summary>
        /// GET /api/space/{spaceId}/booths
        /// Returns all booths for a space.
        /// </summary>
        [HttpGet("{spaceId}/booths")]
        [ProducesResponseType(typeof(List<BoothModel>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<List<BoothModel>>> GetAllBoothsBySpace(
        [FromRoute] int spaceId,
        [FromServices] IDiagnosticContext diag,          // from Serilog.AspNetCore
        [FromServices] IAppLogger<BoothController> log)
        {
            // Attach stable context to *all* logs in this scope (if any)
            using (log.WithProperties(("SpaceId", spaceId)))
            {
                // Validate (don’t log: common/expected; middleware maps to 400)
                if (spaceId <= 0)
                {
                    throw ErrorService.Log(
                    ErrorType.Argument,
                    nameof(GetAllBoothsBySpace),
                    ErrorMessages.Validation.PositiveIntRequired,
                    paramName: nameof(spaceId),
                    parameters: new { spaceId });
                }

                // Time the repository call; warn if slow
                var sw = System.Diagnostics.Stopwatch.StartNew();
                var booths = await _boothRepository.GetAllBoothsBySpaceAsync(spaceId);
                sw.Stop();

                // Push useful facts into the *request completion* log
                diag.Set("SpaceId", spaceId);
                diag.Set("RepoDurationMs", sw.ElapsedMilliseconds);
                diag.Set("BoothCount", booths?.Count ?? 0);

                // Optional: surface slow path without spamming (Warning = unexpected but not fatal)
                //if (sw.ElapsedMilliseconds > ErrorMessages.SlowRepositoryThresholdMs)
                //{
                //    log.Warn(
                //        ErrorMessages.SlowRepositoryCallMessage,
                //        "Fetching booths",
                //        sw.ElapsedMilliseconds
                //    );
                //}

                // Not found is an expected branch -> throw; GlobalException will log once and return 404
                if (booths is null || booths.Count == 0)
                {
                    throw ErrorService.Log(
                    ErrorType.NotFound,
                    nameof(GetAllBoothsBySpace),
                    ErrorMessages.Http.NotFound,
                    parameters: new { spaceId });
                }
                
                // Success: no extra Info log — the middleware will emit:
                // "HTTP GET /api/space/{spaceId}/booths responded 200 in {Elapsed} ms"
                // enriched with SpaceId, BoothCount, RepoDurationMs
                return Ok(booths);
            }
        }

        #endregion

        #region PUT

        /// <summary>
        /// PUT /api/space/{spaceId}/booth/{boothId}
        /// Updates a booth (e.g., name, localization, map spot).
        /// </summary>
        [HttpPut("{spaceId}/booth/{boothId}")]
        [ProducesResponseType(typeof(Response), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<Response>> UpdateBooth(
        [FromRoute] int spaceId,
        [FromRoute] int boothId,
        [FromBody] BoothUpdateDto dto)
        {
            if (spaceId <= 0)
                throw ErrorService.Log(
                    ErrorType.Argument,
                    nameof(UpdateBooth),
                    ErrorMessages.Validation.PositiveIntRequired,
                    paramName: nameof(spaceId),
                    parameters: new { spaceId, boothId });

            if (boothId <= 0)
                throw ErrorService.Log(
                    ErrorType.Argument,
                    nameof(UpdateBooth),
                    ErrorMessages.Validation.PositiveIntRequired,
                    paramName: nameof(boothId),
                    parameters: new { spaceId, boothId });
            if (dto is null)
                throw ErrorService.Log(
                    ErrorType.ArgumentNull,
                    nameof(UpdateBooth),
                    ErrorMessages.Validation.MissingParameter,
                    paramName: nameof(dto),
                    parameters: new { spaceId, boothId });

            var updated = await _boothRepository.UpdateBoothAsync(spaceId, boothId, dto);

            if (updated is null)
                throw ErrorService.Log(
                    ErrorType.NotFound,
                    nameof(UpdateBooth),
                    ErrorMessages.Http.NotFound,
                    parameters: new { spaceId, boothId });

            return Ok(new Response { Booth = updated });
        }

        #endregion

        #region POST

        /// <summary>
        /// POST /api/space/{spaceId}/booth
        /// Creates a new booth in the given space with its localized name.
        /// </summary>
        [HttpPost("{spaceId}/booth")]
        [ProducesResponseType(typeof(BoothWrapper), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<BoothWrapper>> CreateBooth(
            [FromRoute] int spaceId,
            [FromBody] BoothCreateDto boothDto)
        {
            if (spaceId <= 0)
                throw ErrorService.Log(
                    ErrorType.Argument,
                    nameof(CreateBooth),
                    ErrorMessages.Validation.PositiveIntRequired,
                    paramName: nameof(spaceId),
                    parameters: new { spaceId });

            if (boothDto is null)
                throw ErrorService.Log(
                    ErrorType.ArgumentNull,
                    nameof(CreateBooth),
                    ErrorMessages.Validation.MissingParameter,
                    paramName: nameof(boothDto),
                    parameters: new { spaceId });

            var createdBooth = await _boothRepository.CreateBoothAsync(spaceId, boothDto);

            if (createdBooth is null)
                throw ErrorService.Log(
                    ErrorType.InvalidOperation,
                    nameof(CreateBooth),
                    ErrorMessages.Http.Conflict,
                    parameters: new { spaceId });

            // If you have GET-by-id, prefer CreatedAtAction(nameof(GetBoothById), new { spaceId, boothId = createdBooth.Id }, ...)
            return CreatedAtAction(
                nameof(GetAllBoothsBySpace),
                new { spaceId },
                new BoothWrapper { booth = createdBooth }
            );
        }

        #endregion

        #region DELETE

        /// <summary>
        /// DELETE /api/space/{spaceId}/booth/{boothId}
        /// Deletes a booth and all its dependent data:
        ///  • Booth’s own i18n entries
        ///  • All portals under that booth, including each portal’s:
        ///      – i18n entries
        ///      – corresponding & thumbnail media_localization rows
        ///      – corresponding & thumbnail media rows
        ///  • Finally the booth record itself
        /// </summary>
        [HttpDelete("{spaceId}/booth/{boothId}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteBoothCascade([FromRoute] int spaceId, [FromRoute] int boothId)
        {
            if (spaceId <= 0)
                throw ErrorService.Log(
                    ErrorType.Argument,
                    nameof(DeleteBoothCascade),
                    ErrorMessages.Validation.PositiveIntRequired,
                    paramName: nameof(spaceId),
                    parameters: new { spaceId, boothId });

            if (boothId <= 0)
                throw ErrorService.Log(
                    ErrorType.Argument,
                    nameof(DeleteBoothCascade),
                    ErrorMessages.Validation.PositiveIntRequired,
                    paramName: nameof(boothId),
                    parameters: new { spaceId, boothId });

            var success = await _boothRepository.DeleteBoothCascadeAsync(spaceId, boothId);

            if (!success)
                throw ErrorService.Log(
                    ErrorType.NotFound,
                    nameof(DeleteBoothCascade),
                    ErrorMessages.Http.NotFound,
                    parameters: new { spaceId, boothId });

            return NoContent();
        }

        #endregion
    }
}
