using TifoXRCoreWebAPI.Application.Gateways;

namespace GMS.TifoXRCoreWebAPI.Application.Payments
{
    public interface IPaymentGatewayFactory
    {
        IPaymentGateway Get(string gatewayName);
    }

    public sealed class PaymentGatewayFactory : IPaymentGatewayFactory
    {
        private readonly IReadOnlyDictionary<string, IPaymentGateway> _map;
        public PaymentGatewayFactory(IEnumerable<IPaymentGateway> gateways)
        {
            // case-insensitive key map
            _map = gateways.ToDictionary(g => g.Name, g => g, StringComparer.OrdinalIgnoreCase);
        }

        public IPaymentGateway Get(string gatewayName)
            => _map.TryGetValue(gatewayName, out var g)
                ? g
                : throw new InvalidOperationException($"Unsupported gateway '{gatewayName}'.");
    }
}
