// <copyright file="OrderSql.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Saad Sohail</author>
// <date>09/16/2025</date>
// <summary></summary>

namespace GMS.TifoXRCoreWebAPI.Repositories.SQL
{
    internal static class OrderSql
    {
        // ----- Order (read) -----
        internal const string Order_SelectHeader = @"
            SELECT id, user_id, space_id, transaction_type_id, currency_id, status_id,
                   total_gross_amount, total_discount_amount, total_tax_amount, total_fee_amount, total_net_amount,
                   change_reason, original_order_id, session_id, gateway_preferred_id, order_datetime, remarks
            FROM `order` WHERE id=@OrderId AND space_id=@SpaceId;";

        internal const string Order_SelectLines = @"
            SELECT id, item_type_id, item_ref_id, entity_id, shop_id, quantity, unit_amount, currency_id, metadata
            FROM order_line WHERE order_id=@OrderId ORDER BY id;";

        internal const string Order_SelectAdjustments = @"
            SELECT id, order_line_id, item_type_id, code, amount, description_key, metadata
            FROM order_adjustment WHERE order_id=@OrderId ORDER BY id;";

        internal const string Order_SelectEntitlements = @"
            SELECT id, order_line_id, user_id, status, quantity, granted_datetime, revoked_reason, metadata
            FROM entitlement WHERE order_line_id IN (SELECT id FROM order_line WHERE order_id=@OrderId)
            ORDER BY creation_time;";

        internal const string Order_SelectInvoices = @"
            SELECT id, invoice_number, status_id, currency_id, total_amount, issue_datetime, pdf_url
            FROM invoice WHERE order_id=@OrderId ORDER BY issue_datetime;";

        // ----- Intents / charges / refunds (read) -----
        internal const string Intent_SelectByOrder = @"
            SELECT id, payment_gateway_id, status_id, amount, currency_id, client_secret, provider_intent_id
            FROM payment_intent WHERE order_id=@OrderId ORDER BY creation_time;";

        internal const string Charge_SelectByOrder = @"
            SELECT id, payment_intent_id, status_id, amount_captured, currency_id,
                   provider_charge_id, gateway_fee_amount, exchange_rate, payment_datetime
            FROM payment_charge
            WHERE payment_intent_id IN (SELECT id FROM payment_intent WHERE order_id=@OrderId)
            ORDER BY payment_datetime;";

        internal const string Refund_SelectByOrder = @"
            SELECT id, payment_charge_id, status_id, amount, currency_id,
                   provider_refund_id, reason, refund_datetime
            FROM payment_refund
            WHERE payment_charge_id IN (
                SELECT pc.id FROM payment_charge pc
                WHERE pc.payment_intent_id IN (SELECT id FROM payment_intent WHERE order_id=@OrderId)
            )
            ORDER BY refund_datetime;";

        internal const string Order_Insert = @"
                    INSERT INTO `order`
                    (id, user_id, space_id, transaction_type_id, currency_id, status_id,
                     total_gross_amount, total_discount_amount, change_reason, total_tax_amount, total_fee_amount, total_net_amount,
                     original_order_id, session_id, gateway_preferred_id, order_datetime, remarks, idempotency_key, modified_by)
                    VALUES
                    (@Id, @UserId, @SpaceId, @Trx, @CcyId, @StatusId,
                     @Gross, @Disc, NULL, @Tax, @Fees, @Net,
                     NULL, @SessionId, @GatewayPreferredId, NOW(6), @Remarks, @IdemKey, @ModBy);";

        internal const string Order_FindExisting = @"
                    SELECT id, total_net_amount, space_id
                    FROM `order`
                    WHERE idempotency_key = @IdemKey
                    LIMIT 1;";

        internal const string Order_InsertLine = @"
                    INSERT INTO order_line
                    (id, order_id, item_type_id, item_ref_id, entity_id, shop_id,
                     quantity, unit_amount, currency_id, metadata, modified_by)
                    VALUES
                    (@Id, @OrderId, @ItemTypeId, @ItemRefId, @EntityId, @ShopId,
                     @Qty, @UnitAmount, @CcyId, @Meta, @ModBy);";

        internal const string Order_InsertAdjustment = @"
                    INSERT INTO order_adjustment
                    (id, order_id, order_line_id, item_type_id, code, amount,
                        description_key, metadata, modified_by)
                    VALUES
                    (@Id, @OrderId, @OrderLineId, @ItemTypeId, @Code, @Amount, @DescKey, @Meta, @ModBy);";


        // ----- Persistence helpers used by PaymentService -----

        internal const string Charge_Insert = @"
            INSERT INTO payment_charge
            (id, payment_intent_id, status_id, amount_captured, currency_id, provider_charge_id,
             payment_datetime, creation_time, modified_by)
            VALUES (@Id, @IntentId, @Status, @Amt, @Ccy, @ProvCharge, @PaidAt, NOW(6), @ModBy);";

        internal const string Intent_FindPendingForOrder = @"
            SELECT pi.id, pi.status_id, pi.idempotency_key, pi.provider_intent_id,
                   pi.payment_gateway_id, pi.amount, pi.currency_id
            FROM payment_intent pi
            WHERE pi.order_id=@OrderId AND pi.status_id IN (1,2)
            ORDER BY pi.creation_time DESC LIMIT 1;";

        internal const string Intent_FindByIdem = @"
            SELECT id, provider_intent_id FROM payment_intent
            WHERE order_id=@OrderId AND idempotency_key=@Key LIMIT 1;";

        internal const string Intent_Insert = @"
            INSERT INTO payment_intent
            (id, order_id, payment_gateway_id, status_id, amount, currency_id,
             client_secret, provider_intent_id, idempotency_key, creation_time, modified_by)
            VALUES (@Id, @OrderId, @Gw, @Status, @Amt, @Ccy, NULL, @ProvId, @Key, NOW(6), @ModBy);";

        internal const string Intent_Context = @"
            SELECT i.order_id, o.space_id, o.currency_id, o.total_net_amount, o.user_id, i.provider_intent_id, i.payment_gateway_id
            FROM payment_intent i JOIN `order` o ON o.id=i.order_id
            WHERE i.id=@IntentId LIMIT 1;";

        internal const string Order_SumCaptured = @"
            SELECT COALESCE(SUM(pc.amount_captured),0)
            FROM payment_charge pc
            JOIN payment_intent pi ON pi.id = pc.payment_intent_id
            WHERE pi.order_id=@OrderId;";

        internal const string Order_GetTotalNet = @"SELECT total_net_amount FROM `order` WHERE id=@OrderId LIMIT 1;";

        internal const string Order_UpdatePaidStatus = @"
            UPDATE `order` SET status_id=@PaidStatusId, modified_by=@ModBy
            WHERE id=@OrderId AND status_id<>@PaidStatusId;";

        internal const string Order_GetHeader = @"
            SELECT space_id, currency_id, total_net_amount, gateway_preferred_id
            FROM `order` WHERE id=@OrderId LIMIT 1;";

        internal const string Currency_ResolveIso = @"SELECT ISO FROM currency WHERE id=@Id LIMIT 1;";

        internal const string Currency_SelectIsoById = @"SELECT ISO FROM currency WHERE id=@Id LIMIT 1;";

        // ----- Misc -----
        internal const string Order_ExistsInSpace = @"SELECT 1 FROM `order` WHERE id=@OrderId AND space_id=@SpaceId;";
    }
}


