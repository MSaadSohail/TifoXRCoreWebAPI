// <copyright file="RewardModel.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Urvashi Dhingra</author>
// <date>09/16/2025</date>
// <summary>DTOs for Reward definitions and UserReward lifecycle.</summary>

using System;
using System.Collections.Generic;

namespace GMS.TifoXRCoreWebAPI.Models
{
    // ----- Reward definition -----

    public sealed class RewardCreateDto
    {
        public int RewardCompositionTypeId { get; init; }
        public string NameKey { get; init; } = default!;
        public string? DescriptionKey { get; init; }
        public int? EntityId { get; init; }
        public bool ClaimRequired { get; init; } = true;
        public bool IsActive { get; init; } = true;
        public int? MaxTotalClaims { get; init; }
        public int? MaxClaimsPerUser { get; init; }
        public int? CooldownSeconds { get; init; }
        public DateTime? ValidFrom { get; init; }
        public DateTime? ValidTo { get; init; }
        public IReadOnlyList<RewardItemCreateDto>? Items { get; init; }
        public IReadOnlyList<RewardCurrencyCreateDto>? Currencies { get; init; }
    }

    public sealed class RewardUpdateDto
    {
        public string? DescriptionKey { get; init; }
        public int? EntityId { get; init; }
        public bool? ClaimRequired { get; init; }
        public bool? IsActive { get; init; }
        public int? MaxTotalClaims { get; init; }
        public int? MaxClaimsPerUser { get; init; }
        public int? CooldownSeconds { get; init; }
        public DateTime? ValidFrom { get; init; }
        public DateTime? ValidTo { get; init; }
    }

    public sealed class RewardItemCreateDto
    {
        public int ItemId { get; init; }
        public int Quantity { get; init; }
    }

    public sealed class RewardCurrencyCreateDto
    {
        public int CurrencyId { get; init; }
        public int Amount { get; init; }
    }

    public sealed class RewardItemView
    {
        public int Id { get; init; }
        public int ItemId { get; init; }
        public int Quantity { get; init; }
    }

    public sealed class RewardCurrencyView
    {
        public int Id { get; init; }
        public int CurrencyId { get; init; }
        public int Amount { get; init; }
    }

    public sealed class RewardSummaryView
    {
        public int Id { get; init; }
        public int RewardCompositionTypeId { get; init; }
        public string NameKey { get; init; } = default!;
        public string? DescriptionKey { get; init; }
        public int SpaceId { get; init; }
        public int? EntityId { get; init; }
        public bool IsActive { get; init; }
        public bool ClaimRequired { get; init; }
        public DateTime? ValidFrom { get; init; }
        public DateTime? ValidTo { get; init; }
    }

    public sealed class RewardDetailView : RewardSummaryView
    {
        public int? MaxTotalClaims { get; init; }
        public int? MaxClaimsPerUser { get; init; }
        public int? CooldownSeconds { get; init; }
        public IReadOnlyList<RewardItemView> Items { get; init; } = Array.Empty<RewardItemView>();
        public IReadOnlyList<RewardCurrencyView> Currencies { get; init; } = Array.Empty<RewardCurrencyView>();
    }

    // ----- Grant / Claim flow -----

    public sealed class GrantRewardRequest
    {
        public string UserId { get; init; } = default!;
        public int RewardId { get; init; }
        public int SpaceId { get; init; }
        public string IdempotencyKey { get; init; } = default!;
        public int SourceEventId { get; init; }
        public string? GrantSource { get; init; }
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

        public string? GrantSource { get; init; }
        public int SourceEventId { get; init; }
        public string IdempotencyKey { get; init; } = default!;
    }
}
