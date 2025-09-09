using System.Net.Http.Json;
using TifoXRCoreWebAPI.Utilities.PaymentGateways;

namespace TifoXRCoreWebAPI.Application.PaymentGateways
{
    public sealed class ChilizGateway : IPaymentGateway
    {
        private readonly HttpClient _http;
        public string Name => "chiliz";

        public ChilizGateway(IChilizClientFactory factory)
        {
            _http = factory.GetClient();
        }

        public async Task<CreateGatewayIntentResult> CreateIntentAsync(CreateGatewayIntentRequest req)
        {
            var body = new
            {
                amount = req.Amount,              // major units
                currency = "USD",                 // TODO: map from req.CurrencyId
                returnUrl = req.ReturnUrl,
                cancelUrl = req.CancelUrl
            };

            using var httpReq = new HttpRequestMessage(HttpMethod.Post, "payments/intents")
            {
                Content = JsonContent.Create(body)
            };
            httpReq.Headers.TryAddWithoutValidation("Idempotency-Key", req.IdempotencyKey);

            var resp = await _http.SendAsync(httpReq);
            resp.EnsureSuccessStatusCode();
            var json = await resp.Content.ReadFromJsonAsync<ChzCreateIntentResponse>()
                       ?? throw new InvalidOperationException("Empty Chiliz create-intent response.");

            return new CreateGatewayIntentResult(json.intentId, json.approveLink);
        }

        public async Task<CaptureGatewayResult> CaptureAsync(CaptureGatewayRequest req)
        {
            var body = new { };
            using var httpReq = new HttpRequestMessage(HttpMethod.Post, $"payments/intents/{req.ProviderIntentId}/capture")
            {
                Content = JsonContent.Create(body)
            };
            httpReq.Headers.TryAddWithoutValidation("Idempotency-Key", req.IdempotencyKey);

            var resp = await _http.SendAsync(httpReq);
            resp.EnsureSuccessStatusCode();
            var json = await resp.Content.ReadFromJsonAsync<ChzCaptureResponse>()
                       ?? throw new InvalidOperationException("Empty Chiliz capture response.");

            return new CaptureGatewayResult(
                ProviderChargeId: json.chargeId,
                CapturedAmount: json.amount,
                CurrencyIso: json.currency);
        }

        public async Task<GatewayIntentStatusResult?> GetIntentAsync(string providerIntentId)
        {
            var resp = await _http.GetAsync($"payments/intents/{providerIntentId}");
            if (resp.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
            resp.EnsureSuccessStatusCode();
            var json = await resp.Content.ReadFromJsonAsync<ChzGetIntentResponse>()
                       ?? throw new InvalidOperationException("Empty Chiliz get-intent response.");

            return new GatewayIntentStatusResult(json.intentId, json.status, json.approveLink);
        }

        public Task<RefundGatewayResult> RefundAsync(RefundGatewayRequest req)
            => throw new NotImplementedException("Chiliz refunds not implemented yet.");
    }

    // DTOs matching the Chiliz API you call
    internal sealed class ChzCreateIntentResponse
    {
        public string intentId { get; set; } = "";
        public string? approveLink { get; set; }
    }

    internal sealed class ChzCaptureResponse
    {
        public string chargeId { get; set; } = "";
        public decimal amount { get; set; }
        public string currency { get; set; } = "USD";
    }

    internal sealed class ChzGetIntentResponse
    {
        public string intentId { get; set; } = "";
        public string status { get; set; } = "";
        public string? approveLink { get; set; }
    }
}
