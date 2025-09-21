// <copyright file="OrderController.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Saad Sohail</author>
// <date>09/02/2025</date>
// <summary></summary>

using Microsoft.AspNetCore.Mvc;
//
using GMS.TifoXRCoreWebAPI.Models;
using GMS.TifoXRCoreWebAPI.Services;
using GMS.TifoXRCoreWebAPI.Application.PaymentGateways.Utils;

namespace GMS.TifoXRCoreWebAPI.Controllers
{
    [ApiController]
    [Route("api/space/{spaceId:int}/orders")]
    public class OrdersController(
        IOrderService orders, 
        IPaymentService payments, 
        IPaymentQueryService paymentQueries) : ControllerBase
    {
        private readonly IOrderService _orders = orders;
        private readonly IPaymentService _payments = payments;
        private readonly IPaymentQueryService _paymentQueries = paymentQueries;

        // ---------------- Basics ----------------

        [HttpGet("{orderId}")]
        public Task<ActionResult<OrderDto?>> GetOrder(int spaceId, string orderId)
            => Handle(async () => await _orders.GetOrderAsync(spaceId, orderId));

        [HttpGet("{orderId}/entitlements")]
        public Task<ActionResult<List<EntitlementDto>?>> GetEntitlements(int spaceId, string orderId) =>
            Handle(() =>
            {
                if (string.IsNullOrWhiteSpace(orderId))
                    throw new ArgumentException("orderId is required.", nameof(orderId));

                // IMPORTANT: do NOT make this an 'async' lambda; just return the Task from the service
                return _orders.GetEntitlementsByOrderAsync(spaceId, orderId);
            });

        [HttpGet("{orderId}/invoices")]
        public Task<ActionResult<InvoiceListResponse?>> GetInvoices(int spaceId, string orderId)
            => Handle(async () => await _orders.GetInvoicesAsync(spaceId, orderId));

        [HttpPost("create")]
        public Task<ActionResult<CreateOrderResponse>> CreateOrder(int spaceId, [FromBody] CreateOrderRequest req, [FromQuery] int? gatewayId = null)
            => Handle(async () =>
            {
                if (req is null) throw new ArgumentNullException(nameof(req));
                // repository already accepts (spaceId, req, gatewayId) — keep shape identical
                return await HttpContext.RequestServices
                    .GetRequiredService<Repositories.IOrderRepository>()
                    .CreateOrderAsync(spaceId, req, gatewayId);
            });

        // ------------- Payment Intents (queries) -------------

        [HttpGet("{orderId}/checkout/pending")]
        public Task<ActionResult<PendingIntentResponse>> GetCheckoutForOrder(int spaceId, string orderId)
            => Handle(() => _paymentQueries.GetPendingForOrderAsync(spaceId, orderId));

        [HttpGet("checkout/by-item")]
        public Task<ActionResult<PendingIntentResponse>> FindPendingIntentByItem(
            int spaceId, [FromQuery] string userId, [FromQuery] int itemTypeId, [FromQuery] int itemRefId)
            => Handle(() => _paymentQueries.FindPendingByItemAsync(spaceId, userId, itemTypeId, itemRefId));

        // ------------- Payments -------------

        [HttpPost("{orderId}/payment-intents")]
        [HttpPost("{orderId}/checkout")]
        public Task<ActionResult<CreatePaymentIntentResponse>> CreateCheckout(
            int spaceId, string orderId, [FromBody] CreatePaymentIntentRequest req, [FromQuery] int gatewayId)
            => Handle(() =>
            {
                return req is null
                ? throw new ArgumentNullException(nameof(req))
                : _payments.CreateCheckoutAsync(spaceId, orderId, gatewayId, req);
            });

        [HttpGet("{orderId}/payments/stripe/return")]
        public async Task<IActionResult> StripeReturn(
            int spaceId,
            string orderId,
            [FromQuery(Name = "session_id")] string sessionId)
        {
            if (string.IsNullOrWhiteSpace(orderId)) return BadRequest("orderId is required.");
            if (string.IsNullOrWhiteSpace(sessionId)) return BadRequest("session_id is required.");

            var order = await _orders.GetOrderAsync(spaceId, orderId);
            if (order is null) return NotFound();

            var intent = order.PaymentIntents.FirstOrDefault(i =>
                string.Equals(i.ProviderIntentId, sessionId, StringComparison.OrdinalIgnoreCase));

            if (intent is null)
                return NotFound(new { message = "payment_intent not found for provided session_id." });

            var idemKey = $"cap::{orderId}::{intent.Id}::{sessionId}";

            await _payments.CaptureAsync(
                spaceId, orderId, intent.Id,
                new ConfirmPaymentIntentRequest { IdempotencyKey = idemKey, ProviderIntentId = sessionId });

            // return a plain 200; avoids string generic ActionResult
            return Ok("Payment Successful. You can return to your space to continue.");
        }

        [HttpGet("{orderId}/payments/paypal/return")]
        public async Task<IActionResult> PaypalReturn(
            int spaceId,
            string orderId,
            [FromQuery] string token,
            [FromQuery] string? state = null)
        {
            if (string.IsNullOrWhiteSpace(orderId)) return BadRequest("orderId is required.");
            if (string.IsNullOrWhiteSpace(token)) return BadRequest("token is required.");

            var order = await _orders.GetOrderAsync(spaceId, orderId);
            if (order is null) return NotFound();

            var intent = order.PaymentIntents.FirstOrDefault(i =>
                string.Equals(i.ProviderIntentId, token, StringComparison.OrdinalIgnoreCase));

            if (intent is null)
                return NotFound(new { message = "payment_intent not found for provided token." });

            var idemKey = $"cap::{orderId}::{intent.Id}::{token}";

            await _payments.CaptureAsync(
                spaceId, orderId, intent.Id,
                new ConfirmPaymentIntentRequest { IdempotencyKey = idemKey, ProviderIntentId = token });

            return Ok("Payment Successful. You can return to your space to continue.");
        }

        [HttpPost("{orderId}/charges/{chargeId}/refunds")]
        public Task<ActionResult<RefundResponse>> RefundCharge(int spaceId, string orderId, string chargeId, [FromBody] RefundRequest req)
            => Handle(() => _payments.RefundAsync(spaceId, orderId, chargeId, req));

        // ------------- Verify & Reconcile -------------

        [HttpGet("{orderId}/verify")]
        public Task<ActionResult<VerifyPaymentResponse>> Verify(int spaceId, string orderId)
            => Handle(async () =>
            {
                var dto = await _orders.GetOrderAsync(spaceId, orderId) ?? throw new KeyNotFoundException();
                return VerifyResponseMapper.Build(dto);
            });

        [HttpPost("{orderId}/reconcile")]
        public Task<ActionResult<VerifyPaymentResponse>> Reconcile(int spaceId, string orderId, [FromBody] ReconcileRequest body)
            => Handle(async () =>
            {
                var before = await _orders.GetOrderAsync(spaceId, orderId) ?? throw new KeyNotFoundException();

                // Try to auto-capture when caller provides both ids + idempotency key
                if (body?.AttemptCaptureIfApproved == true &&
                    !string.IsNullOrWhiteSpace(body.IntentId) &&
                    !string.IsNullOrWhiteSpace(body.ProviderIntentId) &&
                    !string.IsNullOrWhiteSpace(body.IdempotencyKey))
                {
                    await _payments.CaptureAsync(spaceId, orderId, body.IntentId!, new ConfirmPaymentIntentRequest
                    {
                        IdempotencyKey = body.IdempotencyKey!,
                        ProviderIntentId = body.ProviderIntentId
                    });
                }

                var after = await _orders.GetOrderAsync(spaceId, orderId) ?? throw new KeyNotFoundException();
                return VerifyResponseMapper.Build(after);
            });

        // GET /api/space/{spaceId}/orders/{orderId}/payment-intents/{intentId}/crypto/prepared?sender=0x...
        [HttpGet("{orderId}/payment-intents/{intentId}/crypto/prepared")]
        [HttpGet("{orderId}/checkout/{intentId}/crypto/prepared")]
        public async Task<IActionResult> GetCryptoPrepared(
            int spaceId,
            string orderId,
            string intentId,
            [FromQuery] string sender)
        {
            if (string.IsNullOrWhiteSpace(sender))
                return BadRequest("sender is required (0x EVM address).");

            // Service derives gateway/providerIntentId from the intent context
            var json = await _payments.GetPreparedClientPayloadAsync(spaceId, orderId, intentId, sender);
            return Content(json, "application/json");
        }

        // POST /api/space/{spaceId}/orders/{orderId}/payment-intents/{intentId}/crypto/report-tx
        public sealed record ReportTxRequest(string TxHash);
        [HttpPost("{orderId}/payment-intents/{intentId}/crypto/report-tx")]
        public async Task<IActionResult> ReportOnChainTx(
            int spaceId,
            string orderId,
            string intentId,
            [FromBody] ReportTxRequest body)
        {
            if (body is null || string.IsNullOrWhiteSpace(body.TxHash))
                return BadRequest("txHash is required.");

            // Service resolves gateway + providerIntentId from the intent context
            await _payments.ReportOnChainTxAsync(spaceId, orderId, intentId, body.TxHash);
            return NoContent();
        }

        // ------------- Crypto Helpers -------------

        [HttpGet("~/pay/crypto")]
        [HttpGet("~/pay/crypto.html")]
        [Produces("text/html")]
        public IActionResult CryptoExecute()
        {
            var path = Path.Combine(Environment.CurrentDirectory, "wwwroot", "pay", "crypto.html");
            if (!System.IO.File.Exists(path)) return NotFound("Link not found");
            return PhysicalFile(path, "text/html; charset=utf-8");
        }

        // ---------------- plumbing: unified result helper ----------------
        private async Task<ActionResult<T>> Handle<T>(Func<Task<T>> action)
        {
            try
            {
                var result = await action();
                if (result is null) return NotFound();
                return Ok(result);
            }
            catch (ArgumentException ex) { return BadRequest(ex.Message); }
            catch (KeyNotFoundException) { return NotFound(); }
        }
    }
}