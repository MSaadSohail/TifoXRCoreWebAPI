// <copyright file="IRewardRepository.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Urvashi Dhingra</author>
// <date>09/16/2025</date>
// <summary>Interface to handle Reward repository pattern</summary>

using GMS.TifoXRCoreWebAPI.Models;

namespace GMS.TifoXRCoreWebAPI.Repositories
{
    public interface IRewardRepository
    {
        // ----- Reward definitions -----
        Task<int> CreateAsync(int spaceId, RewardCreateDto dto);
        Task<RewardDetailView?> GetAsync(int rewardId, int spaceId);
        Task<IReadOnlyList<RewardSummaryView>> ListBySpaceAsync(int spaceId);

        Task<int> AddItemAsync(int rewardId, RewardItemCreateDto dto);
        Task<int> AddCurrencyAsync(int rewardId, RewardCurrencyCreateDto dto);
        Task UpdateAsync(int rewardId, int spaceId, RewardUpdateDto dto);
        Task ToggleActiveAsync(int rewardId, int spaceId, bool isActive);
        Task<string?> GetCompositionTypeCodeAsync(int rewardCompositionTypeId);
        Task<bool> HasRuleBindingsAsync(int rewardId);

        // ----- User rewards definitions -----
        Task<int> GrantPendingAsync(GrantRewardRequest req);
        Task<UserRewardView?> GetUserRewardAsync(int userRewardId);
        Task<(bool exists, bool updated)> TrySetDeliveredAsync(int userRewardId);
        Task<(bool exists, bool updated)> TrySetClaimedAsync(int userRewardId);
    }
}