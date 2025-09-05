namespace GMS.TifoXRCoreWebAPI.Application.PaymentGateways
{
    public interface IPaymentGateway
    {
        string Name { get; } // "paypal", "stripe", "crypto" etc.

        Task<CreateGatewayIntentResult> CreateIntentAsync(CreateGatewayIntentRequest req);
        Task<CaptureGatewayResult> CaptureAsync(CaptureGatewayRequest req);
        Task<RefundGatewayResult> RefundAsync(RefundGatewayRequest req);
        Task<GatewayIntentStatusResult?> GetIntentAsync(string providerIntentId);
    }

    // Requests/Responses carry only gateway-agnostic data
    public record CreateGatewayIntentRequest(
        string IdempotencyKey,
        int CurrencyId,
        decimal Amount,              // major units
        string ReturnUrl,
        string CancelUrl);

    public record CreateGatewayIntentResult(
        string ProviderIntentId,
        string? ApproveLink);

    public record CaptureGatewayRequest(
        string IdempotencyKey,
        string ProviderIntentId);

    public record CaptureGatewayResult(
        string ProviderChargeId,
        decimal CapturedAmount,      // major units
        string CurrencyIso);

    public record RefundGatewayRequest(
        string IdempotencyKey,
        string ProviderChargeId,
        decimal Amount,
        int CurrencyId,
        string? Reason);

    public record RefundGatewayResult(
        string ProviderRefundId,
        decimal RefundedAmount,
        int CurrencyId);

    public record GatewayIntentStatusResult(
        string ProviderIntentId,
        string Status,
        string? ApproveLink);

}
