//< copyright file = "ReconcileService.cs" company = "Global Mobile Software LLC" >
//Copyright © 2025 All Rights Reserved
//</copyright>
//<author>Saad Sohail</author>
//<date>9/12/2025</date>
//<summary></summary>

using GMS.TifoXRCoreWebAPI.Models;
using GMS.TifoXRCoreWebAPI.Repositories;
using GMS.TifoXRCoreWebAPI.Services;
using GMS.TifoXRCoreWebAPI.Utilities.Domain.Constants;
using GMS.TifoXRCoreWebAPI.Utilities.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace TifoXRCoreWebAPI.Services
{
    public sealed class ReconcileService : IReconcileService
    {
        private readonly IOrderRepository _repo;
        private readonly IPaymentService _payments;
        private readonly ILogger<ReconcileService> _log;

        public ReconcileService(IOrderRepository repo, IPaymentService payments, ILogger<ReconcileService> log)
        {
            _repo = repo;
            _payments = payments;
            _log = log;
        }

        public async Task<int> ReconcilePendingAsync(CancellationToken ct = default)
        {
            int finalized = 0;

            // Latest pending/processing intents OR paid orders missing artifacts
            var candidates = await _repo.FindOrdersNeedingReconcileAsync(ct); // your repo method

            foreach (var c in candidates)
            {
                ct.ThrowIfCancellationRequested();

                try
                {
                    // Convert Guid IDs to string once
                    var orderId = c.OrderId.ToString();
                    var intentId = c.IntentId?.ToString();
                    var lastChargeId = c.LastChargeId?.ToString();

                    // (A) Order already paid but missing entitlements/invoice → backfill
                    if (c.IsPaid && (!c.HasInvoice || !c.HasEntitlements))
                    {
                        if (!c.HasEntitlements)
                            await _repo.GrantEntitlementsAsync(orderId);

                        if (!c.HasInvoice && lastChargeId is not null)
                            await _repo.InsertInvoiceFromOrderAsync(c.SpaceId, orderId, lastChargeId, c.PaymentGatewayId);

                        finalized++;
                        continue;
                    }

                    // (B) Not paid yet → need a valid intent + provider id to proceed
                    if (intentId is null || c.ProviderIntentId is null)
                        continue;

                    // Ask the gateway for current provider status
                    var gateway = _payments.GetGatewayById(c.PaymentGatewayId); // facade exposes this
                    var provider = await gateway.GetIntentAsync(c.ProviderIntentId); // returns status + approve link

                    if (provider is not null &&
                        string.Equals(provider.Status, GatewayStatus.Approved, StringComparison.OrdinalIgnoreCase))
                    {
                        // Attempt capture (idempotent at provider)
                        var idem = $"reconcile:{intentId}:{DateTimeOffset.UtcNow:yyyyMMddHHmm}";
                        await _payments.CaptureAsync(
                            c.SpaceId,
                            orderId,
                            intentId,
                            new ConfirmPaymentIntentRequest
                            {
                                IdempotencyKey = idem,
                                ProviderIntentId = c.ProviderIntentId
                            });

                        // On success, CaptureAsync inserts charge, marks paid, grants entitlements, inserts invoice
                        finalized++;
                    }
                    else if (provider is not null &&
                             string.Equals(provider.Status, GatewayStatus.Succeeded, StringComparison.OrdinalIgnoreCase))
                    {
                        // Rare provider->DB mismatch: ensure DB terminalization exists
                        if (!c.IsPaid || !c.HasInvoice || !c.HasEntitlements)
                        {
                            if (!c.IsPaid && c.ExpectedPaidMinor.HasValue)
                                await _repo.MarkPaidIfCoveredAsync(orderId, c.ExpectedPaidMinor.Value, (int)PaymentIntentStatus.Succeeded);

                            if (!c.HasEntitlements)
                                await _repo.GrantEntitlementsAsync(orderId);

                            if (!c.HasInvoice && lastChargeId is not null)
                                await _repo.InsertInvoiceFromOrderAsync(c.SpaceId, orderId, lastChargeId, c.PaymentGatewayId);

                            finalized++;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _log.LogWarning(ex, "Reconcile failed for Order {OrderId}", c.OrderId);
                    // continue with next candidate
                }
            }

            return finalized;
        }
    }
}
