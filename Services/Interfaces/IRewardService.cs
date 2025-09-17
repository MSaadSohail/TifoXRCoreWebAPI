// <copyright file="IRewardService.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Urvashi Dhingra</author>
// <date>09/16/2025</date>
// <summary>Service interface for Reward & UserReward</summary>

using GMS.TifoXRCoreWebAPI.Models;

namespace GMS.TifoXRCoreWebAPI.Services
{
    public interface IRewardService
    {
        // Reward definitions
        Task<int> CreateAsync(int spaceId, RewardCreateDto dto);
        Task<RewardView?> GetAsync(int rewardId, int spaceId);
        Task<IReadOnlyList<RewardView>> ListBySpaceAsync(int spaceId);
        Task<int> AddItemAsync(RewardItemDto dto);
        Task<int> AddCurrencyAsync(RewardCurrencyDto dto);

        // User rewards
        Task<int> GrantPendingAsync(GrantRewardRequest req);
        Task<UserRewardView?> GetUserRewardAsync(int userRewardId);
        Task SetDeliveredAsync(int userRewardId);
        Task SetClaimedAsync(int userRewardId);
    }
}
