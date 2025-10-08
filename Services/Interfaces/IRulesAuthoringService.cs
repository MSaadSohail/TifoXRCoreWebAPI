// <copyright file="IRulesAuthoringService.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Saad Sohail</author>
// <date>09/30/2025</date>
// <summary>Authoring service for workflows, rules, and related objects.</summary>

using System.Collections.Generic;
using GMS.TifoXRCoreWebAPI.Models;

namespace GMS.TifoXRCoreWebAPI.Services
{
    public interface IRulesAuthoringService
    {
        Task<RuleDetailView> GetRuleDetailAsync(int spaceId, int ruleId);
        Task<RuleExpressionUpdateResult> UpdateRuleDefinitionAsync(RuleDefinitionUpdateDto dto);
        Task<int> CreateWorkflowAsync(WorkflowCreateDto dto);
        Task<int> CreateRuleAsync(RuleCreateDto dto);
        Task<IReadOnlyList<int>> AddConditionGroupsAsync(int ruleId, IEnumerable<ConditionGroupCreateDto> groups);
        Task<IReadOnlyList<int>> AddConditionsAsync(int groupId, IEnumerable<ConditionCreateDto> conditions);
        Task<int> CreateRuleActionAsync(RuleActionCreateDto dto);
        Task BindRewardAsync(int actionId, int rewardId);
    }
}
