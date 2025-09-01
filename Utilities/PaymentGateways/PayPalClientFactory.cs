
using PaypalServerSdk.Standard;
using PaypalServerSdk.Standard.Authentication;

namespace TifoXRCoreWebAPI.Utilities.PaymentGateways
{
    public sealed class PayPalClientFactory : IPayPalClientFactory
    {
        private readonly PaypalServerSdkClient _client;

        public PayPalClientFactory(Microsoft.Extensions.Configuration.IConfiguration cfg, ILogger<PayPalClientFactory> logger)
        {
            var clientId = cfg["PayPal:ClientId"];
            var secret   = cfg["PayPal:Secret"];
            var env      = cfg["PayPal:Environment"]; // "Sandbox" or "Production"

            if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(secret))
                throw new InvalidOperationException(
                    "PayPal credentials missing (PayPal:ClientId / PayPal:Secret).");

            var environment = string.Equals(env, "Production", StringComparison.OrdinalIgnoreCase)
                ? PaypalServerSdk.Standard.Environment.Production
                : PaypalServerSdk.Standard.Environment.Sandbox;

            _client = new PaypalServerSdkClient
                .Builder()
                .ClientCredentialsAuth(new ClientCredentialsAuthModel
                    .Builder(clientId!, secret!)
                    .Build())
                .Environment(environment)
                .Build();

            logger.LogInformation("PayPal Server SDK initialized for {Env}", environment);
        }

        public PaypalServerSdkClient GetClient() => _client;
    }
}
