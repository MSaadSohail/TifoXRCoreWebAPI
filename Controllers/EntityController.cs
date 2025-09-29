// <copyright file="EntityController.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Syed Hussain</author>
// <date>08/08/2025</date>
// <summary>Controller to handle Entity APIs</summary>

using Microsoft.AspNetCore.Mvc;
//
using GMS.TifoXRCoreWebAPI.Models;
using GMS.TifoXRCoreWebAPI.Repositories;
using GMS.TifoXRCoreWebAPI.Middleware;
using GMS.TifoXRCoreWebAPI.Middleware.Exceptions;

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

        // GET

        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(EntityData), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<EntityData>> Get(int id)
        {
            if (id <= 0)
                throw new ArgumentException(
                    GlobalException.FormatExceptionMessage(
                        "id must be a positive integer.",
                        nameof(Get),
                        new { id }
                    ),
                    nameof(id)
                );

            var entity = await _repo.GetByIdAsync(id);

            if (entity is null)
                throw new ResourceNotFoundException(
                    GlobalException.FormatExceptionMessage(
                        "Entity not found.",
                        nameof(Get),
                        new { id }
                    )
                );

            return Ok(entity);
        }


        // POST

        [HttpPost]
        [ProducesResponseType(typeof(EntityData), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<EntityData>> Create([FromBody] Entity dto)
        {
            if (dto is null)
                throw new ArgumentNullException(
                    nameof(dto),
                    GlobalException.FormatExceptionMessage(
                        "DTO cannot be null.",
                        nameof(Create)
                    )
                );

            if (dto.LocalizedPairs == null || dto.LocalizedPairs.Values == null || !dto.LocalizedPairs.Values.Any())
                throw new ArgumentException(
                    GlobalException.FormatExceptionMessage(
                        "LocalizedPairs.Values cannot be empty.",
                        nameof(Create),
                        new { dto?.LocalizedPairs?.Key }
                    ),
                    nameof(dto.LocalizedPairs)
                );

            var created = await _repo.CreateAsync(dto);

            if (created is null)
                throw new InvalidOperationException(
                    GlobalException.FormatExceptionMessage(
                        "Creation failed.",
                        nameof(Create)
                    )
                );

            return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
        }


        // PUT

        [HttpPut("{id:int}")]
        [ProducesResponseType(typeof(EntityData), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<EntityData>> Update(int id, [FromBody] Entity dto)
        {
            if (id <= 0)
                throw new ArgumentException(
                    GlobalException.FormatExceptionMessage(
                        "id must be a positive integer.",
                        nameof(Update),
                        new { id }
                    ),
                    nameof(id)
                );

            if (dto is null)
                throw new ArgumentNullException(
                    nameof(dto),
                    GlobalException.FormatExceptionMessage(
                        "DTO cannot be null.",
                        nameof(Update),
                        new { id }
                    )
                );

            if (dto.LocalizedPairs == null || dto.LocalizedPairs.Values == null || !dto.LocalizedPairs.Values.Any())
                throw new ArgumentException(
                    GlobalException.FormatExceptionMessage(
                        "LocalizedPairs.Values cannot be empty.",
                        nameof(Update),
                        new { id, dto?.LocalizedPairs?.Key }
                    ),
                    nameof(dto.LocalizedPairs)
                );

            var updated = await _repo.UpdateAsync(id, dto);

            if (updated is null)
                throw new ResourceNotFoundException(
                    GlobalException.FormatExceptionMessage(
                        "Entity not found.",
                        nameof(Update),
                        new { id }
                    )
                );

            return Ok(updated);
        }

    }
}