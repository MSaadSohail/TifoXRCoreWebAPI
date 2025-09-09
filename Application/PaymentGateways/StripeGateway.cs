// <copyright file="StripeGateway.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Saad Sohail</author>
// <date>9/9/2025</date>
// <summary></summary>

using Microsoft.Extensions.Options;
using System.ClientModel.Primitives;
//
using Stripe;
using Stripe.Checkout;

namespace GMS.TifoXRCoreWebAPI.Application.PaymentGateways
{
    /// <summary>
    /// Stripe gateway that mirrors your PayPal pattern:
    /// - CreateIntentAsync -> creates a Checkout Session (manual capture), returns ApproveLink
    /// - GetIntentAsync -> inspects Session/PaymentIntent, returns APPROVED when requires_capture
    /// - CaptureAsync -> captures the PaymentIntent (server-side)
    /// - RefundAsync -> creates a Refund
    /// </summary>
    public sealed class StripeGateway : IPaymentGateway
    {
        private readonly StripeClient _client;
        private readonly ILogger<StripeGateway> _logger;

        public string Name => "stripe";

        public StripeGateway(IOptions<StripeOptions> opts, ILogger<StripeGateway> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            var o = opts?.Value ?? throw new ArgumentNullException(nameof(opts));

            if (string.IsNullOrWhiteSpace(o.SecretKey))
                throw new ArgumentException("Stripe SecretKey is missing.");

            _client = new StripeClient(o.SecretKey);

            _logger.LogInformation("Stripe SDK initialized with API version override {Version}", o.ApiVersion);
        }

        public async Task<CreateGatewayIntentResult> CreateIntentAsync(CreateGatewayIntentRequest req)
        {
            if (req is null) throw new ArgumentNullException(nameof(req));

            if (string.IsNullOrWhiteSpace(req.IdempotencyKey))
                throw new ArgumentException("IdempotencyKey is required.", nameof(req.IdempotencyKey));
            
            if (string.IsNullOrWhiteSpace(req.ReturnUrl) || string.IsNullOrWhiteSpace(req.CancelUrl))
                throw new ArgumentException("ReturnUrl and CancelUrl are required.");

            // TODO: Resolve CurrencyId -> ISO via your repo helper (for now hard-code "USD" like PayPal).
            var currencyIso = "USD"; // match your current PayPal stub; replace when you wire ResolveCurrencyIsoAsync

            // Build a Checkout Session with manual capture so it mirrors "approve -> capture"
            var sessionCreate = new SessionCreateOptions
            {
                Mode = "payment",
                SuccessUrl = $"{req.ReturnUrl}?session_id={{CHECKOUT_SESSION_ID}}",
                CancelUrl = req.CancelUrl,

                // Use a single “order total” line item; Stripe requires unit_amount in the smallest unit.
                LineItems = new List<SessionLineItemOptions>
                {
                    new()
                    {
                        Quantity = 1,
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            Currency = currencyIso,
                            UnitAmountDecimal = decimal.Round(req.Amount * 100m, 0, MidpointRounding.AwayFromZero),
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = "Order",
                                Description = "Order total"
                            }
                        }
                    }
                },

                // The key to mirror your PayPal APPROVED->CAPTURE flow:
                PaymentIntentData = new SessionPaymentIntentDataOptions
                {
                    CaptureMethod = "manual" // -> PaymentIntent.status = requires_capture after successful auth
                },

                // Let Stripe pick the best payment method set (always current-best on latest API).
                AutomaticTax = new SessionAutomaticTaxOptions { Enabled = false },
                ConsentCollection = new SessionConsentCollectionOptions { TermsOfService = "none" },
            };

            var service = new SessionService(_client);
            var reqOptions = new Stripe.RequestOptions { IdempotencyKey = req.IdempotencyKey };
            var session = await service.CreateAsync(sessionCreate, reqOptions);

            if (session == null || string.IsNullOrWhiteSpace(session.Id))
                throw new InvalidOperationException("Stripe Checkout Session creation failed.");

            // ApproveLink = URL the client should open (Stripe-hosted checkout).
            return new CreateGatewayIntentResult(
                ProviderIntentId: session.Id,
                ApproveLink: session.Url
            );
        }

        public async Task<GatewayIntentStatusResult?> GetIntentAsync(string providerIntentId)
        {
            if (string.IsNullOrWhiteSpace(providerIntentId))
                throw new ArgumentException("providerIntentId is required.", nameof(providerIntentId));

            // providerIntentId is the Checkout Session id we issued.
            var sessionSvc = new SessionService(_client);
            var session = await sessionSvc.GetAsync(providerIntentId);

            // Session may not yet have a PaymentIntent if the user hasn't completed checkout.
            if (session.PaymentIntentId == null)
                return new GatewayIntentStatusResult(providerIntentId, "PENDING", session.Url);

            var piSvc = new PaymentIntentService(_client);
            var pi = await piSvc.GetAsync(session.PaymentIntentId);

            // Map Stripe -> our generic status
            // requires_capture ~= "APPROVED" (authorized and ready to capture)
            var status = pi.Status switch
            {
                "requires_capture" => "APPROVED",
                "succeeded" => "SUCCEEDED",
                "processing" => "PROCESSING",
                "requires_payment_method" or "requires_confirmation" => "REQUIRES_ACTION",
                "canceled" => "CANCELED",
                _ => pi.Status?.ToUpperInvariant() ?? "UNKNOWN"
            };

            return new GatewayIntentStatusResult(providerIntentId, status, session.Url);
        }

        public async Task<CaptureGatewayResult> CaptureAsync(CaptureGatewayRequest req)
        {
            if (req is null) throw new ArgumentNullException(nameof(req));
            if (string.IsNullOrWhiteSpace(req.IdempotencyKey))
                throw new ArgumentException("IdempotencyKey is required.", nameof(req.IdempotencyKey));
            if (string.IsNullOrWhiteSpace(req.ProviderIntentId))
                throw new ArgumentException("ProviderIntentId is required.", nameof(req.ProviderIntentId));

            // Step 1: resolve Checkout Session -> PaymentIntent
            var sessionSvc = new SessionService(_client);
            var session = await sessionSvc.GetAsync(req.ProviderIntentId);
            if (string.IsNullOrWhiteSpace(session.PaymentIntentId))
                throw new InvalidOperationException("Stripe session has no PaymentIntent yet (user didn’t complete approval).");

            var piSvc = new PaymentIntentService(_client);

            // Step 2: capture
            var cap = await piSvc.CaptureAsync(
                session.PaymentIntentId,
                new PaymentIntentCaptureOptions(),
                new Stripe.RequestOptions { IdempotencyKey = req.IdempotencyKey });

            if (cap.Status is not ("succeeded" or "requires_capture" or "processing"))
            {
                // Stripe can be asynchronous for some pm types; treat anything non-succeeded as error here
                // to match your current "approved->capture->paid" assumption.
                throw new InvalidOperationException($"Stripe capture did not succeed. Status={cap.Status}");
            }

            // Stripe amounts are in minor units; convert back to major to satisfy CaptureGatewayResult
            long capturedMinor = cap.AmountReceived > 0 ? cap.AmountReceived : cap.Amount;
            decimal capturedMajor = capturedMinor / 100m;

            return new CaptureGatewayResult(
                ProviderChargeId: cap.LatestChargeId ?? cap.Id,
                CapturedAmount: capturedMajor,
                CurrencyIso: cap.Currency?.ToUpperInvariant() ?? "USD"
            );
        }

        public async Task<RefundGatewayResult> RefundAsync(RefundGatewayRequest req)
        {
            if (req is null) throw new ArgumentNullException(nameof(req));
            
            if (string.IsNullOrWhiteSpace(req.ProviderChargeId))
                throw new ArgumentException("ProviderChargeId is required.", nameof(req.ProviderChargeId));

            var refundSvc = new RefundService(_client);
            var create = new RefundCreateOptions
            {
                Charge = req.ProviderChargeId,
                Amount = (long?)decimal.Round(req.Amount * 100m, 0, MidpointRounding.AwayFromZero),
                Reason = string.IsNullOrWhiteSpace(req.Reason) ? null : req.Reason
            };

            var refund = await refundSvc.CreateAsync(create, new Stripe.RequestOptions
            {
                IdempotencyKey = req.IdempotencyKey
            });

            var refundedMinor = refund.Amount;
            var refundedMajor = refundedMinor / 100m;

            return new RefundGatewayResult(
                ProviderRefundId: refund.Id,
                RefundedAmount: refundedMajor,
                CurrencyId: req.CurrencyId
            );
        }
    }
}

