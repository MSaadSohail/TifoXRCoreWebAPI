// <copyright file="PaypalGateway.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Saad Sohail</author>
// <date>09/02/2025</date>
// <summary></summary>

using Microsoft.Extensions.Options;
using System.Globalization;
using PaypalServerSdk.Standard;
using PaypalServerSdk.Standard.Authentication;
using PaypalServerSdk.Standard.Models;
using PpAppContext = PaypalServerSdk.Standard.Models.OrderApplicationContext;

namespace GMS.TifoXRCoreWebAPI.Application.PaymentGateways
{
    public sealed class PaypalGateway : IPaymentGateway
    {
        private readonly PaypalServerSdkClient _sdk;
        private readonly ILogger<PaypalGateway> _logger;

        public string Name => "paypal";

        public PaypalGateway(IOptions<PayPalOptions> opts, ILogger<PaypalGateway> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            var o = opts?.Value ?? throw new ArgumentNullException(nameof(opts));

            if (string.IsNullOrWhiteSpace(o.ClientId)) throw new ArgumentException("PayPal ClientId is missing.");
            if (string.IsNullOrWhiteSpace(o.Secret)) throw new ArgumentException("PayPal Secret is missing.");

            var env = string.Equals(o.Environment, "Production", StringComparison.OrdinalIgnoreCase)
                ? PaypalServerSdk.Standard.Environment.Production
                : PaypalServerSdk.Standard.Environment.Sandbox;

            _sdk = new PaypalServerSdkClient.Builder()
                .ClientCredentialsAuth(new ClientCredentialsAuthModel.Builder(o.ClientId, o.Secret).Build())
                .Environment(env)
                .Build();

            _logger.LogInformation("PayPal SDK initialized for {Env}", env);
        }

        public async Task<CreateGatewayIntentResult> CreateIntentAsync(CreateGatewayIntentRequest req)
        {
            if (req is null) throw new ArgumentNullException(nameof(req));

            if (string.IsNullOrWhiteSpace(req.IdempotencyKey))
                throw new ArgumentException("IdempotencyKey is required.", nameof(req.IdempotencyKey));

            if (string.IsNullOrWhiteSpace(req.ReturnUrl) || string.IsNullOrWhiteSpace(req.CancelUrl))
                throw new ArgumentException("ReturnUrl and CancelUrl are required.");

            // TODO: map CurrencyId -> ISO (USD/EUR/…) via a resolver; hardcoded for now.
            var orderReq = new OrderRequest
            {
                Intent = CheckoutPaymentIntent.Capture,
                PurchaseUnits = new()
                {
                    new PurchaseUnitRequest
                    {
                        Amount = new AmountWithBreakdown
                        {
                            CurrencyCode = "USD",
                            MValue = req.Amount.ToString("0.00", CultureInfo.InvariantCulture)
                        }
                    }
                },
                ApplicationContext = new PpAppContext
                {
                    ReturnUrl = req.ReturnUrl,
                    CancelUrl = req.CancelUrl
                }
            };

            var createInput = new CreateOrderInput(
                contentType: "application/json",
                body: orderReq,
                paypalRequestId: req.IdempotencyKey,
                prefer: "return=representation");

            var created = await _sdk.OrdersController.CreateOrderAsync(createInput);
            if (created is null)
                throw new InvalidOperationException("PayPal CreateOrder returned null response.");
            if (created.Data is null)
                throw new InvalidOperationException("PayPal CreateOrder returned empty body. Check credentials, environment, and headers.");

            var providerOrderId = created.Data.Id
                ?? throw new InvalidOperationException("PayPal order id missing in response.");

            var approve = created.Data.Links?.FirstOrDefault(l => l.Rel == "approve")?.Href;

            return new CreateGatewayIntentResult(providerOrderId, approve);
        }


        public async Task<CaptureGatewayResult> CaptureAsync(CaptureGatewayRequest req)
        {
            if (req is null) throw new ArgumentNullException(nameof(req));
            if (string.IsNullOrWhiteSpace(req.IdempotencyKey))
                throw new ArgumentException("IdempotencyKey is required.", nameof(req.IdempotencyKey));
            if (string.IsNullOrWhiteSpace(req.ProviderIntentId))
                throw new ArgumentException("ProviderIntentId is required.", nameof(req.ProviderIntentId));

            var captured = await _sdk.OrdersController.CaptureOrderAsync(new CaptureOrderInput(
                id: req.ProviderIntentId,
                contentType: "application/json",
                paypalRequestId: req.IdempotencyKey,
                prefer: "return=representation"));

            var cap = captured?.Data?.PurchaseUnits?
                .SelectMany(u => u.Payments?.Captures ?? Enumerable.Empty<OrdersCapture>())
                .FirstOrDefault();

            if (cap is null)
            {
                _logger.LogError("PayPal CaptureOrder returned no capture object for {ProviderIntentId}", req.ProviderIntentId);
                throw new InvalidOperationException("Capture not returned by PayPal.");
            }

            var amount = decimal.Parse(cap.Amount?.MValue ?? "0.00", CultureInfo.InvariantCulture);
            var iso = cap.Amount?.CurrencyCode ?? "USD";

            return new CaptureGatewayResult(cap.Id, amount, iso);
        }

        public async Task<GatewayIntentStatusResult?> GetIntentAsync(string providerIntentId)
        {
            if (string.IsNullOrWhiteSpace(providerIntentId))
                throw new ArgumentException("providerIntentId is required.", nameof(providerIntentId));

            var got = await _sdk.OrdersController.GetOrderAsync(new GetOrderInput(providerIntentId));
            var status = got?.Data?.Status;
            var approve = got?.Data?.Links?.FirstOrDefault(l => l.Rel == "approve")?.Href;

            if (got?.Data is null || status == OrderStatus._Unknown) return null;

            return new GatewayIntentStatusResult(providerIntentId, status!.Value.ToString(), approve);
        }

        public Task<RefundGatewayResult> RefundAsync(RefundGatewayRequest req)
            => throw new NotImplementedException("PayPal refund via capture not implemented yet.");
    }
}
