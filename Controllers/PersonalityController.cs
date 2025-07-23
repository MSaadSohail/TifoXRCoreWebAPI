using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using System.Data;
using TifoXRCoreWebAPI.Models;
using TifoXRCoreWebAPI.Models.Common;
using TifoXRCoreWebAPI.Repositories.Interfaces;

namespace TifoXRCoreWebAPI.Controllers
{
    [ApiController]
    public class PersonalityController : ControllerBase
    {
        private readonly IPersonalityRepository _repository;

        public PersonalityController(IPersonalityRepository repository)
        {
            _repository = repository;
        }

        [HttpGet("api/personality/{id}")]
        [ProducesResponseType(typeof(PersonalityData), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<PersonalityData>> GetPersonalityById(int id)
        {
            var personality = await _repository.GetPersonalityByIdAsync(id);
            return personality == null ? NotFound() : Ok(personality);
        }
    }

}
