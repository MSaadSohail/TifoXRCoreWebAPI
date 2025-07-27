using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using System.Data;
using GMS.TifoXRCoreWebAPI.Models;
using GMS.TifoXRCoreWebAPI.Models.Common;
using GMS.TifoXRCoreWebAPI.Repositories.Interfaces;


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
        [HttpGet("{event_id}")]
        public async Task<ActionResult<EventData>> GetEventDataByID([FromRoute] int event_id)
        {
            var evt = await _eventRepository.GetEventByIdAsync(event_id);
            if (evt == null)
                return NotFound();
            return Ok(evt);
        }


        [HttpPost]
        [ProducesResponseType(typeof(EventData), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<EventData>> CreateEvent([FromBody] Event eventDto)
        {
            if (eventDto == null)
                return BadRequest();

            try
            {
                var created = await _eventRepository.CreateEventAsync(eventDto);
                return CreatedAtAction(nameof(GetEventDataByID), new { event_id = created.Id }, created);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }



        //Update API
        [HttpPut("/api/event/{eventId}")]
        [ProducesResponseType(typeof(EventData), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<EventData>> UpdateEvent([FromRoute] int eventId, [FromBody] Event dto)
        {
            if (dto == null)
                return BadRequest();

            try
            {
                var updated = await _eventRepository.UpdateEventAsync(eventId, dto);
                return Ok(updated);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

    }
}


