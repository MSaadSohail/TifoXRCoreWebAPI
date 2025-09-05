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

        // ---------------------------
        // 1) VERIFY (DB-only, fast)
        // ---------------------------
        // GET /api/space/{spaceId}/orders/{orderId}/verify
        // Returns the authoritative DB view so the Unity "Confirm" button
        // can decide to show inventory or not (no external calls).
        [HttpGet("{orderId}/verify")]
        public async Task<IActionResult> Verify(int spaceId, string orderId)
        {
            if (string.IsNullOrWhiteSpace(orderId))
                return BadRequest("orderId is required.");

            var dto = await _repo.GetOrderAsync(spaceId, orderId); // aggregates order, intents, charges, entitlements, invoices
            if (dto is null) return NotFound();

            var capturedMinor = dto.PaymentIntents
                                   .SelectMany(i => i.Charges)
                                   .Sum(c => c.AmountCapturedMinor);

            var isPaid = capturedMinor >= dto.TotalNetAmount;
            var hasEntitlements = dto.Entitlements?.Any(e => e.GrantedDateTime != null) == true;
            var hasInvoice = dto.Invoices?.Any() == true;

            return Ok(new
            {
                orderId = dto.Id,
                isPaid,
                capturedMinor,
                totalNetMinor = dto.TotalNetAmount,
                hasEntitlements,
                hasInvoice,
                statusId = dto.StatusId
            });
        }

        // --------------------------------------------------------
        // 2) RECONCILE (DB check + optional capture if you pass it)
        // --------------------------------------------------------
        // POST /api/space/{spaceId}/orders/{orderId}/reconcile
        // Body: { "providerIntentId": "...", "intentId": "...", "idempotencyKey": "...", "attemptCaptureIfApproved": true }
        //
        // Strategy:
        // - If DB already shows paid + entitlements → return success (no external calls).
        // - Else if caller gives providerIntentId/intentId, we try Capture via PaymentService
        //   (idempotent in service; it will insert charge, mark paid, grant entitlements, and snapshot invoice).
        //   Then we re-read and return the same summary shape as /verify.
        //
        // NOTE: We intentionally don't call gateway.GetIntent() here because the current IPaymentService
        //       doesn't expose a "GetStatus" surface. Keeping the controller thin and using your service. 
        [HttpPost("{orderId}/reconcile")]
        public async Task<IActionResult> Reconcile(
            int spaceId,
            string orderId,
            [FromBody] ReconcileRequest body)
        {
            if (string.IsNullOrWhiteSpace(orderId))
                return BadRequest("orderId is required.");
            if (body is null)
                return BadRequest("Body is required.");

            // 0) DB fast-path
            var before = await _repo.GetOrderAsync(spaceId, orderId);
            if (before is null) return NotFound();

            var alreadyCaptured = before.PaymentIntents.SelectMany(i => i.Charges)
                                                       .Sum(c => c.AmountCapturedMinor);
            var alreadyPaid = alreadyCaptured >= before.TotalNetAmount;
            var alreadyGranted = before.Entitlements?.Any(e => e.GrantedDateTime != null) == true;

            if (alreadyPaid && alreadyGranted)
            {
                return Ok(new VerifyPaymentResponse
                {
                    OrderId = before.Id,
                    IsPaid = true,
                    CapturedMinor = alreadyCaptured,
                    TotalNetMinor = before.TotalNetAmount,
                    HasEntitlements = true,
                    HasInvoice = before.Invoices?.Any() == true,
                    Missing = []
                });
            }

            // 1) Decide which intent we will operate on (prefer explicit IDs from the request)
            string? intentId = body.IntentId;
            string? providerIntentId = body.ProviderIntentId;

            if (string.IsNullOrWhiteSpace(intentId) || string.IsNullOrWhiteSpace(providerIntentId))
            {
                // If caller only provided providerIntentId (PayPal token) or nothing,
                // search in the order's intents for a match to providerIntentId.
                if (!string.IsNullOrWhiteSpace(providerIntentId))
                {
                    intentId ??= before.PaymentIntents
                        .FirstOrDefault(i =>
                            string.Equals(i.ProviderIntentId, providerIntentId, StringComparison.OrdinalIgnoreCase))
                        ?.Id;
                }
                // If caller only provided intentId, try to fill providerIntentId from DB.
                else if (!string.IsNullOrWhiteSpace(intentId))
                {
                    providerIntentId = before.PaymentIntents
                        .FirstOrDefault(i => string.Equals(i.Id, intentId, StringComparison.OrdinalIgnoreCase))
                        ?.ProviderIntentId;
                }
            }

            // 2) Attempt capture (optional, only if we have both an intentId and a providerIntentId)
            if (body.AttemptCaptureIfApproved &&
                !string.IsNullOrWhiteSpace(intentId) &&
                !string.IsNullOrWhiteSpace(providerIntentId) &&
                !string.IsNullOrWhiteSpace(body.IdempotencyKey))
            {
                // PaymentService will:
                // - check APPROVED on gateway,
                // - CAPTURE,
                // - insert charge,
                // - mark order as Paid if covered,
                // - grant entitlements,
                // - generate invoice snapshot.
                //
                // This keeps the controller thin and idempotent. 
                var confirmReq = new ConfirmPaymentIntentRequest
                {
                    IdempotencyKey = body.IdempotencyKey!,
                    ProviderIntentId = providerIntentId
                };

                // Exceptions (e.g., not approved) will bubble as 400/409 via middleware if you map them.
                await _payments.CaptureAsync(spaceId, orderId, intentId!, confirmReq); // uses your current service
            }

            // 3) Re-read authoritative state and respond
            var after = await _repo.GetOrderAsync(spaceId, orderId); // aggregate re-check
            if (after is null) return NotFound();

            var capMinor = after.PaymentIntents.SelectMany(i => i.Charges).Sum(c => c.AmountCapturedMinor);
            var isPaid = capMinor >= after.TotalNetAmount;
            var hasEntitlements = after.Entitlements?.Any(e => e.GrantedDateTime != null) == true;
            var hasInvoice = after.Invoices?.Any() == true;

            var missing = new List<string>();
            if (!isPaid) missing.Add("charge");
            if (isPaid && !hasEntitlements) missing.Add("entitlements");
            if (isPaid && !hasInvoice) missing.Add("invoice");

            return Ok(new VerifyPaymentResponse
            {
                OrderId = after.Id,
                IsPaid = isPaid,
                CapturedMinor = capMinor,
                TotalNetMinor = after.TotalNetAmount,
                HasEntitlements = hasEntitlements,
                HasInvoice = hasInvoice,
                Missing = missing
            });
        }

        // ---------------------------------------------------------
        // 3) PAYPAL RETURN URL (user approved on PayPal, comes back)
        // ---------------------------------------------------------
        // GET /api/space/{spaceId}/orders/{orderId}/payments/paypal/return?token=EC-XXXX
        //
        // PayPal redirects to ReturnUrl with "token" (their order id).
        // We locate the matching intent by provider_intent_id == token and call Capture.
        // After capture, redirect user to your thank-you page or return JSON for Unity.
        [HttpGet("{orderId}/payments/paypal/return")]
        public async Task<IActionResult> PaypalReturn(
            int spaceId,
            string orderId,
            [FromQuery] string token,
            [FromQuery] string? state = null)
        {
            if (string.IsNullOrWhiteSpace(orderId)) return BadRequest("orderId is required.");
            if (string.IsNullOrWhiteSpace(token)) return BadRequest("token is required.");

            var order = await _repo.GetOrderAsync(spaceId, orderId);
            if (order is null) return NotFound();

            var intent = order.PaymentIntents.FirstOrDefault(i =>
                string.Equals(i.ProviderIntentId, token, StringComparison.OrdinalIgnoreCase));

            if (intent is null)
            {
                // Fallback: nothing found for this token
                return NotFound(new { message = "payment_intent not found for provided token." });
            }

            // Use a deterministic key if you pass back any state; else a new GUID is fine.
            var idemKey = $"cap::{orderId}::{intent.Id}::{token}";

            var resp = await _payments.CaptureAsync(
                spaceId, orderId, intent.Id,
                new ConfirmPaymentIntentRequest
                {
                    IdempotencyKey = idemKey,
                    ProviderIntentId = token
                });

            // For a Unity client, return JSON. For web, you could Redirect to a "success" route.
            return Ok(resp);
        }

        // -----------------------------------------------------------------
        // A) Find pending intent for a specific ORDER (status 1 or 2)
        // -----------------------------------------------------------------
        // GET /api/space/{spaceId}/orders/{orderId}/payment-intents/pending
        [HttpGet("{orderId}/payment-intents/pending")]
        public async Task<IActionResult> GetPendingIntentForOrder(int spaceId, string orderId)
        {
            if (string.IsNullOrWhiteSpace(orderId)) return BadRequest("orderId is required.");

            // validate order belongs to this space
            var order = await _repo.GetOrderAsync(spaceId, orderId);
            if (order is null) return NotFound();

            var pi = await _repo.GetPendingIntentForOrderAsync(orderId);
            if (pi is null) return Ok(new PendingIntentResponse { Found = false });

            return Ok(new PendingIntentResponse
            {
                Found = true,
                OrderId = orderId,
                PaymentIntentId = pi?.IntentId,
                StatusId = pi?.StatusId,
                IdempotencyKey = pi?.IdempotencyKey,         // Unity can reuse this key
                ProviderIntentId = pi?.ProviderIntentId,
                PaymentGatewayId = pi?.PaymentGatewayId,
                AmountMinor = pi?.AmountMinor,
                CurrencyId = pi?.CurrencyId
            });
        }

        // -----------------------------------------------------------------
        // B) Find pending intent for USER + ITEM (status 1 or 2)
        //    Use when Unity no longer has orderId after a restart.
        // -----------------------------------------------------------------
        // GET /api/space/{spaceId}/orders/pending-intent/by-item?userId=...&itemTypeId=...&itemRefId=...
        [HttpGet("pending-intent/by-item")]
        public async Task<IActionResult> FindPendingIntentByItem(
            int spaceId,
            [FromQuery] string userId,
            [FromQuery] int itemTypeId,
            [FromQuery] int itemRefId)
        {
            if (string.IsNullOrWhiteSpace(userId)) return BadRequest("userId is required.");

            var orderId = await _repo.FindLatestOrderIdWithPendingIntentAsync(spaceId, userId, itemTypeId, itemRefId);
            if (string.IsNullOrWhiteSpace(orderId))
                return Ok(new PendingIntentResponse { Found = false });

            var pi = await _repo.GetPendingIntentForOrderAsync(orderId);
            if (pi is null)
                return Ok(new PendingIntentResponse { Found = false });

            return Ok(new PendingIntentResponse
            {
                Found = true,
                OrderId = orderId,
                PaymentIntentId = pi?.IntentId,
                StatusId = pi?.StatusId,
                IdempotencyKey = pi?.IdempotencyKey,         // Unity should pass this back into CreatePaymentIntent
                ProviderIntentId = pi?.ProviderIntentId,
                PaymentGatewayId = pi?.PaymentGatewayId,
                AmountMinor = pi?.AmountMinor,
                CurrencyId = pi?.CurrencyId
            });
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
        [Obsolete("Deprecated on 2025-09-03. Use POST /api/space/{spaceId}/orders/{orderId}/reconcile instead.")]
        public async Task<IActionResult> ConfirmPaymentIntent(
            int spaceId, string orderId, string intentId, [FromBody] ConfirmPaymentIntentRequest req)
        {
            if (string.IsNullOrWhiteSpace(orderId)) return BadRequest("orderId is required.");
            if (string.IsNullOrWhiteSpace(intentId)) return BadRequest("intentId is required.");
            if (req is null) return BadRequest("Body is required.");

            var resp = await _payments.CaptureAsync(spaceId, orderId, intentId, req);
            return Ok(resp);
        }
    }
}
