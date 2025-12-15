// <copyright file="DiscountsController.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Syed Hussain</author>
// <date>11/26/2025</date>
// <summary>SQL statements for discount definitions listing.</summary>

using GMS.TifoXRCoreWebAPI.Middleware.Errors;
using GMS.TifoXRCoreWebAPI.Models;
using GMS.TifoXRCoreWebAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace GMS.TifoXRCoreWebAPI.Controllers
{
    [ApiController]
    [Route("api/space/{spaceId:int}/discounts")]
    [Produces("application/json")]
    public sealed class DiscountsController : ControllerBase
    {
        private readonly IDiscountsService _svc;
        public DiscountsController(IDiscountsService svc) => _svc = svc;

        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<DiscountDto>>> GetDiscounts(int spaceId)
        {
            if (spaceId <= 0)
                throw ErrorService.Exception(
                    ErrorType.Argument, nameof(GetDiscounts),
                    ErrorMessages.Validation.PositiveIntRequired, paramName: nameof(spaceId),
                    parameters: new { spaceId });

            var rows = await _svc.ListActiveBySpaceAsync(spaceId);
            return Ok(rows);
        }

        /// <summary>Create a discount for a space (also inserts i18n for supported locales).</summary>
        [HttpPost]
        [ProducesResponseType(typeof(object), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<object>> CreateDiscount(int spaceId, [FromBody] CreateDiscountDto dto)
        {
            if (spaceId <= 0)
                throw ErrorService.Exception(
                    ErrorType.Argument, nameof(CreateDiscount),
                    ErrorMessages.Validation.PositiveIntRequired, paramName: nameof(spaceId),
                    parameters: new { spaceId });

            if (dto is null)
                throw ErrorService.Exception(
                    ErrorType.ArgumentNull, nameof(CreateDiscount),
                    ErrorMessages.Validation.MissingParameter, paramName: nameof(dto),
                    parameters: new { spaceId });

            // TODO: replace with your authenticated user/actor if available
            var created = await _svc.CreateAsync(spaceId, dto, modifiedBy: "system");

            // We don’t have a GetById route; returning 201 with the created payload
            return Created($"/api/space/{spaceId}/discounts", new { discount = created });
        }

        [HttpPut("{discountId:int}")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<object>> UpdateDiscount(
        int spaceId, int discountId, [FromBody] UpdateDiscountDto dto)
        {
            var updated = await _svc.UpdateAsync(spaceId, discountId, dto, modifiedBy: "system");
            return Ok(new { discount = updated });
        }

        [HttpDelete("{discountId:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteDiscount(
        int spaceId,
        int discountId,
        [FromQuery] bool hard = false)
        {
            // TODO: resolve from auth/user context; consistent with your other endpoints
            var modifiedBy = "system";

            await _svc.DeleteAsync(spaceId, discountId, hard, modifiedBy);
            return NoContent();
        }
    }
}


