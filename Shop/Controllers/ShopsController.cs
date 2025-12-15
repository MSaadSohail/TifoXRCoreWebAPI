// <copyright file="ShopsController.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Syed Hussain</author>
// <date>11/25/2025</date>
// <summary>Endpoints for listing shops by space and fetching a single shop</summary>

using GMS.TifoXRCoreWebAPI.Middleware;
using GMS.TifoXRCoreWebAPI.Middleware.Errors;
using GMS.TifoXRCoreWebAPI.Models;
using GMS.TifoXRCoreWebAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace GMS.TifoXRCoreWebAPI.Controllers
{
    [ApiController]
    [Route("api/space/{spaceId:int}/shops")]
    [Produces("application/json")]
    public sealed class ShopsController : ControllerBase
    {
        private readonly IShopService _svc;
        public ShopsController(IShopService svc) => _svc = svc;

        [HttpGet]
        [ProducesResponseType(typeof(IReadOnlyList<ShopDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IReadOnlyList<ShopDto>>> GetShops(int spaceId)
        {
            if (spaceId <= 0)
                throw ErrorService.Exception(ErrorType.Argument, nameof(GetShops),
                    ErrorMessages.Validation.PositiveIntRequired, paramName: nameof(spaceId),
                    parameters: new { spaceId });

            var rows = await _svc.ListBySpaceAsync(spaceId);
            return Ok(rows);
        }

        [HttpGet("{shopId:int}")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<object>> GetShop(int spaceId, int shopId)
        {
            if (spaceId <= 0)
                throw ErrorService.Exception(ErrorType.Argument, nameof(GetShop),
                    ErrorMessages.Validation.PositiveIntRequired, paramName: nameof(spaceId),
                    parameters: new { spaceId, shopId });

            if (shopId <= 0)
                throw ErrorService.Exception(ErrorType.Argument, nameof(GetShop),
                    ErrorMessages.Validation.PositiveIntRequired, paramName: nameof(shopId),
                    parameters: new { spaceId, shopId });

            var row = await _svc.GetByIdAsync(spaceId, shopId);
            if (row is null)
                throw ErrorService.Exception(ErrorType.NotFound, nameof(GetShop),
                    ErrorMessages.Http.NotFound, parameters: new { spaceId, shopId });

            return Ok(new { shop = row });
        }

        // EXACT PATH per spec: POST /api/space/{spaceId}/shop
        [HttpPost("~/api/space/{spaceId:int}/shop")]
        [ProducesResponseType(typeof(object), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<object>> CreateShop(int spaceId, [FromBody] CreateShopDto dto)
        {
            if (spaceId <= 0)
                throw ErrorService.Exception(ErrorType.Argument, nameof(CreateShop),
                    ErrorMessages.Validation.PositiveIntRequired, paramName: nameof(spaceId),
                    parameters: new { spaceId });

            if (dto is null)
                throw ErrorService.Exception(ErrorType.ArgumentNull, nameof(CreateShop),
                    ErrorMessages.Validation.MissingParameter, paramName: nameof(dto),
                    parameters: new { spaceId });

            try
            {
                var created = await _svc.CreateAsync(spaceId, dto, "system");
                // Reuse GET single for location
                return CreatedAtAction(nameof(GetShop),
                    routeValues: new { spaceId, shopId = created.Id },
                    value: new { shop = created });
            }
            catch (UnsupportedLocalesException ulex)
            {
                // 422 with unsupported locales list
                return UnprocessableEntity(new
                {
                    code = 422,
                    errorMessage = "Unsupported locales for this space.",
                    details = new { unsupported = ulex.Locales }
                });
            }
        }
        /// <summary>
        /// Update a shop’s core details (and optional i18n upsert). Response returns core fields only.
        /// </summary>
        [HttpPut("{shopId:int}")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<object>> UpdateShop(int spaceId, int shopId, [FromBody] UpdateShopDto dto)
        {
            if (spaceId <= 0)
                throw ErrorService.Exception(
                    ErrorType.Argument,
                    nameof(UpdateShop),
                    ErrorMessages.Validation.PositiveIntRequired,
                    paramName: nameof(spaceId),
                    parameters: new { spaceId, shopId });

            if (shopId <= 0)
                throw ErrorService.Exception(
                    ErrorType.Argument,
                    nameof(UpdateShop),
                    ErrorMessages.Validation.PositiveIntRequired,
                    paramName: nameof(shopId),
                    parameters: new { spaceId, shopId });

            var row = await _svc.UpdateAsync(spaceId, shopId, dto, "system");
            return Ok(new { shop = row });
        }

        /// <summary>Hard delete a shop and its shop_items under the space.</summary>
        [HttpDelete("{shopId:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteShop(int spaceId, int shopId)
        {
            // Supply your authenticated user or "system"
            var modifiedBy = User?.Identity?.Name ?? "system";

            await _svc.DeleteAsync(spaceId, shopId, modifiedBy);
            return NoContent();
        }
    }
}
