// <copyright file="RewardRepository.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Urvashi Dhingra</author>
// <date>09/16/2025</date>
// <summary>Repository implementation for Reward and UserReward</summary>

using System.Collections.Generic;
using System.Data.Common;
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
            await using var tx = await conn.BeginTransactionAsync();

            try
            {
                int rewardId;
                await using (var cmd = _db.CreateCommand(conn, RewardSql.InsertReward, tx))
                {
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
                    rewardId = Convert.ToInt32(obj);
                }

                if (dto.Items is { Count: > 0 })
                {
                    foreach (var item in dto.Items)
                    {
                        await using var addItem = _db.CreateCommand(conn, RewardSql.InsertRewardItem, tx);
                        addItem.Parameters.Add(_db.CreateParameter("@RewardId", rewardId));
                        addItem.Parameters.Add(_db.CreateParameter("@ItemId", item.ItemId));
                        addItem.Parameters.Add(_db.CreateParameter("@Quantity", item.Quantity));
                        await addItem.ExecuteScalarAsync();
                    }
                }

                if (dto.Currencies is { Count: > 0 })
                {
                    foreach (var currency in dto.Currencies)
                    {
                        await using var addCurrency = _db.CreateCommand(conn, RewardSql.InsertRewardCurrency, tx);
                        addCurrency.Parameters.Add(_db.CreateParameter("@RewardId", rewardId));
                        addCurrency.Parameters.Add(_db.CreateParameter("@CurrencyId", currency.CurrencyId));
                        addCurrency.Parameters.Add(_db.CreateParameter("@Amount", currency.Amount));
                        await addCurrency.ExecuteScalarAsync();
                    }
                }

                await tx.CommitAsync();
                return rewardId;
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        public async Task<RewardDetailView?> GetAsync(int rewardId, int spaceId)
        {
            await using var conn = await _db.OpenConnectionAsync();

            RewardSnapshot? snapshot = null;
            await using (var cmd = _db.CreateCommand(conn, RewardSql.GetReward))
            {
                cmd.Parameters.Add(_db.CreateParameter("@Id", rewardId));
                cmd.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));

                await using var rdr = await cmd.ExecuteReaderAsync();
                if (await rdr.ReadAsync())
                {
                    snapshot = new RewardSnapshot
                    {
                        Id = rdr.GetInt32(rdr.GetOrdinal("id")),
                        RewardCompositionTypeId = rdr.GetInt32(rdr.GetOrdinal("RewardCompositionTypeId")),
                        NameKey = rdr.GetString(rdr.GetOrdinal("NameKey")),
                        DescriptionKey = rdr.IsDBNull(rdr.GetOrdinal("DescriptionKey")) ? null : rdr.GetString(rdr.GetOrdinal("DescriptionKey")),
                        SpaceId = rdr.GetInt32(rdr.GetOrdinal("SpaceId")),
                        EntityId = rdr.IsDBNull(rdr.GetOrdinal("EntityId")) ? (int?)null : rdr.GetInt32(rdr.GetOrdinal("EntityId")),
                        ClaimRequired = rdr.GetInt32(rdr.GetOrdinal("ClaimRequired")) == 1,
                        IsActive = rdr.GetInt32(rdr.GetOrdinal("IsActive")) == 1,
                        MaxTotalClaims = rdr.IsDBNull(rdr.GetOrdinal("MaxTotalClaims")) ? (int?)null : rdr.GetInt32(rdr.GetOrdinal("MaxTotalClaims")),
                        MaxClaimsPerUser = rdr.IsDBNull(rdr.GetOrdinal("MaxClaimsPerUser")) ? (int?)null : rdr.GetInt32(rdr.GetOrdinal("MaxClaimsPerUser")),
                        CooldownSeconds = rdr.IsDBNull(rdr.GetOrdinal("CooldownSeconds")) ? (int?)null : rdr.GetInt32(rdr.GetOrdinal("CooldownSeconds")),
                        ValidFrom = rdr.IsDBNull(rdr.GetOrdinal("ValidFrom")) ? (DateTime?)null : rdr.GetDateTime(rdr.GetOrdinal("ValidFrom")),
                        ValidTo = rdr.IsDBNull(rdr.GetOrdinal("ValidTo")) ? (DateTime?)null : rdr.GetDateTime(rdr.GetOrdinal("ValidTo"))
                    };
                }
            }

            if (snapshot is null)
                return null;

            var items = await LoadItemsAsync(conn, rewardId);
            var currencies = await LoadCurrenciesAsync(conn, rewardId);

            return new RewardDetailView
            {
                Id = snapshot.Id,
                RewardCompositionTypeId = snapshot.RewardCompositionTypeId,
                NameKey = snapshot.NameKey,
                DescriptionKey = snapshot.DescriptionKey,
                SpaceId = snapshot.SpaceId,
                EntityId = snapshot.EntityId,
                ClaimRequired = snapshot.ClaimRequired,
                IsActive = snapshot.IsActive,
                MaxTotalClaims = snapshot.MaxTotalClaims,
                MaxClaimsPerUser = snapshot.MaxClaimsPerUser,
                CooldownSeconds = snapshot.CooldownSeconds,
                ValidFrom = snapshot.ValidFrom,
                ValidTo = snapshot.ValidTo,
                Items = items,
                Currencies = currencies
            };
        }

        public async Task<IReadOnlyList<RewardSummaryView>> ListBySpaceAsync(int spaceId)
        {
            await using var conn = await _db.OpenConnectionAsync();
            await using var cmd = _db.CreateCommand(conn, RewardSql.ListRewardsBySpace);
            cmd.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));

            var list = new List<RewardSummaryView>();
            await using var rdr = await cmd.ExecuteReaderAsync();

            while (await rdr.ReadAsync())
            {
                list.Add(new RewardSummaryView
                {
                    Id = rdr.GetInt32(rdr.GetOrdinal("id")),
                    RewardCompositionTypeId = rdr.GetInt32(rdr.GetOrdinal("RewardCompositionTypeId")),
                    NameKey = rdr.GetString(rdr.GetOrdinal("NameKey")),
                    DescriptionKey = rdr.IsDBNull(rdr.GetOrdinal("DescriptionKey")) ? null : rdr.GetString(rdr.GetOrdinal("DescriptionKey")),
                    SpaceId = rdr.GetInt32(rdr.GetOrdinal("SpaceId")),
                    EntityId = rdr.IsDBNull(rdr.GetOrdinal("EntityId")) ? (int?)null : rdr.GetInt32(rdr.GetOrdinal("EntityId")),
                    ClaimRequired = rdr.GetInt32(rdr.GetOrdinal("ClaimRequired")) == 1,
                    IsActive = rdr.GetInt32(rdr.GetOrdinal("IsActive")) == 1,
                    ValidFrom = rdr.IsDBNull(rdr.GetOrdinal("ValidFrom")) ? (DateTime?)null : rdr.GetDateTime(rdr.GetOrdinal("ValidFrom")),
                    ValidTo = rdr.IsDBNull(rdr.GetOrdinal("ValidTo")) ? (DateTime?)null : rdr.GetDateTime(rdr.GetOrdinal("ValidTo"))
                });
            }

            return list;
        }

        public async Task<int> AddItemAsync(int rewardId, RewardItemCreateDto dto)
        {
            await using var conn = await _db.OpenConnectionAsync();
            await using var cmd = _db.CreateCommand(conn, RewardSql.InsertRewardItem);

            cmd.Parameters.Add(_db.CreateParameter("@RewardId", rewardId));
            cmd.Parameters.Add(_db.CreateParameter("@ItemId", dto.ItemId));
            cmd.Parameters.Add(_db.CreateParameter("@Quantity", dto.Quantity));

            var obj = await cmd.ExecuteScalarAsync();
            return Convert.ToInt32(obj);
        }

        public async Task<int> AddCurrencyAsync(int rewardId, RewardCurrencyCreateDto dto)
        {
            await using var conn = await _db.OpenConnectionAsync();
            await using var cmd = _db.CreateCommand(conn, RewardSql.InsertRewardCurrency);

            cmd.Parameters.Add(_db.CreateParameter("@RewardId", rewardId));
            cmd.Parameters.Add(_db.CreateParameter("@CurrencyId", dto.CurrencyId));
            cmd.Parameters.Add(_db.CreateParameter("@Amount", dto.Amount));

            var obj = await cmd.ExecuteScalarAsync();
            return Convert.ToInt32(obj);
        }

        public async Task UpdateAsync(int rewardId, int spaceId, RewardUpdateDto dto)
        {
            await using var conn = await _db.OpenConnectionAsync();
            await using var cmd = _db.CreateCommand(conn, RewardSql.UpdateRewardMetadata);

            cmd.Parameters.Add(_db.CreateParameter("@Id", rewardId));
            cmd.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));
            cmd.Parameters.Add(_db.CreateParameter("@DescriptionKey", (object?)dto.DescriptionKey ?? DBNull.Value));
            cmd.Parameters.Add(_db.CreateParameter("@EntityId", (object?)dto.EntityId ?? DBNull.Value));
            cmd.Parameters.Add(_db.CreateParameter("@ClaimRequired", dto.ClaimRequired is null ? DBNull.Value : dto.ClaimRequired.Value ? 1 : 0));
            cmd.Parameters.Add(_db.CreateParameter("@IsActive", dto.IsActive is null ? DBNull.Value : dto.IsActive.Value ? 1 : 0));
            cmd.Parameters.Add(_db.CreateParameter("@MaxTotalClaims", (object?)dto.MaxTotalClaims ?? DBNull.Value));
            cmd.Parameters.Add(_db.CreateParameter("@MaxClaimsPerUser", (object?)dto.MaxClaimsPerUser ?? DBNull.Value));
            cmd.Parameters.Add(_db.CreateParameter("@CooldownSeconds", (object?)dto.CooldownSeconds ?? DBNull.Value));
            cmd.Parameters.Add(_db.CreateParameter("@ValidFrom", (object?)dto.ValidFrom ?? DBNull.Value));
            cmd.Parameters.Add(_db.CreateParameter("@ValidTo", (object?)dto.ValidTo ?? DBNull.Value));

            await cmd.ExecuteNonQueryAsync();
        }

        public async Task ToggleActiveAsync(int rewardId, int spaceId, bool isActive)
        {
            await using var conn = await _db.OpenConnectionAsync();
            await using var cmd = _db.CreateCommand(conn, RewardSql.ToggleRewardActive);

            cmd.Parameters.Add(_db.CreateParameter("@Id", rewardId));
            cmd.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));
            cmd.Parameters.Add(_db.CreateParameter("@IsActive", isActive ? 1 : 0));

            await cmd.ExecuteNonQueryAsync();
        }

        public async Task<string?> GetCompositionTypeCodeAsync(int rewardCompositionTypeId)
        {
            await using var conn = await _db.OpenConnectionAsync();
            await using var cmd = _db.CreateCommand(conn, RewardSql.GetCompositionTypeCode);

            cmd.Parameters.Add(_db.CreateParameter("@Id", rewardCompositionTypeId));

            var obj = await cmd.ExecuteScalarAsync();
            return obj is null or DBNull ? null : Convert.ToString(obj);
        }

        public async Task<bool> HasRuleBindingsAsync(int rewardId)
        {
            await using var conn = await _db.OpenConnectionAsync();
            await using var cmd = _db.CreateCommand(conn, RewardSql.RewardHasRuleBindings);

            cmd.Parameters.Add(_db.CreateParameter("@RewardId", rewardId));

            var obj = await cmd.ExecuteScalarAsync();
            return obj is not null && Convert.ToInt32(obj) > 0;
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

        #region Helpers

        private async Task<IReadOnlyList<RewardItemView>> LoadItemsAsync(DbConnection conn, int rewardId)
        {
            await using var cmd = _db.CreateCommand(conn, RewardSql.ListRewardItems);
            cmd.Parameters.Add(_db.CreateParameter("@RewardId", rewardId));

            var items = new List<RewardItemView>();
            await using var rdr = await cmd.ExecuteReaderAsync();
            while (await rdr.ReadAsync())
            {
                items.Add(new RewardItemView
                {
                    Id = rdr.GetInt32(rdr.GetOrdinal("id")),
                    ItemId = rdr.GetInt32(rdr.GetOrdinal("ItemId")),
                    Quantity = rdr.GetInt32(rdr.GetOrdinal("Quantity"))
                });
            }

            return items;
        }

        private async Task<IReadOnlyList<RewardCurrencyView>> LoadCurrenciesAsync(DbConnection conn, int rewardId)
        {
            await using var cmd = _db.CreateCommand(conn, RewardSql.ListRewardCurrencies);
            cmd.Parameters.Add(_db.CreateParameter("@RewardId", rewardId));

            var currencies = new List<RewardCurrencyView>();
            await using var rdr = await cmd.ExecuteReaderAsync();
            while (await rdr.ReadAsync())
            {
                currencies.Add(new RewardCurrencyView
                {
                    Id = rdr.GetInt32(rdr.GetOrdinal("id")),
                    CurrencyId = rdr.GetInt32(rdr.GetOrdinal("CurrencyId")),
                    Amount = rdr.GetInt32(rdr.GetOrdinal("Amount"))
                });
            }

            return currencies;
        }

        private sealed class RewardSnapshot
        {
            public int Id { get; init; }
            public int RewardCompositionTypeId { get; init; }
            public string NameKey { get; init; } = default!;
            public string? DescriptionKey { get; init; }
            public int SpaceId { get; init; }
            public int? EntityId { get; init; }
            public bool ClaimRequired { get; init; }
            public bool IsActive { get; init; }
            public int? MaxTotalClaims { get; init; }
            public int? MaxClaimsPerUser { get; init; }
            public int? CooldownSeconds { get; init; }
            public DateTime? ValidFrom { get; init; }
            public DateTime? ValidTo { get; init; }
        }

        #endregion
    }
}
