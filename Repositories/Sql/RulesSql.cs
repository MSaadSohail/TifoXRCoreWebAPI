// <copyright file="RulesSql.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Saad Sohail</author>
// <date>09/30/2025</date>
// <summary>SQL statements for rules engine authoring.</summary>

namespace GMS.TifoXRCoreWebAPI.Repositories.Sql
{
    public static class RulesSql
    {
        public const string GetWorkflowSpace = @"
            SELECT TOP 1 space_id FROM workflow WHERE id = @Id;";

        public const string GetRuleContext = @"
            SELECT TOP 1 id, space_id AS SpaceId, workflow_id AS WorkflowId
            FROM rules
            WHERE id = @Id;";

        public const string ConditionGroupBelongsToRule = @"
            SELECT TOP 1 1 FROM rule_condition_group WHERE id = @GroupId AND rule_id = @RuleId;";

        public const string ActionBelongsToRule = @"
            SELECT TOP 1 1 FROM rule_actions WHERE id = @ActionId AND rule_id = @RuleId;";

        public const string GetActionContext = @"
            SELECT TOP 1 ra.rule_id AS RuleId, r.space_id AS SpaceId
            FROM rule_actions ra
            JOIN rules r ON r.id = ra.rule_id
            WHERE ra.id = @Id;";

        public const string GetConditionGroupContext = @"
            SELECT TOP 1 rcg.rule_id AS RuleId, r.space_id AS SpaceId
            FROM rule_condition_group rcg
            JOIN rules r ON r.id = rcg.rule_id
            WHERE rcg.id = @Id;";

        public const string GetConditionContext = @"
            SELECT TOP 1 rc.group_id AS GroupId,
                   rcg.rule_id AS RuleId,
                   r.space_id AS SpaceId
            FROM rule_condition rc
            JOIN rule_condition_group rcg ON rcg.id = rc.group_id
            JOIN rules r ON r.id = rcg.rule_id
            WHERE rc.id = @Id;";

        public const string GetStateTypeIdByName = @"
            SELECT TOP 1 id FROM state_type WHERE type = @Type;";

        public const string GetRuleDetail = @"
            SELECT r.id AS RuleId,
                   r.workflow_id AS WorkflowId,
                   r.space_id AS SpaceId,
                   r.rule_name AS RuleName,
                   r.expression AS Expression,
                   r.target_type AS TargetType,
                   r.success_event AS SuccessEvent,
                   r.priority AS Priority,
                   r.rule_cooldown_seconds AS RuleCooldownSeconds,
                   r.state_type_id AS StateTypeId,
                   r.version AS Version,
                   r.parent_rule_id AS ParentRuleId,
                   w.name AS WorkflowName,
                   et.id AS EventTypeId
            FROM rules r
            JOIN workflow w ON w.id = r.workflow_id
            LEFT JOIN re_event_type et ON et.name = r.target_type
            WHERE r.id = @RuleId;";

        public const string GetRuleConditionGroups = @"
            SELECT g.id AS Id,
                   g.rule_id AS RuleId,
                   g.parent_group_id AS ParentGroupId,
                   g.re_logical_operator_id AS LogicalOperatorId,
                   g.order_index AS OrderIndex,
                   lo.code AS LogicalOperatorCode,
                   lo.engine_format AS LogicalOperatorFormat
            FROM rule_condition_group g
            JOIN re_logical_operator lo ON lo.id = g.re_logical_operator_id
            WHERE g.rule_id = @RuleId
            ORDER BY CASE WHEN g.parent_group_id IS NULL THEN 0 ELSE 1 END,
                     g.parent_group_id,
                     g.order_index,
                     g.id;";

        public const string GetRuleConditions = @"
            SELECT rc.id AS Id,
                   rc.group_id AS GroupId,
                   rc.parameter_id AS ParameterId,
                   rc.comparator_id AS ComparatorId,
                   CAST(rc.negate AS int) AS Negate,
                   rc.right_value_kind AS RightValueKind,
                   rc.right_value_json AS RightValueJson,
                   rc.order_index AS OrderIndex,
                   cmp.code AS ComparatorCode,
                   cmp.engine_format AS ComparatorFormat,
                   cp.[key] AS ParameterKey,
                   cp.source AS ParameterSource,
                   cp.path AS ParameterPath
            FROM rule_condition rc
            JOIN rule_condition_group rcg ON rcg.id = rc.group_id
            JOIN re_comparator cmp ON cmp.id = rc.comparator_id
            JOIN re_context_parameter cp ON cp.id = rc.parameter_id
            WHERE rcg.rule_id = @RuleId
            ORDER BY rc.group_id,
                     rc.order_index,
                     rc.id;";

        public const string UpdateRuleDefinition = @"
            UPDATE rules
            SET rule_name = @RuleName,
                target_type = @TargetType,
                success_event = @SuccessEvent,
                priority = @Priority,
                rule_cooldown_seconds = @RuleCooldownSeconds,
                expression = @Expression,
                modified_time = SYSUTCDATETIME(),
                modified_by = 'system'
            WHERE id = @RuleId;";

        public const string InsertWorkflow = @"
            INSERT INTO workflow (space_id, name, state_type_id, creation_time, modified_by)
            VALUES (@SpaceId, @Name, @StateTypeId, SYSUTCDATETIME(), 'system');
            SELECT CAST(SCOPE_IDENTITY() AS int);";

        public const string InsertRule = @"
            INSERT INTO rules (workflow_id, space_id, rule_name, expression, target_type,
                               version, parent_rule_id, success_event,
                               priority, rule_cooldown_seconds, state_type_id,
                               creation_time, modified_by)
            VALUES (@WorkflowId, @SpaceId, @RuleName, @Expression, @TargetType,
                    @Version, @ParentRuleId, @SuccessEvent,
                    @Priority, @Cooldown, @StateTypeId,
                    SYSUTCDATETIME(), 'system');
            SELECT CAST(SCOPE_IDENTITY() AS int);";

        public const string InsertConditionGroup = @"
            INSERT INTO rule_condition_group (rule_id, parent_group_id, re_logical_operator_id, order_index, creation_time, modified_by)
            VALUES (@RuleId, @ParentGroupId, @LogicalOperatorId, @OrderIndex, SYSUTCDATETIME(), 'system');
            SELECT CAST(SCOPE_IDENTITY() AS int);";

        public const string InsertCondition = @"
            INSERT INTO rule_condition (group_id, parameter_id, comparator_id, negate, right_value_kind, right_value_json, order_index, creation_time, modified_by)
            VALUES (@GroupId, @ParameterId, @ComparatorId, @Negate, @RightValueKind, @RightValueJson, @OrderIndex, SYSUTCDATETIME(), 'system');
            SELECT CAST(SCOPE_IDENTITY() AS int);";

        public const string UpdateConditionGroup = @"
            UPDATE rule_condition_group
            SET parent_group_id = @ParentGroupId,
                re_logical_operator_id = @LogicalOperatorId,
                order_index = @OrderIndex,
                modified_time = SYSUTCDATETIME(),
                modified_by = 'system'
            WHERE id = @Id;";

        public const string UpdateCondition = @"
            UPDATE rule_condition
            SET group_id = @GroupId,
                parameter_id = @ParameterId,
                comparator_id = @ComparatorId,
                negate = @Negate,
                right_value_kind = @RightValueKind,
                right_value_json = @RightValueJson,
                order_index = @OrderIndex,
                modified_time = SYSUTCDATETIME(),
                modified_by = 'system'
            WHERE id = @Id;";

        public const string UpdateRuleExpression = @"
            UPDATE rules
            SET expression = @Expression,
                modified_time = SYSUTCDATETIME(),
                modified_by = 'system'
            WHERE id = @RuleId;";

        public const string InsertRuleAction = @"
            INSERT INTO rule_actions (rule_id, action_type_id, action_name, action_key, action_parameters_json, action_target_ref, order_index, is_active, creation_time, modified_by)
            VALUES (@RuleId, @ActionTypeId, @ActionName, @ActionKey, @ActionParams, @ActionTargetRef, @OrderIndex, @IsActive, SYSUTCDATETIME(), 'system');
            SELECT CAST(SCOPE_IDENTITY() AS int);";

        public const string UpsertRuleActionReward = @"
            MERGE rule_action_reward AS target
            USING (VALUES (@ActionId, @RewardId)) AS src (id, reward_id)
            ON target.id = src.id
            WHEN MATCHED THEN
                UPDATE SET reward_id = src.reward_id,
                           modified_by = 'system'
            WHEN NOT MATCHED THEN
                INSERT (id, reward_id, creation_time, modified_by)
                VALUES (src.id, src.reward_id, SYSUTCDATETIME(), 'system');";

        public const string GetRuntimeWorkflows = @"
            SELECT w.id AS WorkflowId,
                   w.name AS WorkflowName,
                   r.id AS RuleId,
                   r.rule_name AS RuleName,
                   r.expression AS Expression,
                   r.success_event AS SuccessEvent,
                   r.target_type AS EventType,
                   et.id AS EventTypeId
            FROM workflow w
            JOIN rules r ON r.workflow_id = w.id
            LEFT JOIN re_event_type et ON et.name = r.target_type
            WHERE w.space_id = @SpaceId
            ORDER BY w.id, r.priority, r.id;";

        public const string GetRuntimeEventParameters = @"
            SELECT etp.re_event_type_id AS EventTypeId,
                   cp.[key] AS ParameterKey,
                   cp.source AS Source,
                   cp.path AS Path,
                   CAST(etp.is_required AS int) AS IsRequired,
                   etp.default_value_json AS DefaultValueJson
            FROM re_event_type_parameter etp
            JOIN re_context_parameter cp ON cp.id = etp.parameter_id
            WHERE etp.re_event_type_id = @EventTypeId
            ORDER BY etp.id;";
    }
}
