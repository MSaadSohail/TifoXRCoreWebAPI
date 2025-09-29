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
            NOW(6), 'system');
            SELECT LAST_INSERT_ID();";

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
              (claim_required + 0) AS ClaimRequired,
              (is_active + 0) AS IsActive,
              valid_from AS ValidFrom, 
              valid_to AS ValidTo
            FROM reward
            WHERE id=@Id AND space_id=@SpaceId
            LIMIT 1;";

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
              (claim_required + 0) AS ClaimRequired,
              (is_active + 0)      AS IsActive,
              valid_from          AS ValidFrom,
              valid_to            AS ValidTo
            FROM reward
            WHERE space_id = @SpaceId
            ORDER BY id DESC;";

        public const string InsertRewardItem = @"
            INSERT INTO reward_items (reward_id, item_id, quantity, creation_time, modified_by)
            VALUES (@RewardId, @ItemId, @Quantity, NOW(6), 'system');
            SELECT LAST_INSERT_ID();";

        public const string InsertRewardCurrency = @"
            INSERT INTO reward_currency (reward_id, currency_id, amount, creation_time, modified_by)
            VALUES (@RewardId, @CurrencyId, @Amount, NOW(6), 'system');
            SELECT LAST_INSERT_ID();";

        // User rewards
        public const string LookupPendingStatus = @"SELECT id FROM reward_status WHERE status = 'Pending' LIMIT 1;";

        public const string InsertUserRewardIdempotent = @"
            INSERT INTO user_reward
            (user_id, reward_id, space_id, reward_status_id, 
            grant_source, source_event_id, idempotency_key, 
            claimed_at, expired_at, creation_time, modified_by)
            VALUES (@UserId, @RewardId, @SpaceId, @PendingId, 
            @GrantSource, @SourceEventId, @Idem, 
            NOW(6), NULL, NOW(6), 'system')
            ON DUPLICATE KEY UPDATE id = LAST_INSERT_ID(id);
            SELECT LAST_INSERT_ID();";

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
            SET reward_status_id = (SELECT id FROM reward_status WHERE status='Delivered' LIMIT 1),
                modified_by = 'system'
            WHERE id=@Id
              AND reward_status_id = (SELECT id FROM reward_status WHERE status='Pending' LIMIT 1);";

        public const string SetClaimedGuarded = @"
            UPDATE user_reward
            SET reward_status_id = (SELECT id FROM reward_status WHERE status='Claimed' LIMIT 1),
                claimed_at = NOW(6),
                modified_by = 'system'
            WHERE id=@Id
                AND reward_status_id IN (
                    SELECT id FROM reward_status WHERE status IN ('Pending','Delivered'));";

        public const string ExistsUserReward = @"
            SELECT 1 FROM user_reward WHERE id = @Id LIMIT 1;";
    }
}
