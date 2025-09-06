// <copyright file="EntityController.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Syed Hussain</author>
// <date>08/08/2025</date>
// <summary>Controller to handle Entity APIs</summary>
using GMS.TifoXRCoreWebAPI.Middleware;
using GMS.TifoXRCoreWebAPI.Middleware.Errors;
using GMS.TifoXRCoreWebAPI.Middleware.Exceptions;
using GMS.TifoXRCoreWebAPI.Models;
using GMS.TifoXRCoreWebAPI.Repositories.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace GMS.TifoXRCoreWebAPI.Controllers
{
    [ApiController]
    [Route("api/entity")]
    public class EntityController : ControllerBase
    {
        private readonly IEntityRepository _repo;

        public EntityController(IEntityRepository repo)
        {
            _repo = repo;
        }

        #region GET

        /// <summary>
        /// GET /api/entity/{id}
        /// Returns a single entity by id.
        /// </summary>
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(EntityData), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<EntityData>> Get(int id)
        {
            if (id <= 0)
                throw ErrorService.Log(
                    ErrorType.Argument,
                    nameof(Get),
                    ErrorMessages.Validation.PositiveIntRequired,
                    paramName: nameof(id),
                    parameters: new { id });

            var entity = await _repo.GetByIdAsync(id);

            if (entity is null)
                throw ErrorService.Log(
                    ErrorType.NotFound,
                    nameof(Get),
                    ErrorMessages.Http.NotFound,
                    parameters: new { id });

            return Ok(entity);
        }

        #endregion

        #region POST

        /// <summary>
        /// POST /api/entity
        /// Creates a new entity (including its localized values).
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(EntityData), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<EntityData>> Create([FromBody] Entity dto)
        {
            if (dto is null)
                throw ErrorService.Log(
                    ErrorType.ArgumentNull,
                    nameof(Create),
                    ErrorMessages.Validation.MissingParameter,
                    paramName: nameof(dto));

            if (dto.LocalizedPairs == null || dto.LocalizedPairs.Values == null || !dto.LocalizedPairs.Values.Any())
                throw ErrorService.Log(
                    ErrorType.Argument,
                    nameof(Create),
                    ErrorMessages.Validation.EmptyCollection,
                    paramName: nameof(dto.LocalizedPairs),
                    parameters: new { key = dto.LocalizedPairs?.Key });

            var created = await _repo.CreateAsync(dto);

            if (created is null)
                throw ErrorService.Log(
                    ErrorType.InvalidOperation,
                    nameof(Create),
                    ErrorMessages.Http.Conflict);

            return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
        }

        #endregion

        #region PUT

        /// <summary>
        /// PUT /api/entity/{id}
        /// Updates an existing entity (including its localized values).
        /// </summary>
        [HttpPut("{id:int}")]
        [ProducesResponseType(typeof(EntityData), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<EntityData>> Update(int id, [FromBody] Entity dto)
        {
            if (id <= 0)
                throw ErrorService.Log(
                    ErrorType.Argument,
                    nameof(Update),
                    ErrorMessages.Validation.PositiveIntRequired,
                    paramName: nameof(id),
                    parameters: new { id });

            if (dto is null)
                throw ErrorService.Log(
                    ErrorType.ArgumentNull,
                    nameof(Update),
                    ErrorMessages.Validation.MissingParameter,
                    paramName: nameof(dto),
                    parameters: new { id });

            if (dto.LocalizedPairs == null || dto.LocalizedPairs.Values == null || !dto.LocalizedPairs.Values.Any())
                throw ErrorService.Log(
                    ErrorType.Argument,
                    nameof(Update),
                    ErrorMessages.Validation.EmptyCollection,
                    paramName: nameof(dto.LocalizedPairs),
                    parameters: new { id, key = dto.LocalizedPairs?.Key });

            var updated = await _repo.UpdateAsync(id, dto);

            if (updated is null)
                throw ErrorService.Log(
                    ErrorType.NotFound,
                    nameof(Update),
                    ErrorMessages.Http.NotFound,
                    parameters: new { id });

            return Ok(updated);
        }

        #endregion
    }
}