// <copyright file="OrderModel.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Saad Sohail</author>
// <date>09/02/2025</date>
// <summary></summary>

namespace GMS.TifoXRCoreWebAPI.Models
{
    public sealed class OrderDto
    {
        public string Id { get; init; } = default!;
        public int SpaceId { get; init; }
        public string UserId { get; init; } = default!;
        public int TransactionTypeId { get; init; }
        public int CurrencyId { get; init; }
        public int StatusId { get; init; }

        // all minor units
        public long TotalGrossAmount { get; init; }
        public long TotalDiscountAmount { get; init; }
        public long TotalTaxAmount { get; init; }
        public long TotalFeeAmount { get; init; }
        public long TotalNetAmount { get; init; }

        public string? ChangeReason { get; init; }
        public string? OriginalOrderId { get; init; }
        public string SessionId { get; init; } = default!;
        public int? GatewayPreferredId { get; init; }
        public DateTime OrderDateTime { get; init; }
        public string? Remarks { get; init; }

        public List<OrderLineDto> LineItems { get; init; } = new();
        public List<OrderAdjustmentDto> Adjustments { get; init; } = new();
        public List<PaymentIntentDto> PaymentIntents { get; init; } = new();
        public List<EntitlementDto> Entitlements { get; init; } = new();
        public List<InvoiceSummaryDto> Invoices { get; init; } = new();
    }

    public sealed class OrderLineDto
    {
        public string Id { get; init; } = default!;
        public int ItemTypeId { get; init; }
        public string ItemRefId { get; init; } = default!;
        public int? EntityId { get; init; }
        public int? ShopId { get; init; }
        public int Quantity { get; init; }
        public long UnitAmountMinor { get; init; }
        public int CurrencyId { get; init; }
        public string? MetadataJson { get; init; }
    }

    public sealed class OrderAdjustmentDto
    {
        public string Id { get; init; } = default!;
        public string? OrderLineId { get; init; }
        public int ItemTypeId { get; init; }
        public string? Code { get; init; }
        public long AmountMinor { get; init; }
        public string? DescriptionKey { get; init; }
        public string? MetadataJson { get; init; }
    }

    public sealed class PaymentIntentDto
    {
        public string Id { get; init; } = default!;
        public int PaymentGatewayId { get; init; }
        public int StatusId { get; init; }
        public long AmountMinor { get; init; }       // << renamed from Amount
        public int CurrencyId { get; init; }
        public string? ClientSecret { get; init; }
        public string? ProviderIntentId { get; init; }
        public List<PaymentChargeDto> Charges { get; init; } = new();
    }

    public sealed class PaymentChargeDto
    {
        public string Id { get; init; } = default!;
        public int StatusId { get; init; }
        public long AmountCapturedMinor { get; init; } // << renamed from AmountCaptured
        public int CurrencyId { get; init; }
        public string? ProviderChargeId { get; init; }
        public long? GatewayFeeAmountMinor { get; init; } // optional; minor units if you expose it
        public decimal? ExchangeRate { get; init; }
        public DateTime PaymentDateTime { get; init; }
        public List<PaymentRefundDto> Refunds { get; init; } = new();
    }

    public sealed class PaymentRefundDto
    {
        public string Id { get; init; } = default!;
        public int StatusId { get; init; }
        public long AmountMinor { get; init; }       // << renamed from Amount
        public int CurrencyId { get; init; }
        public string? ProviderRefundId { get; init; }
        public string? Reason { get; init; }
        public DateTime RefundDateTime { get; init; }
    }

    public sealed class InvoiceSummaryDto
    {
        public string Id { get; init; } = default!;
        public string InvoiceNumber { get; init; } = default!;
        public int StatusId { get; init; }
        public int CurrencyId { get; init; }
        public long TotalAmountMinor { get; init; }  // << was decimal TotalAmount
        public DateTime IssueDateTime { get; init; }
        public string? PdfUrl { get; init; }
    }

    // --- Create Order (client -> API) ---
    public sealed class CreateOrderRequest
    {
        public string IdempotencyKey { get; init; } = default!;
        public string UserId { get; init; } = default!;
        public int CurrencyId { get; init; }          // << was string Currency (ISO)
        public int TransactionTypeId { get; init; } = 1;
        public string? SessionId { get; init; }
        public List<CreateOrderLineRequest> LineItems { get; init; } = new();
        public List<CreateOrderAdjustmentRequest>? Adjustments { get; init; } = new();
        public string? Remarks { get; init; }
    }

    public sealed class CreateOrderLineRequest
    {
        public int ItemTypeId { get; init; }
        public int ItemRefId { get; init; } = default!;
        public int Quantity { get; init; }
        public long UnitAmountMinor { get; init; }
        public object? Metadata { get; init; }
        public int? EntityId { get; init; }
        public int? ShopId { get; init; }
    }

    public sealed class CreateOrderAdjustmentRequest
    {
        public int ItemTypeId { get; init; }    //FIX ME: TBD
        public string? OrderLineItemId { get; init; }
        public string? Code { get; init; }
        public long AmountMinor { get; init; }       // negative for discounts
        public string? DescriptionKey { get; init; }
        public object? Metadata { get; init; }
    }

    public sealed class CreateOrderResponse
    {
        public string OrderId { get; init; } = default!;
        public long TotalNetMinor { get; init; }
        public int CurrencyId { get; init; }         // << was string Currency
    }

    // --- Payment Intents ---
    public sealed class CreatePaymentIntentRequest
    {
        public string IdempotencyKey { get; init; } = default!;
        //public int Gateway { get; init; } = default!; // "paypal" etc.
        //public long? AmountMinor { get; init; }          // defaults to order total if null
    }

    public sealed class CreatePaymentIntentResponse
    {
        public string PaymentIntentId { get; init; } = default!;
        public int PaymentGatewayId { get; init; }
        public int StatusId { get; init; }
        public string? ClientSecret { get; init; }
        public string? ProviderIntentId { get; init; }
        public string? ApproveLink { get; init; }
    }

    public sealed class ConfirmPaymentIntentRequest
    {
        public string IdempotencyKey { get; init; } = default!;
        public string? ProviderIntentId { get; init; }
    }

    public sealed class ConfirmPaymentIntentResponse
    {
        public string OrderId { get; init; } = default!;
        public string PaymentIntentId { get; init; } = default!;
        public string? ProviderChargeId { get; init; }
        public int PaymentStatusId { get; init; }
    }

    public sealed class InvoiceListResponse
    {
        public string OrderId { get; init; } = default!;
        public List<InvoiceSummaryDto> Invoices { get; init; } = new();
    }

    public sealed class EntitlementDto
    {
        public string Id { get; init; } = default!;
        public string OrderLineId { get; init; } = default!;
        public string UserId { get; init; } = default!;
        public int Status { get; init; }
        public int Quantity { get; init; }
        public DateTime? GrantedDateTime { get; init; }
        public string? RevokedReason { get; init; }
        public string? MetadataJson { get; init; }
    }

    // -------------------
    // Request/Response
    // -------------------
    public sealed class ReconcileRequest
    {
        public string? ProviderIntentId { get; init; }     // e.g., PayPal token from client (optional)
        public string? IntentId { get; init; }             // your internal payment_intent.id (optional)
        public string? IdempotencyKey { get; init; }       // required if AttemptCaptureIfApproved = true
        public bool AttemptCaptureIfApproved { get; init; } = false;
    }

    public sealed class VerifyPaymentResponse
    {
        public string OrderId { get; init; } = default!;
        public bool IsPaid { get; init; }
        public long CapturedMinor { get; init; }
        public long TotalNetMinor { get; init; }
        public bool HasEntitlements { get; init; }
        public bool HasInvoice { get; init; }
        public List<string> Missing { get; init; } = new();
    }

    public sealed class PendingIntentResponse
    {
        public bool Found { get; init; }
        public string? OrderId { get; init; }
        public string? PaymentIntentId { get; init; }
        public int? StatusId { get; init; }            // 1=require_action, 2=processing
        public string? IdempotencyKey { get; init; }   // <- critical: reuse this
        public string? ProviderIntentId { get; init; } // PayPal order token (EC-XXXX)
        public int? PaymentGatewayId { get; init; }
        public long? AmountMinor { get; init; }
        public int? CurrencyId { get; init; }
    }

    public sealed class RefundRequest
    {
        public string IdempotencyKey { get; set; } = default!;
        public long? AmountMinor { get; set; } // null => full refund
        public string? Reason { get; set; }
    }

    public sealed class RefundResponse
    {
        public string OrderId { get; set; } = default!;
        public string ChargeId { get; set; } = default!;
        public string RefundId { get; set; } = default!;
        public string ProviderRefundId { get; set; } = default!;
        public int StatusId { get; set; } // e.g., 3 = succeeded
        public long RefundedAmountMinor { get; set; }
        public int CurrencyId { get; set; }
    }

}
