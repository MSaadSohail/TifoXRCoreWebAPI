// <copyright file="RewardController.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Urvashi Dhingra</author>
// <date>09/16/2025</date>
// <summary>Reward definition + UserReward lifecycle endpoints</summary>

using Microsoft.AspNetCore.Mvc;
//
using GMS.TifoXRCoreWebAPI.Models;
using GMS.TifoXRCoreWebAPI.Services;
using GMS.TifoXRCoreWebAPI.Middleware;
using GMS.TifoXRCoreWebAPI.Middleware.Exceptions;

namespace GMS.TifoXRCoreWebAPI.Controllers
{
    // ==============================
    // Reward definitions (catalog)
    // ==============================
    [ApiController]
    [Route("api/space/{spaceId:int}/rewards")]
    [Produces("application/json")]
    public sealed class RewardsController : ControllerBase
    {
        private readonly IRewardService _svc;
        public RewardsController(IRewardService svc) => _svc = svc;

        // POST /api/space/{spaceId}/rewards
        [HttpPost]
        public async Task<ActionResult<object>> Create(int spaceId, [FromBody] RewardCreateDto dto)
        {
            if (dto is null)
                throw new ArgumentNullException(nameof(dto),
                    GlobalException.FormatExceptionMessage("Body is required.", nameof(Create), new { spaceId }));

            var id = await _svc.CreateAsync(spaceId, dto);
            return Ok(new { id });
        }

        // GET /api/space/{spaceId}/rewards/{rewardId}
        [HttpGet("{rewardId:int}")]
        public async Task<ActionResult<RewardView>> Get(int spaceId, int rewardId)
        {
            var row = await _svc.GetAsync(rewardId, spaceId);
            if (row is null)
                throw new ResourceNotFoundException(
                    GlobalException.FormatExceptionMessage("Reward not found.", nameof(Get), new { spaceId, rewardId }));
            return Ok(row);
        }

        // GET /api/space/{spaceId}/rewards
        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<RewardView>>> List(int spaceId)
        {
            var rows = await _svc.ListBySpaceAsync(spaceId);
            return Ok(rows);
        }

        // POST /api/space/{spaceId}/rewards/{rewardId}/items
        [HttpPost("{rewardId:int}/items")]
        public async Task<ActionResult<object>> AddItem(int spaceId, int rewardId, [FromBody] RewardItemDto dto)
        {
            if (dto is null)
                throw new ArgumentNullException(nameof(dto),
                    GlobalException.FormatExceptionMessage("Body is required.", nameof(AddItem), new { spaceId, rewardId }));

            if (dto.RewardId != rewardId)
                throw new ArgumentException(
                    GlobalException.FormatExceptionMessage("Route rewardId must match body RewardId.", nameof(AddItem), new { spaceId, rewardId, dto.RewardId }),
                    nameof(rewardId));

            var id = await _svc.AddItemAsync(dto);
            return Ok(new { id });
        }

        // POST /api/space/{spaceId}/rewards/{rewardId}/currencies
        [HttpPost("{rewardId:int}/currencies")]
        public async Task<ActionResult<object>> AddCurrency(int spaceId, int rewardId, [FromBody] RewardCurrencyDto dto)
        {
            if (dto is null)
                throw new ArgumentNullException(nameof(dto),
                    GlobalException.FormatExceptionMessage("Body is required.", nameof(AddCurrency), new { spaceId, rewardId }));

            if (dto.RewardId != rewardId)
                throw new ArgumentException(
                    GlobalException.FormatExceptionMessage("Route rewardId must match body RewardId.", nameof(AddCurrency), new { spaceId, rewardId, dto.RewardId }),
                    nameof(rewardId));

            var id = await _svc.AddCurrencyAsync(dto);
            return Ok(new { id });
        }
    }

    // ===========================================
    // User reward instances (grant / claim flow)
    // ===========================================
    [ApiController]
    [Route("api/user-rewards")]
    [Produces("application/json")]
    public sealed class UserRewardsController : ControllerBase
    {
        private readonly IRewardService _svc;
        public UserRewardsController(IRewardService svc) => _svc = svc;

        // POST /api/user-rewards/grant
        [HttpPost("grant")]
        public async Task<ActionResult<object>> Grant([FromBody] GrantRewardRequest req)
        {
            if (req is null)
                throw new ArgumentNullException(nameof(req),
                    GlobalException.FormatExceptionMessage("Body is required.", nameof(Grant), new { }));

            if (string.IsNullOrWhiteSpace(req.IdempotencyKey))
                throw new ArgumentException(
                    GlobalException.FormatExceptionMessage("IdempotencyKey is required.", nameof(Grant), new { req.UserId, req.RewardId, req.SpaceId }),
                    nameof(req.IdempotencyKey));

            var userRewardId = await _svc.GrantPendingAsync(req);
            return Ok(new { id = userRewardId });
        }

        // GET /api/user-rewards/{id}
        [HttpGet("{id:int}")]
        public async Task<ActionResult<UserRewardView>> GetUserReward(int id)
        {
            var row = await _svc.GetUserRewardAsync(id);
            if (row is null)
                throw new ResourceNotFoundException(
                    GlobalException.FormatExceptionMessage("UserReward not found.", nameof(GetUserReward), new { id }));
            return Ok(row);
        }

        // POST /api/user-rewards/{id}/deliver
        [HttpPost("{id:int}/deliver")]
        public async Task<IActionResult> MarkDelivered(int id)
        {
            await _svc.SetDeliveredAsync(id);
            return NoContent();
        }

        // POST /api/user-rewards/{id}/claim
        [HttpPost("{id:int}/claim")]
        public async Task<IActionResult> MarkClaimed(int id)
        {
            await _svc.SetClaimedAsync(id);
            return NoContent();
        }
    }
}
