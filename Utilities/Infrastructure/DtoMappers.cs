// <copyright file="DtoMappers.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Saad Sohail</author>
// <date>9/16/2025</date>
// <summary></summary>

using System.Data;
//
using GMS.TifoXRCoreWebAPI.Models;

namespace GMS.TifoXRCoreWebAPI.Utilities.Infrastructure
{
    internal static class DtoMappers
    {
        public static OrderDto MapOrderHeader(IDataRecord r)
        {
            return new OrderDto
            {
                Id                  = r.GetString(r.GetOrdinal("id")),
                UserId              = r.GetString(r.GetOrdinal("user_id")),
                SpaceId             = r.GetInt32(r.GetOrdinal("space_id")),
                TransactionTypeId   = r.GetInt32(r.GetOrdinal("transaction_type_id")),
                CurrencyId          = r.GetInt32(r.GetOrdinal("currency_id")),
                StatusId            = r.GetInt32(r.GetOrdinal("status_id")),
                TotalGrossAmount    = r.GetInt64(r.GetOrdinal("total_gross_amount")),
                TotalDiscountAmount = r.GetInt64(r.GetOrdinal("total_discount_amount")),
                TotalTaxAmount      = r.GetInt64(r.GetOrdinal("total_tax_amount")),
                TotalFeeAmount      = r.GetInt64(r.GetOrdinal("total_fee_amount")),
                TotalNetAmount      = r.GetInt64(r.GetOrdinal("total_net_amount")),
                ChangeReason        = GetNullableString(r, r.GetOrdinal("change_reason")),
                OriginalOrderId     = GetNullableString(r, r.GetOrdinal("original_order_id")),
                SessionId           = r.GetString(r.GetOrdinal("session_id")),
                GatewayPreferredId  = GetNullableInt32(r,r.GetOrdinal("gateway_preferred_id")),
                OrderDateTime       = r.GetDateTime(r.GetOrdinal("order_datetime")),
                Remarks             = GetNullableString(r, r.GetOrdinal("remarks")),
                LineItems       = [],
                Adjustments     = [],
                PaymentIntents  = [],
                Entitlements    = [],
                Invoices        = []
            };
        }

        public static OrderLineDto MapOrderLine(IDataRecord r)
        {
            var unitMajor = r.GetDecimal(r.GetOrdinal("unit_amount"));

            return new OrderLineDto
            {
                Id              = r.GetString(r.GetOrdinal("id")),
                ItemTypeId      = r.GetInt32(r.GetOrdinal("item_type_id")),
                ItemRefId       = r.GetString(r.GetOrdinal("item_ref_id")),
                EntityId        = GetNullableInt32(r, r.GetOrdinal("entity_id")),
                ShopId          = GetNullableInt32(r, r.GetOrdinal("shop_id")),
                Quantity        = r.GetInt32(r.GetOrdinal("quantity")),
                UnitAmountMinor = MoneyConverter.ToMinor(unitMajor),
                CurrencyId      = r.GetInt32(r.GetOrdinal("currency_id")),
                MetadataJson    = GetNullableString(r, r.GetOrdinal("metadata"))
            };
        }

        // --- Adjustments ---
        public static OrderAdjustmentDto MapOrderAdjustment(IDataRecord r) => new OrderAdjustmentDto
        {
            Id              = r.GetString(r.GetOrdinal("id")),
            OrderLineId     = GetNullableString(r, r.GetOrdinal("order_line_id")),
            ItemTypeId      = r.GetInt32(r.GetOrdinal("item_type_id")),
            Code            = GetNullableString(r, r.GetOrdinal("code")),
            AmountMinor     = r.GetInt64(r.GetOrdinal("amount")),
            DescriptionKey  = GetNullableString(r, r.GetOrdinal("description_key")),
            MetadataJson    = GetNullableString(r, r.GetOrdinal("metadata"))
        };

        // --- Entitlements ---
        public static EntitlementDto MapEntitlement(IDataRecord r) => new EntitlementDto
        {
            Id              = r.GetString(r.GetOrdinal("id")),
            OrderLineId     = r.GetString(r.GetOrdinal("order_line_id")),
            UserId          = r.GetString(r.GetOrdinal("user_id")),
            Status          = r.GetInt32(r.GetOrdinal("status")),
            Quantity        = r.GetInt32(r.GetOrdinal("quantity")),
            GrantedDateTime = r.IsDBNull(r.GetOrdinal("granted_datetime")) ? null : r.GetDateTime(r.GetOrdinal("granted_datetime")),
            RevokedReason   = GetNullableString(r, r.GetOrdinal("revoked_reason")),
            MetadataJson    = GetNullableString(r, r.GetOrdinal("metadata"))
        };

        // --- Invoices (summary list) ---
        public static InvoiceSummaryDto MapInvoiceSummary(IDataRecord r)
        {
            var totalMajor = r.GetDecimal(r.GetOrdinal("total_amount"));

            return new InvoiceSummaryDto
            {
                Id               = r.GetString(r.GetOrdinal("id")),
                InvoiceNumber    = r.GetString(r.GetOrdinal("invoice_number")),
                StatusId         = r.GetInt32(r.GetOrdinal("status_id")),
                CurrencyId       = r.GetInt32(r.GetOrdinal("currency_id")),
                TotalAmountMinor = MoneyConverter.ToMinor(totalMajor),
                IssueDateTime    = r.GetDateTime(r.GetOrdinal("issue_datetime")),
                PdfUrl           = GetNullableString(r, r.GetOrdinal("pdf_url"))
            };
        }

        // --- Payment graph leaves ---
        public static PaymentIntentDto MapPaymentIntent(IDataRecord r) => new PaymentIntentDto
        {
            Id               = r.GetString(r.GetOrdinal("id")),
            PaymentGatewayId = r.GetInt32(r.GetOrdinal("payment_gateway_id")),
            StatusId         = r.GetInt32(r.GetOrdinal("status_id")),
            AmountMinor      = r.GetInt64(r.GetOrdinal("amount")),
            CurrencyId       = r.GetInt32(r.GetOrdinal("currency_id")),
            ClientSecret     = GetNullableString(r, r.GetOrdinal("client_secret")),
            ProviderIntentId = GetNullableString(r, r.GetOrdinal("provider_intent_id")),
            Charges     = []
        };

        public static PaymentChargeDto MapPaymentCharge(IDataRecord r) => new PaymentChargeDto
        {
            Id                      = r.GetString(r.GetOrdinal("id")),
            StatusId                = r.GetInt32(r.GetOrdinal("status_id")),
            AmountCapturedMinor     = r.GetInt64(r.GetOrdinal("amount_captured")),
            CurrencyId              = r.GetInt32(r.GetOrdinal("currency_id")),
            ProviderChargeId        = GetNullableString(r, r.GetOrdinal("provider_charge_id")),
            GatewayFeeAmountMinor   = GetNullableInt64(r, r.GetOrdinal("gateway_fee_amount")),
            ExchangeRate            = r.IsDBNull(r.GetOrdinal("exchange_rate")) 
                                    ? null 
                                    : r.GetDecimal(r.GetOrdinal("exchange_rate")),
            PaymentDateTime         = r.GetDateTime(r.GetOrdinal("payment_datetime")),
            Refunds     = []
        };

        public static PaymentRefundDto MapPaymentRefund(IDataRecord r) => new PaymentRefundDto
        {
            Id                  = r.GetString(r.GetOrdinal("id")),
            StatusId            = r.GetInt32(r.GetOrdinal("status_id")),
            AmountMinor         = r.GetInt64(r.GetOrdinal("amount")),
            CurrencyId          = r.GetInt32(r.GetOrdinal("currency_id")),
            ProviderRefundId    = GetNullableString(r, r.GetOrdinal("provider_refund_id")),
            Reason              = GetNullableString(r, r.GetOrdinal("reason")),
            RefundDateTime      = r.GetDateTime(r.GetOrdinal("refund_datetime"))
        };

        private static string? GetNullableString(IDataRecord r, int ordinal)
            => r.IsDBNull(ordinal) ? null : r.GetString(ordinal);
        private static int? GetNullableInt32(IDataRecord r, int ordinal)
            => r.IsDBNull(ordinal) ? (int?)null : r.GetInt32(ordinal);

        private static long? GetNullableInt64(IDataRecord r, int ordinal)
            => r.IsDBNull(ordinal) ? (long?)null : r.GetInt64(ordinal);

        // add other small mappers (lines, adjustments, intents graph, refunds, invoices, entitlements)
    }
}

