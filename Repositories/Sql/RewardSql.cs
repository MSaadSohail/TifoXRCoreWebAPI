// <copyright file="RewardSql.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Urvashi Dhingra</author>
// <date>09/16/2025</date>
// <summary>SQL statements for Reward & UserReward</summary>

namespace GMS.TifoXRCoreWebAPI.Repositories.Sql
{
    public static class RewardSql
    {
        // Reward definitions
        public const string InsertReward = @"
            INSERT INTO reward
            (reward_composition_type_id, name_key, description_key, space_id, entity_id, 
            max_total_claims, max_claims_per_user, cooldown_seconds,
            claim_required, is_active, valid_from, valid_to,
            creation_time, modified_by)
            VALUES (@RewardCompositionTypeId, @NameKey, @DescriptionKey, @SpaceId, @EntityId,
            @MaxTotalClaims, @MaxClaimsPerUser, @CooldownSeconds,
            @ClaimRequired, @IsActive, @ValidFrom, @ValidTo,
            SYSUTCDATETIME(), 'system');
            SELECT CAST(SCOPE_IDENTITY() AS int);";

        public const string GetReward = @"
            SELECT
              id,
              reward_composition_type_id AS RewardCompositionTypeId,
              name_key  AS NameKey,
              description_key AS DescriptionKey,
              space_id  AS SpaceId,
              entity_id AS EntityId,
              max_total_claims AS MaxTotalClaims, 
              max_claims_per_user AS MaxClaimsPerUser,
              cooldown_seconds AS CooldownSeconds,
              CAST(claim_required AS int) AS ClaimRequired,
              CAST(is_active AS int) AS IsActive,
              valid_from AS ValidFrom,
              valid_to AS ValidTo
            FROM reward
            WHERE id=@Id AND space_id=@SpaceId
            ;";

        public const string ListRewardsBySpace = @"
            SELECT
              id,
              reward_composition_type_id AS RewardCompositionTypeId,
              name_key            AS NameKey,
              description_key     AS DescriptionKey,
              space_id            AS SpaceId,
              entity_id           AS EntityId,
              max_total_claims    AS MaxTotalClaims,
              max_claims_per_user AS MaxClaimsPerUser,
              cooldown_seconds    AS CooldownSeconds,
              CAST(claim_required AS int) AS ClaimRequired,
              CAST(is_active AS int)      AS IsActive,
              valid_from          AS ValidFrom,
              valid_to            AS ValidTo
            FROM reward
            WHERE space_id = @SpaceId
            ORDER BY id DESC;";

        public const string InsertRewardItem = @"
            INSERT INTO reward_items (reward_id, item_id, quantity, creation_time, modified_by)
            VALUES (@RewardId, @ItemId, @Quantity, SYSUTCDATETIME(), 'system');
            SELECT CAST(SCOPE_IDENTITY() AS int);";

        public const string InsertRewardCurrency = @"
            INSERT INTO reward_currency (reward_id, currency_id, amount, creation_time, modified_by)
            VALUES (@RewardId, @CurrencyId, @Amount, SYSUTCDATETIME(), 'system');
            SELECT CAST(SCOPE_IDENTITY() AS int);";

        public const string ListRewardItems = @"
            SELECT id, item_id AS ItemId, quantity AS Quantity
            FROM reward_items
            WHERE reward_id = @RewardId
            ORDER BY id;";

        public const string ListRewardCurrencies = @"
            SELECT id, currency_id AS CurrencyId, amount AS Amount
            FROM reward_currency
            WHERE reward_id = @RewardId
            ORDER BY id;";

        public const string UpdateRewardMetadata = @"
            UPDATE reward
            SET description_key     = COALESCE(@DescriptionKey, description_key),
                entity_id           = COALESCE(@EntityId, entity_id),
                claim_required      = COALESCE(@ClaimRequired, claim_required),
                is_active           = COALESCE(@IsActive, is_active),
                max_total_claims    = COALESCE(@MaxTotalClaims, max_total_claims),
                max_claims_per_user = COALESCE(@MaxClaimsPerUser, max_claims_per_user),
                cooldown_seconds    = COALESCE(@CooldownSeconds, cooldown_seconds),
                valid_from          = COALESCE(@ValidFrom, valid_from),
                valid_to            = COALESCE(@ValidTo, valid_to),
                modified_by         = 'system'
            WHERE id = @Id AND space_id = @SpaceId;";

        public const string ToggleRewardActive = @"
            UPDATE reward
            SET is_active = @IsActive,
                modified_by = 'system'
            WHERE id = @Id AND space_id = @SpaceId;";

        public const string GetCompositionTypeCode = @"
            SELECT type
            FROM reward_composition_type
            WHERE id = @Id;";

        public const string RewardHasRuleBindings = @"
            SELECT COUNT(*)
            FROM rule_action_reward rar
            WHERE rar.reward_id = @RewardId;";

        // User rewards
        public const string LookupPendingStatus = @"SELECT TOP 1 id FROM reward_status WHERE status = 'Pending';";

        public const string InsertUserRewardIdempotent = @"
            MERGE user_reward AS target
            USING (VALUES (@UserId, @RewardId, @SpaceId, @PendingId, @GrantSource, @SourceEventId, @Idem)) AS src
                (user_id, reward_id, space_id, reward_status_id, grant_source, source_event_id, idempotency_key)
            ON target.idempotency_key = src.idempotency_key
            WHEN NOT MATCHED THEN
                INSERT (user_id, reward_id, space_id, reward_status_id,
                        grant_source, source_event_id, idempotency_key,
                        claimed_at, expired_at, creation_time, modified_by)
                VALUES (src.user_id, src.reward_id, src.space_id, src.reward_status_id,
                        src.grant_source, src.source_event_id, src.idempotency_key,
                        SYSUTCDATETIME(), NULL, SYSUTCDATETIME(), 'system')
            WHEN MATCHED THEN
                UPDATE SET id = target.id
            OUTPUT inserted.id;";

        public const string GetUserRewardView = @"
            SELECT
              ur.id,
              ur.user_id         AS UserId,
              ur.reward_id       AS RewardId,
              ur.space_id        AS SpaceId,
              rs.status          AS Status,
              ur.reward_status_id AS RewardStatusId,
              ur.grant_source    AS GrantSource,
              ur.source_event_id AS SourceEventId,
              ur.idempotency_key AS IdempotencyKey,
              ur.claimed_at      AS ClaimedAt,
              ur.expired_at      AS ExpiredAt
            FROM user_reward ur
            JOIN reward_status rs ON rs.id = ur.reward_status_id
            WHERE ur.id = @Id;";

        public const string SetDeliveredGuarded = @"
            UPDATE user_reward
            SET reward_status_id = (SELECT TOP 1 id FROM reward_status WHERE status='Delivered'),
                modified_by = 'system'
            WHERE id=@Id
              AND reward_status_id = (SELECT TOP 1 id FROM reward_status WHERE status='Pending');";

        public const string SetClaimedGuarded = @"
            UPDATE user_reward
            SET reward_status_id = (SELECT TOP 1 id FROM reward_status WHERE status='Claimed'),
                claimed_at = SYSUTCDATETIME(),
                modified_by = 'system'
            WHERE id=@Id
                AND reward_status_id IN (
                    SELECT id FROM reward_status WHERE status IN ('Pending','Delivered'));";

        public const string ExistsUserReward = @"
            SELECT TOP 1 1 FROM user_reward WHERE id = @Id;";
    }
}
