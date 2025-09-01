// Controllers/OrdersController.cs
using GMS.TifoXRCoreWebAPI.Models;
using GMS.TifoXRCoreWebAPI.Repositories.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace GMS.TifoXRCoreWebAPI.Controllers
{
    [ApiController]
    [Route("api/space/{spaceId:int}/orders")]
    public sealed class OrdersController : ControllerBase
    {
        private readonly IOrderRepository _repo;

        public OrdersController(IOrderRepository repo)
        {
            _repo = repo;
        }

        [HttpGet("{orderId}")]
        public async Task<IActionResult> GetOrder(int spaceId, string orderId)
        {
            if (string.IsNullOrWhiteSpace(orderId))
                return BadRequest("orderId is required.");

            var dto = await _repo.GetOrderAsync(spaceId, orderId);
            if (dto is null)
                return NotFound();

            return Ok(dto);
        }

        [HttpGet("{orderId}/entitlements")]
        public async Task<IActionResult> GetEntitlements(int spaceId, string orderId)
        {
            if (string.IsNullOrWhiteSpace(orderId))
                return BadRequest("orderId is required.");

            var result = await _repo.GetEntitlementsByOrderAsync(spaceId, orderId);
            if (result is null)
                return NotFound(); // order not found in this space

            return Ok(result); // possibly empty list
        }

        [HttpPost]
        public async Task<IActionResult> Create(int spaceId, [FromBody] CreateOrderRequest req)
        {
            if (req is null) return BadRequest("Body is required.");
            // Prefer path spaceId; ignore/override any body space id to avoid leakage
            var resp = await _repo.CreateOrderAsync(spaceId, req);
            return Ok(resp);
        }
    }
}
