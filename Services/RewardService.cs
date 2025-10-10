// <copyright file="RewardService.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Urvashi Dhingra</author>
// <date>09/16/2025</date>
// <summary>Service implementation for Reward & UserReward</summary>

using System;
using System.Collections.Generic;
using GMS.TifoXRCoreWebAPI.Models;
using GMS.TifoXRCoreWebAPI.Repositories;

namespace GMS.TifoXRCoreWebAPI.Services
{
    public sealed class RewardService : IRewardService
    {
        private readonly IRewardRepository _repo;

        public RewardService(IRewardRepository repo)
            => _repo = repo;

        // Reward definitions
        public async Task<int> CreateAsync(int spaceId, RewardCreateDto dto)
        {
            if (dto is null) throw new ArgumentNullException(nameof(dto));

            var typeCode = await _repo.GetCompositionTypeCodeAsync(dto.RewardCompositionTypeId)
                ?? throw new InvalidOperationException($"Unknown reward composition type id {dto.RewardCompositionTypeId}.");

            ValidateRewardLines(typeCode, dto.Items, dto.Currencies);
            return await _repo.CreateAsync(spaceId, dto);
        }

        public Task<RewardDetailView?> GetAsync(int rewardId, int spaceId)
            => _repo.GetAsync(rewardId, spaceId);

        public Task<IReadOnlyList<RewardSummaryView>> ListBySpaceAsync(int id)
            => _repo.ListBySpaceAsync(id);

        public async Task<int> AddItemAsync(int rewardId, RewardItemCreateDto dto)
        {
            if (await _repo.HasRuleBindingsAsync(rewardId))
                throw new InvalidOperationException("Reward structure cannot be modified once it is referenced by a rule action.");

            return await _repo.AddItemAsync(rewardId, dto);
        }

        public async Task<int> AddCurrencyAsync(int rewardId, RewardCurrencyCreateDto dto)
        {
            if (await _repo.HasRuleBindingsAsync(rewardId))
                throw new InvalidOperationException("Reward structure cannot be modified once it is referenced by a rule action.");

            return await _repo.AddCurrencyAsync(rewardId, dto);
        }

        public Task UpdateAsync(int rewardId, int spaceId, RewardUpdateDto dto)
            => _repo.UpdateAsync(rewardId, spaceId, dto);

        public Task ToggleActiveAsync(int rewardId, int spaceId, bool isActive)
            => _repo.ToggleActiveAsync(rewardId, spaceId, isActive);

        // User rewards
        public Task<int> GrantPendingAsync(GrantRewardRequest req)
            => _repo.GrantPendingAsync(req);

        public Task<UserRewardView?> GetUserRewardAsync(int userRewardId)
            => _repo.GetUserRewardAsync(userRewardId);

        public Task<(bool exists, bool updated)> TrySetDeliveredAsync(int userRewardId)
            => _repo.TrySetDeliveredAsync(userRewardId);

        public Task<(bool exists, bool updated)> TrySetClaimedAsync(int userRewardId)
            => _repo.TrySetClaimedAsync(userRewardId);

        private static void ValidateRewardLines(string typeCode, IReadOnlyList<RewardItemCreateDto>? items, IReadOnlyList<RewardCurrencyCreateDto>? currencies)
        {
            var itemCount = items?.Count ?? 0;
            var currencyCount = currencies?.Count ?? 0;
            var totalLines = itemCount + currencyCount;

            if (totalLines == 0)
                throw new InvalidOperationException("At least one reward line (item or currency) is required.");

            if (string.Equals(typeCode, "SINGLE", StringComparison.OrdinalIgnoreCase))
            {
                if (totalLines != 1)
                    throw new InvalidOperationException("SINGLE rewards must contain exactly one component (item or currency).");
            }
            else if (string.Equals(typeCode, "BUNDLE", StringComparison.OrdinalIgnoreCase))
            {
                if (totalLines < 1)
                    throw new InvalidOperationException("BUNDLE rewards must contain at least one component.");

                if (items is { Count: > 0 })
                {
                    var seen = new HashSet<int>();
                    foreach (var item in items)
                    {
                        if (!seen.Add(item.ItemId))
                            throw new InvalidOperationException("Duplicate item ids are not allowed within the same reward bundle.");
                    }
                }

                if (currencies is { Count: > 0 })
                {
                    var seen = new HashSet<int>();
                    foreach (var currency in currencies)
                    {
                        if (!seen.Add(currency.CurrencyId))
                            throw new InvalidOperationException("Duplicate currency ids are not allowed within the same reward bundle.");
                    }
                }
            }
        }
    }
}
