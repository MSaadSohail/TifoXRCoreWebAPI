// <copyright file="ShopSql.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Syed Hussain</author>
// <date>11/25/2025</date>
// <summary>SQL statements for Shop</summary>

namespace GMS.TifoXRCoreWebAPI.Repositories.Sql
{
    public static class ShopSql
    {
        // List all shops in a space (no i18n expansion)
        public const string ListShopsBySpace = @"
SELECT
  s.id,
  s.space_id         AS SpaceId,
  s.platform_type_id AS PlatformTypeId,
  s.entity_id        AS EntityId,
  s.name_key         AS NameKey,
  s.creation_time    AS CreationTime,
  s.modified_time    AS ModifiedTime,
  s.modified_by      AS ModifiedBy
FROM shop s
WHERE s.space_id = @SpaceId
ORDER BY s.id;";



        //public const string GetShopByIdInSpace = @"
        //    SELECT
        //      s.id,
        //      s.space_id,
        //      s.platform_type_id,
        //      s.entity_id,
        //      s.name_key,
        //      s.creation_time,
        //      s.modified_time,
        //      s.modified_by
        //    FROM shop s
        //    WHERE s.space_id = @SpaceId
        //      AND s.id = @ShopId
        //    LIMIT 1;";


        //public const string ListBySpace = @"
        //    SELECT id, space_id AS SpaceId, platform_type_id AS PlatformTypeId, entity_id AS EntityId,
        //           name_key AS NameKey, creation_time AS CreationTime, modified_time AS ModifiedTime,
        //           modified_by AS ModifiedBy
        //    FROM shop
        //    WHERE space_id = @SpaceId
        //    ORDER BY id;";

        // GET BY ID (no i18n expansion)
        public const string GetById = @"
            SELECT id, space_id AS SpaceId, platform_type_id AS PlatformTypeId, entity_id AS EntityId,
                   name_key AS NameKey, creation_time AS CreationTime, modified_time AS ModifiedTime,
                   modified_by AS ModifiedBy
            FROM shop
            WHERE space_id = @SpaceId AND id = @ShopId
            LIMIT 1;";

        // CREATE SHOP
        public const string InsertShop = @"
            INSERT INTO shop (space_id, platform_type_id, entity_id, name_key, creation_time, modified_time, modified_by)
            VALUES (@SpaceId, @PlatformTypeId, @EntityId, @NameKey, NOW(6), CURRENT_TIMESTAMP(0), @ModifiedBy);
            SELECT LAST_INSERT_ID();";

        // Reference checks
        public const string ExistsSpace = @"SELECT 1 FROM space WHERE id = @SpaceId LIMIT 1;";
        public const string ExistsPlatformType = @"SELECT 1 FROM platform_type WHERE id = @Id LIMIT 1;";
        public const string ExistsEntity = @"SELECT 1 FROM entity WHERE id = @Id LIMIT 1;";

        // Locales supported by space
        public const string GetSupportedLocales = @"
            SELECT locale_id
            FROM supported_languages
            WHERE space_id = @SpaceId;";

        // I18n upsert/insert (simple insert; use ON DUPLICATE KEY if you enforce unique(space_id,key,locale_id))
        public const string InsertI18n = @"
            INSERT INTO i18n (space_id, `key`, locale_id, value, creation_time, modified_time, modified_by)
            VALUES (@SpaceId, @Key, @LocaleId, @Value, NOW(6), NOW(6), @ModifiedBy)
            ON DUPLICATE KEY UPDATE value = VALUES(value), modified_time = NOW(6), modified_by = VALUES(modified_by);";

        // Add near your other SQL constants

        public const string GetCurrentNameKey = @"
    SELECT name_key
    FROM shop
    WHERE id = @ShopId AND space_id = @SpaceId
    LIMIT 1;";

        public const string CheckDuplicateNameKey = @"
    SELECT COUNT(*)
    FROM shop
    WHERE space_id = @SpaceId
      AND name_key = @NameKey
      AND id <> @ShopId;";

        public const string UpdateShopPartial = @"
    UPDATE shop
    SET
      platform_type_id = COALESCE(@PlatformTypeId, platform_type_id),
      entity_id        = CASE WHEN @HasEntityId = 1 THEN @EntityId ELSE entity_id END,
      name_key         = COALESCE(@NameKey, name_key),
      modified_time    = CURRENT_TIMESTAMP(0),
      modified_by      = @ModifiedBy
    WHERE id = @ShopId AND space_id = @SpaceId;";

        public const string UpdateI18nValue = @"
    UPDATE i18n
       SET value = @Value,
           modified_time = NOW(6),
           modified_by = @ModifiedBy
     WHERE space_id = @SpaceId
       AND `key` = @Key
       AND locale_id = @LocaleId;";

        public const string ExistsShopInSpace = @"
            SELECT 1
            FROM shop s
            WHERE s.id = @ShopId
              AND s.space_id = @SpaceId
            LIMIT 1;";

        // Delete all mappings for this shop guarded by space (safe if shop_items lacks space_id)
        public const string DeleteShopItemsByShopInSpace = @"
            DELETE si
            FROM shop_items si
            JOIN shop s ON s.id = si.shop_id
                       AND s.space_id = @SpaceId
            WHERE si.shop_id = @ShopId;";

        // Delete the shop row itself
        public const string DeleteShopByIdInSpace = @"
            DELETE FROM shop
            WHERE id = @ShopId AND space_id = @SpaceId;";

        public const string GetShopNameKey = @"
            SELECT name_key
            FROM shop
            WHERE id = @ShopId AND space_id = @SpaceId
            LIMIT 1;";

        // --- NEW: item keys used ONLY by this shop in this space (safe to remove i18n) ---
        public const string SelectExclusiveItemKeysForShopInSpace = @"
            SELECT DISTINCT i.name_key AS NameKey, i.description_key AS DescriptionKey
            FROM shop_items si
            JOIN item i
              ON i.id = si.item_id
             AND i.space_id = @SpaceId
            WHERE si.shop_id = @ShopId
              AND NOT EXISTS (
                  SELECT 1
                  FROM shop_items si2
                  JOIN shop s2 ON s2.id = si2.shop_id AND s2.space_id = @SpaceId
                  WHERE si2.item_id = si.item_id
                    AND si2.shop_id <> @ShopId
              );";

        // --- NEW: delete i18n for a single key under a space ---
        public const string DeleteI18nByKey = @"
            DELETE FROM i18n
            WHERE space_id = @SpaceId AND `key` = @Key;";

    }
}
