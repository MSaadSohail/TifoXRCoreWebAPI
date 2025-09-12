// <copyright file="ReconcileService.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Saad Sohail</author>
// <date>9/12/2025</date>
// <summary></summary>

//using GMS.TifoXRCoreWebAPI.Repositories;
//using GMS.TifoXRCoreWebAPI.Services;
//using GMS.TifoXRCoreWebAPI.Utilities.Domain.Constants;

//namespace TifoXRCoreWebAPI.Services
//{
//    public sealed class ReconcileService : IReconcileService
//    {
//        private readonly IOrderRepository _repo;
//        private readonly IPaymentService _payments;
//        private readonly ILogger<ReconcileService> _log;

//        public ReconcileService(IOrderRepository repo, IPaymentService payments, ILogger<ReconcileService> log)
//        {
//            _repo = repo;
//            _payments = payments;
//            _log = log;
//        }

//        public async Task<int> ReconcilePendingAsync(CancellationToken ct = default)
//        {
//            int finalized = 0;

//            // 1) Find candidates: latest intent per order in RequiresAction or Processing OR
//            // paid orders missing entitlements/invoice (backfill).
//            var candidates = await _repo.FindOrdersNeedingReconcileAsync(ct); // <-- add in repo (below)

//            foreach (var c in candidates)
//            {
//                ct.ThrowIfCancellationRequested();

//                try
//                {
//                    // (A) If order is already Paid but invoice/entitlements missing, backfill & continue.
//                    if (c.IsPaid && (!c.HasInvoice || !c.HasEntitlements))
//                    {
//                        if (!c.HasEntitlements)
//                            await _repo.GrantEntitlementsAsync(c.OrderId);

//                        if (!c.HasInvoice && c.LastChargeId is not null)
//                            await _repo.InsertInvoiceFromOrderAsync(c.SpaceId, c.OrderId, c.LastChargeId, c.PaymentGatewayId);

//                        finalized++;
//                        continue;
//                    }

//                    // (B) Not paid yet → check latest intent + provider status
//                    if (c.IntentId is null || c.ProviderIntentId is null) continue;

//                    var status = await _payments.GetGatewayStatusAsync(c.PaymentGatewayId, c.ProviderIntentId, ct);
//                    if (string.Equals(status, GatewayStatus.Approved, StringComparison.OrdinalIgnoreCase))
//                    {
//                        // Attempt capture (idempotent at gateway)
//                        var idem = $"reconcile:{c.IntentId}:{DateTimeOffset.UtcNow:yyyyMMddHHmm}";
//                        var capture = await _payments.CaptureAsync(
//                            c.SpaceId, 
//                            c.OrderId, 
//                            c.IntentId, 
//                            idem, 
//                            ct);

//                        // On success, CaptureAsync already: inserts charge, marks paid, grants entitlements, inserts invoice
//                        finalized++;
//                    }
//                    else if (string.Equals(status, GatewayStatus.Succeeded, StringComparison.OrdinalIgnoreCase))
//                    {
//                        // Rare provider->DB mismatch: ensure DB terminalization exists
//                        if (!c.IsPaid || !c.HasInvoice || !c.HasEntitlements)
//                        {
//                            if (!c.IsPaid && c.ExpectedPaidMinor.HasValue)
//                                await _repo.MarkPaidIfCoveredAsync(c.OrderId, c.ExpectedPaidMinor.Value, /*paidStatusId*/ 3);

//                            if (!c.HasEntitlements)
//                                await _repo.GrantEntitlementsAsync(c.OrderId);

//                            if (!c.HasInvoice && c.LastChargeId is not null)
//                                await _repo.InsertInvoiceFromOrderAsync(c.SpaceId, c.OrderId, c.LastChargeId, c.PaymentGatewayId);

//                            finalized++;
//                        }
//                    }
//                }
//                catch (Exception ex)
//                {
//                    _log.LogWarning(ex, "Reconcile failed for Order {OrderId}", c.OrderId);
//                    // continue with next candidate
//                }
//            }

//            return finalized;
//        }
//    }
//}
