// <copyright file="RulesRepository.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Saad Sohail</author>
// <date>09/30/2025</date>
// <summary>Rules engine authoring data access.</summary>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using GMS.TifoXRCoreWebAPI.Models;
using GMS.TifoXRCoreWebAPI.Repositories.Sql;
using GMS.TifoXRCoreWebAPI.Utilities.Infrastructure;
using Microsoft.Extensions.Logging;

namespace GMS.TifoXRCoreWebAPI.Repositories
{
    public sealed class RulesRepository : IRulesRepository
    {
        private readonly IDbProvider _db;
        private readonly ILogger<RulesRepository> _logger;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = false,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        public RulesRepository(IDbProvider db, ILogger<RulesRepository> logger)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        private static string Serialize(object? value)
            => JsonSerializer.Serialize(value, JsonOptions);

        public async Task<(bool exists, int spaceId)> TryGetWorkflowSpaceAsync(int workflowId)
        {
            _logger.LogDebug("Fetching workflow space for workflow {WorkflowId}.", workflowId);
            await using var conn = await _db.OpenConnectionAsync();
            await using var cmd = _db.CreateCommand(conn, RulesSql.GetWorkflowSpace);
            cmd.Parameters.Add(_db.CreateParameter("@Id", workflowId));

            var obj = await cmd.ExecuteScalarAsync();
            var result = obj is null ? (false, 0) : (true, Convert.ToInt32(obj));
            _logger.LogDebug(
                "Workflow space lookup for workflow {WorkflowId} returned Exists={Exists}, SpaceId={SpaceId}.",
                workflowId,
                result.exists,
                result.spaceId);
            return result;
        }

        public async Task<(bool exists, int spaceId, int workflowId)> TryGetRuleContextAsync(int ruleId)
        {
            _logger.LogDebug("Fetching rule context for rule {RuleId}.", ruleId);
            await using var conn = await _db.OpenConnectionAsync();
            await using var cmd = _db.CreateCommand(conn, RulesSql.GetRuleContext);
            cmd.Parameters.Add(_db.CreateParameter("@Id", ruleId));

            await using var rdr = await cmd.ExecuteReaderAsync();
            if (!await rdr.ReadAsync())
            {
                _logger.LogDebug("Rule context not found for rule {RuleId}.", ruleId);
                return (false, 0, 0);
            }

            var spaceId = rdr.GetInt32(rdr.GetOrdinal("SpaceId"));
            var workflowId = rdr.GetInt32(rdr.GetOrdinal("WorkflowId"));
            _logger.LogDebug(
                "Rule context for rule {RuleId}: SpaceId={SpaceId}, WorkflowId={WorkflowId}.",
                ruleId,
                spaceId,
                workflowId);
            return (true, spaceId, workflowId);
        }

        public async Task<(bool exists, int ruleId, int spaceId)> TryGetActionContextAsync(int actionId)
        {
            _logger.LogDebug("Fetching action context for action {ActionId}.", actionId);
            await using var conn = await _db.OpenConnectionAsync();
            await using var cmd = _db.CreateCommand(conn, RulesSql.GetActionContext);
            cmd.Parameters.Add(_db.CreateParameter("@Id", actionId));

            await using var rdr = await cmd.ExecuteReaderAsync();
            if (!await rdr.ReadAsync())
            {
                _logger.LogDebug("Action context not found for action {ActionId}.", actionId);
                return (false, 0, 0);
            }

            var ruleId = rdr.GetInt32(rdr.GetOrdinal("RuleId"));
            var spaceId = rdr.GetInt32(rdr.GetOrdinal("SpaceId"));
            _logger.LogDebug(
                "Action context for action {ActionId}: RuleId={RuleId}, SpaceId={SpaceId}.",
                actionId,
                ruleId,
                spaceId);
            return (true, ruleId, spaceId);
        }

        public async Task<(bool exists, int ruleId, int spaceId)> TryGetConditionGroupContextAsync(int groupId)
        {
            _logger.LogDebug("Fetching condition group context for group {GroupId}.", groupId);
            await using var conn = await _db.OpenConnectionAsync();
            await using var cmd = _db.CreateCommand(conn, RulesSql.GetConditionGroupContext);
            cmd.Parameters.Add(_db.CreateParameter("@Id", groupId));

            await using var rdr = await cmd.ExecuteReaderAsync();
            if (!await rdr.ReadAsync())
            {
                _logger.LogDebug("Condition group context not found for group {GroupId}.", groupId);
                return (false, 0, 0);
            }

            var ruleId = rdr.GetInt32(rdr.GetOrdinal("RuleId"));
            var spaceId = rdr.GetInt32(rdr.GetOrdinal("SpaceId"));
            _logger.LogDebug(
                "Condition group context for group {GroupId}: RuleId={RuleId}, SpaceId={SpaceId}.",
                groupId,
                ruleId,
                spaceId);
            return (true, ruleId, spaceId);
        }

        public async Task<bool> ConditionGroupBelongsToRuleAsync(int groupId, int ruleId)
        {
            _logger.LogDebug(
                "Checking if condition group {GroupId} belongs to rule {RuleId}.",
                groupId,
                ruleId);
            await using var conn = await _db.OpenConnectionAsync();
            await using var cmd = _db.CreateCommand(conn, RulesSql.ConditionGroupBelongsToRule);
            cmd.Parameters.Add(_db.CreateParameter("@GroupId", groupId));
            cmd.Parameters.Add(_db.CreateParameter("@RuleId", ruleId));

            var obj = await cmd.ExecuteScalarAsync();
            var belongs = obj is not null;
            _logger.LogDebug(
                "Condition group {GroupId} belongs to rule {RuleId}: {Result}.",
                groupId,
                ruleId,
                belongs);
            return belongs;
        }

        public async Task<bool> ActionBelongsToRuleAsync(int actionId, int ruleId)
        {
            _logger.LogDebug(
                "Checking if action {ActionId} belongs to rule {RuleId}.",
                actionId,
                ruleId);
            await using var conn = await _db.OpenConnectionAsync();
            await using var cmd = _db.CreateCommand(conn, RulesSql.ActionBelongsToRule);
            cmd.Parameters.Add(_db.CreateParameter("@ActionId", actionId));
            cmd.Parameters.Add(_db.CreateParameter("@RuleId", ruleId));

            var obj = await cmd.ExecuteScalarAsync();
            var belongs = obj is not null;
            _logger.LogDebug(
                "Action {ActionId} belongs to rule {RuleId}: {Result}.",
                actionId,
                ruleId,
                belongs);
            return belongs;
        }

        public async Task<int?> GetStateTypeIdByNameAsync(string type)
        {
            _logger.LogDebug("Resolving state type id for type {Type}.", type);
            await using var conn = await _db.OpenConnectionAsync();
            await using var cmd = _db.CreateCommand(conn, RulesSql.GetStateTypeIdByName);
            cmd.Parameters.Add(_db.CreateParameter("@Type", type));

            var obj = await cmd.ExecuteScalarAsync();
            var stateTypeId = obj is null ? null : Convert.ToInt32(obj);
            _logger.LogDebug("State type lookup for {Type} returned {StateTypeId}.", type, stateTypeId);
            return stateTypeId;
        }

        public async Task<int> CreateWorkflowAsync(WorkflowCreateDto dto, int stateTypeId)
        {
            if (dto is null) throw new ArgumentNullException(nameof(dto));

            _logger.LogInformation(
                "Creating workflow with payload {Payload} and state type {StateTypeId}.",
                Serialize(dto),
                stateTypeId);
            await using var conn = await _db.OpenConnectionAsync();
            await using var cmd = _db.CreateCommand(conn, RulesSql.InsertWorkflow);

            cmd.Parameters.Add(_db.CreateParameter("@SpaceId", dto.SpaceId));
            cmd.Parameters.Add(_db.CreateParameter("@Name", dto.Name));
            cmd.Parameters.Add(_db.CreateParameter("@StateTypeId", stateTypeId));

            var obj = await cmd.ExecuteScalarAsync();
            var workflowId = Convert.ToInt32(obj);
            _logger.LogInformation(
                "Workflow created with id {WorkflowId} for space {SpaceId}.",
                workflowId,
                dto.SpaceId);
            return workflowId;
        }

        public async Task<int> CreateRuleAsync(RuleCreateDto dto, int stateTypeId)
        {
            if (dto is null) throw new ArgumentNullException(nameof(dto));

            _logger.LogInformation(
                "Creating rule with payload {Payload} and state type {StateTypeId}.",
                Serialize(dto),
                stateTypeId);
            await using var conn = await _db.OpenConnectionAsync();
            await using var cmd = _db.CreateCommand(conn, RulesSql.InsertRule);

            cmd.Parameters.Add(_db.CreateParameter("@WorkflowId", dto.WorkflowId));
            cmd.Parameters.Add(_db.CreateParameter("@SpaceId", dto.SpaceId));
            cmd.Parameters.Add(_db.CreateParameter("@RuleName", dto.RuleName));
            cmd.Parameters.Add(_db.CreateParameter("@Expression", dto.Expression ?? string.Empty));
            cmd.Parameters.Add(_db.CreateParameter("@TargetType", (object?)dto.TargetType ?? DBNull.Value));
            cmd.Parameters.Add(_db.CreateParameter("@Version", (object?)dto.Version ?? DBNull.Value));
            cmd.Parameters.Add(_db.CreateParameter("@ParentRuleId", (object?)dto.ParentRuleId ?? DBNull.Value));
            cmd.Parameters.Add(_db.CreateParameter("@SuccessEvent", (object?)dto.SuccessEvent ?? DBNull.Value));
            cmd.Parameters.Add(_db.CreateParameter("@Priority", dto.Priority));
            cmd.Parameters.Add(_db.CreateParameter("@Cooldown", dto.RuleCooldownSeconds));
            cmd.Parameters.Add(_db.CreateParameter("@StateTypeId", stateTypeId));

            var obj = await cmd.ExecuteScalarAsync();
            var ruleId = Convert.ToInt32(obj);
            _logger.LogInformation(
                "Rule created with id {RuleId} for workflow {WorkflowId} in space {SpaceId}.",
                ruleId,
                dto.WorkflowId,
                dto.SpaceId);
            return ruleId;
        }

        public async Task<IReadOnlyList<int>> InsertConditionGroupsAsync(IEnumerable<ConditionGroupCreateDto> dtos)
        {
            if (dtos is null) throw new ArgumentNullException(nameof(dtos));

            var dtoList = dtos.ToList();
            _logger.LogInformation(
                "Inserting {Count} condition groups with payload {Payload}.",
                dtoList.Count,
                Serialize(dtoList));
            await using var conn = await _db.OpenConnectionAsync();
            await using var tx = await conn.BeginTransactionAsync();

            var ids = new List<int>();
            try
            {
                foreach (var dto in dtoList)
                {
                    await using var cmd = _db.CreateCommand(conn, RulesSql.InsertConditionGroup, tx);
                    cmd.Parameters.Add(_db.CreateParameter("@RuleId", dto.RuleId));
                    cmd.Parameters.Add(_db.CreateParameter("@ParentGroupId", (object?)dto.ParentGroupId ?? DBNull.Value));
                    cmd.Parameters.Add(_db.CreateParameter("@LogicalOperatorId", dto.LogicalOperatorId));
                    cmd.Parameters.Add(_db.CreateParameter("@OrderIndex", dto.OrderIndex));

                    var obj = await cmd.ExecuteScalarAsync();
                    ids.Add(Convert.ToInt32(obj));
                }

                await tx.CommitAsync();
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }

            _logger.LogInformation(
                "Inserted condition groups with ids {Ids} for payload {Payload}.",
                Serialize(ids),
                Serialize(dtoList));
            return ids;
        }

        public async Task<IReadOnlyList<int>> InsertConditionsAsync(IEnumerable<ConditionCreateDto> dtos)
        {
            if (dtos is null) throw new ArgumentNullException(nameof(dtos));

            var dtoList = dtos.ToList();
            _logger.LogInformation(
                "Inserting {Count} conditions with payload {Payload}.",
                dtoList.Count,
                Serialize(dtoList));
            await using var conn = await _db.OpenConnectionAsync();
            await using var tx = await conn.BeginTransactionAsync();

            var ids = new List<int>();
            try
            {
                foreach (var dto in dtoList)
                {
                    await using var cmd = _db.CreateCommand(conn, RulesSql.InsertCondition, tx);
                    cmd.Parameters.Add(_db.CreateParameter("@GroupId", dto.GroupId));
                    cmd.Parameters.Add(_db.CreateParameter("@ParameterId", dto.ParameterId));
                    cmd.Parameters.Add(_db.CreateParameter("@ComparatorId", dto.ComparatorId));
                    cmd.Parameters.Add(_db.CreateParameter("@Negate", dto.Negate ? 1 : 0));
                    cmd.Parameters.Add(_db.CreateParameter("@RightValueKind", dto.RightValueKind));
                    cmd.Parameters.Add(_db.CreateParameter("@RightValueJson", (object?)dto.RightValueJson ?? DBNull.Value));
                    cmd.Parameters.Add(_db.CreateParameter("@OrderIndex", dto.OrderIndex));

                    var obj = await cmd.ExecuteScalarAsync();
                    ids.Add(Convert.ToInt32(obj));
                }

                await tx.CommitAsync();
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }

            _logger.LogInformation(
                "Inserted conditions with ids {Ids} for payload {Payload}.",
                Serialize(ids),
                Serialize(dtoList));
            return ids;
        }

        public async Task<int> CreateRuleActionAsync(RuleActionCreateDto dto)
        {
            if (dto is null) throw new ArgumentNullException(nameof(dto));

            _logger.LogInformation(
                "Creating rule action with payload {Payload}.",
                Serialize(dto));
            await using var conn = await _db.OpenConnectionAsync();
            await using var cmd = _db.CreateCommand(conn, RulesSql.InsertRuleAction);

            cmd.Parameters.Add(_db.CreateParameter("@RuleId", dto.RuleId));
            cmd.Parameters.Add(_db.CreateParameter("@ActionTypeId", dto.ActionTypeId));
            cmd.Parameters.Add(_db.CreateParameter("@ActionName", dto.ActionName));
            cmd.Parameters.Add(_db.CreateParameter("@ActionKey", dto.ActionKey));
            cmd.Parameters.Add(_db.CreateParameter("@ActionParams", dto.ActionParametersJson ?? "{}"));
            cmd.Parameters.Add(_db.CreateParameter("@ActionTargetRef", (object?)dto.ActionTargetRef ?? DBNull.Value));
            cmd.Parameters.Add(_db.CreateParameter("@OrderIndex", dto.OrderIndex));
            cmd.Parameters.Add(_db.CreateParameter("@IsActive", dto.IsActive ? 1 : 0));

            var obj = await cmd.ExecuteScalarAsync();
            var actionId = Convert.ToInt32(obj);
            _logger.LogInformation(
                "Rule action created with id {ActionId} for rule {RuleId}.",
                actionId,
                dto.RuleId);
            return actionId;
        }

        public async Task BindRewardToActionAsync(int actionId, int rewardId)
        {
            _logger.LogInformation(
                "Binding reward {RewardId} to action {ActionId}.",
                rewardId,
                actionId);
            await using var conn = await _db.OpenConnectionAsync();
            await using var cmd = _db.CreateCommand(conn, RulesSql.UpsertRuleActionReward);

            cmd.Parameters.Add(_db.CreateParameter("@ActionId", actionId));
            cmd.Parameters.Add(_db.CreateParameter("@RewardId", rewardId));

            await cmd.ExecuteNonQueryAsync();
            _logger.LogInformation(
                "Reward {RewardId} bound to action {ActionId}.",
                rewardId,
                actionId);
        }

        public async Task<IReadOnlyList<RuntimeWorkflowDefinition>> GetRuntimeWorkflowsAsync(int spaceId)
        {
            _logger.LogInformation("Loading runtime workflows for space {SpaceId}.", spaceId);
            await using var conn = await _db.OpenConnectionAsync();

            var builders = new Dictionary<int, RuntimeWorkflowBuilder>();
            var eventTypeIds = new HashSet<int>();

            await using (var cmd = _db.CreateCommand(conn, RulesSql.GetRuntimeWorkflows))
            {
                cmd.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));

                await using var rdr = await cmd.ExecuteReaderAsync();
                if (!rdr.HasRows)
                {
                    _logger.LogInformation(
                        "No runtime workflows found in database for space {SpaceId}.",
                        spaceId);
                    return Array.Empty<RuntimeWorkflowDefinition>();
                }

                var workflowIdIdx = rdr.GetOrdinal("WorkflowId");
                var workflowNameIdx = rdr.GetOrdinal("WorkflowName");
                var ruleIdIdx = rdr.GetOrdinal("RuleId");
                var ruleNameIdx = rdr.GetOrdinal("RuleName");
                var expressionIdx = rdr.GetOrdinal("Expression");
                var successEventIdx = rdr.GetOrdinal("SuccessEvent");
                var eventTypeIdx = rdr.GetOrdinal("EventType");
                var eventTypeIdIdx = rdr.GetOrdinal("EventTypeId");

                while (await rdr.ReadAsync())
                {
                    var workflowId = rdr.GetInt32(workflowIdIdx);
                    var workflowName = rdr.GetString(workflowNameIdx);
                    var eventType = rdr.IsDBNull(eventTypeIdx)
                        ? workflowName
                        : rdr.GetString(eventTypeIdx);
                    var eventTypeId = rdr.IsDBNull(eventTypeIdIdx)
                        ? (int?)null
                        : rdr.GetInt32(eventTypeIdIdx);

                    if (!builders.TryGetValue(workflowId, out var builder))
                    {
                        builder = new RuntimeWorkflowBuilder(workflowId, workflowName, eventType, eventTypeId);
                        builders.Add(workflowId, builder);
                        _logger.LogDebug(
                            "Discovered workflow {WorkflowName} ({WorkflowId}) for event {EventType} (EventTypeId={EventTypeId}).",
                            workflowName,
                            workflowId,
                            eventType,
                            eventTypeId);
                    }

                    if (eventTypeId.HasValue)
                    {
                        eventTypeIds.Add(eventTypeId.Value);
                    }

                    if (rdr.IsDBNull(expressionIdx))
                    {
                        continue;
                    }

                    var expression = rdr.GetString(expressionIdx);
                    if (string.IsNullOrWhiteSpace(expression))
                    {
                        continue;
                    }

                    var rule = new RuntimeRuleDefinition
                    {
                        RuleId = rdr.GetInt32(ruleIdIdx),
                        RuleName = rdr.GetString(ruleNameIdx),
                        Expression = expression,
                        SuccessEvent = rdr.IsDBNull(successEventIdx) ? null : rdr.GetString(successEventIdx)
                    };

                    builder.AddRule(rule);
                    _logger.LogDebug(
                        "Added rule {RuleName} ({RuleId}) with expression {Expression} to workflow {WorkflowName}.",
                        rule.RuleName,
                        rule.RuleId,
                        rule.Expression,
                        workflowName);
                }
            }

            if (builders.Count == 0)
            {
                _logger.LogInformation(
                    "Runtime workflow query for space {SpaceId} returned no active workflows.",
                    spaceId);
                return Array.Empty<RuntimeWorkflowDefinition>();
            }

            var parameterCache = new Dictionary<int, IReadOnlyDictionary<string, RuntimeParameterDefinition>>();

            foreach (var eventTypeId in eventTypeIds)
            {
                await using var paramCmd = _db.CreateCommand(conn, RulesSql.GetRuntimeEventParameters);
                paramCmd.Parameters.Add(_db.CreateParameter("@EventTypeId", eventTypeId));

                await using var paramRdr = await paramCmd.ExecuteReaderAsync();
                if (!paramRdr.HasRows)
                {
                    _logger.LogDebug(
                        "No parameters configured for event type id {EventTypeId}.",
                        eventTypeId);
                    continue;
                }

                var keyIdx = paramRdr.GetOrdinal("ParameterKey");
                var sourceIdx = paramRdr.GetOrdinal("Source");
                var pathIdx = paramRdr.GetOrdinal("Path");
                var requiredIdx = paramRdr.GetOrdinal("IsRequired");
                var defaultIdx = paramRdr.GetOrdinal("DefaultValueJson");

                var parameters = new Dictionary<string, RuntimeParameterDefinition>(StringComparer.OrdinalIgnoreCase);

                while (await paramRdr.ReadAsync())
                {
                    var key = paramRdr.GetString(keyIdx);

                    parameters[key] = new RuntimeParameterDefinition
                    {
                        Key = key,
                        Source = paramRdr.GetString(sourceIdx),
                        Path = paramRdr.GetString(pathIdx),
                        IsRequired = paramRdr.GetInt32(requiredIdx) != 0,
                        DefaultValueJson = paramRdr.IsDBNull(defaultIdx) ? null : paramRdr.GetString(defaultIdx)
                    };
                }

                parameterCache[eventTypeId] = parameters;
                _logger.LogDebug(
                    "Loaded {Count} parameters for event type id {EventTypeId}: {Parameters}.",
                    parameters.Count,
                    eventTypeId,
                    Serialize(parameters));
            }

            foreach (var builder in builders.Values)
            {
                if (builder.EventTypeId.HasValue &&
                    parameterCache.TryGetValue(builder.EventTypeId.Value, out var parameters))
                {
                    builder.SetParameters(parameters);
                    _logger.LogDebug(
                        "Assigned {Count} parameters to workflow {WorkflowName} ({WorkflowId}).",
                        parameters.Count,
                        builder.WorkflowName,
                        builder.WorkflowId);
                }
            }

            var definitions = builders.Values
                .Select(b => b.ToDefinition())
                .ToList();

            _logger.LogInformation(
                "Built {Count} runtime workflows for space {SpaceId}: {Definitions}.",
                definitions.Count,
                spaceId,
                Serialize(definitions));

            return definitions;
        }

        private sealed class RuntimeWorkflowBuilder
        {
            private readonly List<RuntimeRuleDefinition> _rules = new();
            private IReadOnlyDictionary<string, RuntimeParameterDefinition> _parameters =
                new Dictionary<string, RuntimeParameterDefinition>(StringComparer.OrdinalIgnoreCase);

            public RuntimeWorkflowBuilder(int workflowId, string workflowName, string eventType, int? eventTypeId)
            {
                WorkflowId = workflowId;
                WorkflowName = workflowName;
                EventType = eventType;
                EventTypeId = eventTypeId;
            }

            public int WorkflowId { get; }
            public string WorkflowName { get; }
            public string EventType { get; }
            public int? EventTypeId { get; }

            public void AddRule(RuntimeRuleDefinition rule) => _rules.Add(rule);

            public void SetParameters(IReadOnlyDictionary<string, RuntimeParameterDefinition> parameters)
                => _parameters = parameters;

            public RuntimeWorkflowDefinition ToDefinition()
                => new()
                {
                    WorkflowId = WorkflowId,
                    WorkflowName = WorkflowName,
                    EventType = EventType,
                    EventTypeId = EventTypeId,
                    Rules = _rules,
                    Parameters = _parameters
                };
        }
    }
}
