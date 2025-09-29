// <copyright file="RulesEvaluationController.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Saad Sohail</author>
// <date>09/30/2025</date>
// <summary></summary>

using Microsoft.AspNetCore.Mvc;
//
using GMS.TifoXRCoreWebAPI.Models;
using GMS.TifoXRCoreWebAPI.Services;

namespace GMS.TifoXRCoreWebAPI.Controllers
{
    [ApiController]
    [Route("api/space/{spaceId}/rules")]
    public sealed class RulesEvaluationController(
        IRulesEvaluationService service, 
        ILogger<RulesEvaluationController> logger) : ControllerBase
    {
        private readonly IRulesEvaluationService _service = service;
        private readonly ILogger<RulesEvaluationController> _logger = logger;

        [HttpPost("evaluate")]
        [ProducesResponseType(typeof(RulesEngineEvaluationResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<RulesEngineEvaluationResponse>> EvaluateAsync(
            int spaceId,
            [FromBody] RulesEngineEvaluationRequest request,
            CancellationToken cancellationToken)
        {
            if (request is null)
            {
                return BadRequest("Request body is required.");
            }

            if (spaceId <= 0)
            {
                return BadRequest("spaceId must be a positive integer.");
            }

            if (string.IsNullOrWhiteSpace(request.EventType))
            {
                return BadRequest("eventType is required.");
            }

            try
            {
                var response = await _service.EvaluateAsync(request, cancellationToken);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to evaluate rules for event {EventType} in space {SpaceId}.", request.EventType, spaceId);
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while evaluating the rules.");
            }
        }
    }
}
