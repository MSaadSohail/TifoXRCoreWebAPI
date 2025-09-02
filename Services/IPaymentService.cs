using GMS.TifoXRCoreWebAPI.Models;

namespace TifoXRCoreWebAPI.Services
{
    public interface IPaymentService
    {
        Task<CreatePaymentIntentResponse> CreateIntentAsync(int spaceId, string orderId, CreatePaymentIntentRequest req);
        Task<ConfirmPaymentIntentResponse> CaptureAsync(int spaceId, string orderId, string intentId, ConfirmPaymentIntentRequest req);
    }
}
