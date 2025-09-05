// Application/PaymentGateways/PayPalOptions.cs
namespace GMS.TifoXRCoreWebAPI.Application.PaymentGateways
{
    public sealed class PayPalOptions
    {
        public string ClientId { get; set; } = default!;
        public string Secret { get; set; } = default!;
        public string Environment { get; set; } = "Sandbox"; // or "Production"
    }
}
