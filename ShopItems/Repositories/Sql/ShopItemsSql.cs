// <copyright file="ShopItemsSql.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Syed Hussain</author>
// <date>11/25/2025</date>
// <summary>SQL for listing items of a shop within a space.</summary>

namespace GMS.TifoXRCoreWebAPI.Repositories.Sql
{
    public static class ShopItemsSql
    {

        public const string ExistsShopInSpace = @"
            SELECT 1
            FROM shop s
            WHERE s.id = @ShopId
              AND s.space_id = @SpaceId
            LIMIT 1;";

        // Main listing query (filters by space via JOIN on shop + item)
        public const string ListItemsByShop = @"
SELECT
  -- core
  i.id                              AS item_id,
  i.item_type_id                    AS item_type_id,
  i.name_key                        AS name_key,
  i.description_key                 AS description_key,
  (i.is_available + 0)              AS is_available,
  COALESCE(si.amount, ip.base_cost) AS price_minor_units,
  si.stock_quantity                 AS stock_quantity,
  rc.currency_id                    AS currency_id,

  -- discount (may be NULL)
  d.name_key                        AS discount_name_key,
  d.discount_type_id                AS discount_type_id,
  d.value                           AS discount_value_minor_units,
  d.start_at                        AS discount_start_at,
  d.end_at                          AS discount_end_at,

  -- merchandise (NULL unless type=1)
  m.merchandise_type_id             AS merch_type_id,
  m.merchandise_category_id         AS merch_category_id,
  m.asset_url                       AS merch_asset_url,
  m.thumbnail_media_id              AS merch_thumb_media_id,

  -- ticket (NULL unless type=2)
  t.event_id                        AS ticket_event_id,
  t.ticket_type_id                  AS ticket_type_id,
  t.seat                            AS ticket_seat,
  t.section                         AS ticket_section,
  t.valid_from                      AS ticket_valid_from,
  t.valid_to                        AS ticket_valid_to,
  (t.is_transferable + 0)           AS ticket_is_transferable,

  -- badge (NULL unless type=3)
  b.media_id                        AS badge_media_id,

  -- media joins (per-locale like Portal)
  sl.locale_id                      AS locale_id,

  -- merchandise thumbnail media metadata + per-locale link
  tm.media_type_id                  AS merch_thumb_media_type_id,
  tml.media_link                    AS merch_thumb_media_link,
  tm.text_key                       AS merch_thumb_text_key,
  tm.description_key                AS merch_thumb_desc_key,
  i18n_mt.value                     AS merch_thumb_text_value,
  i18n_md.value                     AS merch_thumb_desc_value,

  -- badge media metadata + per-locale link
  bm.media_type_id                  AS badge_media_type_id,
  bml.media_link                    AS badge_media_link,
  bm.text_key                       AS badge_media_text_key,
  bm.description_key                AS badge_media_desc_key,
  i18n_bt.value                     AS badge_media_text_value,
  i18n_bd.value                     AS badge_media_desc_value

FROM shop_items si
JOIN shop s               ON s.id = si.shop_id AND s.space_id = @SpaceId
JOIN item i               ON i.id = si.item_id AND i.space_id = @SpaceId
JOIN regional_currency rc ON rc.id = si.regional_currency_id
LEFT JOIN item_prices ip  ON ip.item_id = i.id
                         AND ip.regional_currency_id = si.regional_currency_id

-- highest-value active discount by item + region/global
LEFT JOIN (
  SELECT *
  FROM (
    SELECT
      idsc.item_id,
      COALESCE(idsc.region_id, 0) AS region_key,
      d2.id                        AS discount_id,
      d2.name_key,
      d2.discount_type_id,
      d2.value,
      d2.start_at,
      d2.end_at,
      ROW_NUMBER() OVER (
        PARTITION BY idsc.item_id, COALESCE(idsc.region_id, 0)
        ORDER BY d2.value DESC, d2.end_at DESC, d2.id DESC
      ) AS rn
    FROM item_discounts idsc
    JOIN discounts d2 ON d2.id = idsc.discount_id
    WHERE NOW() BETWEEN d2.start_at AND d2.end_at
  ) ranked
  WHERE ranked.rn = 1
) d
  ON d.item_id = i.id
 AND (d.region_key = rc.region_id OR d.region_key = 0)

-- subtype rows (1:1 with item)
LEFT JOIN merchandise m ON m.id = i.id
LEFT JOIN ticket      t ON t.id = i.id
LEFT JOIN badge       b ON b.id = i.id

-- per-locale stitch like Portal
LEFT JOIN supported_languages sl ON sl.space_id = @SpaceId

-- merchandise thumbnail media
LEFT JOIN media tm
  ON tm.id = m.thumbnail_media_id
 AND tm.space_id = @SpaceId
LEFT JOIN media_localization tml
  ON tml.media_id  = m.thumbnail_media_id
 AND tml.locale_id = sl.locale_id
LEFT JOIN i18n i18n_mt
  ON i18n_mt.`key` = tm.text_key
 AND i18n_mt.space_id = @SpaceId
 AND i18n_mt.locale_id = sl.locale_id
LEFT JOIN i18n i18n_md
  ON i18n_md.`key` = tm.description_key
 AND i18n_md.space_id = @SpaceId
 AND i18n_md.locale_id = sl.locale_id

-- badge media
LEFT JOIN media bm
  ON bm.id = b.media_id
 AND bm.space_id = @SpaceId
LEFT JOIN media_localization bml
  ON bml.media_id  = b.media_id
 AND bml.locale_id = sl.locale_id
LEFT JOIN i18n i18n_bt
  ON i18n_bt.`key` = bm.text_key
 AND i18n_bt.space_id = @SpaceId
 AND i18n_bt.locale_id = sl.locale_id
LEFT JOIN i18n i18n_bd
  ON i18n_bd.`key` = bm.description_key
 AND i18n_bd.space_id = @SpaceId
 AND i18n_bd.locale_id = sl.locale_id

WHERE si.shop_id = @ShopId
ORDER BY i.id, sl.locale_id;";




        public const string GetItemSnapshot = @"
            SELECT id, space_id AS SpaceId, name_key AS NameKey, description_key AS DescriptionKey
            FROM item WHERE id = @ItemId LIMIT 1;";

        public const string ExistsRegionalCurrency = @"
            SELECT 1 FROM regional_currency WHERE id = @RegionalCurrencyId LIMIT 1;";

        public const string GetSupportedLocales = @"
            SELECT locale_id FROM supported_languages WHERE space_id = @SpaceId;";

        // ---- create mapping ----
        public const string InsertShopItem = @"
            INSERT INTO shop_items
                (shop_id, item_id, regional_currency_id, amount, stock_quantity, creation_time, modified_time, modified_by)
            VALUES
                (@ShopId, @ItemId, @RegionalCurrencyId, @Amount, COALESCE(@StockQuantity, 0), NOW(6), CURRENT_TIMESTAMP(0), @ModifiedBy);
            SELECT LAST_INSERT_ID();";

        // ---- i18n upsert primitives ----
        public const string UpdateI18nValue = @"
            UPDATE i18n
               SET value = @Value, modified_time = NOW(6), modified_by = @ModifiedBy
             WHERE space_id = @SpaceId AND `key` = @Key AND locale_id = @LocaleId;";

        public const string InsertI18nValue = @"
            INSERT INTO i18n (space_id, `key`, locale_id, value, creation_time, modified_time, modified_by)
            VALUES (@SpaceId, @Key, @LocaleId, @Value, NOW(6), NOW(6), @ModifiedBy);";

        // ---- select created envelope (aliases match ShopItemsListRow) ----
        public const string SelectCreatedShopItemEnvelope = @"
SELECT
  i.id                              AS item_id,
  i.item_type_id                    AS item_type_id,
  i.name_key                        AS name_key,
  i.description_key                 AS description_key,
  (i.is_available + 0)              AS is_available,
  COALESCE(si.amount, ip.base_cost) AS price_minor_units,
  si.stock_quantity                 AS stock_quantity,
  rc.currency_id                    AS currency_id,
  d.name_key                        AS discount_name_key,
  d.discount_type_id                AS discount_type_id,
  d.value                           AS discount_value_minor_units,
  d.start_at                        AS discount_start_at,
  d.end_at                          AS discount_end_at
FROM shop_items si
JOIN item i               ON i.id = si.item_id
JOIN shop s               ON s.id = si.shop_id AND s.space_id = @SpaceId
JOIN regional_currency rc ON rc.id = si.regional_currency_id
LEFT JOIN item_prices ip  ON ip.item_id = i.id
                          AND ip.regional_currency_id = si.regional_currency_id
LEFT JOIN (
    SELECT *
    FROM (
        SELECT
          idsc.item_id,
          COALESCE(idsc.region_id, 0) AS region_key,
          d2.id                        AS discount_id,
          d2.name_key,
          d2.discount_type_id,
          d2.value,
          d2.start_at,
          d2.end_at,
          ROW_NUMBER() OVER (
            PARTITION BY idsc.item_id, COALESCE(idsc.region_id, 0)
            ORDER BY d2.value DESC, d2.end_at DESC, d2.id DESC
          ) rn
        FROM item_discounts idsc
        JOIN discounts d2 ON d2.id = idsc.discount_id
        WHERE NOW() BETWEEN d2.start_at AND d2.end_at
    ) ranked
    WHERE ranked.rn = 1
) d
  ON d.item_id = i.id
 AND (d.region_key = rc.region_id OR d.region_key = 0)
WHERE si.shop_id = @ShopId
  AND si.item_id = @ItemId
  AND si.regional_currency_id = @RegionalCurrencyId
LIMIT 1;";


        public const string ExistsDiscount = @"
    SELECT 1 FROM discounts d WHERE d.id = @DiscountId LIMIT 1;";

        public const string UpsertItemDiscount = @"
    INSERT INTO item_discounts (item_id, region_id, discount_id, creation_time, modified_time, modified_by)
    VALUES (@ItemId, @RegionId, @DiscountId, NOW(6), CURRENT_TIMESTAMP(0), @ModifiedBy)
    ON DUPLICATE KEY UPDATE
      modified_time = CURRENT_TIMESTAMP(0),
      modified_by   = @ModifiedBy;";


        public const string ExistsMappingInSpace = @"
SELECT 1
FROM shop_items si
JOIN shop s ON s.id = si.shop_id AND s.space_id = @SpaceId
JOIN item i ON i.id = si.item_id AND i.space_id = @SpaceId
WHERE si.shop_id = @ShopId
  AND si.item_id = @ItemId
  AND si.regional_currency_id = @RegionalCurrencyId
LIMIT 1;";

        public const string UpdateShopItem = @"
UPDATE shop_items
SET
  amount         = @Amount,                          -- NULL clears override (fallback to base_cost)
  stock_quantity = COALESCE(@StockQuantity, stock_quantity),
  modified_time  = CURRENT_TIMESTAMP(0),
  modified_by    = @ModifiedBy
WHERE shop_id = @ShopId
  AND item_id = @ItemId
  AND regional_currency_id = @RegionalCurrencyId;";

        /* Select the updated envelope using the same aliasing your mapper expects.
           Uses scalar correlated subqueries for discount, matching your latest list query semantics. */
        public const string SelectUpdatedShopItemEnvelope = @"
SELECT
  i.id                              AS Id,
  i.item_type_id                    AS Item_Type_Id,
  i.name_key                        AS Name_Key,
  i.description_key                 AS Description_Key,
  (i.is_available + 0)              AS Is_Available,
  COALESCE(si.amount, ip.base_cost) AS Price_Minor_Units,
  si.stock_quantity                 AS Stock_Quantity,
  rc.currency_id                    AS Currency_Id,

  ( /* discount_name_key */
    SELECT d2.name_key
    FROM item_discounts id2
    JOIN discounts d2 ON d2.id = id2.discount_id
    WHERE id2.item_id = i.id
      AND (id2.region_id = rc.region_id OR id2.region_id IS NULL)
      AND NOW() BETWEEN d2.start_at AND d2.end_at
    ORDER BY
      CASE WHEN id2.region_id IS NULL THEN 1 ELSE 0 END,
      d2.value DESC, d2.end_at DESC, d2.id DESC
    LIMIT 1
  ) AS Discount_Name_Key,

  ( /* discount_type_id */
    SELECT d2.discount_type_id
    FROM item_discounts id2
    JOIN discounts d2 ON d2.id = id2.discount_id
    WHERE id2.item_id = i.id
      AND (id2.region_id = rc.region_id OR id2.region_id IS NULL)
      AND NOW() BETWEEN d2.start_at AND d2.end_at
    ORDER BY
      CASE WHEN id2.region_id IS NULL THEN 1 ELSE 0 END,
      d2.value DESC, d2.end_at DESC, d2.id DESC
    LIMIT 1
  ) AS Discount_Type,

  ( /* discount value */
    SELECT d2.value
    FROM item_discounts id2
    JOIN discounts d2 ON d2.id = id2.discount_id
    WHERE id2.item_id = i.id
      AND (id2.region_id = rc.region_id OR id2.region_id IS NULL)
      AND NOW() BETWEEN d2.start_at AND d2.end_at
    ORDER BY
      CASE WHEN id2.region_id IS NULL THEN 1 ELSE 0 END,
      d2.value DESC, d2.end_at DESC, d2.id DESC
    LIMIT 1
  ) AS Discount_Value_Minor_Units,

  ( /* discount start */
    SELECT d2.start_at
    FROM item_discounts id2
    JOIN discounts d2 ON d2.id = id2.discount_id
    WHERE id2.item_id = i.id
      AND (id2.region_id = rc.region_id OR id2.region_id IS NULL)
      AND NOW() BETWEEN d2.start_at AND d2.end_at
    ORDER BY
      CASE WHEN id2.region_id IS NULL THEN 1 ELSE 0 END,
      d2.value DESC, d2.end_at DESC, d2.id DESC
    LIMIT 1
  ) AS Discount_Start_At,

  ( /* discount end */
    SELECT d2.end_at
    FROM item_discounts id2
    JOIN discounts d2 ON d2.id = id2.discount_id
    WHERE id2.item_id = i.id
      AND (id2.region_id = rc.region_id OR id2.region_id IS NULL)
      AND NOW() BETWEEN d2.start_at AND d2.end_at
    ORDER BY
      CASE WHEN id2.region_id IS NULL THEN 1 ELSE 0 END,
      d2.value DESC, d2.end_at DESC, d2.id DESC
    LIMIT 1
  ) AS Discount_End_At

FROM shop_items si
JOIN item i               ON i.id = si.item_id
JOIN shop s               ON s.id = si.shop_id AND s.space_id = @SpaceId
JOIN regional_currency rc ON rc.id = si.regional_currency_id
LEFT JOIN item_prices ip  ON ip.item_id = i.id
                          AND ip.regional_currency_id = si.regional_currency_id
WHERE si.shop_id = @ShopId
  AND si.item_id = @ItemId
  AND si.regional_currency_id = @RegionalCurrencyId
LIMIT 1;";

        public const string MappingExistsInSpace = @"
SELECT 1
FROM shop_items si
JOIN shop s ON s.id = si.shop_id AND s.space_id = @SpaceId
JOIN item i ON i.id = si.item_id AND i.space_id = @SpaceId
WHERE si.shop_id = @ShopId
  AND si.item_id = @ItemId
  AND si.regional_currency_id = @RegionalCurrencyId
LIMIT 1;";
        public const string GetItemKeysForMapping = @"
SELECT i.name_key AS NameKey, i.description_key AS DescriptionKey
FROM shop_items si
JOIN shop s ON s.id = si.shop_id AND s.space_id = @SpaceId
JOIN item i ON i.id = si.item_id AND i.space_id = @SpaceId
WHERE si.shop_id = @ShopId
  AND si.item_id = @ItemId
  AND si.regional_currency_id = @RegionalCurrencyId
LIMIT 1;";

        public const string DeleteShopItemMapping = @"
DELETE FROM shop_items
WHERE shop_id = @ShopId
  AND item_id = @ItemId
  AND regional_currency_id = @RegionalCurrencyId;";

        public const string CountListingsOfItemInSpace = @"
SELECT COUNT(*)
FROM shop_items si
JOIN shop s ON s.id = si.shop_id
WHERE s.space_id = @SpaceId
  AND si.item_id = @ItemId;";

        public const string DeleteI18nByKeyInSpace = @"
DELETE FROM i18n
WHERE space_id = @SpaceId AND `key` = @Key;";

        public const string DeleteMapping = @"
DELETE si
FROM shop_items si
JOIN shop s ON s.id = si.shop_id AND s.space_id = @SpaceId
JOIN item i ON i.id = si.item_id  AND i.space_id = @SpaceId
WHERE si.shop_id = @ShopId
  AND si.item_id = @ItemId
  AND si.regional_currency_id = @RegionalCurrencyId;";

        public const string UpsertItemPrice = @"
INSERT INTO item_prices (item_id, regional_currency_id, base_cost, creation_time, modified_time, modified_by)
VALUES (@ItemId, @RegionalCurrencyId, @BaseCost, NOW(6), CURRENT_TIMESTAMP(0), @ModifiedBy)
ON DUPLICATE KEY UPDATE
  base_cost = VALUES(base_cost),
  modified_time = CURRENT_TIMESTAMP(0),
  modified_by = VALUES(modified_by);";

        

        //public const string SelectSupportedLocales = @"SELECT locale_id FROM supported_languages WHERE space_id = @SpaceId;";

        public const string SelectUuid = @"SELECT UUID();";

        public const string InsertMedia = @"
INSERT INTO media (id, space_id, media_type_id, text_key, description_key)
VALUES (@Id, @SpaceId, @MediaTypeId, @TextKey, @DescKey);";

        public const string InsertI18nBare = @"
INSERT INTO i18n (`key`, locale_id, value, space_id)
VALUES (@Key, @LocaleId, @Value, @SpaceId);";

        public const string InsertMediaLocalization = @"
INSERT INTO media_localization (media_id, locale_id, media_link)
VALUES (@MediaId, @LocaleId, @MediaLink);";


        public const string InsertItem = @"
INSERT INTO item (item_type_id, entity_id, space_id, name_key, description_key, is_available,
                  creation_time, modified_time, modified_by)
VALUES (@ItemTypeId, @EntityId, @SpaceId, @NameKey, @DescriptionKey, @IsAvailable,
        NOW(6), CURRENT_TIMESTAMP(0), @ModifiedBy);
SELECT LAST_INSERT_ID();";

        public const string InsertMerchandise = @"
INSERT INTO merchandise (id, merchandise_type_id, merchandise_category_id, asset_url, thumbnail_media_id,
                         creation_time, modified_time, modified_by)
VALUES (@ItemId, @MerchTypeId, @MerchCatId, @AssetUrl, @ThumbMediaId,
        NOW(6), CURRENT_TIMESTAMP(0), @ModifiedBy);";

        public const string InsertTicket = @"
INSERT INTO ticket (id, event_id, ticket_type_id, seat, section, valid_from, valid_to, is_transferable,
                    creation_time, modified_time, modified_by)
VALUES (@ItemId, @EventId, @TicketTypeId, @Seat, @Section, @ValidFrom, @ValidTo, @IsTransferable,
        NOW(6), CURRENT_TIMESTAMP(0), @ModifiedBy);";

        public const string InsertBadge = @"
INSERT INTO badge (id, name_key, description_key, media_id, creation_time, modified_time, modified_by)
VALUES (@ItemId, @NameKey, @DescriptionKey, @MediaId, NOW(6), CURRENT_TIMESTAMP(0), @ModifiedBy);";

        public const string InsertItemPrice = @"
INSERT INTO item_prices (item_id, regional_currency_id, base_cost, creation_time, modified_time, modified_by)
VALUES (@ItemId, @RegionalCurrencyId, @BaseCost, NOW(6), CURRENT_TIMESTAMP(0), @ModifiedBy);";

        public const string InsertShopItemId = @"
INSERT INTO shop_items (shop_id, item_id, regional_currency_id, amount, stock_quantity, creation_time, modified_time, modified_by)
VALUES (@ShopId, @ItemId, @RegionalCurrencyId, @Amount, COALESCE(@StockQuantity,0), NOW(6), CURRENT_TIMESTAMP(0), @ModifiedBy);";



        public const string ExistsItemPriceForCurrency = @"
        SELECT 1
        FROM item_prices
        WHERE item_id = @ItemId
          AND regional_currency_id = @RegionalCurrencyId
        LIMIT 1;";

        // One-row reader used by GetOneAsync (same columns as ListItemsByShop/MapRow expects)
        public const string SelectOneByShop = @"
        /* must project the same columns your MapRow reads */
        SELECT
            i.id                              AS item_id,
            i.item_type_id,
            i.name_key,
            i.description_key,
            i.is_available,

            si.stock_quantity,
            si.amount                         AS override_amount,
            rc.currency_id,

            -- effective price
            COALESCE(si.amount, ip.base_cost) AS price_minor_units,

            -- discount (nullable)
            d.name_key        AS discount_name_key,
            d.discount_type_id,
            d.value_minor_units AS discount_value_minor_units,
            d.start_at        AS discount_start_at,
            d.end_at          AS discount_end_at,

            -- merchandise (nullable)
            m.merchandise_type_id  AS merch_type_id,
            m.merchandise_category_id AS merch_category_id,
            m.asset_url            AS merch_asset_url,
            m.thumbnail_media_id   AS merch_thumbnail_media_id,

            -- ticket (nullable)
            t.event_id     AS ticket_event_id,
            t.ticket_type_id,
            t.seat         AS ticket_seat,
            t.section      AS ticket_section,
            t.valid_from   AS ticket_valid_from,
            t.valid_to     AS ticket_valid_to,
            t.is_transferable AS ticket_is_transferable,

            -- badge (nullable)
            b.media_id     AS badge_media_id

        FROM shop_items si
        JOIN item i              ON i.id = si.item_id AND i.space_id = @SpaceId
        JOIN regional_currency rc ON rc.id = si.regional_currency_id
        JOIN item_prices ip      ON ip.item_id = i.id AND ip.regional_currency_id = si.regional_currency_id
        LEFT JOIN merchandise m  ON m.id = i.id
        LEFT JOIN ticket t       ON t.id = i.id
        LEFT JOIN badge b        ON b.id = i.id

        -- active discount (if any).
        LEFT JOIN item_discounts idis
               ON idis.item_id = i.id
        LEFT JOIN discounts d
               ON d.id = idis.discount_id
              AND (d.region_id IS NULL OR d.region_id = rc.region_id)
              AND (NOW() BETWEEN d.start_at AND d.end_at)

        WHERE si.shop_id = @ShopId
          AND i.id = @ItemId
          AND si.regional_currency_id = @RegionalCurrencyId
        LIMIT 1;";

        public const string DeleteShopItemsForShopItem = @"
DELETE FROM shop_items
 WHERE shop_id = @ShopId
   AND item_id = @ItemId;";

        public const string DeleteItemDiscountsByItem = @"
DELETE FROM item_discounts
 WHERE item_id = @ItemId;";


    }
}
