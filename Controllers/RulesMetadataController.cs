// <copyright file="RulesMetadataController.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Saad Sohail</author>
// <date>10/01/2025</date>
// <summary>CRUD endpoints for rules metadata (re_* tables).</summary>

using System.Collections.Generic;
using System.Threading.Tasks;
using GMS.TifoXRCoreWebAPI.Middleware;
using GMS.TifoXRCoreWebAPI.Middleware.Errors;
using GMS.TifoXRCoreWebAPI.Models;
using GMS.TifoXRCoreWebAPI.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GMS.TifoXRCoreWebAPI.Controllers
{
    [ApiController]
    [Route("api/rules/metadata")]
    [Produces("application/json")]
    public sealed class RulesMetadataController : ControllerBase
    {
        private readonly IRulesMetadataService _svc;

        public RulesMetadataController(IRulesMetadataService svc)
            => _svc = svc;

        [HttpGet("event-types")]
        [ProducesResponseType(typeof(IReadOnlyList<EventTypeView>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IReadOnlyList<EventTypeView>>> GetEventTypes()
            => Ok(await _svc.GetEventTypesAsync());

        [HttpPost("event-types")]
        [ProducesResponseType(typeof(object), StatusCodes.Status201Created)]
        public async Task<ActionResult<object>> CreateEventType([FromBody] EventTypeCreateDto dto)
        {
            if (dto is null)
                throw ErrorService.Exception(
                    ErrorType.ArgumentNull,
                    nameof(CreateEventType),
                    ErrorMessages.Validation.MissingParameter,
                    paramName: nameof(dto));

            if (string.IsNullOrWhiteSpace(dto.Name))
                throw ErrorService.Exception(
                    ErrorType.Argument,
                    nameof(CreateEventType),
                    ErrorMessages.Validation.InvalidFormat,
                    paramName: nameof(dto.Name));

            var id = await _svc.CreateEventTypeAsync(dto);
            return CreatedAtAction(nameof(CreateEventType), new { id }, new { id });
        }

        [HttpPut("event-types/{id:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> UpdateEventType(int id, [FromBody] EventTypeUpdateDto dto)
        {
            if (id <= 0)
                throw ErrorService.Exception(
                    ErrorType.Argument,
                    nameof(UpdateEventType),
                    ErrorMessages.Validation.PositiveIntRequired,
                    paramName: nameof(id),
                    parameters: new { id });

            if (dto is null)
                throw ErrorService.Exception(
                    ErrorType.ArgumentNull,
                    nameof(UpdateEventType),
                    ErrorMessages.Validation.MissingParameter,
                    paramName: nameof(dto),
                    parameters: new { id });

            if (dto.Id != id)
                throw ErrorService.Exception(
                    ErrorType.Argument,
                    nameof(UpdateEventType),
                    ErrorMessages.Validation.RouteBodyMismatch,
                    paramName: nameof(dto.Id),
                    parameters: new { expected = id, actual = dto.Id });

            if (string.IsNullOrWhiteSpace(dto.Name))
                throw ErrorService.Exception(
                    ErrorType.Argument,
                    nameof(UpdateEventType),
                    ErrorMessages.Validation.InvalidFormat,
                    paramName: nameof(dto.Name),
                    parameters: new { id });

            var updated = await _svc.UpdateEventTypeAsync(dto);
            if (!updated)
                throw ErrorService.Exception(
                    ErrorType.NotFound,
                    nameof(UpdateEventType),
                    ErrorMessages.Http.NotFound,
                    parameters: new { id });

            return NoContent();
        }

        [HttpGet("context-parameters")]
        [ProducesResponseType(typeof(IReadOnlyList<ContextParameterView>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IReadOnlyList<ContextParameterView>>> GetContextParameters()
            => Ok(await _svc.GetContextParametersAsync());

        [HttpPost("context-parameters")]
        [ProducesResponseType(typeof(object), StatusCodes.Status201Created)]
        public async Task<ActionResult<object>> CreateContextParameter([FromBody] ContextParameterCreateDto dto)
        {
            if (dto is null)
                throw ErrorService.Exception(
                    ErrorType.ArgumentNull,
                    nameof(CreateContextParameter),
                    ErrorMessages.Validation.MissingParameter,
                    paramName: nameof(dto));

            ValidateContextParameterPayload(nameof(CreateContextParameter), dto);

            var id = await _svc.CreateContextParameterAsync(dto);
            return CreatedAtAction(nameof(CreateContextParameter), new { id }, new { id });
        }

        [HttpPut("context-parameters/{id:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> UpdateContextParameter(int id, [FromBody] ContextParameterUpdateDto dto)
        {
            if (id <= 0)
                throw ErrorService.Exception(
                    ErrorType.Argument,
                    nameof(UpdateContextParameter),
                    ErrorMessages.Validation.PositiveIntRequired,
                    paramName: nameof(id),
                    parameters: new { id });

            if (dto is null)
                throw ErrorService.Exception(
                    ErrorType.ArgumentNull,
                    nameof(UpdateContextParameter),
                    ErrorMessages.Validation.MissingParameter,
                    paramName: nameof(dto),
                    parameters: new { id });

            if (dto.Id != id)
                throw ErrorService.Exception(
                    ErrorType.Argument,
                    nameof(UpdateContextParameter),
                    ErrorMessages.Validation.RouteBodyMismatch,
                    paramName: nameof(dto.Id),
                    parameters: new { expected = id, actual = dto.Id });

            ValidateContextParameterPayload(nameof(UpdateContextParameter), dto);

            var updated = await _svc.UpdateContextParameterAsync(dto);
            if (!updated)
                throw ErrorService.Exception(
                    ErrorType.NotFound,
                    nameof(UpdateContextParameter),
                    ErrorMessages.Http.NotFound,
                    parameters: new { id });

            return NoContent();
        }

        [HttpGet("event-types/{eventTypeId:int}/parameters")]
        [ProducesResponseType(typeof(IReadOnlyList<EventTypeParameterView>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IReadOnlyList<EventTypeParameterView>>> GetEventTypeParameters(int eventTypeId)
        {
            if (eventTypeId <= 0)
                throw ErrorService.Exception(
                    ErrorType.Argument,
                    nameof(GetEventTypeParameters),
                    ErrorMessages.Validation.PositiveIntRequired,
                    paramName: nameof(eventTypeId),
                    parameters: new { eventTypeId });

            var rows = await _svc.GetEventTypeParametersAsync(eventTypeId);
            return Ok(rows);
        }

        [HttpPost("event-types/{eventTypeId:int}/parameters")]
        [ProducesResponseType(typeof(object), StatusCodes.Status201Created)]
        public async Task<ActionResult<object>> CreateEventTypeParameter(int eventTypeId, [FromBody] EventTypeParameterCreateDto dto)
        {
            if (eventTypeId <= 0)
                throw ErrorService.Exception(
                    ErrorType.Argument,
                    nameof(CreateEventTypeParameter),
                    ErrorMessages.Validation.PositiveIntRequired,
                    paramName: nameof(eventTypeId),
                    parameters: new { eventTypeId });

            if (dto is null)
                throw ErrorService.Exception(
                    ErrorType.ArgumentNull,
                    nameof(CreateEventTypeParameter),
                    ErrorMessages.Validation.MissingParameter,
                    paramName: nameof(dto),
                    parameters: new { eventTypeId });

            if (dto.ReEventTypeId != eventTypeId)
                throw ErrorService.Exception(
                    ErrorType.Argument,
                    nameof(CreateEventTypeParameter),
                    ErrorMessages.Validation.RouteBodyMismatch,
                    paramName: nameof(dto.ReEventTypeId),
                    parameters: new { expected = eventTypeId, actual = dto.ReEventTypeId });

            ValidateEventTypeParameterPayload(nameof(CreateEventTypeParameter), dto);

            var id = await _svc.CreateEventTypeParameterAsync(dto);
            return CreatedAtAction(nameof(CreateEventTypeParameter), new { eventTypeId, id }, new { id });
        }

        [HttpPut("event-type-parameters/{id:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> UpdateEventTypeParameter(int id, [FromBody] EventTypeParameterUpdateDto dto)
        {
            if (id <= 0)
                throw ErrorService.Exception(
                    ErrorType.Argument,
                    nameof(UpdateEventTypeParameter),
                    ErrorMessages.Validation.PositiveIntRequired,
                    paramName: nameof(id),
                    parameters: new { id });

            if (dto is null)
                throw ErrorService.Exception(
                    ErrorType.ArgumentNull,
                    nameof(UpdateEventTypeParameter),
                    ErrorMessages.Validation.MissingParameter,
                    paramName: nameof(dto),
                    parameters: new { id });

            if (dto.Id != id)
                throw ErrorService.Exception(
                    ErrorType.Argument,
                    nameof(UpdateEventTypeParameter),
                    ErrorMessages.Validation.RouteBodyMismatch,
                    paramName: nameof(dto.Id),
                    parameters: new { expected = id, actual = dto.Id });

            if (dto.ReEventTypeId <= 0)
                throw ErrorService.Exception(
                    ErrorType.Argument,
                    nameof(UpdateEventTypeParameter),
                    ErrorMessages.Validation.PositiveIntRequired,
                    paramName: nameof(dto.ReEventTypeId),
                    parameters: new { id, dto.ReEventTypeId });

            ValidateEventTypeParameterPayload(nameof(UpdateEventTypeParameter), dto);

            var updated = await _svc.UpdateEventTypeParameterAsync(dto);
            if (!updated)
                throw ErrorService.Exception(
                    ErrorType.NotFound,
                    nameof(UpdateEventTypeParameter),
                    ErrorMessages.Http.NotFound,
                    parameters: new { id });

            return NoContent();
        }

        private static void ValidateContextParameterPayload(string method, ContextParameterCreateDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Key))
                throw ErrorService.Exception(
                    ErrorType.Argument,
                    method,
                    ErrorMessages.Validation.InvalidFormat,
                    paramName: nameof(dto.Key));

            if (string.IsNullOrWhiteSpace(dto.Source))
                throw ErrorService.Exception(
                    ErrorType.Argument,
                    method,
                    ErrorMessages.Validation.InvalidFormat,
                    paramName: nameof(dto.Source));

            if (string.IsNullOrWhiteSpace(dto.Path))
                throw ErrorService.Exception(
                    ErrorType.Argument,
                    method,
                    ErrorMessages.Validation.InvalidFormat,
                    paramName: nameof(dto.Path));

            if (dto.RePathTypeId <= 0)
                throw ErrorService.Exception(
                    ErrorType.Argument,
                    method,
                    ErrorMessages.Validation.PositiveIntRequired,
                    paramName: nameof(dto.RePathTypeId),
                    parameters: new { dto.RePathTypeId });

            if (string.IsNullOrWhiteSpace(dto.DataType))
                throw ErrorService.Exception(
                    ErrorType.Argument,
                    method,
                    ErrorMessages.Validation.InvalidFormat,
                    paramName: nameof(dto.DataType));

            if (string.IsNullOrWhiteSpace(dto.UiLabel))
                throw ErrorService.Exception(
                    ErrorType.Argument,
                    method,
                    ErrorMessages.Validation.InvalidFormat,
                    paramName: nameof(dto.UiLabel));

            if (string.IsNullOrWhiteSpace(dto.UiHelpKey))
                throw ErrorService.Exception(
                    ErrorType.Argument,
                    method,
                    ErrorMessages.Validation.InvalidFormat,
                    paramName: nameof(dto.UiHelpKey));

            if (dto.ReDropdownValueProviderId.HasValue && dto.ReDropdownValueProviderId.Value <= 0)
                throw ErrorService.Exception(
                    ErrorType.Argument,
                    method,
                    ErrorMessages.Validation.PositiveIntRequired,
                    paramName: nameof(dto.ReDropdownValueProviderId),
                    parameters: new { dto.ReDropdownValueProviderId });
        }

        private static void ValidateEventTypeParameterPayload(string method, EventTypeParameterCreateDto dto)
        {
            if (dto.ReEventTypeId <= 0)
                throw ErrorService.Exception(
                    ErrorType.Argument,
                    method,
                    ErrorMessages.Validation.PositiveIntRequired,
                    paramName: nameof(dto.ReEventTypeId),
                    parameters: new { dto.ReEventTypeId });

            if (dto.ParameterId <= 0)
                throw ErrorService.Exception(
                    ErrorType.Argument,
                    method,
                    ErrorMessages.Validation.PositiveIntRequired,
                    paramName: nameof(dto.ParameterId),
                    parameters: new { dto.ParameterId });
        }
    }
}
