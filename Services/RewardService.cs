// <copyright file="RewardService.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Urvashi Dhingra</author>
// <date>09/16/2025</date>
// <summary>Service implementation for Reward & UserReward</summary>

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
        public Task<int> CreateAsync(int spaceId, RewardCreateDto dto) 
            => _repo.CreateAsync(spaceId, dto);

        public Task<RewardView?> GetAsync(int rewardId, int spaceId) 
            => _repo.GetAsync(rewardId, spaceId);

        public Task<IReadOnlyList<RewardView>> ListBySpaceAsync(int id) 
            => _repo.ListBySpaceAsync(id);

        public Task<int> AddItemAsync(RewardItemDto dto) 
            => _repo.AddItemAsync(dto);

        public Task<int> AddCurrencyAsync(RewardCurrencyDto dto) 
            => _repo.AddCurrencyAsync(dto);


        // User rewards
        public Task<int> GrantPendingAsync(GrantRewardRequest req) 
            => _repo.GrantPendingAsync(req);

        public Task<UserRewardView?> GetUserRewardAsync(int userRewardId) 
            => _repo.GetUserRewardAsync(userRewardId);

        public Task SetDeliveredAsync(int userRewardId) 
            => _repo.SetDeliveredAsync(userRewardId);

        public Task SetClaimedAsync(int userRewardId) 
            => _repo.SetClaimedAsync(userRewardId);
    }
}
