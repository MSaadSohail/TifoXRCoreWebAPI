// <copyright file="CreateIntentHandler.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Saad Sohail</author>
// <date>9/12/2025</date>
// <summary></summary>

using GMS.TifoXRCoreWebAPI.Models;
using GMS.TifoXRCoreWebAPI.Repositories;
using GMS.TifoXRCoreWebAPI.Application.PaymentGateways;
using GMS.TifoXRCoreWebAPI.Utilities;

namespace GMS.TifoXRCoreWebAPI.Services.PaymentHandlers
{
    public sealed class CreateIntentHandler(
        IPaymentGatewayResolver resolver, 
        IOrderRepository orders)
    {
        private readonly IPaymentGatewayResolver _resolver = resolver;
        private readonly IOrderRepository _orders = orders;

        // FIX ME: Fetch from lookup tables
        private const int STATUS_REQUIRES_ACTION = 1;
        private const int STATUS_SUCCEEDED = 3;

        public async Task<CreatePaymentIntentResponse> ExecuteAsync(
            int spaceId, string orderId, int gatewayId, CreatePaymentIntentRequest req)
        {
            if (string.IsNullOrWhiteSpace(orderId))
                throw new ArgumentException("orderId is required.", nameof(orderId));
            if (req is null)
                throw new ArgumentNullException(nameof(req));
            if (string.IsNullOrWhiteSpace(req.IdempotencyKey))
                throw new ArgumentException("IdempotencyKey is required.", nameof(req.IdempotencyKey));

            // Header: (SpaceId, CurrencyId, TotalNetMinor, GatewayPreferredId)
            var header = await _orders.GetOrderHeaderAsync(orderId);

            if (header is not { } h || h.SpaceId != spaceId)
                throw new InvalidOperationException("Order not found in this space.");

            var gateway = _resolver.GetById(gatewayId);
            var amountMajor = MoneyConverter.ToMajor(h.TotalNetMinor);

            // ---------- Guardrail: same order + same gateway (ignore new idempotency key) ----------
            // If an intent already exists for this (order, gateway):
            // - Status 3 (Succeeded): return WITHOUT approve link.
            // - Status 1 (RequiresAction): rebuild approve link and return.
            // - Any other status: return without approve link.
            var order = await _orders.GetOrderAsync(spaceId, orderId)
                       ?? throw new InvalidOperationException("Order not found.");

            var gwIntents = order.PaymentIntents
                .Where(i => i.PaymentGatewayId == gatewayId)
                .ToList();

            if (gwIntents.Count > 0)
            {
                var usePending = gwIntents.FirstOrDefault(i => i.StatusId == STATUS_REQUIRES_ACTION
                                                              && !string.IsNullOrWhiteSpace(i.ProviderIntentId));
                var useSucceeded = gwIntents.FirstOrDefault(i => i.StatusId == STATUS_SUCCEEDED);
                var useAny = usePending ?? useSucceeded ?? gwIntents[0];

                // Already paid => never send approve link
                if (useAny.StatusId == STATUS_SUCCEEDED)
                {
                    return new CreatePaymentIntentResponse
                    {
                        PaymentIntentId = useAny.Id,
                        PaymentGatewayId = gatewayId,
                        StatusId = STATUS_SUCCEEDED,
                        ClientSecret = null,
                        ProviderIntentId = useAny.ProviderIntentId,
                        ApproveLink = null
                    };
                }

                // Still requires action => rebuild approve link and return
                if (useAny.StatusId == STATUS_REQUIRES_ACTION && !string.IsNullOrWhiteSpace(useAny.ProviderIntentId))
                {
                    var providerStatus = await gateway.GetIntentAsync(useAny.ProviderIntentId!);

                    string? approveLink;
                    string providerPidToUse;

                    if (providerStatus is null && string.Equals(gateway.Name, "crypto", StringComparison.OrdinalIgnoreCase))
                    {
                        // STALE → refresh provider PID in-place
                        amountMajor = MoneyConverter.ToMajor(h.TotalNetMinor); 
                        var (newPid, newApproveLink) = await RefreshCryptoProviderIntentAsync(
                            spaceId, orderId, useAny.Id, gatewayId, amountMajor, h.CurrencyId);

                        providerPidToUse = newPid;

                        // Build your standard crypto approve link using the new PID
                        approveLink = BuildCryptoApproveLinkFromProviderLink(
                            newApproveLink, spaceId, orderId, useAny.Id, newPid);
                    }
                    else
                    {
                        providerPidToUse = useAny.ProviderIntentId!;
                        var providerApprove = providerStatus?.ApproveLink;

                        approveLink = string.Equals(gateway.Name, "crypto", StringComparison.OrdinalIgnoreCase)
                            ? BuildCryptoApproveLinkFromProviderLink(
                                  providerApprove, spaceId, orderId, useAny.Id, providerPidToUse)
                            : providerApprove;
                    }

                    return new CreatePaymentIntentResponse
                    {
                        PaymentIntentId = useAny.Id,
                        PaymentGatewayId = gatewayId,
                        StatusId = STATUS_REQUIRES_ACTION,
                        ClientSecret = null,
                        ProviderIntentId = providerPidToUse,
                        ApproveLink = approveLink
                    };
                }

                // Any other status => return without an approve link (do NOT create a new intent)
                return new CreatePaymentIntentResponse
                {
                    PaymentIntentId = useAny.Id,
                    PaymentGatewayId = gatewayId,
                    StatusId = useAny.StatusId,
                    ClientSecret = null,
                    ProviderIntentId = useAny.ProviderIntentId,
                    ApproveLink = null
                };
            }

            // ---------- Idempotency: existing intent with the same key ----------
            var existing = await _orders.FindPaymentIntentByIdempotencyAsync(orderId, req.IdempotencyKey);
            
            if (existing is not null)
            {
                // Load the specific intent's current StatusId from the order graph
                var orderForIdem = await _orders.GetOrderAsync(spaceId, orderId)
                                  ?? throw new InvalidOperationException("Order not found.");

                var pi = orderForIdem.PaymentIntents.FirstOrDefault(i => i.Id == existing.Value.Id);
                if (pi is null)
                    throw new InvalidOperationException("Payment intent not found on order.");

                if (pi.StatusId == STATUS_SUCCEEDED)
                {
                    // Already paid => do NOT send approve link
                    return new CreatePaymentIntentResponse
                    {
                        PaymentIntentId = existing.Value.Id,
                        PaymentGatewayId = gatewayId,
                        StatusId = STATUS_SUCCEEDED,
                        ClientSecret = null,
                        ProviderIntentId = existing.Value.ProviderIntentId,
                        ApproveLink = null
                    };
                }

                string? approveLink = null;
                
                if (pi.StatusId == STATUS_REQUIRES_ACTION && !string.IsNullOrWhiteSpace(existing.Value.ProviderIntentId))
                {
                    var providerStatus = await gateway.GetIntentAsync(existing.Value.ProviderIntentId);
                    var providerApprove = providerStatus?.ApproveLink;

                    approveLink = string.Equals(gateway.Name, "crypto", StringComparison.OrdinalIgnoreCase)
                        ? BuildCryptoApproveLinkFromProviderLink(
                              providerApprove,
                              spaceId,
                              orderId,
                              existing.Value.Id,
                              existing.Value.ProviderIntentId!)
                        : providerApprove;
                }

                return new CreatePaymentIntentResponse
                {
                    PaymentIntentId = existing.Value.Id,
                    PaymentGatewayId = gatewayId,
                    StatusId = pi.StatusId,
                    ClientSecret = null,
                    ProviderIntentId = existing.Value.ProviderIntentId,
                    ApproveLink = approveLink
                };
            }

            // ---------- Fresh provider intent ----------
            var created = await gateway.CreateIntentAsync(
                new CreateGatewayIntentRequest(
                    IdempotencyKey: req.IdempotencyKey,
                    Amount: amountMajor,
                    CurrencyId: h.CurrencyId,
                    ReturnUrl: $"https://localhost:7017/api/space/{spaceId}/orders/{orderId}/payments/{gateway.Name}/return",
                    CancelUrl: $"https://localhost:7017/pay/cancel")
            );

            if (string.IsNullOrWhiteSpace(created.ProviderIntentId))
                throw new InvalidOperationException("ProviderIntentId was not returned by gateway.");

            // Persist our intent (no approve link stored)
            var piId = await _orders.InsertPaymentIntentAsync(
                orderId: orderId,
                gatewayId: gatewayId,
                statusId: STATUS_REQUIRES_ACTION,
                amountMinor: h.TotalNetMinor,
                currencyId: h.CurrencyId,
                providerIntentId: created.ProviderIntentId!,
                idempotencyKey: req.IdempotencyKey
            );

            // Build human-facing approve link for the response (no persistence)
            string? freshApprove = string.Equals(gateway.Name, "crypto", StringComparison.OrdinalIgnoreCase)
                ? BuildCryptoApproveLinkFromProviderLink(
                      created.ApproveLink,
                      spaceId,
                      orderId,
                      piId,
                      created.ProviderIntentId!)
                : created.ApproveLink;

            return new CreatePaymentIntentResponse
            {
                PaymentIntentId = piId,
                PaymentGatewayId = gatewayId,
                StatusId = STATUS_REQUIRES_ACTION,
                ClientSecret = null,
                ProviderIntentId = created.ProviderIntentId,
                ApproveLink = freshApprove
            };
        }

        // providerLink (crypto) is often "https://host/api/crypto/intents/{pid}" or ".../pay/crypto.html".
        // We emit "https://host/pay/crypto.html?spaceId=...&orderId=...&intentId=...&pid=...".
        private static string BuildCryptoApproveLinkFromProviderLink(
            string? providerLink,
            int spaceId,
            string orderId,
            string intentId,
            string providerIntentId)
        {
            string origin;
            if (!string.IsNullOrWhiteSpace(providerLink) &&
                Uri.TryCreate(providerLink, UriKind.Absolute, out var uri))
            {
                origin = $"{uri.Scheme}://{uri.Authority}";
            }
            else
            {
                origin = "https://localhost:7017"; // fallback if provider didn't include host
            }

            return $"{origin}/pay/crypto.html"
                 + $"?spaceId={spaceId}"
                 + $"&orderId={Uri.EscapeDataString(orderId)}"
                 + $"&intentId={Uri.EscapeDataString(intentId)}"
                 + $"&pid={Uri.EscapeDataString(providerIntentId)}";
        }

        private async Task<(string NewProviderIntentId, string? ApproveLink)> RefreshCryptoProviderIntentAsync(
            int spaceId,
            string orderId,
            string intentId,
            int gatewayId,
            decimal amountMajor,
            int currencyId)
        {
            var gateway = _resolver.GetById(gatewayId);

            // Create a fresh provider intent (provider idempotency can be a transient value; not persisted)
            var created = await gateway.CreateIntentAsync(new CreateGatewayIntentRequest(
                IdempotencyKey: $"refresh::{intentId}::{DateTime.UtcNow.Ticks}",
                Amount: amountMajor,
                CurrencyId: currencyId,
                ReturnUrl: $"https://localhost:7017/api/space/{spaceId}/orders/{orderId}/payments/{gateway.Name}/return",
                CancelUrl: $"https://localhost:7017/pay/cancel"
            ));

            if (string.IsNullOrWhiteSpace(created.ProviderIntentId))
                throw new InvalidOperationException("Crypto refresh failed: no ProviderIntentId from gateway.");

            // Persist the new PID on the SAME payment_intent id
            var rows = await _orders.UpdatePaymentIntentProviderIdAsync(intentId, created.ProviderIntentId!);
            
            if (rows != 1)
                throw new InvalidOperationException("Crypto refresh failed: could not update provider_intent_id.");

            return (created.ProviderIntentId!, created.ApproveLink);
        }
    }
}
