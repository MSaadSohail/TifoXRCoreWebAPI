// <copyright file="DtoMappers.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Saad Sohail</author>
// <date>9/16/2025</date>
// <summary></summary>

using System.Data;
using GMS.TifoXRCoreWebAPI.Models;

namespace GMS.TifoXRCoreWebAPI.Utilities.Infrastructure
{
    internal static class DtoMappers
    {
        public static OrderDto MapOrderHeader(IDataRecord r)
        {
            return new OrderDto
            {
                Id = r.GetString(r.GetOrdinal("id")),
                UserId = r.GetString(r.GetOrdinal("user_id")),
                SpaceId = r.GetInt32(r.GetOrdinal("space_id")),
                TransactionTypeId = r.GetInt32(r.GetOrdinal("transaction_type_id")),
                CurrencyId = r.GetInt32(r.GetOrdinal("currency_id")),
                StatusId = r.GetInt32(r.GetOrdinal("status_id")),
                TotalGrossAmount = r.GetInt64(r.GetOrdinal("total_gross_amount")),
                TotalDiscountAmount = r.GetInt64(r.GetOrdinal("total_discount_amount")),
                TotalTaxAmount = r.GetInt64(r.GetOrdinal("total_tax_amount")),
                TotalFeeAmount = r.GetInt64(r.GetOrdinal("total_fee_amount")),
                TotalNetAmount = r.GetInt64(r.GetOrdinal("total_net_amount")),
                ChangeReason = GetNullableString(r, r.GetOrdinal("change_reason")),
                OriginalOrderId = GetNullableString(r, r.GetOrdinal("original_order_id")),
                SessionId = r.GetString(r.GetOrdinal("session_id")),
                GatewayPreferredId = r.IsDBNull(r.GetOrdinal("gateway_preferred_id"))
                                      ? null : r.GetInt32(r.GetOrdinal("gateway_preferred_id")),
                OrderDateTime = r.GetDateTime(r.GetOrdinal("order_datetime")),
                Remarks = GetNullableString(r, r.GetOrdinal("remarks")),
                LineItems = new(),
                Adjustments = new(),
                PaymentIntents = new(),
                Entitlements = new(),
                Invoices = new()
            };
        }

        private static string? GetNullableString(IDataRecord r, int ordinal)
            => r.IsDBNull(ordinal) ? null : r.GetString(ordinal);

        // add other small mappers (lines, adjustments, intents graph, refunds, invoices, entitlements)
    }
}

