// <copyright file="EventController.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Syed Hussain</author>
// <date>07/28/2025</date>
// <summary>Controller to handle Event APIs</summary>
using GMS.TifoXRCoreWebAPI.Models;
using GMS.TifoXRCoreWebAPI.Models.Common;
using GMS.TifoXRCoreWebAPI.Repositories.Interfaces;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using System.Data;
using TifoXRCoreWebAPI.Middleware;


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
                throw new ArgumentException(
                    GlobalException.FormatExceptionMessage(
                        "event_id must be a positive integer.",
                        nameof(GetEventDataByID),
                        new { event_id }
                    ),
                    nameof(event_id)
                );

            var evt = await _eventRepository.GetEventByIdAsync(event_id);

            if (evt is null)
                throw new KeyNotFoundException(
                    GlobalException.FormatExceptionMessage(
                        "Event not found.",
                        nameof(GetEventDataByID),
                        new { event_id }
                    )
                );

            return Ok(evt);
        }



        [HttpPost]
        [ProducesResponseType(typeof(EventData), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<EventData>> CreateEvent([FromBody] Event eventDto)
        {
            if (eventDto is null)
                throw new ArgumentNullException(
                    nameof(eventDto),
                    GlobalException.FormatExceptionMessage(
                        "DTO cannot be null.",
                        nameof(CreateEvent)
                    )
                );

            var created = await _eventRepository.CreateEventAsync(eventDto);

            if (created is null)
                throw new InvalidOperationException(
                    GlobalException.FormatExceptionMessage(
                        "Creation failed.",
                        nameof(CreateEvent)
                    )
                );

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
                throw new ArgumentException(
                    GlobalException.FormatExceptionMessage(
                        "eventId must be a positive integer.",
                        nameof(UpdateEvent),
                        new { eventId }
                    ),
                    nameof(eventId)
                );

            if (dto is null)
                throw new ArgumentNullException(
                    nameof(dto),
                    GlobalException.FormatExceptionMessage(
                        "DTO cannot be null.",
                        nameof(UpdateEvent),
                        new { eventId }
                    )
                );

            var updated = await _eventRepository.UpdateEventAsync(eventId, dto);

            if (updated is null)
                throw new KeyNotFoundException(
                    GlobalException.FormatExceptionMessage(
                        "Event not found.",
                        nameof(UpdateEvent),
                        new { eventId }
                    )
                );

            return Ok(updated);
        }


    }
}


