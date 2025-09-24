// <copyright file="PaymentService.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Saad Sohail</author>
// <date>09/02/2025</date>
// <summary></summary>

using GMS.TifoXRCoreWebAPI.Models;
using GMS.TifoXRCoreWebAPI.Application.PaymentGateways;
using GMS.TifoXRCoreWebAPI.Services.PaymentHandlers; 

namespace GMS.TifoXRCoreWebAPI.Services
{
    /// <summary>
    /// Slim façade that delegates to focused payment handlers.
    /// </summary>
    public sealed class PaymentService(
        CreateIntentHandler create,
        CaptureHandler capture,
        RefundHandler refund,
        ReportOnChainHandler reportOnChain,
        GetPreparedPayloadHandler getPrepared,
        IPaymentGatewayResolver resolver) : IPaymentService
    {
        private readonly CreateIntentHandler _create = create;
        private readonly CaptureHandler _capture = capture;
        private readonly RefundHandler _refund = refund;
        private readonly ReportOnChainHandler _reportOnChain = reportOnChain;  
        private readonly GetPreparedPayloadHandler _getPrepared = getPrepared;   
        private readonly IPaymentGatewayResolver _resolver = resolver;

        public Task<CreatePaymentIntentResponse> CreateCheckoutAsync(int spaceId, string orderId, int gatewayId, CreatePaymentIntentRequest req)
            => _create.ExecuteAsync(spaceId, orderId, gatewayId, req);

        public Task<ConfirmPaymentIntentResponse> CaptureAsync(int spaceId, string orderId, string intentId, ConfirmPaymentIntentRequest req)
            => _capture.ExecuteAsync(spaceId, orderId, intentId, req);

        public Task<RefundResponse> RefundAsync(int spaceId, string orderId, string chargeId, RefundRequest req)
            => _refund.ExecuteAsync(spaceId, orderId, chargeId, req);

        public Task ReportOnChainTxAsync(int spaceId, string orderId, string intentId, string txHash)
            => _reportOnChain.ExecuteAsync(spaceId, orderId, intentId, txHash);

        public Task<string> GetPreparedClientPayloadAsync(int spaceId, string orderId, string intentId, string sender)
            => _getPrepared.ExecuteAsync(spaceId, orderId, intentId, sender);

        public IPaymentGateway GetGatewayByName(string name) => _resolver.GetByName(name);
        public IPaymentGateway GetGatewayById(int gatewayId) => _resolver.GetById(gatewayId);
    }
}
