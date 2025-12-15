using GMS.TifoXRCoreWebAPI.Models;
using GMS.TifoXRCoreWebAPI.Models.Item;
using GMS.TifoXRCoreWebAPI.Repositories.Interfaces;
using GMS.TifoXRCoreWebAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace GMS.TifoXRCoreWebAPI.Controllers
{
    [ApiController]
    [Route("api/space/{spaceId:int}/item")]
    public sealed class ItemController : ControllerBase
    {
        private readonly IItemService _svc;
        public ItemController(IItemService svc) => _svc = svc;

        [HttpPost]
        [ProducesResponseType(typeof(ItemCreatedEnvelope), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CreateItem(int spaceId, [FromBody] CreateItemDto dto)
        {
            var created = await _svc.AddAsync(spaceId, dto, modifiedBy: "system");
            return CreatedAtAction(
                actionName: nameof(GetById),
                routeValues: new { spaceId, itemId = created.ItemId },
                value: created
            );
        }

        // Minimal “get by id” to satisfy CreatedAtAction (you can flesh this out later)
        [HttpGet("{itemId:int}")]
        [ProducesResponseType(typeof(ItemCreatedEnvelope), StatusCodes.Status200OK)]
        public IActionResult GetById(int spaceId, int itemId)
        {
            // If you don’t have a reader yet, return 200 with a stub containing the id.
            return Ok(new { item_id = itemId, space_id = spaceId });
        }

        [HttpGet]
        [ProducesResponseType(typeof(IReadOnlyList<ShopItemsListRow>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IReadOnlyList<ShopItemsListRow>>> ListAll(int spaceId)
        {
            var rows = await _svc.ListAllAsync(spaceId);
            return Ok(rows);
        }

        [HttpPut("{itemId:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdateItem(int spaceId, int itemId, [FromBody] UpdateItemDto dto)
        {
            await _svc.UpdateAsync(spaceId, itemId, dto, modifiedBy: "system");
            return NoContent();
        }

        // Controllers/ItemController.cs  (add action)
        [HttpDelete("{itemId:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteItem(int spaceId, int itemId)
        {
            await _svc.DeleteAsync(spaceId, itemId, modifiedBy: "system");
            return NoContent();
        }


    }
}
