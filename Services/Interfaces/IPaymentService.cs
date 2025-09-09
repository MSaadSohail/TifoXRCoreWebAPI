// <copyright file="IPaymentService.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Saad Sohail</author>
// <date>09/02/2025</date>
// <summary>Interface to handle general payment services</summary>

using GMS.TifoXRCoreWebAPI.Models;

namespace GMS.TifoXRCoreWebAPI.Services
{
    public interface IPaymentService
    {
        Task<CreatePaymentIntentResponse> CreateIntentAsync(int spaceId, string orderId, int gatewayId, CreatePaymentIntentRequest req);
        Task<ConfirmPaymentIntentResponse> CaptureAsync(int spaceId, string orderId, string intentId, ConfirmPaymentIntentRequest req);
        Task<RefundResponse> RefundAsync(int spaceId, string orderId, string chargeId, RefundRequest req);
    }
}
