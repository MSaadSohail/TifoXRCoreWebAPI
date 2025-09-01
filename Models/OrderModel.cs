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

        public List<OrderLineDto> Lines { get; init; } = new();
        public List<OrderAdjustmentDto> Adjustments { get; init; } = new();
        public List<PaymentIntentDto> PaymentIntents { get; init; } = new();
        public List<EntitlementDto> Entitlements { get; init; } = new();
        public List<InvoiceSummaryDto> Invoices { get; init; } = new();
    }

    public sealed class OrderLineDto
    {
        public string Id { get; init; } = default!;
        public int ItemTypeId { get; init; }
        public string ItemRefId { get; init; } = default!; // CHAR(36) in schema
        public int? EntityId { get; init; }
        public int? ShopId { get; init; }
        public int Quantity { get; init; }
        public decimal UnitAmount { get; init; }
        public int CurrencyId { get; init; }
        public string? MetadataJson { get; init; }
    }

    public sealed class OrderAdjustmentDto
    {
        public string Id { get; init; } = default!;
        public string? OrderLineId { get; init; }
        public int ItemTypeId { get; init; }
        public string? Code { get; init; }
        public long Amount { get; init; }
        public string? DescriptionKey { get; init; }
        public string? MetadataJson { get; init; }
    }

    public sealed class PaymentIntentDto
    {
        public string Id { get; init; } = default!;
        public int PaymentGatewayId { get; init; }
        public int StatusId { get; init; }
        public long Amount { get; init; }
        public int CurrencyId { get; init; }
        public string? ClientSecret { get; init; }
        public string? ProviderIntentId { get; init; }

        public List<PaymentChargeDto> Charges { get; init; } = new();
    }

    public sealed class PaymentChargeDto
    {
        public string Id { get; init; } = default!;
        public int StatusId { get; init; }
        public long AmountCaptured { get; init; }
        public int CurrencyId { get; init; }
        public string? ProviderChargeId { get; init; }
        public long? GatewayFeeAmount { get; init; }
        public decimal? ExchangeRate { get; init; }
        public DateTime PaymentDateTime { get; init; }

        public List<PaymentRefundDto> Refunds { get; init; } = new();
    }

    public sealed class PaymentRefundDto
    {
        public string Id { get; init; } = default!;
        public int StatusId { get; init; }
        public long Amount { get; init; }
        public int CurrencyId { get; init; }
        public string? ProviderRefundId { get; init; }
        public string? Reason { get; init; }
        public DateTime RefundDateTime { get; init; }
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

    public sealed class InvoiceSummaryDto
    {
        public string Id { get; init; } = default!;
        public string InvoiceNumber { get; init; } = default!;
        public int StatusId { get; init; }
        public int CurrencyId { get; init; }
        public decimal TotalAmount { get; init; }
        public DateTime IssueDateTime { get; init; }
        public string? PdfUrl { get; init; }
    }



    public sealed class CreateOrderRequest
    {
        public string IdempotencyKey { get; init; } = default!;
        public string UserId { get; init; } = default!;
        public string Currency { get; init; } = "USD"; // e.g., "USD"
        public int TransactionTypeId { get; init; } = 1; // purchase
        public List<CreateOrderLineRequest> Lines { get; init; } = new();
        public List<CreateOrderAdjustmentRequest>? Adjustments { get; init; } = new();
        public string? Remarks { get; init; }
        public string? SessionId { get; init; } // optional; will auto-generate if not provided
    }

    public sealed class CreateOrderLineRequest
    {
        public int ItemTypeId { get; init; }
        public string ItemRefId { get; init; } = default!;   // CHAR(36) in schema is fine for "1"
        public int Quantity { get; init; }
        public long UnitAmountMinor { get; init; } // e.g., 10000 for $100.00
        public object? Metadata { get; init; }  // serialized to JSON
        public int? EntityId { get; init; }
        public int? ShopId { get; init; }
    }

    public sealed class CreateOrderAdjustmentRequest
    {
        public int ItemTypeId { get; init; }
        public string? OrderLineId { get; init; }  // optional link to a line
        public string? Code { get; init; } // e.g., "TAX5", "FEE1", "PROMO10"
        public long AmountMinor { get; init; }   // negative = discount
        public string? DescriptionKey { get; init; }
        public object? Metadata { get; init; } // serialized to JSON
    }

    public sealed class CreateOrderResponse
    {
        public string OrderId { get; init; } = default!;
        public long TotalNetMinor { get; init; }
        public string Currency { get; init; } = default!;
    }

}
