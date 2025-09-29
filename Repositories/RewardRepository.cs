// <copyright file="RewardRepository.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Urvashi Dhingra</author>
// <date>09/16/2025</date>
// <summary>Repository implementation for Reward and UserReward</summary>

using GMS.TifoXRCoreWebAPI.Models;
using GMS.TifoXRCoreWebAPI.Repositories.Sql;
using GMS.TifoXRCoreWebAPI.Utilities.Infrastructure;

namespace GMS.TifoXRCoreWebAPI.Repositories
{
    /// <summary>
    /// Concrete repository for Reward &amp; UserReward operations.
    /// </summary>
    public sealed class RewardRepository : IRewardRepository
    {
        private readonly IDbProvider _db;
        public RewardRepository(IDbProvider db) => _db = db;

        // ---------- Reward definitions ----------

        public async Task<int> CreateAsync(int spaceId, RewardCreateDto dto)
        {
            await using var conn = await _db.OpenConnectionAsync();
            await using var cmd = _db.CreateCommand(conn, RewardSql.InsertReward);

            cmd.Parameters.Add(_db.CreateParameter("@RewardCompositionTypeId", dto.RewardCompositionTypeId));
            cmd.Parameters.Add(_db.CreateParameter("@NameKey", dto.NameKey));
            cmd.Parameters.Add(_db.CreateParameter("@DescriptionKey", (object?)dto.DescriptionKey ?? DBNull.Value));
            cmd.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));
            cmd.Parameters.Add(_db.CreateParameter("@EntityId", (object?)dto.EntityId ?? DBNull.Value));

            cmd.Parameters.Add(_db.CreateParameter("@MaxTotalClaims", (object?)dto.MaxTotalClaims ?? DBNull.Value));
            cmd.Parameters.Add(_db.CreateParameter("@MaxClaimsPerUser", (object?)dto.MaxClaimsPerUser ?? DBNull.Value));
            cmd.Parameters.Add(_db.CreateParameter("@CooldownSeconds", (object?)dto.CooldownSeconds ?? DBNull.Value));

            cmd.Parameters.Add(_db.CreateParameter("@ClaimRequired", dto.ClaimRequired ? 1 : 0));
            cmd.Parameters.Add(_db.CreateParameter("@IsActive", dto.IsActive ? 1 : 0));

            cmd.Parameters.Add(_db.CreateParameter("@ValidFrom", (object?)dto.ValidFrom ?? DBNull.Value));
            cmd.Parameters.Add(_db.CreateParameter("@ValidTo", (object?)dto.ValidTo ?? DBNull.Value));

            var obj = await cmd.ExecuteScalarAsync();
            return Convert.ToInt32(obj);
        }

        public async Task<RewardView?> GetAsync(int rewardId, int spaceId)
        {
            await using var conn = await _db.OpenConnectionAsync();
            await using var cmd = _db.CreateCommand(conn, RewardSql.GetReward);

            cmd.Parameters.Add(_db.CreateParameter("@Id", rewardId));
            cmd.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));

            await using var rdr = await cmd.ExecuteReaderAsync();
            if (!(await rdr.ReadAsync())) return null;

            return new RewardView
            {
                Id = rdr.GetInt32(rdr.GetOrdinal("id")),
                RewardCompositionTypeId = rdr.GetInt32(rdr.GetOrdinal("RewardCompositionTypeId")),
                NameKey = rdr.GetString(rdr.GetOrdinal("NameKey")),
                DescriptionKey = rdr.IsDBNull(rdr.GetOrdinal("DescriptionKey")) ? null : rdr.GetString(rdr.GetOrdinal("DescriptionKey")),
                SpaceId = rdr.GetInt32(rdr.GetOrdinal("SpaceId")),
                EntityId = rdr.IsDBNull(rdr.GetOrdinal("EntityId")) ? (int?)null : rdr.GetInt32(rdr.GetOrdinal("EntityId"))
            };
        }

        public async Task<IReadOnlyList<RewardView>> ListBySpaceAsync(int spaceId)
        {
            await using var conn = await _db.OpenConnectionAsync();
            await using var cmd = _db.CreateCommand(conn, RewardSql.ListRewardsBySpace);

            cmd.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));

            var list = new List<RewardView>();
            await using var rdr = await cmd.ExecuteReaderAsync();

            int o_id = -1, o_type = -1, o_name = -1, o_desc = -1, o_space = -1, o_entity = -1;
            bool ord = false;

            while (await rdr.ReadAsync())
            {
                if (!ord)
                {
                    o_id = rdr.GetOrdinal("id");
                    o_type = rdr.GetOrdinal("RewardCompositionTypeId");
                    o_name = rdr.GetOrdinal("NameKey");
                    o_desc = rdr.GetOrdinal("DescriptionKey");
                    o_space = rdr.GetOrdinal("SpaceId");
                    o_entity = rdr.GetOrdinal("EntityId");
                    ord = true;
                }

                list.Add(new RewardView
                {
                    Id = rdr.GetInt32(o_id),
                    RewardCompositionTypeId = rdr.GetInt32(o_type),
                    NameKey = rdr.GetString(o_name),
                    DescriptionKey = rdr.IsDBNull(o_desc) ? null : rdr.GetString(o_desc),
                    SpaceId = rdr.GetInt32(o_space),
                    EntityId = rdr.IsDBNull(o_entity) ? (int?)null : rdr.GetInt32(o_entity)
                });
            }

            return list;
        }

        public async Task<int> AddItemAsync(int rewardId, RewardItemDto dto)
        {
            await using var conn = await _db.OpenConnectionAsync();
            await using var cmd = _db.CreateCommand(conn, RewardSql.InsertRewardItem);

            cmd.Parameters.Add(_db.CreateParameter("@RewardId", rewardId));
            cmd.Parameters.Add(_db.CreateParameter("@ItemId", dto.ItemId));
            cmd.Parameters.Add(_db.CreateParameter("@Quantity", dto.Quantity));

            var obj = await cmd.ExecuteScalarAsync();
            return Convert.ToInt32(obj);
        }

        public async Task<int> AddCurrencyAsync(int rewardId, RewardCurrencyDto dto)
        {
            await using var conn = await _db.OpenConnectionAsync();
            await using var cmd = _db.CreateCommand(conn, RewardSql.InsertRewardCurrency);

            cmd.Parameters.Add(_db.CreateParameter("@RewardId", rewardId));
            cmd.Parameters.Add(_db.CreateParameter("@CurrencyId", dto.CurrencyId));
            cmd.Parameters.Add(_db.CreateParameter("@Amount", dto.Amount));

            var obj = await cmd.ExecuteScalarAsync();
            return Convert.ToInt32(obj);
        }

        // ---------- User rewards ----------

        public async Task<int> GrantPendingAsync(GrantRewardRequest req)
        {
            await using var conn = await _db.OpenConnectionAsync();

            // 1) get Pending status id
            int pendingId;
            await using (var get = _db.CreateCommand(conn, RewardSql.LookupPendingStatus))
            {
                var obj = await get.ExecuteScalarAsync();
                pendingId = Convert.ToInt32(obj);
            }

            // 2) insert (idempotent) and return id
            await using var cmd = _db.CreateCommand(conn, RewardSql.InsertUserRewardIdempotent);
            cmd.Parameters.Add(_db.CreateParameter("@UserId", req.UserId));
            cmd.Parameters.Add(_db.CreateParameter("@RewardId", req.RewardId));
            cmd.Parameters.Add(_db.CreateParameter("@SpaceId", req.SpaceId));
            cmd.Parameters.Add(_db.CreateParameter("@PendingId", pendingId));
            cmd.Parameters.Add(_db.CreateParameter("@GrantSource", (object?)req.GrantSource ?? DBNull.Value));
            cmd.Parameters.Add(_db.CreateParameter("@SourceEventId", req.SourceEventId));
            cmd.Parameters.Add(_db.CreateParameter("@Idem", req.IdempotencyKey));

            var res = await cmd.ExecuteScalarAsync();
            return Convert.ToInt32(res);
        }

        public async Task<UserRewardView?> GetUserRewardAsync(int userRewardId)
        {
            await using var conn = await _db.OpenConnectionAsync();
            await using var cmd = _db.CreateCommand(conn, RewardSql.GetUserRewardView);

            cmd.Parameters.Add(_db.CreateParameter("@Id", userRewardId));

            await using var rdr = await cmd.ExecuteReaderAsync();
            if (!(await rdr.ReadAsync())) return null;

            return new UserRewardView
            {
                Id = rdr.GetInt32(rdr.GetOrdinal("id")),
                UserId = rdr.GetString(rdr.GetOrdinal("UserId")),
                RewardId = rdr.GetInt32(rdr.GetOrdinal("RewardId")),
                SpaceId = rdr.GetInt32(rdr.GetOrdinal("SpaceId")),
                Status = rdr.GetString(rdr.GetOrdinal("Status")),
                ClaimedAt = rdr.IsDBNull(rdr.GetOrdinal("ClaimedAt")) ? (DateTime?)null : rdr.GetDateTime(rdr.GetOrdinal("ClaimedAt")),
                ExpiredAt = rdr.IsDBNull(rdr.GetOrdinal("ExpiredAt")) ? (DateTime?)null : rdr.GetDateTime(rdr.GetOrdinal("ExpiredAt")),
                GrantSource = rdr.IsDBNull(rdr.GetOrdinal("GrantSource")) ? null : rdr.GetString(rdr.GetOrdinal("GrantSource")),
                SourceEventId = rdr.GetInt32(rdr.GetOrdinal("SourceEventId")),
                IdempotencyKey = rdr.GetString(rdr.GetOrdinal("IdempotencyKey"))
            };
        }

        public async Task<(bool exists, bool updated)> TrySetDeliveredAsync(int userRewardId)
        {
            await using var conn = await _db.OpenConnectionAsync();

            // Exists?
            await using (var existsCmd = _db.CreateCommand(conn, RewardSql.ExistsUserReward))
            {
                existsCmd.Parameters.Add(_db.CreateParameter("@Id", userRewardId));
                var existsObj = await existsCmd.ExecuteScalarAsync();
                if (existsObj is null) return (exists: false, updated: false);
            }

            // Guarded update
            await using var upd = _db.CreateCommand(conn, RewardSql.SetDeliveredGuarded);
            upd.Parameters.Add(_db.CreateParameter("@Id", userRewardId));
            var affected = await upd.ExecuteNonQueryAsync(); // affected rows
            return (exists: true, updated: affected > 0);
        }

        public async Task<(bool exists, bool updated)> TrySetClaimedAsync(int userRewardId)
        {
            await using var conn = await _db.OpenConnectionAsync();

            await using (var existsCmd = _db.CreateCommand(conn, RewardSql.ExistsUserReward))
            {
                existsCmd.Parameters.Add(_db.CreateParameter("@Id", userRewardId));
                var existsObj = await existsCmd.ExecuteScalarAsync();
                if (existsObj is null) return (exists: false, updated: false);
            }

            await using var upd = _db.CreateCommand(conn, RewardSql.SetClaimedGuarded);
            upd.Parameters.Add(_db.CreateParameter("@Id", userRewardId));
            var affected = await upd.ExecuteNonQueryAsync();
            return (exists: true, updated: affected > 0);
        }
    }
}