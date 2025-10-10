// <copyright file="RewardController.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Urvashi Dhingra</author>
// <date>09/16/2025</date>
// <summary>Reward definition and UserReward lifecycle endpoints</summary>

using GMS.TifoXRCoreWebAPI.Middleware;
using GMS.TifoXRCoreWebAPI.Middleware.Errors;
//
using GMS.TifoXRCoreWebAPI.Models;
using GMS.TifoXRCoreWebAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace GMS.TifoXRCoreWebAPI.Controllers
{
    // =========================================================================
    // REWARD
    // =========================================================================
    [ApiController]
    [Route("api/space/{spaceId:int}/rewards")]
    [Produces("application/json")]
    public sealed class RewardsController : ControllerBase
    {
        private readonly IRewardService _svc;
        public RewardsController(IRewardService svc) => _svc = svc;

        #region CREATE

        /// <summary>
        /// Create a reward definition for the specified space.
        /// <returns>201 with new reward id.</returns>
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(object), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<object>> CreateReward(int spaceId, [FromBody] RewardCreateDto dto)
        {
            if (spaceId <= 0)
                throw ErrorService.Exception(
                    ErrorType.Argument,
                    nameof(CreateReward),
                    ErrorMessages.Validation.PositiveIntRequired,
                    paramName: nameof(spaceId),
                    parameters: new { spaceId });

            if (dto is null)
                throw ErrorService.Exception(
                    ErrorType.ArgumentNull,
                    nameof(CreateReward),
                    ErrorMessages.Validation.MissingParameter,
                    paramName: nameof(dto),
                    parameters: new { spaceId });

            var id = await _svc.CreateAsync(spaceId, dto);

            return CreatedAtAction(nameof(GetRewardById), new { spaceId, rewardId = id }, new { id });
        }

        #endregion

        #region READ (GET / LIST)

        /// <summary>
        /// Get a reward definition by id within a space.
        /// </summary>
        [HttpGet("{rewardId:int}")]
        [ProducesResponseType(typeof(RewardDetailView), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<RewardDetailView>> GetRewardById(int spaceId, int rewardId)
        {
            if (spaceId <= 0)
                throw ErrorService.Exception(
                    ErrorType.Argument,
                    nameof(GetRewardById),
                    ErrorMessages.Validation.PositiveIntRequired,
                    paramName: nameof(spaceId),
                    parameters: new { spaceId, rewardId });

            if (rewardId <= 0)
                throw ErrorService.Exception(
                    ErrorType.Argument,
                    nameof(GetRewardById),
                    ErrorMessages.Validation.PositiveIntRequired,
                    paramName: nameof(rewardId),
                    parameters: new { spaceId, rewardId });

            var row = await _svc.GetAsync(rewardId, spaceId);
            if (row is null)
                throw ErrorService.Exception(
                    ErrorType.NotFound,
                    nameof(GetRewardById),
                    ErrorMessages.Http.NotFound,
                    parameters: new { spaceId, rewardId });

            return Ok(row);
        }

        /// <summary>
        /// List all reward definitions for a space.
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(IReadOnlyList<RewardSummaryView>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IReadOnlyList<RewardSummaryView>>> ListRewardsBySpace(int spaceId)
        {
            if (spaceId <= 0)
                throw ErrorService.Exception(
                    ErrorType.Argument,
                    nameof(ListRewardsBySpace),
                    ErrorMessages.Validation.PositiveIntRequired,
                    paramName: nameof(spaceId),
                    parameters: new { spaceId });

            var rows = await _svc.ListBySpaceAsync(spaceId);
            return Ok(rows);
        }

        #endregion

        #region ATTACH (ITEMS / CURRENCIES)

        /// <summary>
        /// Attach an item component to a reward (e.g., item + quantity).
        /// </summary>
        [HttpPost("{rewardId:int}/items")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<object>> AddItemToReward(int spaceId, int rewardId, [FromBody] RewardItemCreateDto dto)
        {
            if (spaceId <= 0)
                throw ErrorService.Exception(
                    ErrorType.Argument,
                    nameof(AddItemToReward),
                    ErrorMessages.Validation.PositiveIntRequired,
                    paramName: nameof(spaceId),
                    parameters: new { spaceId, rewardId });

            if (rewardId <= 0)
                throw ErrorService.Exception(
                    ErrorType.Argument,
                    nameof(AddItemToReward),
                    ErrorMessages.Validation.PositiveIntRequired,
                    paramName: nameof(rewardId),
                    parameters: new { spaceId, rewardId });

            if (dto is null)
                throw ErrorService.Exception(
                    ErrorType.ArgumentNull,
                    nameof(AddItemToReward),
                    ErrorMessages.Validation.MissingParameter,
                    paramName: nameof(dto),
                    parameters: new { spaceId, rewardId });

            var reward = await _svc.GetAsync(rewardId, spaceId);
            if (reward is null)
                throw ErrorService.Exception(
                    ErrorType.NotFound,
                    nameof(AddItemToReward),
                    ErrorMessages.Http.NotFound,
                    parameters: new { spaceId, rewardId });

            var id = await _svc.AddItemAsync(rewardId, dto);
            return Ok(new { id });
        }

        /// <summary>
        /// Attach a currency component to a reward (e.g., currency + amount).
        /// </summary>
        [HttpPost("{rewardId:int}/currencies")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<object>> AddCurrencyToReward(int spaceId, int rewardId, [FromBody] RewardCurrencyCreateDto dto)
        {
            if (spaceId <= 0)
                throw ErrorService.Exception(
                    ErrorType.Argument,
                    nameof(AddCurrencyToReward),
                    ErrorMessages.Validation.PositiveIntRequired,
                    paramName: nameof(spaceId),
                    parameters: new { spaceId, rewardId });

            if (rewardId <= 0)
                throw ErrorService.Exception(
                    ErrorType.Argument,
                    nameof(AddCurrencyToReward),
                    ErrorMessages.Validation.PositiveIntRequired,
                    paramName: nameof(rewardId),
                    parameters: new { spaceId, rewardId });

            if (dto is null)
                throw ErrorService.Exception(
                    ErrorType.ArgumentNull,
                    nameof(AddCurrencyToReward),
                    ErrorMessages.Validation.MissingParameter,
                    paramName: nameof(dto),
                    parameters: new { spaceId, rewardId });

            var reward = await _svc.GetAsync(rewardId, spaceId);
            if (reward is null)
                throw ErrorService.Exception(
                    ErrorType.NotFound,
                    nameof(AddCurrencyToReward),
                    ErrorMessages.Http.NotFound,
                    parameters: new { spaceId, rewardId });

            var id = await _svc.AddCurrencyAsync(rewardId, dto);
            return Ok(new { id });
        }

        /// <summary>
        /// Update mutable reward metadata (limits, validity window, etc.).
        /// </summary>
        [HttpPatch("{rewardId:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdateRewardMetadata(int spaceId, int rewardId, [FromBody] RewardUpdateDto dto)
        {
            if (spaceId <= 0)
                throw ErrorService.Exception(
                    ErrorType.Argument,
                    nameof(UpdateRewardMetadata),
                    ErrorMessages.Validation.PositiveIntRequired,
                    paramName: nameof(spaceId),
                    parameters: new { spaceId, rewardId });

            if (rewardId <= 0)
                throw ErrorService.Exception(
                    ErrorType.Argument,
                    nameof(UpdateRewardMetadata),
                    ErrorMessages.Validation.PositiveIntRequired,
                    paramName: nameof(rewardId),
                    parameters: new { spaceId, rewardId });

            if (dto is null)
                throw ErrorService.Exception(
                    ErrorType.ArgumentNull,
                    nameof(UpdateRewardMetadata),
                    ErrorMessages.Validation.MissingParameter,
                    paramName: nameof(dto),
                    parameters: new { spaceId, rewardId });

            await _svc.UpdateAsync(rewardId, spaceId, dto);
            return NoContent();
        }

        /// <summary>
        /// Activate a reward definition.
        /// </summary>
        [HttpPost("{rewardId:int}:activate")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> ActivateReward(int spaceId, int rewardId)
        {
            if (spaceId <= 0)
                throw ErrorService.Exception(
                    ErrorType.Argument,
                    nameof(ActivateReward),
                    ErrorMessages.Validation.PositiveIntRequired,
                    paramName: nameof(spaceId),
                    parameters: new { spaceId, rewardId });

            if (rewardId <= 0)
                throw ErrorService.Exception(
                    ErrorType.Argument,
                    nameof(ActivateReward),
                    ErrorMessages.Validation.PositiveIntRequired,
                    paramName: nameof(rewardId),
                    parameters: new { spaceId, rewardId });

            await _svc.ToggleActiveAsync(rewardId, spaceId, true);
            return NoContent();
        }

        /// <summary>
        /// Deactivate a reward definition.
        /// </summary>
        [HttpPost("{rewardId:int}:deactivate")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> DeactivateReward(int spaceId, int rewardId)
        {
            if (spaceId <= 0)
                throw ErrorService.Exception(
                    ErrorType.Argument,
                    nameof(DeactivateReward),
                    ErrorMessages.Validation.PositiveIntRequired,
                    paramName: nameof(spaceId),
                    parameters: new { spaceId, rewardId });

            if (rewardId <= 0)
                throw ErrorService.Exception(
                    ErrorType.Argument,
                    nameof(DeactivateReward),
                    ErrorMessages.Validation.PositiveIntRequired,
                    paramName: nameof(rewardId),
                    parameters: new { spaceId, rewardId });

            await _svc.ToggleActiveAsync(rewardId, spaceId, false);
            return NoContent();
        }
    }

    #endregion

    // =========================================================================
    // USER REWARDS
    // =========================================================================

    [ApiController]
    [Route("api/user-rewards")]
    [Produces("application/json")]
    public sealed class UserRewardsController : ControllerBase
    {
        private readonly IRewardService _svc;
        public UserRewardsController(IRewardService svc) => _svc = svc;

        #region GET

        /// <summary>
        /// Get a user-reward instance by id.
        /// </summary>
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(UserRewardView), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<UserRewardView>> GetUserRewardById(int id)
        {
            if (id <= 0)
                throw ErrorService.Exception(
                    ErrorType.Argument,
                    nameof(GetUserRewardById),
                    ErrorMessages.Validation.PositiveIntRequired,
                    paramName: nameof(id),
                    parameters: new { id });

            var row = await _svc.GetUserRewardAsync(id);
            if (row is null)
                throw ErrorService.Exception(
                    ErrorType.NotFound,
                    nameof(GetUserRewardById),
                    ErrorMessages.Http.NotFound,
                    parameters: new { id });

            return Ok(row);
        }

        #endregion

        #region POST

        /// <summary>
        /// Grant a reward to a user in <b>Pending</b> state (idempotent).
        /// </summary>
        [HttpPost("grant")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<object>> GrantPendingUserReward([FromBody] GrantRewardRequest req)
        {
            if (req is null)
                throw ErrorService.Exception(
                    ErrorType.ArgumentNull,
                    nameof(GrantPendingUserReward),
                    ErrorMessages.Validation.MissingParameter,
                    paramName: nameof(req),
                    parameters: new { });

            if (string.IsNullOrWhiteSpace(req.IdempotencyKey))
                throw ErrorService.Exception(
                    ErrorType.Argument,
                    nameof(GrantPendingUserReward),
                    ErrorMessages.Validation.MissingParameter,
                    paramName: nameof(req.IdempotencyKey),
                    parameters: new { req.UserId, req.RewardId, req.SpaceId });

            var userRewardId = await _svc.GrantPendingAsync(req);
            return Ok(new { id = userRewardId });
        }

        /// <summary>
        /// Mark a user-reward as <b>Delivered</b>.
        /// </summary>
        [HttpPost("{id:int}/deliver")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> MarkUserRewardAsDelivered(int id)
        {
            if (id <= 0)
                throw ErrorService.Exception(
                    ErrorType.Argument,
                    nameof(MarkUserRewardAsDelivered),
                    ErrorMessages.Validation.PositiveIntRequired,
                    paramName: nameof(id),
                    parameters: new { id });

            var (exists, updated) = await _svc.TrySetDeliveredAsync(id);

            if (!exists)
                throw ErrorService.Exception(
                    ErrorType.NotFound,
                    nameof(MarkUserRewardAsDelivered),
                    ErrorMessages.Http.NotFound,
                    parameters: new { id });

            if (!updated)
                // exists but status didn’t allow transition (e.g., already Claimed)
                throw ErrorService.Exception(
                    ErrorType.Conflict,
                    nameof(MarkUserRewardAsDelivered),
                    ErrorMessages.Http.Conflict,
                    parameters: new { id },
                    extra: "User reward cannot transition to Delivered from its current status."
                );

            var row = await _svc.GetUserRewardAsync(id);
            return Ok(row); // 200 with current snapshot
        }

        /// <summary>
        /// Mark a user-reward as <b>Claimed</b>.
        /// </summary>
        [HttpPost("{id:int}/claim")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> MarkUserRewardAsClaimed(int id)
        {
            if (id <= 0)
                throw ErrorService.Exception(
                    ErrorType.Argument,
                    nameof(MarkUserRewardAsClaimed),
                    ErrorMessages.Validation.PositiveIntRequired,
                    paramName: nameof(id),
                    parameters: new { id });

            var (exists, updated) = await _svc.TrySetClaimedAsync(id);

            if (!exists)
                throw ErrorService.Exception(
                    ErrorType.NotFound,
                    nameof(MarkUserRewardAsClaimed),
                    ErrorMessages.Http.NotFound,
                    parameters: new { id });

            if (!updated)
                throw ErrorService.Exception(
                    ErrorType.Conflict,
                    nameof(MarkUserRewardAsClaimed),
                    ErrorMessages.Http.Conflict,
                    parameters: new { id },
                    extra: "User reward cannot transition to Claimed from its current status."
                );

            var row = await _svc.GetUserRewardAsync(id);
            return Ok(row);
        }

        #endregion
    }
}
