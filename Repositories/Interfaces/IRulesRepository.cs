// <copyright file="IRulesRepository.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Saad Sohail</author>
// <date>09/30/2025</date>
// <summary>Data access for rules engine authoring.</summary>

using System.Collections.Generic;
using GMS.TifoXRCoreWebAPI.Models;

namespace GMS.TifoXRCoreWebAPI.Repositories
{
    public interface IRulesRepository
    {
        Task<(bool exists, int spaceId)> TryGetWorkflowSpaceAsync(int workflowId);
        Task<(bool exists, int spaceId, int workflowId)> TryGetRuleContextAsync(int ruleId);
        Task<(bool exists, int ruleId, int spaceId)> TryGetActionContextAsync(int actionId);
        Task<(bool exists, int ruleId, int spaceId)> TryGetConditionGroupContextAsync(int groupId);
        Task<(bool exists, int groupId, int ruleId, int spaceId)> TryGetConditionContextAsync(int conditionId);
        Task<bool> ConditionGroupBelongsToRuleAsync(int groupId, int ruleId);
        Task<bool> ActionBelongsToRuleAsync(int actionId, int ruleId);
        Task<int?> GetStateTypeIdByNameAsync(string type);

        Task<RuleDetailRecord?> GetRuleDetailAsync(int ruleId);
        Task<IReadOnlyList<ConditionGroupDetailView>> GetRuleConditionGroupsAsync(int ruleId);
        Task<IReadOnlyList<ConditionDetailView>> GetRuleConditionsAsync(int ruleId);

        Task UpdateRuleDefinitionAsync(RuleDefinitionUpdateDto dto, string expression);

        Task<int> CreateWorkflowAsync(WorkflowCreateDto dto, int stateTypeId);
        Task<int> CreateRuleAsync(RuleCreateDto dto, int stateTypeId);
        Task<IReadOnlyList<int>> InsertConditionGroupsAsync(IEnumerable<ConditionGroupCreateDto> dtos);
        Task<IReadOnlyList<int>> InsertConditionsAsync(IEnumerable<ConditionCreateDto> dtos);
        Task UpdateConditionGroupAsync(ConditionGroupUpdateDto dto);
        Task UpdateConditionAsync(ConditionUpdateDto dto);
        Task UpdateRuleExpressionAsync(int ruleId, string expression);
        Task<int> CreateRuleActionAsync(RuleActionCreateDto dto);
        Task BindRewardToActionAsync(int actionId, int rewardId);

        Task<IReadOnlyList<RuntimeWorkflowDefinition>> GetRuntimeWorkflowsAsync(int spaceId);
    }
}
