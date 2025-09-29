using System;
using System.Threading;
using System.Threading.Tasks;
using GMS.TifoXRCoreWebAPI.Models;
using GMS.TifoXRCoreWebAPI.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace GMS.TifoXRCoreWebAPI.Controllers
{
    [ApiController]
    [Route("api/rules/runtime")]
    public sealed class RulesEvaluationController : ControllerBase
    {
        private readonly IRulesEvaluationService _service;
        private readonly ILogger<RulesEvaluationController> _logger;

        public RulesEvaluationController(IRulesEvaluationService service, ILogger<RulesEvaluationController> logger)
        {
            _service = service;
            _logger = logger;
        }

        [HttpPost("evaluate")]
        [ProducesResponseType(typeof(RulesEngineEvaluationResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<RulesEngineEvaluationResponse>> EvaluateAsync(
            [FromBody] RulesEngineEvaluationRequest request,
            CancellationToken cancellationToken)
        {
            if (request is null)
            {
                return BadRequest("Request body is required.");
            }

            if (request.SpaceId <= 0)
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
                _logger.LogError(ex, "Failed to evaluate rules for event {EventType} in space {SpaceId}.", request.EventType, request.SpaceId);
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while evaluating the rules.");
            }
        }
    }
}
