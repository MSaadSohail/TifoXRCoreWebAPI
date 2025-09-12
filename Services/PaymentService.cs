// <copyright file="PaymentService.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Saad Sohail</author>
// <date>09/02/2025</date>
// <summary></summary>

using GMS.TifoXRCoreWebAPI.Models;
using GMS.TifoXRCoreWebAPI.Application.PaymentGateways;

namespace GMS.TifoXRCoreWebAPI.Services
{
    /// <summary>
    /// Slim façade that delegates to focused payment handlers.
    /// </summary>
    public sealed class PaymentService : IPaymentService
    {
        private readonly Payments.CreateIntentHandler _create;
        private readonly Payments.CaptureHandler _capture;
        private readonly Payments.RefundHandler _refund;
        private readonly IPaymentGatewayResolver _resolver;

        public PaymentService(
            Payments.CreateIntentHandler create,
            Payments.CaptureHandler capture,
            Payments.RefundHandler refund,
            IPaymentGatewayResolver resolver)
        {
            _create = create;
            _capture = capture;
            _refund = refund;
            _resolver = resolver;
        }

        public Task<CreatePaymentIntentResponse> CreateIntentAsync(int spaceId, string orderId, int gatewayId, CreatePaymentIntentRequest req)
            => _create.ExecuteAsync(spaceId, orderId, gatewayId, req);

        public Task<ConfirmPaymentIntentResponse> CaptureAsync(int spaceId, string orderId, string intentId, ConfirmPaymentIntentRequest req)
            => _capture.ExecuteAsync(spaceId, orderId, intentId, req);

        public Task<RefundResponse> RefundAsync(int spaceId, string orderId, string chargeId, RefundRequest req)
            => _refund.ExecuteAsync(spaceId, orderId, chargeId, req);

        // Preserving these helpers to maintain your public API
        public IPaymentGateway GetGatewayByName(string name) => _resolver.GetByName(name);
        public IPaymentGateway GetGatewayById(int gatewayId) => _resolver.GetById(gatewayId);
    }
}
