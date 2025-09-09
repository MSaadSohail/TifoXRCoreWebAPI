using Microsoft.Extensions.Options;

namespace TifoXRCoreWebAPI.Utilities.PaymentGateways
{
    public sealed class ChilizOptions
    {
        public string BaseUrl { get; set; } = "";
        public string ApiKey { get; set; } = "";
        public string? WebhookSecret { get; set; }
        public string Environment { get; set; } = "Sandbox";
    }
    public sealed class ChilizClientFactory : IChilizClientFactory
    {
        private readonly HttpClient _client;
        public string WebhookSecret { get; }

        public ChilizClientFactory(IHttpClientFactory httpFactory, IOptions<ChilizOptions> opts)
        {
            var o = opts.Value ?? throw new InvalidOperationException("Chiliz options missing.");
            _client = httpFactory.CreateClient("Chiliz");
            _client.BaseAddress = new Uri(o.BaseUrl);
            _client.DefaultRequestHeaders.Add("Authorization", $"Bearer {o.ApiKey}");
            WebhookSecret = o.WebhookSecret ?? throw new InvalidOperationException("Chiliz webhook secret missing.");
        }

        public HttpClient GetClient() => _client;
    }

}
