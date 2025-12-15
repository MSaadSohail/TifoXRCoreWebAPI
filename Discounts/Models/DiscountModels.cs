// <copyright file="DiscountsModels.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Syed Hussain</author>
// <date>11/26/2025</date>
// <summary>DTOs for listing discounts by space.</summary>

using GMS.TifoXRCoreWebAPI.Models.Common;

namespace GMS.TifoXRCoreWebAPI.Models
{
    public sealed class DiscountDto
    {
        public int Id { get; init; }
        public int SpaceId { get; init; }
        public int DiscountTypeId { get; init; }
        public string NameKey { get; init; } = default!;
        public long ValueMinorUnits { get; init; }
        public DateTime? StartAt { get; init; }
        public DateTime? EndAt { get; init; }
        public string? Metadata { get; init; }
        public DateTime CreationTime { get; init; }
        public DateTime ModifiedTime { get; init; }
        public string ModifiedBy { get; init; } = default!;
    }
    public sealed class CreateDiscountDto
    {
        public int DiscountTypeId { get; init; }
        public string NameKey { get; init; } = default!;
        public long Value { get; init; }                       // minor units
        public DateTime? StartAt { get; init; }
        public DateTime? EndAt { get; init; }
        public string? Metadata { get; init; }                 // JSON string (nullable)

        // Optional: localized label for the discount’s name_key
        public LocalizedPairs? LocalizedPairs { get; init; }
    }
    public sealed class UpdateDiscountDto
    {
        public int? DiscountTypeId { get; init; }
        public string? NameKey { get; init; }
        public long? Value { get; init; }
        public DateTime? StartAt { get; init; }
        public DateTime? EndAt { get; init; }
        public string? Metadata { get; init; }

        // Required only if LocalizedPairs is supplied
        public int? SpaceId { get; init; }

        // Optional i18n upsert
        public LocalizedPairs? LocalizedPairs { get; init; }
    }
}
