// <copyright file="PaypalGateway.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Saad Sohail</author>
// <date>09/02/2025</date>
// <summary></summary>

using System.Text;
using System.Text.Json;
using System.Globalization;
using System.Net.Http.Headers;
using Microsoft.Extensions.Options;
//
using PaypalServerSdk.Standard;
using PaypalServerSdk.Standard.Models;
using PaypalServerSdk.Standard.Authentication;
using PpAppContext = PaypalServerSdk.Standard.Models.OrderApplicationContext;
using TifoXRCoreWebAPI.Application.PaymentGateways.Utils;
using GMS.TifoXRCoreWebAPI.Application.PaymentGateways;

namespace TifoXRCoreWebAPI.Application.PaymentGateways.Paypal
{
    public sealed class PaypalGateway : IPaymentGateway
    {
        private readonly PaypalServerSdkClient _sdk;
        private readonly ILogger<PaypalGateway> _logger;
        private readonly string _clientId;
        private readonly string _secret;
        private readonly string _environment;
        private readonly HttpClient _http = new ();

        public string Name => "paypal";

        private string BaseUrl => string.Equals(_environment, "Production", StringComparison.OrdinalIgnoreCase)
                                ? "https://api-m.paypal.com"
                                : "https://api-m.sandbox.paypal.com";

        public PaypalGateway(IOptions<PayPalOptions> opts, ILogger<PaypalGateway> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            var o = opts?.Value ?? throw new ArgumentNullException(nameof(opts));

            if (string.IsNullOrWhiteSpace(o.ClientId)) throw new ArgumentException("PayPal ClientId is missing.");
            if (string.IsNullOrWhiteSpace(o.Secret)) throw new ArgumentException("PayPal Secret is missing.");

            _clientId = o.ClientId;
            _secret = o.Secret;
            _environment = string.IsNullOrWhiteSpace(o.Environment) ? "Sandbox" : o.Environment;

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

            return new GatewayIntentStatusResult(
                providerIntentId, 
                PayPalStatusMapper.ToGeneric(status!.Value.ToString()), 
                approve);
        }

        public async Task<RefundGatewayResult> RefundAsync(RefundGatewayRequest req)
        {
            if (req is null) throw new ArgumentNullException(nameof(req));
            if (string.IsNullOrWhiteSpace(req.ProviderChargeId))
                throw new ArgumentException("ProviderChargeId (PayPal capture id) is required.", nameof(req.ProviderChargeId));

            var url = $"{BaseUrl}/v2/payments/captures/{req.ProviderChargeId}/refund";
            var token = await GetAccessTokenAsync();

            using var msg = new HttpRequestMessage(HttpMethod.Post, url);
            msg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            if (!string.IsNullOrWhiteSpace(req.IdempotencyKey))
                msg.Headers.TryAddWithoutValidation("PayPal-Request-Id", req.IdempotencyKey);

            // Full refund: NO BODY. Partial refund: include amount.
            if (req.Amount > 0m)
            {
                var currencyCode = CurrencyMapper.ResolveIso(req.CurrencyId);
                var payload = new
                {
                    amount = new
                    {
                        currency_code = currencyCode,
                        value = decimal.Round(req.Amount, 2, MidpointRounding.AwayFromZero)
                                    .ToString("0.00", CultureInfo.InvariantCulture)
                    },
                    note_to_payer = string.IsNullOrWhiteSpace(req.Reason) ? null : req.Reason
                };
                msg.Content = JsonContent.Create(payload);
            }

            using var resp = await _http.SendAsync(msg);

            // On 422, surface the PayPal error body so you see the real cause
            if (!resp.IsSuccessStatusCode)
            {
                var raw = await resp.Content.ReadAsStringAsync();
                _logger.LogWarning("PayPal refund failed: {Status} {Body}", (int)resp.StatusCode, raw);

                // Try to extract a meaningful message from PayPal's error JSON
                try
                {
                    var err = JsonDocument.Parse(raw).RootElement;
                    var name = err.TryGetProperty("name", out var n) ? n.GetString() : null;
                    var message = err.TryGetProperty("message", out var m) ? m.GetString() : null;
                    var details = err.TryGetProperty("details", out var d) ? d.ToString() : null;
                    throw new InvalidOperationException($"PayPal refund error [{name}]: {message} {details}");
                }
                catch
                {
                    // if not JSON, throw generic with body
                    throw new InvalidOperationException($"PayPal refund error: {(int)resp.StatusCode} {raw}");
                }
            }

            var ok = await resp.Content.ReadFromJsonAsync<JsonElement>();
            var providerRefundId = ok.GetProperty("id").GetString()
                ?? throw new InvalidOperationException("PayPal refund response missing id.");

            // Amount (major): for full refunds PayPal returns capture amount
            decimal refundedMajor = req.Amount > 0m
                ? req.Amount
                : ok.TryGetProperty("amount", out var amt) &&
                   amt.TryGetProperty("value", out var val) &&
                   decimal.TryParse(val.GetString(), NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed)
                        ? parsed
                        : 0m;

            return new RefundGatewayResult(
                ProviderRefundId: providerRefundId,
                RefundedAmount: refundedMajor,
                CurrencyId: req.CurrencyId
            );
        }

        private async Task<string> GetAccessTokenAsync()
        {
            var tokenUrl = $"{BaseUrl}/v1/oauth2/token";
            var req = new HttpRequestMessage(HttpMethod.Post, tokenUrl);

            var basic = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{_clientId}:{_secret}"));
            
            req.Headers.Authorization = new AuthenticationHeaderValue("Basic", basic);
            req.Content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "client_credentials")
            });

            using var resp = await _http.SendAsync(req);
            resp.EnsureSuccessStatusCode();

            var json = await resp.Content.ReadFromJsonAsync<JsonElement>();
            var token = json.GetProperty("access_token").GetString();
            
            if (string.IsNullOrWhiteSpace(token))
                throw new InvalidOperationException("PayPal OAuth returned no access_token.");

            return token!;
        }
       
    }
}
