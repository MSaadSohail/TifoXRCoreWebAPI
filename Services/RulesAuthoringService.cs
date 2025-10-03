// <copyright file="RulesAuthoringService.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Saad Sohail</author>
// <date>09/30/2025</date>
// <summary>Coordinates repository operations for rules engine authoring.</summary>

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using GMS.TifoXRCoreWebAPI.Models;
using GMS.TifoXRCoreWebAPI.Repositories;

namespace GMS.TifoXRCoreWebAPI.Services
{
    public sealed class RulesAuthoringService : IRulesAuthoringService
    {
        private readonly IRulesRepository _repo;
        private readonly IRewardRepository _rewardRepo;
        private readonly IRulesEvaluationService _evaluationService;
        private readonly IRulesMetadataRepository _metadataRepo;
        private const string DefaultStateTypeName = "Published";

        private static readonly IReadOnlyDictionary<string, string> ComparatorFallbackFormats =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [ComparatorCodes.Eq] = "{0} == {1}",
                [ComparatorCodes.Neq] = "{0} != {1}",
                [ComparatorCodes.Gt] = "{0} > {1}",
                [ComparatorCodes.Gte] = "{0} >= {1}",
                [ComparatorCodes.Lt] = "{0} < {1}",
                [ComparatorCodes.Lte] = "{0} <= {1}",
                [ComparatorCodes.Contains] = "{0}.Contains({1})",
                [ComparatorCodes.StartsWith] = "{0}.StartsWith({1})",
                [ComparatorCodes.EndsWith] = "{0}.EndsWith({1})",
                [ComparatorCodes.In] = "{1}.Contains({0})",
                [ComparatorCodes.NotIn] = "!{1}.Contains({0})",
                [ComparatorCodes.Exists] = "{0} != null"
            };

        public RulesAuthoringService(
            IRulesRepository repo,
            IRewardRepository rewardRepo,
            IRulesEvaluationService evaluationService,
            IRulesMetadataRepository metadataRepo)
        {
            _repo = repo ?? throw new ArgumentNullException(nameof(repo));
            _rewardRepo = rewardRepo ?? throw new ArgumentNullException(nameof(rewardRepo));
            _evaluationService = evaluationService ?? throw new ArgumentNullException(nameof(evaluationService));
            _metadataRepo = metadataRepo ?? throw new ArgumentNullException(nameof(metadataRepo));
        }

        public async Task<RuleDetailView> GetRuleDetailAsync(int spaceId, int ruleId)
        {
            var rule = await _repo.GetRuleDetailAsync(ruleId);
            if (rule is null)
                throw new InvalidOperationException($"Rule {ruleId} does not exist.");

            if (rule.SpaceId != spaceId)
                throw new InvalidOperationException(
                    $"Rule {ruleId} belongs to space {rule.SpaceId} and cannot be accessed from space {spaceId}.");

            var groups = await _repo.GetRuleConditionGroupsAsync(ruleId);
            var conditions = await _repo.GetRuleConditionsAsync(ruleId);

            IReadOnlyList<EventTypeParameterView> eventTypeParameters = Array.Empty<EventTypeParameterView>();
            if (rule.EventTypeId.HasValue)
            {
                eventTypeParameters = await _metadataRepo.GetEventTypeParametersAsync(rule.EventTypeId.Value);
            }

            return new RuleDetailView
            {
                Rule = rule,
                ConditionGroups = groups,
                Conditions = conditions,
                EventTypeParameters = eventTypeParameters
            };
        }

        public async Task<RuleExpressionUpdateResult> UpdateRuleDefinitionAsync(
            int spaceId,
            int ruleId,
            RuleDefinitionUpdateDto dto)
        {
            if (dto is null) throw new ArgumentNullException(nameof(dto));

            var (exists, currentSpaceId, _) = await _repo.TryGetRuleContextAsync(ruleId);
            if (!exists)
                throw new InvalidOperationException($"Rule {ruleId} does not exist.");

            if (currentSpaceId != spaceId)
                throw new InvalidOperationException(
                    $"Rule {ruleId} belongs to space {currentSpaceId} and cannot be updated from space {spaceId}.");

            var groups = await _repo.GetRuleConditionGroupsAsync(ruleId);
            var conditions = await _repo.GetRuleConditionsAsync(ruleId);
            var expression = ComposeExpression(groups, conditions);

            await _repo.UpdateRuleDefinitionAsync(ruleId, dto, expression);
            _evaluationService.Invalidate(currentSpaceId);

            return new RuleExpressionUpdateResult
            {
                RuleId = ruleId,
                Expression = expression
            };
        }

        public async Task<int> CreateWorkflowAsync(int spaceId, WorkflowCreateDto dto)
        {
            if (dto is null) throw new ArgumentNullException(nameof(dto));
            var stateTypeId = await ResolveStateTypeIdAsync(dto.StateTypeId);
            var id = await _repo.CreateWorkflowAsync(spaceId, dto, stateTypeId);
            _evaluationService.Invalidate(spaceId);
            return id;
        }

        public async Task<int> CreateRuleAsync(int spaceId, int workflowId, RuleCreateDto dto)
        {
            if (dto is null) throw new ArgumentNullException(nameof(dto));

            var (workflowExists, workflowSpace) = await _repo.TryGetWorkflowSpaceAsync(workflowId);
            if (!workflowExists)
                throw new InvalidOperationException($"Workflow {workflowId} does not exist.");

            if (workflowSpace != spaceId)
                throw new InvalidOperationException("Rule space id must match the workflow's space id.");

            var stateTypeId = await ResolveStateTypeIdAsync(dto.StateTypeId);
            var id = await _repo.CreateRuleAsync(spaceId, workflowId, dto, stateTypeId);
            _evaluationService.Invalidate(spaceId);
            return id;
        }

        public async Task<IReadOnlyList<int>> AddConditionGroupsAsync(int ruleId, IEnumerable<ConditionGroupCreateDto> groups)
        {
            if (groups is null) throw new ArgumentNullException(nameof(groups));

            var (exists, spaceId, _) = await _repo.TryGetRuleContextAsync(ruleId);
            if (!exists)
                throw new InvalidOperationException($"Rule {ruleId} does not exist.");

            var ids = await _repo.InsertConditionGroupsAsync(ruleId, groups);
            _evaluationService.Invalidate(spaceId);
            return ids;
        }

        public async Task<ConditionCreateResult> AddConditionsAsync(int groupId, IEnumerable<ConditionCreateDto> conditions)
        {
            if (conditions is null) throw new ArgumentNullException(nameof(conditions));

            var (exists, ruleId, spaceId) = await _repo.TryGetConditionGroupContextAsync(groupId);
            if (!exists)
                throw new InvalidOperationException($"Condition group {groupId} does not exist.");

            var ids = await _repo.InsertConditionsAsync(groupId, conditions);
            var expression = await RefreshRuleExpressionAsync(ruleId);
            _evaluationService.Invalidate(spaceId);
            return new ConditionCreateResult
            {
                Ids = ids,
                Expression = expression
            };
        }

        public async Task<RuleExpressionUpdateResult> UpdateConditionGroupAsync(
            int ruleId,
            int groupId,
            ConditionGroupUpdateDto dto)
        {
            if (dto is null) throw new ArgumentNullException(nameof(dto));

            var (exists, currentRuleId, spaceId) = await _repo.TryGetConditionGroupContextAsync(groupId);
            if (!exists)
                throw new InvalidOperationException($"Condition group {groupId} does not exist.");

            if (currentRuleId != ruleId)
                throw new InvalidOperationException(
                    $"Condition group {groupId} belongs to rule {currentRuleId} and cannot be updated for rule {ruleId}.");

            if (dto.ParentGroupId.HasValue)
            {
                var belongs = await _repo.ConditionGroupBelongsToRuleAsync(dto.ParentGroupId.Value, currentRuleId);
                if (!belongs)
                    throw new InvalidOperationException(
                        $"Parent condition group {dto.ParentGroupId.Value} does not belong to rule {currentRuleId}.");
            }

            await _repo.UpdateConditionGroupAsync(groupId, dto);

            var expression = await RefreshRuleExpressionAsync(currentRuleId);
            _evaluationService.Invalidate(spaceId);

            return new RuleExpressionUpdateResult
            {
                RuleId = currentRuleId,
                Expression = expression
            };
        }

        public async Task<RuleExpressionUpdateResult> UpdateConditionAsync(
            int groupId,
            int conditionId,
            ConditionUpdateDto dto)
        {
            if (dto is null) throw new ArgumentNullException(nameof(dto));

            var (exists, currentGroupId, ruleId, spaceId) = await _repo.TryGetConditionContextAsync(conditionId);
            if (!exists)
                throw new InvalidOperationException($"Condition {conditionId} does not exist.");

            if (groupId != currentGroupId)
            {
                var belongs = await _repo.ConditionGroupBelongsToRuleAsync(groupId, ruleId);
                if (!belongs)
                    throw new InvalidOperationException(
                        $"Condition group {groupId} does not belong to rule {ruleId}.");
            }

            await _repo.UpdateConditionAsync(conditionId, groupId, dto);

            var expression = await RefreshRuleExpressionAsync(ruleId);
            _evaluationService.Invalidate(spaceId);

            return new RuleExpressionUpdateResult
            {
                RuleId = ruleId,
                Expression = expression
            };
        }

        public async Task<int> CreateRuleActionAsync(int ruleId, RuleActionCreateDto dto)
        {
            if (dto is null) throw new ArgumentNullException(nameof(dto));

            var (ruleExists, spaceId, _) = await _repo.TryGetRuleContextAsync(ruleId);
            if (!ruleExists)
                throw new InvalidOperationException($"Rule {ruleId} does not exist.");

            var id = await _repo.CreateRuleActionAsync(ruleId, dto);
            _evaluationService.Invalidate(spaceId);
            return id;
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

        private async Task<string> RefreshRuleExpressionAsync(int ruleId)
        {
            var groups = await _repo.GetRuleConditionGroupsAsync(ruleId);
            var conditions = await _repo.GetRuleConditionsAsync(ruleId);
            var expression = ComposeExpression(groups, conditions);
            await _repo.UpdateRuleExpressionAsync(ruleId, expression);
            return expression;
        }

        private static string ComposeExpression(
            IReadOnlyList<ConditionGroupDetailView> groups,
            IReadOnlyList<ConditionDetailView> conditions)
        {
            if (groups is null || groups.Count == 0)
            {
                var conditionExpressions = conditions
                    .OrderBy(c => c.OrderIndex)
                    .ThenBy(c => c.Id)
                    .Select(ComposeCondition)
                    .Where(static expr => !string.IsNullOrWhiteSpace(expr))
                    .ToList();

                return CombineUsingFormat("({0} && {1})", conditionExpressions);
            }

            var groupedChildren = groups
                .Where(g => g.ParentGroupId.HasValue)
                .GroupBy(g => g.ParentGroupId!.Value)
                .ToDictionary(
                    g => g.Key,
                    g => g.OrderBy(x => x.OrderIndex).ThenBy(x => x.Id).ToList());

            var conditionLookup = conditions
                .GroupBy(c => c.GroupId)
                .ToDictionary(
                    g => g.Key,
                    g => g.OrderBy(x => x.OrderIndex).ThenBy(x => x.Id).ToList());

            var rootGroups = groups
                .Where(g => g.ParentGroupId is null)
                .OrderBy(g => g.OrderIndex)
                .ThenBy(g => g.Id)
                .ToList();

            var rootExpressions = new List<string>();
            foreach (var root in rootGroups)
            {
                var expr = ComposeGroup(root, groupedChildren, conditionLookup);
                if (!string.IsNullOrWhiteSpace(expr))
                {
                    rootExpressions.Add(expr);
                }
            }

            return CombineUsingFormat("({0} && {1})", rootExpressions);
        }

        private static string ComposeGroup(
            ConditionGroupDetailView group,
            IReadOnlyDictionary<int, List<ConditionGroupDetailView>> groupedChildren,
            IReadOnlyDictionary<int, List<ConditionDetailView>> conditionLookup)
        {
            var parts = new List<string>();

            if (conditionLookup.TryGetValue(group.Id, out var groupConditions))
            {
                foreach (var condition in groupConditions)
                {
                    var expr = ComposeCondition(condition);
                    if (!string.IsNullOrWhiteSpace(expr))
                    {
                        parts.Add(expr);
                    }
                }
            }

            if (groupedChildren.TryGetValue(group.Id, out var childGroups))
            {
                foreach (var child in childGroups)
                {
                    var expr = ComposeGroup(child, groupedChildren, conditionLookup);
                    if (!string.IsNullOrWhiteSpace(expr))
                    {
                        parts.Add(expr);
                    }
                }
            }

            if (parts.Count == 0)
            {
                return string.Empty;
            }

            var format = string.IsNullOrWhiteSpace(group.LogicalOperatorFormat)
                ? "({0} && {1})"
                : group.LogicalOperatorFormat;

            return CombineUsingFormat(format, parts);
        }

        private static string ComposeCondition(ConditionDetailView condition)
        {
            var left = EscapeFormatArgument(condition.ParameterKey);

            if (TryFormatInListExpression(condition, left, out var listExpression))
            {
                return condition.Negate ? $"!({listExpression})" : listExpression;
            }

            var right = EscapeFormatArgument(FormatRightValue(condition));
            var format = string.IsNullOrWhiteSpace(condition.ComparatorFormat)
                ? GetComparatorFallbackFormat(condition.ComparatorCode)
                : condition.ComparatorFormat;

            string expression;
            try
            {
                expression = string.Format(CultureInfo.InvariantCulture, format, left, right);
            }
            catch (FormatException)
            {
                var fallback = GetComparatorFallbackFormat(condition.ComparatorCode);
                expression = string.Format(CultureInfo.InvariantCulture, fallback, left, right);
            }

            return condition.Negate ? $"!({expression})" : expression;
        }

        private static bool TryFormatInListExpression(ConditionDetailView condition, string left, out string expression)
        {
            expression = string.Empty;

            if (!string.Equals(condition.ComparatorCode, ComparatorCodes.In, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (!string.Equals(condition.RightValueKind, RightValueKinds.List, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(condition.RightValueJson))
            {
                return false;
            }

            try
            {
                using var doc = JsonDocument.Parse(condition.RightValueJson);
                if (doc.RootElement.ValueKind != JsonValueKind.Array)
                {
                    return false;
                }

                var comparisons = new List<string>();
                foreach (var element in doc.RootElement.EnumerateArray())
                {
                    var formattedValue = FormatLiteralElement(element);
                    comparisons.Add($"{left} == {formattedValue}");
                }

                if (comparisons.Count == 0)
                {
                    expression = "false";
                }
                else
                {
                    expression = CombineUsingFormat("({0} || {1})", comparisons);
                }

                return true;
            }
            catch (JsonException)
            {
                return false;
            }
        }

        private static string GetComparatorFallbackFormat(string? comparatorCode)
        {
            if (!string.IsNullOrWhiteSpace(comparatorCode) &&
                ComparatorFallbackFormats.TryGetValue(comparatorCode, out var format))
            {
                return format;
            }

            return "{0} == {1}";
        }

        private static string EscapeFormatArgument(string? value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value ?? string.Empty;
            }

            return value.Replace("{", "{{").Replace("}", "}}");
        }

        private static string CombineUsingFormat(string format, IReadOnlyList<string> expressions)
        {
            if (expressions.Count == 0)
            {
                return string.Empty;
            }

            if (expressions.Count == 1)
            {
                return expressions[0];
            }

            var result = expressions[0];
            for (var i = 1; i < expressions.Count; i++)
            {
                result = string.Format(CultureInfo.InvariantCulture, format, result, expressions[i]);
            }

            return result;
        }

        private static string FormatRightValue(ConditionDetailView condition)
        {
            if (string.IsNullOrWhiteSpace(condition.RightValueJson))
            {
                return "null";
            }

            var json = condition.RightValueJson.Trim();

            if (string.Equals(condition.RightValueKind, RightValueKinds.Parameter, StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    using var doc = JsonDocument.Parse(json);
                    var root = doc.RootElement;

                    if (root.ValueKind == JsonValueKind.String)
                    {
                        return root.GetString() ?? "null";
                    }

                    if (root.ValueKind == JsonValueKind.Object)
                    {
                        if (root.TryGetProperty("parameterKey", out var key))
                        {
                            return key.GetString() ?? "null";
                        }

                        if (root.TryGetProperty("key", out var alt))
                        {
                            return alt.GetString() ?? "null";
                        }
                    }
                }
                catch (JsonException)
                {
                    return json;
                }

                return "null";
            }

            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("value", out var valueProperty))
                {
                    return FormatLiteralElement(valueProperty);
                }

                return FormatLiteralElement(root);
            }
            catch (JsonException)
            {
                return json;
            }
        }

        private static string FormatLiteralElement(JsonElement element)
        {
            return element.ValueKind switch
            {
                JsonValueKind.Null => "null",
                JsonValueKind.String => element.GetRawText(),
                JsonValueKind.Number => element.GetRawText(),
                JsonValueKind.True => element.GetRawText(),
                JsonValueKind.False => element.GetRawText(),
                _ => element.GetRawText(),
            };
        }
    }
}
