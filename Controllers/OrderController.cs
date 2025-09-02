// Controllers/OrdersController.cs
using GMS.TifoXRCoreWebAPI.Models;
using GMS.TifoXRCoreWebAPI.Repositories.Interfaces;
using Microsoft.AspNetCore.Mvc;
using TifoXRCoreWebAPI.Services;

namespace GMS.TifoXRCoreWebAPI.Controllers
{
    [ApiController]
    [Route("api/space/{spaceId:int}/orders")]
    public class OrdersController(IOrderRepository repo, IPaymentService payments) : ControllerBase
    {
        private readonly IOrderRepository _repo = repo;
        private readonly IPaymentService _payments = payments;

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

        [HttpGet("{orderId}/invoices")]
        public async Task<IActionResult> GetInvoices(int spaceId, string orderId)
        {
            if (string.IsNullOrWhiteSpace(orderId)) return BadRequest("orderId is required.");
            var resp = await _repo.GetInvoicesByOrderAsync(spaceId, orderId);
            if (resp is null) return NotFound();
            return Ok(resp);
        }

        [HttpPost]
        public async Task<IActionResult> Create(int spaceId, [FromBody] CreateOrderRequest req)
        {
            if (req is null) return BadRequest("Body is required.");

            var resp = await _repo.CreateOrderAsync(spaceId, req);

            return Ok(resp);
        }

        [HttpPost("{orderId}/payment-intents")]
        public async Task<IActionResult> CreatePaymentIntent(
            int spaceId, string orderId, [FromBody] CreatePaymentIntentRequest req)
        {
            if (string.IsNullOrWhiteSpace(orderId)) return BadRequest("orderId is required.");
            if (req is null) return BadRequest("Body is required.");

            var resp = await _payments.CreateIntentAsync(spaceId, orderId, req);
            return Ok(resp);
        }

        [HttpPost("{orderId}/payment-intents/{intentId}/confirm")]
        public async Task<IActionResult> ConfirmPaymentIntent(
            int spaceId, string orderId, string intentId, [FromBody] ConfirmPaymentIntentRequest req)
        {
            if (string.IsNullOrWhiteSpace(orderId)) return BadRequest("orderId is required.");
            if (string.IsNullOrWhiteSpace(intentId)) return BadRequest("intentId is required.");
            if (req is null) return BadRequest("Body is required.");

            var resp = await _payments.CaptureAsync(spaceId, orderId, intentId, req);
            return Ok(resp);
        }

        [HttpPost("{orderId}/payment-intents/{intentId}/capture")]
        public async Task<IActionResult> CapturePaymentIntent(
            int spaceId, string orderId, string intentId, [FromBody] ConfirmPaymentIntentRequest req)
        {
            if (string.IsNullOrWhiteSpace(orderId)) return BadRequest("orderId is required.");
            if (string.IsNullOrWhiteSpace(intentId)) return BadRequest("intentId is required.");
            if (req is null) return BadRequest("Body is required.");

            var result = await _payments.CaptureAsync(spaceId, orderId, intentId, req);
            return Ok(result);
        }
    }
}
