// <copyright file="ItemSql.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Syed Hussain</author>
// <date>12/03/2025</date>
// <summary>SQL for standalone item creation.</summary>

namespace GMS.TifoXRCoreWebAPI.Repositories.Sql
{
    public static class ItemSql
    {
        public const string InsertItem = @"
            INSERT INTO item (item_type_id, entity_id, space_id, name_key, description_key, is_available, creation_time, modified_time, modified_by)
            VALUES (@ItemTypeId, @EntityId, @SpaceId, @NameKey, @DescriptionKey, @IsAvailable, NOW(6), CURRENT_TIMESTAMP(0), @ModifiedBy);
            SELECT LAST_INSERT_ID();";

        public const string InsertMerchandise = @"
            INSERT INTO merchandise (id, merchandise_type_id, merchandise_category_id, asset_url, thumbnail_media_id, creation_time, modified_time, modified_by)
            VALUES (@ItemId, @MerchTypeId, @MerchCatId, @AssetUrl, @ThumbMediaId, NOW(6), CURRENT_TIMESTAMP(0), @ModifiedBy);";

        public const string InsertTicket = @"
            INSERT INTO ticket (id, event_id, ticket_type_id, seat, section, valid_from, valid_to, is_transferable, creation_time, modified_time, modified_by)
            VALUES (@ItemId, @EventId, @TicketTypeId, @Seat, @Section, @ValidFrom, @ValidTo, @IsTransferable, NOW(6), CURRENT_TIMESTAMP(0), @ModifiedBy);";

        public const string InsertBadge = @"
            INSERT INTO badge (id, name_key, description_key, media_id, creation_time, modified_time, modified_by)
            VALUES (@ItemId, @NameKey, @DescriptionKey, @MediaId, NOW(6), CURRENT_TIMESTAMP(0), @ModifiedBy);";

        public const string ExistsRegionalCurrency = @"SELECT 1 FROM regional_currency WHERE id = @RegionalCurrencyId LIMIT 1;";

        public const string InsertItemPrice = @"
            INSERT INTO item_prices (item_id, regional_currency_id, base_cost, creation_time, modified_time, modified_by)
            VALUES (@ItemId, @RegionalCurrencyId, @BaseCost, NOW(6), CURRENT_TIMESTAMP(0), @ModifiedBy);";

        public const string UpdateI18nValue = @"
            UPDATE i18n SET value = @Value, modified_time = NOW(6), modified_by = @ModifiedBy
            WHERE space_id = @SpaceId AND `key` = @Key AND locale_id = @LocaleId;";

        public const string InsertI18nValue = @"
            INSERT INTO i18n (space_id, `key`, locale_id, value, creation_time, modified_time, modified_by)
            VALUES (@SpaceId, @Key, @LocaleId, @Value, NOW(6), NOW(6), @ModifiedBy);";

        public const string GetSupportedLocales = @"SELECT locale_id FROM supported_languages WHERE space_id = @SpaceId;";

        // Minimal selector to return created item (basic + subtype foreign IDs/fields)
        public const string SelectCreatedItemEnvelope = @"
            SELECT
              i.id               AS item_id,
              i.item_type_id,
              i.name_key,
              i.description_key,
              i.is_available
            FROM item i
            WHERE i.id = @ItemId AND i.space_id = @SpaceId
            LIMIT 1;";

        public const string NewUuid = @"SELECT UUID();";
        public const string InsertMedia = @"
        INSERT INTO media (id, space_id, media_type_id, text_key, description_key, creation_time, modified_time, modified_by)
        VALUES (@Id, @SpaceId, @MediaTypeId, @TextKey, @DescKey, NOW(6), CURRENT_TIMESTAMP(0), @ModifiedBy);";

        public const string InsertI18nSimple = @"
        INSERT INTO i18n (`key`, locale_id, value, space_id, creation_time, modified_time, modified_by)
        VALUES (@Key, @LocaleId, @Value, @SpaceId, NOW(6), NOW(6), @ModifiedBy);";

        public const string InsertMediaLocalization = @"
        INSERT INTO media_localization (media_id, locale_id, media_link)
        VALUES (@MediaId, @LocaleId, @MediaLink);";


        public const string ListAllBySpace = @"
SELECT
  -- core
  i.id                                   AS item_id,
  i.item_type_id                         AS item_type_id,
  i.name_key                              AS name_key,
  i.description_key                       AS description_key,
  i.is_available                          AS is_available,

  -- pricing (ALL currencies for this item)
  ip.base_cost                            AS price_minor_units,
  ip.regional_currency_id                 AS currency_id,

  -- keep API contract (no shop stock at item-level)
  0                                       AS stock_quantity,

  -- discount (global or region-scoped to this price row)
  d.name_key                              AS discount_name_key,
  d.discount_type_id                      AS discount_type_id,
  d.value                                 AS discount_value_minor_units,
  d.start_at                              AS discount_start_at,
  d.end_at                                AS discount_end_at,

  -- subtype: merchandise
  m.merchandise_type_id                   AS merch_type_id,
  m.merchandise_category_id               AS merch_category_id,
  m.asset_url                             AS merch_asset_url,
  m.thumbnail_media_id                    AS merch_thumb_media_id,

  -- subtype: ticket
  t.event_id                              AS ticket_event_id,
  t.ticket_type_id                        AS ticket_type_id,
  t.seat                                  AS ticket_seat,
  t.section                               AS ticket_section,
  t.valid_from                            AS ticket_valid_from,
  t.valid_to                              AS ticket_valid_to,
  t.is_transferable                       AS ticket_is_transferable,

  -- subtype: badge
  b.media_id                              AS badge_media_id,

  -- media fan-out (per-locale, like Portal)
  sl.locale_id                            AS locale_id,
  mtl.media_link                          AS merch_thumb_media_link,
  bml.media_link                          AS badge_media_link

FROM item i

-- include all prices for the item (0..n rows per item)
LEFT JOIN item_prices ip
  ON ip.item_id = i.id

-- match active discounts that are global or scoped to this currency row
LEFT JOIN item_discounts idis
  ON idis.item_id = i.id
 AND (idis.region_id IS NULL OR idis.region_id = ip.regional_currency_id)
LEFT JOIN discounts d
  ON d.id = idis.discount_id
 AND (d.start_at IS NULL OR NOW() >= d.start_at)
 AND (d.end_at   IS NULL OR NOW() <= d.end_at)

-- subtypes share PK = item.id
LEFT JOIN merchandise m ON m.id = i.id
LEFT JOIN ticket      t ON t.id = i.id
LEFT JOIN badge       b ON b.id = i.id

-- per-locale fan-out for media links
LEFT JOIN supported_languages sl
  ON sl.space_id = i.space_id

LEFT JOIN media_localization mtl
  ON mtl.media_id  = m.thumbnail_media_id
 AND mtl.locale_id = sl.locale_id

LEFT JOIN media_localization bml
  ON bml.media_id  = b.media_id
 AND bml.locale_id = sl.locale_id

WHERE i.space_id = @SpaceId
ORDER BY i.id, ip.regional_currency_id, sl.locale_id;
";



        public const string ExistsItemInSpace = @"
SELECT 1 FROM item WHERE id = @ItemId AND space_id = @SpaceId LIMIT 1;";

        public const string GetItemSnapshot = @"
SELECT id, space_id AS SpaceId, name_key AS NameKey, description_key AS DescriptionKey
FROM item WHERE id = @ItemId;";

        public const string UpdateItemBasics = @"
UPDATE item
SET
  is_available  = COALESCE(@IsAvailable, is_available),
  modified_time = CURRENT_TIMESTAMP(0),
  modified_by   = @ModifiedBy
WHERE id = @ItemId AND space_id = @SpaceId;";




        // Prices
        public const string UpsertItemPrice = @"
INSERT INTO item_prices (item_id, regional_currency_id, base_cost, modified_by)
VALUES (@ItemId, @RegionalCurrencyId, @BaseCost, @ModifiedBy)
ON DUPLICATE KEY UPDATE
  base_cost    = VALUES(base_cost),
  modified_by  = VALUES(modified_by),
  modified_time= CURRENT_TIMESTAMP(0);";

        // Merchandise
        public const string UpdateMerchandise = @"
UPDATE merchandise
SET
  merchandise_type_id     = COALESCE(@MerchTypeId, merchandise_type_id),
  merchandise_category_id = COALESCE(@MerchCategoryId, merchandise_category_id),
  asset_url               = COALESCE(@AssetUrl, asset_url),
  thumbnail_media_id      = @ThumbMediaId_Value,
  modified_time           = CURRENT_TIMESTAMP(0),
  modified_by             = @ModifiedBy
WHERE id = @ItemId;";

        // Ticket
        public const string UpdateTicket = @"
UPDATE ticket
SET
  event_id        = COALESCE(@EventId, event_id),
  ticket_type_id  = COALESCE(@TicketTypeId, ticket_type_id),
  seat            = COALESCE(@Seat, seat),
  section         = COALESCE(@Section, section),
  valid_from      = COALESCE(@ValidFrom, valid_from),
  valid_to        = COALESCE(@ValidTo, valid_to),
  is_transferable = COALESCE(@IsTransferable, is_transferable),
  modified_time   = CURRENT_TIMESTAMP(0),
  modified_by     = @ModifiedBy
WHERE id = @ItemId;";

        // Badge
        public const string UpdateBadge = @"
UPDATE badge
   SET media_id        = @MediaId_Value,
       name_key        = @NameKey_Value,
       description_key = @DescKey_Value,
       modified_by     = @ModifiedBy,
       modified_time   = UTC_TIMESTAMP()
 WHERE id = @ItemId;
";




        //DELETE QUERIES

        public const string GetItemDeleteInfo = @"
        SELECT 
            i.id,
            i.space_id,
            i.item_type_id,
            i.name_key,
            i.description_key,
            m.thumbnail_media_id AS merch_thumb_media_id,
            b.media_id           AS badge_media_id
        FROM item i
        LEFT JOIN merchandise m ON m.id = i.id
        LEFT JOIN badge       b ON b.id = i.id
        WHERE i.id = @ItemId AND i.space_id = @SpaceId;";

        public const string GetMediaKeys = @"
        SELECT text_key, description_key
        FROM media
        WHERE id = @MediaId AND space_id = @SpaceId;";

        public const string DeleteMediaLocalization = @"
        DELETE FROM media_localization
        WHERE media_id = @MediaId;";

        public const string DeleteMedia = @"
        DELETE FROM media
        WHERE id = @MediaId AND space_id = @SpaceId;";

        public const string DeleteI18nByKey = @"
        DELETE FROM i18n
        WHERE `key` = @Key AND space_id = @SpaceId;";

        public const string DeleteItemDiscounts = @"
        DELETE FROM item_discounts
        WHERE item_id = @ItemId;";

        public const string DeleteItemPrices = @"
        DELETE FROM item_prices
        WHERE item_id = @ItemId;";

        // Recommended: remove shop mappings to avoid orphans
        public const string DeleteShopItems = @"
        DELETE FROM shop_items
        WHERE item_id = @ItemId;";

        public const string DeleteMerchandise = @"DELETE FROM merchandise WHERE id = @ItemId;";
        public const string DeleteTicket = @"DELETE FROM ticket       WHERE id = @ItemId;";
        public const string DeleteBadge = @"DELETE FROM badge        WHERE id = @ItemId;";

        public const string DeleteItem = @"
        DELETE FROM item
        WHERE id = @ItemId AND space_id = @SpaceId;";
    }
}
