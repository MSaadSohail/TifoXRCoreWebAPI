// <copyright file="ShopItemsController.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Syed Hussain</author>
// <date>11/25/2025</date>
// <summary>Endpoints for listing items of a shop (within a space).</summary>

using GMS.TifoXRCoreWebAPI.Middleware;
using GMS.TifoXRCoreWebAPI.Middleware.Errors;
using GMS.TifoXRCoreWebAPI.Models;
using GMS.TifoXRCoreWebAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace GMS.TifoXRCoreWebAPI.Controllers
{
    [ApiController]
    [Route("api/space/{spaceId:int}/shop/{shopId:int}/items")]
    [Produces("application/json")]
    public sealed class ShopItemsController : ControllerBase
    {
        private readonly IShopItemsService _svc;
        public ShopItemsController(IShopItemsService svc) => _svc = svc;

        /// <summary>
        /// Get all items listed in a particular shop for a given space, including price, stock, and currency.
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(IReadOnlyList<ShopItemsListRow>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IReadOnlyList<ShopItemsListRow>>> GetAllItemsByShop(int spaceId, int shopId)
        {
            if (spaceId <= 0)
                throw ErrorService.Exception(
                    ErrorType.Argument,
                    nameof(GetAllItemsByShop),
                    ErrorMessages.Validation.PositiveIntRequired,
                    paramName: nameof(spaceId),
                    parameters: new { spaceId, shopId });

            if (shopId <= 0)
                throw ErrorService.Exception(
                    ErrorType.Argument,
                    nameof(GetAllItemsByShop),
                    ErrorMessages.Validation.PositiveIntRequired,
                    paramName: nameof(shopId),
                    parameters: new { spaceId, shopId });

            try
            {
                var rows = await _svc.ListByShopAsync(spaceId, shopId);
                return Ok(rows);
            }
            catch (KeyNotFoundException)
            {
                throw ErrorService.Exception(
                    ErrorType.NotFound,
                    nameof(GetAllItemsByShop),
                    ErrorMessages.Http.NotFound,
                    parameters: new { spaceId, shopId });
            }
        }

        [HttpGet("grouped")]
        [ProducesResponseType(typeof(ShopItemsGroupedResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<ShopItemsGroupedResponse>> GetItemsByShopGrouped(
    int spaceId, int shopId)
        {
            if (spaceId <= 0)
                throw ErrorService.Exception(
                    ErrorType.Argument,
                    nameof(GetItemsByShopGrouped),
                    ErrorMessages.Validation.PositiveIntRequired,
                    paramName: nameof(spaceId),
                    parameters: new { spaceId, shopId });

            if (shopId <= 0)
                throw ErrorService.Exception(
                    ErrorType.Argument,
                    nameof(GetItemsByShopGrouped),
                    ErrorMessages.Validation.PositiveIntRequired,
                    paramName: nameof(shopId),
                    parameters: new { spaceId, shopId });

            try
            {
                var grouped = await _svc.ListByShopGroupedAsync(spaceId, shopId);
                return Ok(grouped);
            }
            catch (KeyNotFoundException)
            {
                throw ErrorService.Exception(
                    ErrorType.NotFound,
                    nameof(GetItemsByShopGrouped),
                    ErrorMessages.Http.NotFound,
                    parameters: new { spaceId, shopId });
            }
        }


        /// <summary>
        /// Add an item to a shop with optional price override & stock; i18n upsert support.
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(ShopItemsListRow), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ShopItemsListRow>> Add(int spaceId, int shopId, [FromBody] CreateShopItemLinkDto dto)
        {
            if (spaceId <= 0 || shopId <= 0)
                throw ErrorService.Exception(
                    ErrorType.Argument, nameof(Add),
                    ErrorMessages.Validation.PositiveIntRequired,
                    parameters: new { spaceId, shopId });

            if (dto is null)
                throw ErrorService.Exception(
                    ErrorType.ArgumentNull, nameof(Add),
                    ErrorMessages.Validation.MissingParameter,
                    paramName: nameof(dto),
                    parameters: new { spaceId, shopId });

            // TODO: replace with authenticated user if available
            var modifiedBy = "system";

            var row = await _svc.AddAsync(spaceId, shopId, dto, modifiedBy);
            return CreatedAtAction(nameof(GetAllItemsByShop), new { spaceId, shopId }, row);
        }

        /// <summary>
        /// Update a shop item mapping (amount override and/or stock), and optional i18n upserts.
        /// </summary>
        [HttpPut("{itemId:int}")]
        [ProducesResponseType(typeof(ShopItemsListRow), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ShopItemsListRow>> UpdateShopItem(int spaceId, int shopId, int itemId, [FromBody] UpdateShopItemDto dto)
        {
            if (spaceId <= 0)
                throw ErrorService.Exception(ErrorType.Argument, nameof(UpdateShopItem),
                    ErrorMessages.Validation.PositiveIntRequired, paramName: nameof(spaceId), parameters: new { spaceId, shopId, itemId });

            if (shopId <= 0)
                throw ErrorService.Exception(ErrorType.Argument, nameof(UpdateShopItem),
                    ErrorMessages.Validation.PositiveIntRequired, paramName: nameof(shopId), parameters: new { spaceId, shopId, itemId });

            if (itemId <= 0)
                throw ErrorService.Exception(ErrorType.Argument, nameof(UpdateShopItem),
                    ErrorMessages.Validation.PositiveIntRequired, paramName: nameof(itemId), parameters: new { spaceId, shopId, itemId });

            if (dto is null)
                throw ErrorService.Exception(ErrorType.ArgumentNull, nameof(UpdateShopItem),
                    ErrorMessages.Validation.MissingParameter, paramName: nameof(dto), parameters: new { spaceId, shopId, itemId });

            // use your real actor if available
            var modifiedBy = "system";
            var row = await _svc.UpdateAsync(spaceId, shopId, itemId, dto, modifiedBy);
            return Ok(row);
        }

        [HttpDelete("{itemId:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteShopItem(int spaceId, int shopId, int itemId)
        {
            await _svc.DeleteAsync(spaceId, shopId, itemId);
            return NoContent();
        }


    }
}
