namespace TifoXRCoreWebAPI.Utilities.PaymentGateways
{
    public interface IChilizClientFactory
    {
        HttpClient GetClient();      // interface members are public by default
        string WebhookSecret { get; }
    }
}
