// <copyright file="IRulesAuthoringService.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Saad Sohail</author>
// <date>09/30/2025</date>
// <summary>Authoring service for workflows, rules, and related objects.</summary>

using GMS.TifoXRCoreWebAPI.Models;

namespace GMS.TifoXRCoreWebAPI.Services
{
    public interface IRulesAuthoringService
    {
        Task<RuleDetailView> GetRuleDetailAsync(int spaceId, int ruleId);
        Task<RuleExpressionUpdateResult> UpdateRuleDefinitionAsync(int spaceId, int ruleId, RuleDefinitionUpdateDto dto);
        Task<int> CreateWorkflowAsync(int spaceId, WorkflowCreateDto dto);
        Task<int> CreateRuleAsync(int spaceId, int workflowId, RuleCreateDto dto);
        Task<IReadOnlyList<int>> AddConditionGroupsAsync(int ruleId, IEnumerable<ConditionGroupCreateDto> groups);
        Task<ConditionCreateResult> AddConditionsAsync(int groupId, IEnumerable<ConditionCreateDto> conditions);
        Task<RuleExpressionUpdateResult> UpdateConditionGroupAsync(int ruleId, int groupId, ConditionGroupUpdateDto dto);
        Task<RuleExpressionUpdateResult> UpdateConditionAsync(int groupId, int conditionId, ConditionUpdateDto dto);
        Task<int> CreateRuleActionAsync(int ruleId, RuleActionCreateDto dto);
        Task BindRewardAsync(int actionId, int rewardId);
    }
}
