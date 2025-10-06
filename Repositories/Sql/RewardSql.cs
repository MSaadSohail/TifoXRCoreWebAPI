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
            (reward_type_id, name_key, description_key, space_id, entity_id, creation_time, modified_by)
            VALUES (@RewardTypeId, @NameKey, @DescriptionKey, @SpaceId, @EntityId, NOW(6), 'system');
            SELECT LAST_INSERT_ID();";

        public const string GetReward = @"
            SELECT id, reward_type_id AS RewardTypeId, name_key AS NameKey, description_key AS DescriptionKey,
                   space_id AS SpaceId, entity_id AS EntityId
            FROM reward
            WHERE id=@Id AND space_id=@SpaceId
            LIMIT 1;";

        public const string ListRewardsBySpace = @"
            SELECT id, reward_type_id AS RewardTypeId, name_key AS NameKey, description_key AS DescriptionKey,
                   space_id AS SpaceId, entity_id AS EntityId
            FROM reward
            WHERE space_id=@SpaceId
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
            (user_id, reward_id, space_id, reward_status_id, idempotency_key, creation_time, modified_by)
            VALUES (@UserId, @RewardId, @SpaceId, @PendingId, @Idem, NOW(6), 'system')
            ON DUPLICATE KEY UPDATE id = LAST_INSERT_ID(id);
            SELECT LAST_INSERT_ID();";

        public const string GetUserRewardView = @"
            SELECT ur.id, ur.user_id AS UserId, ur.reward_id AS RewardId, ur.space_id AS SpaceId,
                   rs.status AS Status, ur.claimed_at AS ClaimedAt, ur.expired_at AS ExpiredAt
            FROM user_reward ur
            JOIN reward_status rs ON rs.id = ur.reward_status_id
            WHERE ur.id = @Id;";

        public const string SetDelivered = @"
            UPDATE user_reward
            SET reward_status_id = (SELECT id FROM reward_status WHERE status='Delivered' LIMIT 1),
                modified_by = 'system'
            WHERE id=@Id;";

        public const string SetClaimed = @"
            UPDATE user_reward
            SET reward_status_id = (SELECT id FROM reward_status WHERE status='Claimed' LIMIT 1),
                claimed_at = NOW(6),
                modified_by = 'system'
            WHERE id=@Id;";
    }
}
