// <copyright file="CreateIntentHandler.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Saad Sohail</author>
// <date>9/12/2025</date>
// <summary></summary>

using GMS.TifoXRCoreWebAPI.Models;
using GMS.TifoXRCoreWebAPI.Repositories;
using GMS.TifoXRCoreWebAPI.Utilities.Domain.Enums;
using GMS.TifoXRCoreWebAPI.Application.PaymentGateways;

namespace GMS.TifoXRCoreWebAPI.Services.PaymentHandlers
{
    public sealed class CreateIntentHandler(IPaymentGatewayResolver resolver, IOrderRepository orders)
    {
        private readonly IPaymentGatewayResolver _resolver = resolver;
        private readonly IOrderRepository _orders = orders;

        public async Task<CreatePaymentIntentResponse> ExecuteAsync(
            int spaceId, string orderId, int gatewayId, CreatePaymentIntentRequest req)
        {
            if (req is null) 
                throw new ArgumentNullException(nameof(req));

            if (string.IsNullOrWhiteSpace(req.IdempotencyKey))
                throw new ArgumentException("IdempotencyKey is required: {0}", nameof(req.IdempotencyKey));

            var header = await _orders.GetOrderHeaderAsync(orderId);

            if (header is not { } h || h.SpaceId != spaceId)
                throw new InvalidOperationException("Order not found in this space.");

            var gateway = _resolver.GetById(gatewayId);
            var amountMinor = h.TotalNetMinor;
            var amountMajor = amountMinor / 100m; // keep behavior identical; you can swap to MoneyConverter later

            // Idempotency short-circuit
            var existing = await _orders.FindPaymentIntentByIdempotencyAsync(orderId, req.IdempotencyKey);
            if (existing is not null)
            {
                string? approve = null;
                if (!string.IsNullOrWhiteSpace(existing.Value.ProviderIntentId))
                {
                    var st = await gateway.GetIntentAsync(existing.Value.ProviderIntentId!);
                    approve = st?.ApproveLink;
                }

                return new CreatePaymentIntentResponse
                {
                    PaymentIntentId = existing.Value.Id,
                    PaymentGatewayId = gatewayId,
                    StatusId = (int)PaymentIntentStatus.RequiresAction,
                    ProviderIntentId = existing.Value.ProviderIntentId,
                    ApproveLink = approve
                };
            }

            // Create provider intent
            var created = await gateway.CreateIntentAsync(new CreateGatewayIntentRequest(
                IdempotencyKey: req.IdempotencyKey,
                CurrencyId: h.CurrencyId,
                Amount: amountMajor,
                ReturnUrl: $"https://localhost:7017/api/space/{spaceId}/orders/{orderId}/payments/{gateway.Name}/return",
                CancelUrl: $"https://your.app/cancel"));

            if (string.IsNullOrWhiteSpace(created.ProviderIntentId))
                throw new InvalidOperationException("Gateway did not return a provider intent id.");

            // Persist our intent
            var piId = await _orders.InsertPaymentIntentAsync(
                orderId: orderId,
                gatewayId: gatewayId,
                statusId: (int)PaymentIntentStatus.RequiresAction,
                amountMinor: amountMinor,
                currencyId: h.CurrencyId,
                providerIntentId: created.ProviderIntentId!,
                idempotencyKey: req.IdempotencyKey);

            var approveLink = created.ApproveLink;

            // Crypto unified approve URL
            if (string.Equals(gateway.Name, "crypto", StringComparison.OrdinalIgnoreCase))
            {
                var baseUrl = "https://localhost:7017";
                approveLink = $"{baseUrl}/pay/crypto.html?spaceId={spaceId}&orderId={orderId}&intentId={piId}&pid={created.ProviderIntentId}";
            }

            return new CreatePaymentIntentResponse
            {
                PaymentIntentId = piId,
                PaymentGatewayId = gatewayId,
                StatusId = (int)PaymentIntentStatus.RequiresAction,
                ProviderIntentId = created.ProviderIntentId,
                ApproveLink = approveLink
            };
        }
    }
}
