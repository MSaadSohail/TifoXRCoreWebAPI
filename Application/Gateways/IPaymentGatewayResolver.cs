// Application/PaymentGateways/IPaymentGatewayResolver.cs
namespace GMS.TifoXRCoreWebAPI.Application.PaymentGateways
{
    public interface IPaymentGatewayResolver
    {
        IPaymentGateway GetById(int gatewayId);     // e.g., 1=stripe, 2=paypal, 3=wallet
        IPaymentGateway GetByName(string name);     // e.g., "stripe", "paypal", "crypto"
    }
}
