// <copyright file="RulesModels.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Urvashi Dhingra</author>
// <date>09/24/2025</date>
// <summary>DTOs for Rules Engine authoring, runtime ingestion, and auditing.</summary>

using System;
using System.Collections.Generic;
using System.Text.Json;

namespace GMS.TifoXRCoreWebAPI.Models
{
    // =========================
    // Lookups / Enums
    // =========================

    /// <summary>String codes for comparators</summary>
    public static class ComparatorCodes
    {
        public const string Eq = "eq";
        public const string Neq = "neq";
        public const string Gt = "gt";
        public const string Gte = "gte";
        public const string Lt = "lt";
        public const string Lte = "lte";
        public const string Contains = "contains";
        public const string StartsWith = "starts_with";
        public const string EndsWith = "ends_with";
        public const string In = "in";
        public const string NotIn = "not_in";
        public const string Between = "between";
        public const string Exists = "exists";
        public const string WithinLast = "within_last";
    }

    /// <summary>How right-hand values are interpreted in rule_condition.</summary>
    public static class RightValueKinds
    {
        public const string Literal = "literal";
        public const string Parameter = "parameter";
        public const string List = "list";
        public const string RelativeTime = "relative_time";
    }

    // =========================
    // Authoring: Workflows & Rules
    // =========================

    public sealed class WorkflowCreateDto
    {
        public int SpaceId { get; init; }
        public string Name { get; init; } = default!;
        public int? StateTypeId { get; init; }
    }

    public sealed class WorkflowView
    {
        public int Id { get; init; }
        public int SpaceId { get; init; }
        public string Name { get; init; } = default!;
        public int StateTypeId { get; init; }
    }

    public sealed class RuleCreateDto
    {
        public int WorkflowId { get; init; }
        public int SpaceId { get; init; }
        public string RuleName { get; init; } = default!;
        public string? Expression { get; init; }
        public string? TargetType { get; init; }
        public int? Version { get; init; }
        public int? ParentRuleId { get; init; }
        public string? SuccessEvent { get; init; }
        public int Priority { get; init; } = 100;
        public int RuleCooldownSeconds { get; init; } = 0;
        public int? StateTypeId { get; init; }
    }

    public sealed class RuleView
    {
        public int Id { get; init; }
        public int WorkflowId { get; init; }
        public int SpaceId { get; init; }
        public string RuleName { get; init; } = default!;
        public int? Version { get; init; }
        public int Priority { get; init; }
        public int RuleCooldownSeconds { get; init; }
        public int StateTypeId { get; init; }
    }

    // =========================
    // Authoring: Parameters & Event Types
    // =========================

    public sealed class ContextParameterCreateDto
    {
        public string Key { get; init; } = default!;
        public string Source { get; init; } = "event";
        public string Path { get; init; } = default!;
        public int RePathTypeId { get; init; }
        public string DataType { get; init; } = default!;
        public string UiLabel { get; init; } = default!;
        public string? UiHelpKey { get; init; }
        public string? Unit { get; init; }
        public int? ReDropdownValueProviderId { get; init; }
        public string? ExampleValue { get; init; }
    }

    public sealed class ContextParameterView
    {
        public int Id { get; init; }
        public string Key { get; init; } = default!;
        public string Source { get; init; } = default!;
        public string Path { get; init; } = default!;
        public int RePathTypeId { get; init; }
        public string DataType { get; init; } = default!;
        public string UiLabel { get; init; } = default!;
        public string? Unit { get; init; }
    }

    public sealed class EventTypeCreateDto
    {
        public string Name { get; init; } = default!;
    }

    public sealed class EventTypeView
    {
        public int Id { get; init; }
        public string Name { get; init; } = default!;
    }

    public sealed class EventTypeParameterUpsertDto
    {
        public int ReEventTypeId { get; init; }
        public int ParameterId { get; init; }
        public bool IsRequired { get; init; } = true;
        public string? DefaultValueJson { get; init; }
    }

    // =========================
    // Authoring: Condition Tree
    // =========================

    public sealed class ConditionGroupCreateDto
    {
        public int RuleId { get; init; }
        public int? ParentGroupId { get; init; }
        public int LogicalOperatorId { get; init; }
        public int OrderIndex { get; init; } = 1;
        public string? NameKey { get; init; }
        public string? DescriptionKey { get; init; }
    }

    public sealed class ConditionGroupView
    {
        public int Id { get; init; }
        public int RuleId { get; init; }
        public int? ParentGroupId { get; init; }
        public int LogicalOperatorId { get; init; }
        public int OrderIndex { get; init; }
    }

    public sealed class ConditionCreateDto
    {
        public int GroupId { get; init; }
        public int ParameterId { get; init; }
        public int ComparatorId { get; init; }
        public bool Negate { get; init; } = false;
        public string RightValueKind { get; init; } = RightValueKinds.Literal;
        public string? RightValueJson { get; init; }
        public int OrderIndex { get; init; } = 1;
    }

    public sealed class ConditionView
    {
        public int Id { get; init; }
        public int GroupId { get; init; }
        public int ParameterId { get; init; }
        public int ComparatorId { get; init; }
        public bool Negate { get; init; }
        public string RightValueKind { get; init; } = default!;
        public string? RightValueJson { get; init; }
        public int OrderIndex { get; init; }
    }

    // =========================
    // Authoring: Rule Parameters (named constants per rule)
    // =========================

    public sealed class RuleParameterDto
    {
        public int RuleId { get; init; }
        public string Name { get; init; } = default!;
        public string ValueJson { get; init; } = default!;
    }

    // =========================
    // Authoring: Actions
    // =========================

    public sealed class RuleActionCreateDto
    {
        public int RuleId { get; init; }
        public int ActionTypeId { get; init; }
        public string ActionName { get; init; } = default!;
        public string ActionKey { get; init; } = default!;
        public string? ActionParametersJson { get; init; }
        public string? ActionTargetRef { get; init; }
        public int OrderIndex { get; init; } = 1;
        public bool IsActive { get; init; } = true;
    }

    public sealed class RuleActionView
    {
        public int Id { get; init; }
        public int RuleId { get; init; }
        public int ActionTypeId { get; init; }
        public string ActionName { get; init; } = default!;
        public string ActionKey { get; init; } = default!;
        public bool IsActive { get; init; }
        public int OrderIndex { get; init; }
    }

    public sealed class RuleActionRewardCreateDto
    {
        /// <summary>FK to parent rule_actions.id</summary>
        public int Id { get; init; }
        public int RewardId { get; init; }
    }

    public sealed class RuleActionRewardBindDto
    {
        public int RewardId { get; init; }
    }

    public sealed class RuleActionModerationCreateDto
    {
        /// <summary>FK to parent rule_actions.id</summary>
        public int Id { get; init; }
        public int ModerationActionTypeId { get; init; }
        public int TargetTypeId { get; init; }
        public int TargetSelectorId { get; init; }
        public string? ContentRefTemplate { get; init; }
        public int? DurationSeconds { get; init; }
        public string? ReasonCode { get; init; }
        public string? MessageTemplateKey { get; init; }
        public bool? Silent { get; init; }
        public bool AutoEscalate { get; init; } = false;
        public int EscalateAfterSeconds { get; init; } = 0;
    }

    // =========================
    // Runtime: Events (ingestion)
    // =========================

    public sealed class IngestEventRequest
    {
        public int SpaceId { get; init; }
        public int ReEventTypeId { get; init; }
        public string? ActorUserId { get; init; }
        public string? TargetUserId { get; init; }
        public string? ContentOwnerUserId { get; init; }
        public string? ContentRef { get; init; }
        public DateTime OccurredAt { get; init; }
        public string? PropertiesJson { get; init; }
    }

    public sealed class EventView
    {
        public int Id { get; init; }
        public int SpaceId { get; init; }
        public int ReEventTypeId { get; init; }
        public string? ActorUserId { get; init; }
        public DateTime OccurredAt { get; init; }
        public string? PropertiesJson { get; init; }
        public DateTime IngestedAt { get; init; }
    }

    // =========================
    // Runtime: Rules evaluation
    // =========================

    public sealed class RulesEngineEvaluationRequest
    {
        public int SpaceId { get; set; }
        public string EventType { get; init; } = default!;
        public DateTime OccurredAt { get; init; }
        public string? ActorUserId { get; init; }
        public string? TargetUserId { get; init; }
        public string? ContentOwnerUserId { get; init; }
        public string? ContentRef { get; init; }
        public Dictionary<string, JsonElement>? Properties { get; init; }
    }

    public sealed class RuntimeWorkflowDefinition
    {
        public int WorkflowId { get; init; }
        public string WorkflowName { get; init; } = default!;
        public string EventType { get; init; } = default!;
        public int? EventTypeId { get; init; }
        public IReadOnlyList<RuntimeRuleDefinition> Rules { get; init; } = Array.Empty<RuntimeRuleDefinition>();
        public IReadOnlyDictionary<string, RuntimeParameterDefinition> Parameters { get; init; }
            = new Dictionary<string, RuntimeParameterDefinition>(StringComparer.OrdinalIgnoreCase);
    }

    public sealed class RuntimeRuleDefinition
    {
        public int RuleId { get; init; }
        public string RuleName { get; init; } = default!;
        public string Expression { get; init; } = default!;
        public string? SuccessEvent { get; init; }
    }

    public sealed class RuntimeParameterDefinition
    {
        public string Key { get; init; } = default!;
        public string Source { get; init; } = default!;
        public string Path { get; init; } = default!;
        public bool IsRequired { get; init; }
        public string? DefaultValueJson { get; init; }
    }

    public sealed class RulesEngineEvaluationResponse
    {
        public bool AnyRuleMatched { get; init; }
        public IReadOnlyList<RuleEvaluationOutcome> Outcomes { get; init; } = Array.Empty<RuleEvaluationOutcome>();
        public IReadOnlyList<RewardDecision> RewardDecisions { get; init; } = Array.Empty<RewardDecision>();
        public IReadOnlyList<string> Messages { get; init; } = Array.Empty<string>();
    }

    public sealed class RuleEvaluationOutcome
    {
        public string RuleName { get; init; } = default!;
        public bool IsSuccess { get; init; }
        public string? SuccessEvent { get; init; }
        public string? ErrorMessage { get; init; }
        public string? RewardCode { get; init; }
    }

    public sealed class RewardDecision
    {
        public string RuleName { get; init; } = default!;
        public bool Granted { get; init; }
        public string? RewardCode { get; init; }
        public string? Notes { get; init; }
    }

    // =========================
    // Audit: Evaluation & Dispatch
    // =========================

    public sealed class RuleEvaluationLogView
    {
        public int Id { get; init; }
        public int EventId { get; init; }
        public int WorkflowId { get; init; }
        public int RuleId { get; init; }
        public int RuleVersion { get; init; }
        public bool Matched { get; init; }
        public string? InputsHash { get; init; }
        public DateTime EvaluatedAt { get; init; }
        public int SpaceId { get; init; }
        public int? LatencyMs { get; init; }
    }

    public sealed class RuleActionDispatchView
    {
        public int Id { get; init; }
        public int RuleId { get; init; }
        public int RuleVersion { get; init; }
        public string ActionKey { get; init; } = default!;
        public string IdempotencyKey { get; init; } = default!;
        public int EventId { get; init; }
        public string? UserId { get; init; }
        public int? RewardId { get; init; }
        public int? UserRewardId { get; init; }
        public int? ModerationCaseId { get; init; }
        public string Status { get; init; } = default!;
        public string? ErrorCode { get; init; }
        public string? ErrorMessage { get; init; }
        public DateTime DispatchedAt { get; init; }
        public DateTime? CompletedAt { get; init; }
        public int SpaceId { get; init; }
    }

    // =========================
    // Moderation Case (if action type = moderation)
    // =========================

    public sealed class ModerationCaseView
    {
        public int Id { get; init; }
        public int SpaceId { get; init; }
        public string Source { get; init; } = default!;
        public int? RuleId { get; init; }
        public int? RuleVersion { get; init; }
        public int ModerationActionTypeId { get; init; }
        public string TargetKind { get; init; } = default!;
        public string? TargetUserId { get; init; }
        public string? ContentRef { get; init; }
        public string? ReasonCode { get; init; }
        public string? MessageTemplateKey { get; init; }
        public int? DurationSeconds { get; init; }
        public DateTime IssuedAt { get; init; }
        public DateTime? EffectiveFrom { get; init; }
        public DateTime? EffectiveTo { get; init; }
        public string Status { get; init; } = default!;
        public DateTime? RevokedAt { get; init; }
        public string? RevokedBy { get; init; }
        public string? RevokeReason { get; init; }
        public string? Notes { get; init; }
    }
}
