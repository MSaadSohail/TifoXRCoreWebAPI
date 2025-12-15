// <copyright file="ShopItemsModels.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Syed Hussain</author>
// <date>11/25/2025</date>
// <summary>DTOs for listing items of a shop within a space.</summary>

using System.Text.Json.Serialization;
using GMS.TifoXRCoreWebAPI.Models.Common;

namespace GMS.TifoXRCoreWebAPI.Models
{
    /// <summary>
    /// Envelope row returned by GET /api/space/{spaceId}/shop/{shopId}/items
    /// and by POST create (created row).
    /// </summary>
    

    public sealed class ShopItemsItem
    {
        public int Id { get; init; }
        public int Item_Type_Id { get; init; }           // numeric item_type_id
        public string Name_Key { get; init; } = default!;
        public string? Description_Key { get; init; }
        public bool Is_Available { get; init; }
    }

    public sealed class ShopItemsShopPart
    {
        public long Price_Minor_Units { get; init; }     // COALESCE(override, base_cost)
        public int Stock_Quantity { get; init; }
        public int Currency_Id { get; init; }            // numeric currency_id
        public ShopItemsDiscount? Discount { get; init; }
    }

    public sealed class ShopItemsDiscount
    {
        public string Name_Key { get; init; } = default!;
        public int Discount_Type { get; init; }          // numeric discount_type_id
        public long Value_Minor_Units { get; init; }
        public DateTime Start_At { get; init; }
        public DateTime End_At { get; init; }
    }

    // ----------------- CREATE (POST) DTO -----------------

    /// <summary>
    /// Payload for POST /api/space/{spaceId}/shop/{shopId}/items
    /// Snake_case JSON names are preserved with JsonPropertyName.
    /// </summary>
    public sealed class CreateShopItemDtoV2
    {
        // item
        public int ItemTypeId { get; init; }          // 1=Merch, 2=Ticket, 3=Badge (example)
        public int SpaceId { get; init; }             // can be inferred from route, keep if convenient
        public int? EntityId { get; init; }           // optional
        public string NameKey { get; init; } = default!;
        public string? DescriptionKey { get; init; }
        public bool IsAvailable { get; init; } = true;

        public LocalizedPairs? NameLocalizedPairs { get; init; }
        public LocalizedPairs? DescriptionLocalizedPairs { get; init; }

        // pricing + mapping
        public int RegionalCurrencyId { get; init; }  // for item_prices + shop_items
        public long BaseCost { get; init; }           // item_prices.base_cost (minor units)
        public long? Amount { get; init; }            // OPTIONAL shop_items override
        public int? StockQuantity { get; init; }      // defaults to 0 if null

        // exactly ONE of these should be populated based on ItemTypeId
        public MerchandiseCreateDto? Merchandise { get; init; }
        public TicketCreateDto? Ticket { get; init; }
        public BadgeCreateDto? Badge { get; init; }
    }

    public sealed class CreateShopItemLinkDto
    {
        public int ItemId { get; init; }                 // existing item
        public int RegionalCurrencyId { get; init; }     // must exist in item_prices for this item
        public long? Amount { get; init; }               // optional shop override
        public int? StockQuantity { get; init; }         // optional stock

        // optional i18n touch-ups
        public LocalizedPairs? NameLocalizedPairs { get; init; }
        public LocalizedPairs? DescriptionLocalizedPairs { get; init; }

        // optional discount attachment
        public DiscountAttachmentDto? Discount { get; init; }  // { DiscountId, RegionId? }
    }

    public sealed class MerchandiseCreateDto
    {
        public int MerchandiseTypeId { get; init; }
        public int? MerchandiseCategoryId { get; init; }
        public string? AssetUrl { get; init; }
        public string? ThumbnailMediaId { get; init; } // or full MediaCreateDto if you want to create on the fly
        public MediaCreateDto? ThumbnailMedia { get; init; } // optional: create new media
    }

    public sealed class TicketCreateDto
    {
        public int EventId { get; init; }
        public int TicketTypeId { get; init; }
        public string? Seat { get; init; }
        public string? Section { get; init; }
        public DateTime? ValidFrom { get; init; }
        public DateTime? ValidTo { get; init; }
        public bool IsTransferable { get; init; }
    }

    public sealed class BadgeCreateDto
    {
        public string? MediaId { get; init; }        // icon

        public MediaCreateDto? Media { get; init; }

        public LocalizedPairs? NameLocalizedPairs { get; init; }
        public LocalizedPairs? DescriptionLocalizedPairs { get; init; }
        public string? MediaTextKey { get; init; }   // optional i18n keys if you support it
        public string? MediaDescriptionKey { get; init; }
    }



    /// <summary>
    /// Minimal snapshot of item for validations (service/repo internal).
    /// </summary>
    public sealed class ItemNameDescSnapshot
    {
        public int Id { get; init; }
        public int SpaceId { get; init; }
        public string NameKey { get; init; } = default!;
        public string? DescriptionKey { get; init; }
    }

    public sealed class DiscountAttachmentDto
    {
        public int DiscountId { get; init; }      // must exist in discounts
        public int? RegionId { get; init; }       // null => global discount
    }

    public sealed class UpdateShopItemDto
    {
        public int RegionalCurrencyId { get; set; }

        public long? Amount { get; set; }

        public int? StockQuantity { get; set; }

        public LocalizedPairs? NameLocalizedPairs { get; set; }

        public LocalizedPairs? DescriptionLocalizedPairs { get; set; }
    }


    public sealed class ShopItemsListRow
    {
        public ShopItemsItem Item { get; init; } = default!;
        public ShopItemsShopPart Shop_Item { get; init; } = default!;

        public MerchandiseRead? Merchandise { get; set; }   // <- set
        public TicketRead? Ticket { get; set; }   // <- set
        public BadgeRead? Badge { get; set; }   // <- set
    }

    public sealed class MerchandiseRead
    {
        public int Merchandise_Type_Id { get; init; }
        public int? Merchandise_Category_Id { get; init; }
        public string? Asset_Url { get; init; }

        public MediaData? Thumbnail { get; set; }  // <- set
    }

    public sealed class TicketRead
    {
        public int Event_Id { get; init; }
        public int Ticket_Type_Id { get; init; }
        public string? Seat { get; init; }
        public string? Section { get; init; }
        public DateTime? Valid_From { get; init; }
        public DateTime? Valid_To { get; init; }
        public bool Is_Transferable { get; init; }
    }

    public sealed class BadgeRead
    {
        // Full media payload for the badge icon (id, type, links, optional text/desc i18n)
        public MediaData? Media { get; init; }
    }

    /// <summary>
    /// Mirrors how Portal returns media: core ids + optional text/description i18n
    /// plus per-locale links.
    /// </summary>

    //NEW_RESPONSE_MODELS

    public sealed class ShopItemsGroupedResponse
    {
        [JsonPropertyName("merchandiseList")]
        public List<MerchOut> MerchandiseList { get; init; } = new();

        [JsonPropertyName("ticketsList")]
        public List<TicketOut> TicketsList { get; init; } = new();

        [JsonPropertyName("badgesList")]
        public List<BadgeOut> BadgesList { get; init; } = new();
    }

    public sealed class ShopItemPriceOut
    {
        public long priceMinorUnits { get; init; }
        public int stockQuantity { get; init; }
        public int currencyId { get; init; }
        public DiscountOut? discount { get; init; }
    }

    public sealed class DiscountOut
    {
        public string nameKey { get; init; } = default!;
        public int discountType { get; init; }
        public long valueMinorUnits { get; init; }
        public DateTime startAt { get; init; }
        public DateTime endAt { get; init; }
    }

    public sealed class MerchOut
    {
        public int id { get; init; }
        public int itemTypeId { get; init; }
        public string nameKey { get; init; } = default!;
        public string? descriptionKey { get; init; }
        public bool isAvailable { get; init; }

        public ShopItemPriceOut shopItem { get; init; } = default!;

        public int merchandiseTypeId { get; init; }
        public int? merchandiseCategoryId { get; init; }
        public string? assetUrl { get; init; }

        // Your MediaData class already uses camelCase, so reuse it
        public MediaData? thumbnail { get; init; }
    }

    public sealed class TicketOut
    {
        public int id { get; init; }
        public int itemTypeId { get; init; }
        public string nameKey { get; init; } = default!;
        public string? descriptionKey { get; init; }
        public bool isAvailable { get; init; }
        public ShopItemPriceOut shopItem { get; init; } = default!;

        public int eventId { get; init; }
        public int ticketTypeId { get; init; }
        public string? seat { get; init; }
        public string? section { get; init; }
        public DateTime? validFrom { get; init; }
        public DateTime? validTo { get; init; }
        public bool isTransferable { get; init; }
    }

    public sealed class BadgeOut
    {
        public int id { get; init; }
        public int itemTypeId { get; init; }
        public string nameKey { get; init; } = default!;
        public string? descriptionKey { get; init; }
        public bool isAvailable { get; init; }
        public ShopItemPriceOut shopItem { get; init; } = default!;

        public MediaData? media { get; init; }
    }


}
