// <copyright file="EventController.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Syed Hussain</author>
// <date>07/28/2025</date>
// <summary>Controller to handle Event APIs</summary>

using GMS.TifoXRCoreWebAPI.Middleware.Errors;
using GMS.TifoXRCoreWebAPI.Models;
using GMS.TifoXRCoreWebAPI.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace GMS.TifoXRCoreWebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EventController : ControllerBase
    {
        

        private readonly IEventRepository _eventRepository;

        public EventController(IEventRepository eventRepository)
        {
            _eventRepository = eventRepository;
        }

        /// <summary>
        /// GET /api/event/{event_id}
        /// Fetches all event fields except creation_time, modified_time, modified_by,
        /// plus localized name and description.
        /// </summary>
        [HttpGet("{event_id:int}")]
        [ProducesResponseType(typeof(EventData), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<EventData>> GetEventDataByID([FromRoute] int event_id)
        {
            if (event_id <= 0)
                throw ErrorService.Exception(
                    ErrorType.Argument,
                    nameof(GetEventDataByID),
                    ErrorMessages.Validation.PositiveIntRequired,
                    paramName: nameof(event_id),
                    parameters: new { event_id });

            var evt = await _eventRepository.GetEventByIdAsync(event_id);

            if (evt is null)
                throw ErrorService.Exception(
                    ErrorType.NotFound,
                    nameof(GetEventDataByID),
                    ErrorMessages.Http.NotFound,
                    parameters: new { event_id });

            return Ok(evt);
        }



        [HttpPost]
        [ProducesResponseType(typeof(EventData), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<EventData>> CreateEvent([FromBody] Event eventDto)
        {
            if (eventDto is null)
                throw ErrorService.Exception(
                    ErrorType.ArgumentNull,
                    nameof(CreateEvent),
                    ErrorMessages.Validation.MissingParameter,
                    paramName: nameof(eventDto));

            var created = await _eventRepository.CreateEventAsync(eventDto);

            if (created is null)
                throw ErrorService.Exception(
                    ErrorType.InvalidOperation,
                    nameof(CreateEvent),
                    ErrorMessages.Http.Conflict,
                    parameters: new { eventDto });

            return CreatedAtAction(nameof(GetEventDataByID), new { event_id = created.Id }, created);
        }




        //Update API
        [HttpPut("/api/event/{eventId:int}")]
        [ProducesResponseType(typeof(EventData), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<EventData>> UpdateEvent([FromRoute] int eventId, [FromBody] Event dto)
        {
            if (eventId <= 0)
                throw ErrorService.Exception(
                    ErrorType.Argument,
                    nameof(UpdateEvent),
                    ErrorMessages.Validation.PositiveIntRequired,
                    paramName: nameof(eventId),
                    parameters: new { eventId });

            if (dto is null)
                throw ErrorService.Exception(
                    ErrorType.ArgumentNull,
                    nameof(UpdateEvent),
                    ErrorMessages.Validation.MissingParameter,
                    paramName: nameof(dto),
                    parameters: new { eventId });

            var updated = await _eventRepository.UpdateEventAsync(eventId, dto);

            if (updated is null)
                throw ErrorService.Exception(
                    ErrorType.NotFound,
                    nameof(UpdateEvent),
                    ErrorMessages.Http.NotFound,
                    parameters: new { eventId });

            return Ok(updated);
        }


    }
}


