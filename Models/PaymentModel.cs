// <copyright file="PaymentModel.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Urvashi Dhingra</author>
// <date>08/29/2025</date>
// <summary>DTOs and data models for Payments</summary>

using System;

namespace GMS.TifoXRCoreWebAPI.Models
{
    public class CreatePaymentIntentDto
    {
        public string OrderId { get; set; } = "";
        public int PaymentGatewayId { get; set; }
        public int? CurrencyId { get; set; }
        public string? Network { get; set; }
        public string IdempotencyKey { get; set; } = "";
    }

    public class PaymentIntentData
    {
        public string Id { get; set; } = "";
        public int SpaceId { get; set; }
        public string OrderId { get; set; } = "";
        public int PaymentGatewayId { get; set; }
        public int StatusId { get; set; }
        public long Amount { get; set; }
        public int CurrencyId { get; set; }
        public string? ProviderIntentId { get; set; }
        public string? ApproveLink { get; set; }
        public string? MetadataJson { get; set; }
    }

    public class PaymentChargeData
    {
        public string Id { get; set; } = "";
        public int SpaceId { get; set; }
        public string OrderId { get; set; } = "";
        public string PaymentIntentId { get; set; } = "";
        public long AmountCaptured { get; set; }
        public int StatusId { get; set; }
        public int CurrencyId { get; set; }
        public string ProviderChargeId { get; set; } = "";
        public string? MetadataJson { get; set; }
        public DateTime? PaymentDateTime { get; set; }
    }

    public class CreateRefundDto
    {
        public string PaymentChargeId { get; set; } = "";
        public long Amount { get; set; }
        public string? Reason { get; set; }
    }

    public class PaymentRefundData
    {
        public string Id { get; set; } = "";
        public int SpaceId { get; set; }
        public string PaymentChargeId { get; set; } = "";
        public long Amount { get; set; }
        public int StatusId { get; set; }
        public int CurrencyId { get; set; }
        public string? ProviderRefundId { get; set; }
        public string? Reason { get; set; }
    }
}
