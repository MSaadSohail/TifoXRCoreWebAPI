// <copyright file="ItemModels.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Syed Hussain</author>
// <date>12/03/2025</date>
// <summary>DTOs for creating standalone items (with subtype & optional price).</summary>

using GMS.TifoXRCoreWebAPI.Models.Common;

namespace GMS.TifoXRCoreWebAPI.Models.Item
{
    public sealed class CreateItemDto
    {
        // item
        public int ItemTypeId { get; init; }         // 1=Merch, 2=Ticket, 3=Badge
        public int SpaceId { get; init; }
        public int? EntityId { get; init; }
        public string NameKey { get; init; } = default!;
        public string? DescriptionKey { get; init; }
        public bool IsAvailable { get; init; } = true;

        public LocalizedPairs? NameLocalizedPairs { get; init; }
        public LocalizedPairs? DescriptionLocalizedPairs { get; init; }

        // optional one-price convenience (like ShopItems V2)
        public int? RegionalCurrencyId { get; init; }
        public long? BaseCost { get; init; }

        // exactly one of these based on ItemTypeId
        public MerchandiseCreateDto? Merchandise { get; init; }
        public TicketCreateDto? Ticket { get; init; }
        public BadgeCreateDto? Badge { get; init; }
    }


    public sealed class UpdateItemDto
    {
        public bool? IsAvailable { get; init; }

        // Optional i18n updates (same rules as before)
        public LocalizedPairs? NameLocalizedPairs { get; init; }
        public LocalizedPairs? DescriptionLocalizedPairs { get; init; }

        // Prices to upsert (no delete by default)
        public List<PriceUpsertDto>? Prices { get; init; }

        // Subtype patches (provide only if this item is of that type)
        public MerchandiseUpdateDto? Merchandise { get; init; }
        public TicketUpdateDto? Ticket { get; init; }
        public BadgeUpdateDto? Badge { get; init; }
    }

    public sealed class PriceUpsertDto
    {
        public int RegionalCurrencyId { get; init; }
        public long BaseCost { get; init; }
    }

    public sealed class MerchandiseUpdateDto
    {
        // Patch semantics: only non-null properties are applied
        public int? MerchandiseTypeId { get; init; }
        public int? MerchandiseCategoryId { get; init; }
        public string? AssetUrl { get; init; }

        // Media:
        public string? ThumbnailMediaId { get; init; }  // use existing
        public MediaCreateDto? ThumbnailMedia { get; init; }  // create new and set
        public bool? ClearThumbnail { get; init; }  // set NULL explicitly
    }

    public sealed class TicketUpdateDto
    {
        public int? EventId { get; init; }
        public int? TicketTypeId { get; init; }
        public string? Seat { get; init; }
        public string? Section { get; init; }
        public DateTime? ValidFrom { get; init; }
        public DateTime? ValidTo { get; init; }
        public bool? IsTransferable { get; init; }
    }

    public sealed class BadgeUpdateDto
    {
        public bool? ClearMedia { get; init; }
        public string? MediaId { get; init; }
        public MediaCreateDto? Media { get; init; }

        // Optional: if provided, we’ll use Key (or keep current) and upsert the Values.
        public LocalizedPairs? NameLocalizedPairs { get; init; }          // Key + Values
        public LocalizedPairs? DescriptionLocalizedPairs { get; init; }   // Key + Values
    }


}
