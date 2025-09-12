// <copyright file="OrderRepository.Reconcile.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Saad Sohail</author>
// <date>9/12/2025</date>
// <summary></summary>

using GMS.TifoXRCoreWebAPI.Models;

namespace GMS.TifoXRCoreWebAPI.Repositories
{
    public sealed partial class OrderRepository : IOrderRepository
    {
        public async Task<IReadOnlyList<ReconcileCandidate>> FindOrdersNeedingReconcileAsync(CancellationToken ct)
        {
            // Keep this SQL aligned with your schema; example logic:
            // - latest intent per order where status in (RequiresAction=1, Processing=2)
            // - OR orders paid but missing entitlements/invoice
            const string sql = @"
                SELECT
                  o.id               AS OrderId,
                  o.space_id         AS SpaceId,
                  o.gateway_preferred_id AS PaymentGatewayId,
                  (o.status_id = 3)  AS IsPaid,                    -- adjust if your 'Paid' differs
                  (inv.cnt > 0)      AS HasInvoice,
                  (ent.cnt > 0)      AS HasEntitlements,
                  pi.id              AS IntentId,
                  pi.provider_intent_id AS ProviderIntentId,
                  ch.id              AS LastChargeId,
                  o.total_net_amount AS ExpectedPaidMinor
                FROM `order` o
                LEFT JOIN (
                    SELECT order_id, MAX(creation_time) AS last_ct
                    FROM payment_intent
                    GROUP BY order_id
                ) last ON last.order_id = o.id
                LEFT JOIN payment_intent pi
                  ON pi.order_id = o.id AND pi.creation_time = last.last_ct
                LEFT JOIN (
                    SELECT order_id, COUNT(*) cnt FROM invoice GROUP BY order_id
                ) inv ON inv.order_id = o.id
                LEFT JOIN (
                    SELECT order_id, COUNT(*) cnt FROM entitlement GROUP BY order_id
                ) ent ON ent.order_id = o.id
                LEFT JOIN (
                    SELECT order_id, MAX(id) id FROM charge GROUP BY order_id
                ) ch ON ch.order_id = o.id
                WHERE
                  -- need reconcile if:
                  (pi.status_id IN (1,2))                                  -- requires_action or processing
                  OR (o.status_id = 3 AND (IFNULL(inv.cnt,0)=0 OR IFNULL(ent.cnt,0)=0)); -- paid but missing artifacts
                ";

            await using var conn = await _db.OpenConnectionAsync();
            await using (var cmd = _db.CreateCommand(conn, sql))
            {
            }

            //var items = await conn.QueryAsync<ReconcileCandidate>(sql);

            return null; // items.AsList();
        }
    }
}
