// <copyright file="PaymentIntentSql.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Saad Sohail</author>
// <date>9/16/2025</date>
// <summary></summary>

namespace GMS.TifoXRCoreWebAPI.Repositories.SQL
{
    public static class PaymentIntentSql
    {
        internal const string Intent_UpdateStatus = @"
            UPDATE payment_intent
            SET status_id=@Status, modified_by=@ModBy
            WHERE id=@IntentId;";

        internal const string Reconcile_FindCandidates = @"
                SELECT
                  o.id                    AS OrderId,
                  o.space_id              AS SpaceId,
                  o.gateway_preferred_id  AS PaymentGatewayId,
                  (o.status_id = 3)       AS IsPaid,
                  (inv.cnt > 0)           AS HasInvoice,
                  (ent.cnt > 0)           AS HasEntitlements,
                  pi.id                   AS IntentId,
                  pi.provider_intent_id   AS ProviderIntentId,
                  ch.last_charge_id       AS LastChargeId,
                  o.total_net_amount      AS ExpectedPaidMinor
                FROM `order` o
                LEFT JOIN (
                    SELECT order_id, MAX(creation_time) AS last_ct
                    FROM payment_intent GROUP BY order_id
                ) last ON last.order_id = o.id
                LEFT JOIN payment_intent pi
                  ON pi.order_id = o.id AND pi.creation_time = last.last_ct
                LEFT JOIN (SELECT order_id, COUNT(*) cnt FROM invoice GROUP BY order_id) inv
                  ON inv.order_id = o.id
                LEFT JOIN (
                    SELECT ol.order_id, COUNT(*) cnt
                    FROM entitlement e
                    JOIN order_line ol ON ol.id = e.order_line_id
                    GROUP BY ol.order_id
                ) ent ON ent.order_id = o.id
                LEFT JOIN (
                    SELECT pi.order_id, MAX(pc.id) AS last_charge_id
                    FROM payment_charge pc
                    JOIN payment_intent pi ON pi.id = pc.payment_intent_id
                    GROUP BY pi.order_id
                ) ch ON ch.order_id = o.id
                WHERE (pi.status_id IN (1,2)) OR (o.status_id = 3 AND (IFNULL(inv.cnt,0)=0 OR IFNULL(ent.cnt,0)=0));
            ";

        internal const string sqlOrderSnapshot = @"
                SELECT
                  o.user_id, o.space_id, o.currency_id, o.remarks,
                  COALESCE((SELECT SUM(ol.unit_amount * ol.quantity) FROM order_line ol WHERE ol.order_id=o.id),0) AS subtotal_major,
                  COALESCE((SELECT SUM(CASE WHEN oa.amount < 0 THEN (-oa.amount)/@MinorDiv ELSE 0 END) FROM order_adjustment oa WHERE oa.order_id=o.id),0) AS discount_major,
                  COALESCE((SELECT SUM(CASE WHEN (@TaxTypeId IS NOT NULL AND oa.item_type_id=@TaxTypeId) OR (@TaxTypeId IS NULL AND oa.code LIKE 'TAX_%') THEN oa.amount/@MinorDiv ELSE 0 END)
                            FROM order_adjustment oa WHERE oa.order_id=o.id),0) AS tax_major,
                  COALESCE((SELECT SUM(CASE WHEN (@FeeTypeId IS NOT NULL AND oa.item_type_id=@FeeTypeId) OR (@FeeTypeId IS NULL AND oa.code LIKE 'FEE_%') THEN oa.amount/@MinorDiv ELSE 0 END)
                            FROM order_adjustment oa WHERE oa.order_id=o.id),0) AS fee_major
                FROM `order` o
                WHERE o.id=@OrderId AND o.space_id=@SpaceId
                LIMIT 1;";

        internal const string sqlChargeSnapshot = @"
                SELECT pc.provider_charge_id, pi.payment_gateway_id
                FROM payment_charge pc
                JOIN payment_intent pi ON pi.id = pc.payment_intent_id
                WHERE pc.id=@ChargeId
                LIMIT 1;";

        internal const string sqlInsertInvoice = @"
                INSERT INTO invoice
                (id, invoice_number, user_id, order_id, subscription_id,
                 status_id, payment_charge_id, payment_gateway_id, provider_charge_id,
                 space_id, issue_datetime, currency_id,
                 subtotal_amount, discount_amount, tax_amount, fee_amount, total_amount,
                 bill_to_name, bill_to_email, notes, pdf_url, metadata,
                 creation_time, modified_by)
                VALUES
                (@Id, @No, @UserId, @OrderId, NULL,
                 @Status, @ChargeId, @GatewayId, @ProvChargeId,
                 @SpaceId, NOW(6), @CurrencyId,
                 @Subtotal, @Discount, @Tax, @Fee, @Total,
                 @BillToName, @BillToEmail, @Notes, NULL, @Metadata,
                 NOW(6), @ModBy);";

        internal const string Intent_FindLatestOrderWithPending = @"
            SELECT o.id
            FROM `order` o
            JOIN order_line ol ON ol.order_id = o.id
            JOIN payment_intent pi ON pi.order_id = o.id AND pi.status_id IN (1,2) -- requires_action / processing
            WHERE o.space_id=@SpaceId AND o.user_id=@UserId
              AND ol.item_type_id=@ItemTypeId AND ol.item_ref_id=@ItemRefId
            ORDER BY pi.creation_time DESC
            LIMIT 1;";

        internal const string Refund_Insert = @"
            INSERT INTO payment_refund
            (id, payment_charge_id, status_id, amount, currency_id, provider_refund_id, reason,
             refund_datetime, creation_time, modified_by)
            VALUES (@Id, @ChargeId, @Status, @Amt, @Ccy, @ProvRefund, @Reason,
                    NOW(6), NOW(6), @ModBy);";

        internal const string Charge_GetContext = @"
            SELECT
              o.id                                   AS order_id,
              o.space_id,
              o.currency_id,
              pi.payment_gateway_id,
              pc.provider_charge_id,
              pc.amount_captured                              AS amount_captured_minor,
              COALESCE(SUM(pr.amount), 0)                     AS total_refunded_so_far_minor
            FROM payment_charge pc
            JOIN payment_intent pi ON pi.id = pc.payment_intent_id
            JOIN `order` o        ON o.id  = pi.order_id
            LEFT JOIN payment_refund pr ON pr.payment_charge_id = pc.id
                                       AND pr.status_id = 3      -- succeeded only
            WHERE pc.id = @ChargeId
            GROUP BY o.id, o.space_id, o.currency_id, pi.payment_gateway_id, pc.provider_charge_id, pc.amount_captured;
        ";
    }
}

