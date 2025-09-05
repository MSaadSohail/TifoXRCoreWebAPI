// Application/PaymentGateways/PaymentGatewayRegistry.cs
using System.Collections.Concurrent;

namespace GMS.TifoXRCoreWebAPI.Application.PaymentGateways
{
    public sealed class PaymentGatewayRegistry : IPaymentGatewayResolver
    {
        private readonly ConcurrentDictionary<string, IPaymentGateway> _byName =
            new(StringComparer.OrdinalIgnoreCase);

        private readonly ConcurrentDictionary<int, IPaymentGateway> _byId = new();

        public PaymentGatewayRegistry(IEnumerable<IPaymentGateway> gateways)
        {
            foreach (var g in gateways)
            {
                // Map well-known names to ids — align with your lookup table
                // Example mapping (adjust to your DB):
                // stripe=1, paypal=2, crypto=3
                var id = g.Name.ToLowerInvariant() switch
                {
                    "stripe" => 1,
                    "paypal" => 2,
                    "crypto" => 3,
                    _ => 0
                };

                _byName[g.Name] = g;
                if (id > 0) _byId[id] = g;
            }
        }

        public IPaymentGateway GetById(int gatewayId) =>
            _byId.TryGetValue(gatewayId, out var g)
                ? g
                : throw new KeyNotFoundException($"Gateway id {gatewayId} not registered.");

        public IPaymentGateway GetByName(string name) =>
            _byName.TryGetValue(name, out var g)
                ? g
                : throw new KeyNotFoundException($"Gateway '{name}' not registered.");
    }
}
