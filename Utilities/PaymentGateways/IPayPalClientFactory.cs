using PaypalServerSdk.Standard;

namespace TifoXRCoreWebAPI.Utilities.PaymentGateways
{
    public interface IPayPalClientFactory
    {
        /// <summary>
        /// Returns a fully configured PayPal Server SDK client (Sandbox or Production).
        /// Register as a singleton in DI.
        /// </summary>
        PaypalServerSdkClient GetClient();
    }
}
