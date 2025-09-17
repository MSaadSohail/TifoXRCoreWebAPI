// <copyright file="OrderRepository.Reconcile.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Saad Sohail</author>
// <date>09/12/2025</date>
// <summary></summary>

using GMS.TifoXRCoreWebAPI.Models;
using static GMS.TifoXRCoreWebAPI.Repositories.SQL.PaymentIntentSql;

namespace GMS.TifoXRCoreWebAPI.Repositories
{
    public sealed partial class OrderRepository
    {
        public async Task<IReadOnlyList<ReconcileCandidate>> FindOrdersNeedingReconcileAsync(CancellationToken ct)
        {
            var list = new List<ReconcileCandidate>();

            await using var conn = await _db.OpenConnectionAsync();
            await using var cmd = _db.CreateCommand(conn, Reconcile_FindCandidates);
            await using var r = await cmd.ExecuteReaderAsync(ct);

            while (await r.ReadAsync(ct))
            {
                var candidate = new ReconcileCandidate
                {
                    OrderId = r.GetString(r.GetOrdinal("OrderId")),
                    SpaceId = r.GetInt32(r.GetOrdinal("SpaceId")),
                    PaymentGatewayId = r.GetInt32(r.GetOrdinal("PaymentGatewayId")),
                    IsPaid = r.GetBoolean(r.GetOrdinal("IsPaid")),
                    HasInvoice = r.GetBoolean(r.GetOrdinal("HasInvoice")),
                    HasEntitlements = r.GetBoolean(r.GetOrdinal("HasEntitlements")),
                    IntentId = r.IsDBNull(r.GetOrdinal("IntentId")) ? null : r.GetString(r.GetOrdinal("IntentId")),
                    ProviderIntentId = r.IsDBNull(r.GetOrdinal("ProviderIntentId")) ? null : r.GetString(r.GetOrdinal("ProviderIntentId")),
                    LastChargeId = r.IsDBNull(r.GetOrdinal("LastChargeId")) ? null : r.GetString(r.GetOrdinal("LastChargeId")),
                    ExpectedPaidMinor = r.GetInt64(r.GetOrdinal("ExpectedPaidMinor"))
                };

                list.Add(candidate);
            }

            return list;
        }
    }
}

