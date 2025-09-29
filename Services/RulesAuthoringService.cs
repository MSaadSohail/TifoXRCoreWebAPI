// <copyright file="RulesAuthoringService.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Saad Sohail</author>
// <date>09/30/2025</date>
// <summary>Coordinates repository operations for rules engine authoring.</summary>

using GMS.TifoXRCoreWebAPI.Models;
using GMS.TifoXRCoreWebAPI.Repositories;

namespace GMS.TifoXRCoreWebAPI.Services
{
    public sealed class RulesAuthoringService : IRulesAuthoringService
    {
        private readonly IRulesRepository _repo;
        private readonly IRewardRepository _rewardRepo;
        private const string DefaultStateTypeName = "Published";

        public RulesAuthoringService(IRulesRepository repo, IRewardRepository rewardRepo)
        {
            _repo = repo;
            _rewardRepo = rewardRepo;
        }

        public async Task<int> CreateWorkflowAsync(WorkflowCreateDto dto)
        {
            if (dto is null) throw new ArgumentNullException(nameof(dto));
            var stateTypeId = await ResolveStateTypeIdAsync(dto.StateTypeId);
            return await _repo.CreateWorkflowAsync(dto, stateTypeId);
        }

        public async Task<int> CreateRuleAsync(RuleCreateDto dto)
        {
            if (dto is null) throw new ArgumentNullException(nameof(dto));

            var (workflowExists, workflowSpace) = await _repo.TryGetWorkflowSpaceAsync(dto.WorkflowId);
            if (!workflowExists)
                throw new InvalidOperationException($"Workflow {dto.WorkflowId} does not exist.");

            if (workflowSpace != dto.SpaceId)
                throw new InvalidOperationException("Rule space id must match the workflow's space id.");

            var stateTypeId = await ResolveStateTypeIdAsync(dto.StateTypeId);
            return await _repo.CreateRuleAsync(dto, stateTypeId);
        }

        public async Task<IReadOnlyList<int>> AddConditionGroupsAsync(int ruleId, IEnumerable<ConditionGroupCreateDto> groups)
        {
            if (groups is null) throw new ArgumentNullException(nameof(groups));

            var (exists, _, _) = await _repo.TryGetRuleContextAsync(ruleId);
            if (!exists)
                throw new InvalidOperationException($"Rule {ruleId} does not exist.");

            foreach (var group in groups)
            {
                if (group.RuleId != ruleId)
                    throw new InvalidOperationException("Condition group payload rule id must match the route rule id.");
            }

            return await _repo.InsertConditionGroupsAsync(groups);
        }

        public async Task<IReadOnlyList<int>> AddConditionsAsync(int groupId, IEnumerable<ConditionCreateDto> conditions)
        {
            if (conditions is null) throw new ArgumentNullException(nameof(conditions));

            var (exists, _, _) = await _repo.TryGetConditionGroupContextAsync(groupId);
            if (!exists)
                throw new InvalidOperationException($"Condition group {groupId} does not exist.");

            foreach (var condition in conditions)
            {
                if (condition.GroupId != groupId)
                    throw new InvalidOperationException("Condition payload group id must match the route group id.");
            }

            return await _repo.InsertConditionsAsync(conditions);
        }

        public async Task<int> CreateRuleActionAsync(RuleActionCreateDto dto)
        {
            if (dto is null) throw new ArgumentNullException(nameof(dto));

            var (ruleExists, _, _) = await _repo.TryGetRuleContextAsync(dto.RuleId);
            if (!ruleExists)
                throw new InvalidOperationException($"Rule {dto.RuleId} does not exist.");

            return await _repo.CreateRuleActionAsync(dto);
        }

        public async Task BindRewardAsync(int actionId, int rewardId)
        {
            var (actionExists, _, spaceId) = await _repo.TryGetActionContextAsync(actionId);
            if (!actionExists)
                throw new InvalidOperationException($"Rule action {actionId} does not exist.");

            var reward = await _rewardRepo.GetAsync(rewardId, spaceId);
            if (reward is null)
                throw new InvalidOperationException($"Reward {rewardId} does not exist in space {spaceId}.");

            await _repo.BindRewardToActionAsync(actionId, rewardId);
        }

        private async Task<int> ResolveStateTypeIdAsync(int? stateTypeId)
        {
            if (stateTypeId.HasValue)
                return stateTypeId.Value;

            var fallback = await _repo.GetStateTypeIdByNameAsync(DefaultStateTypeName);
            if (!fallback.HasValue)
                throw new InvalidOperationException($"State type '{DefaultStateTypeName}' is not configured.");

            return fallback.Value;
        }
    }
}
