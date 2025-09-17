// <copyright file="RewardModel.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Urvashi Dhingra</author>
// <date>09/16/2025</date>
// <summary>DTOs for Reward definitions and UserReward lifecycle.</summary>

namespace GMS.TifoXRCoreWebAPI.Models
{
    public enum RewardType { Currency = 1, Item = 2, Mixed = 3 }

    // ----- Reward definition -----

    public sealed class RewardCreateDto
    {
        public int RewardTypeId { get; init; }
        public string NameKey { get; init; } = default!;
        public string? DescriptionKey { get; init; }
        public int? EntityId { get; init; }
    }

    public sealed class RewardItemDto
    {
        public int RewardId { get; init; }
        public int ItemId { get; init; }
        public int Quantity { get; init; }
    }

    public sealed class RewardCurrencyDto
    {
        public int RewardId { get; init; }
        public int CurrencyId { get; init; }
        public int Amount { get; init; }
    }

    public sealed class RewardView
    {
        public int Id { get; init; }
        public int RewardTypeId { get; init; }
        public string NameKey { get; init; } = default!;
        public string? DescriptionKey { get; init; }
        public int SpaceId { get; init; }
        public int? EntityId { get; init; }
    }

    // ----- Grant / Claim flow -----

    public sealed class GrantRewardRequest
    {
        public string UserId { get; init; } = default!;
        public int RewardId { get; init; }
        public int SpaceId { get; init; }
        public string IdempotencyKey { get; init; } = default!;
    }

    public sealed class ClaimRewardRequest
    {
        public string UserId { get; init; } = default!;
    }

    public sealed class UserRewardView
    {
        public int Id { get; init; }
        public string UserId { get; init; } = default!;
        public int RewardId { get; init; }
        public int SpaceId { get; init; }
        public string Status { get; init; } = default!;
        public DateTime? ClaimedAt { get; init; }
        public DateTime? ExpiredAt { get; init; }
    }
}
