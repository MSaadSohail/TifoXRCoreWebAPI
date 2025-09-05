// <copyright file="IPaymentsRepository.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Urvashi Dhingra</author>
// <date>08/29/2025</date>
// <summary>Interface to handle Payments repository pattern</summary>

using GMS.TifoXRCoreWebAPI.Models;

namespace GMS.TifoXRCoreWebAPI.Repositories.Interfaces
{
    public interface IPaymentsRepository
    {
        // Intents
        Task<PaymentIntentData?> GetPaymentIntentAsync(int spaceId, string intentId);
        Task<PaymentIntentData?> CreatePaymentIntentAsync(int spaceId, CreatePaymentIntentDto dto);

        // Charges
        Task<PaymentChargeData?> GetChargeAsync(int spaceId, string chargeId);

        // Refunds
        Task<PaymentRefundData?> CreateRefundAsync(int spaceId, CreateRefundDto dto);
        Task<PaymentRefundData?> GetRefundAsync(int spaceId, string refundId);
    }
}
