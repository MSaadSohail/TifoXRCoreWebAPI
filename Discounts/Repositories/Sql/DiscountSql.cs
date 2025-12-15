// <copyright file="DiscountSql.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Syed Hussain</author>
// <date>11/26/2025</date>
// <summary>SQL for listing discounts by space (active only).</summary>
namespace GMS.TifoXRCoreWebAPI.Repositories.Sql
{
    public static class DiscountsSql
    {
        public const string ListActiveBySpace = @"
            SELECT
              d.id,
              d.space_id,
              d.discount_type_id,
              d.name_key,
              d.value            AS value_minor_units,
              d.start_at,
              d.end_at,
              d.metadata,
              d.creation_time,
              d.modified_time,
              d.modified_by
            FROM discounts d
            WHERE d.space_id = @SpaceId
              AND (d.start_at IS NULL OR NOW() >= d.start_at)
              AND (d.end_at   IS NULL OR NOW() <= d.end_at)
            ORDER BY d.id;";

        public const string ExistsDiscountType = @"
            SELECT 1 FROM discount_type WHERE id = @DiscountTypeId LIMIT 1;";

        public const string GetSupportedLocales = @"
            SELECT locale_id FROM supported_languages WHERE space_id = @SpaceId;";

        public const string InsertDiscount = @"
            INSERT INTO discounts (
              space_id, discount_type_id, name_key, value, start_at, end_at, metadata,
              creation_time, modified_time, modified_by
            )
            VALUES (
              @SpaceId, @DiscountTypeId, @NameKey, @Value, @StartAt, @EndAt, @Metadata,
              NOW(6), CURRENT_TIMESTAMP(0), @ModifiedBy
            );
            SELECT LAST_INSERT_ID();";

        public const string InsertI18n = @"
            INSERT INTO i18n (space_id, `key`, locale_id, value, creation_time, modified_time, modified_by)
            VALUES (@SpaceId, @Key, @LocaleId, @Value, NOW(6), NOW(6), @ModifiedBy);";

        public const string GetById = @"
            SELECT
              d.id,
              d.space_id,
              d.discount_type_id,
              d.name_key,
              d.value            AS value_minor_units,
              d.start_at,
              d.end_at,
              d.metadata,
              d.creation_time,
              d.modified_time,
              d.modified_by
            FROM discounts d
            WHERE d.id = @DiscountId AND d.space_id = @SpaceId
            LIMIT 1;";

        public const string GetForUpdate = @"
            SELECT id, space_id, name_key
            FROM discounts
            WHERE id = @DiscountId AND space_id = @SpaceId
            FOR UPDATE;";

        // Uniqueness check for name_key within the same space
        public const string ExistsNameKeyInSpaceExceptId = @"
            SELECT 1
            FROM discounts
            WHERE space_id = @SpaceId
              AND name_key = @NameKey
              AND id <> @DiscountId
            LIMIT 1;";

        public const string UpdateDiscount = @"
            UPDATE discounts
            SET
              discount_type_id = COALESCE(@NewTypeId, discount_type_id),
              name_key         = COALESCE(@NewNameKey, name_key),
              value            = COALESCE(@NewValue, value),
              start_at         = COALESCE(@NewStartAt, start_at),
              end_at           = COALESCE(@NewEndAt, end_at),
              metadata         = COALESCE(@NewMetadata, metadata),
              modified_time    = CURRENT_TIMESTAMP(0),
              modified_by      = @ModifiedBy
            WHERE id = @DiscountId AND space_id = @SpaceId;";

        // Reuse these if already present in your file (shown here for clarity)
        public const string UpdateI18nValue = @"
            UPDATE i18n
               SET value = @Value, modified_time = NOW(6), modified_by = @ModifiedBy
             WHERE space_id = @SpaceId AND `key` = @Key AND locale_id = @LocaleId;";

        public const string SoftExpireNow = @"
    UPDATE discounts
       SET end_at        = CASE
                             WHEN end_at IS NULL OR end_at > NOW(2) THEN NOW(2)
                             ELSE end_at
                           END,
           modified_time = CURRENT_TIMESTAMP(2),
           modified_by   = @ModifiedBy
     WHERE id = @DiscountId AND space_id = @SpaceId;";

        public const string HardDelete = @"
    DELETE FROM discounts
     WHERE id = @DiscountId AND space_id = @SpaceId;";

        public const string ExistsInSpace = @"
    SELECT 1
    FROM discounts
    WHERE id = @DiscountId AND space_id = @SpaceId
    LIMIT 1;";
    }
}

