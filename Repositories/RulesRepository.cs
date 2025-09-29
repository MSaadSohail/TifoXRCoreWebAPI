// <copyright file="RulesRepository.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Saad Sohail</author>
// <date>09/30/2025</date>
// <summary>Rules engine authoring data access.</summary>

using System;
using System.Collections.Generic;
using System.Linq;
using GMS.TifoXRCoreWebAPI.Models;
using GMS.TifoXRCoreWebAPI.Repositories.Sql;
using GMS.TifoXRCoreWebAPI.Utilities.Infrastructure;

namespace GMS.TifoXRCoreWebAPI.Repositories
{
    public sealed class RulesRepository : IRulesRepository
    {
        private readonly IDbProvider _db;
        public RulesRepository(IDbProvider db) => _db = db;

        public async Task<(bool exists, int spaceId)> TryGetWorkflowSpaceAsync(int workflowId)
        {
            await using var conn = await _db.OpenConnectionAsync();
            await using var cmd = _db.CreateCommand(conn, RulesSql.GetWorkflowSpace);
            cmd.Parameters.Add(_db.CreateParameter("@Id", workflowId));

            var obj = await cmd.ExecuteScalarAsync();
            return obj is null ? (false, 0) : (true, Convert.ToInt32(obj));
        }

        public async Task<(bool exists, int spaceId, int workflowId)> TryGetRuleContextAsync(int ruleId)
        {
            await using var conn = await _db.OpenConnectionAsync();
            await using var cmd = _db.CreateCommand(conn, RulesSql.GetRuleContext);
            cmd.Parameters.Add(_db.CreateParameter("@Id", ruleId));

            await using var rdr = await cmd.ExecuteReaderAsync();
            if (!await rdr.ReadAsync())
                return (false, 0, 0);

            return (true, rdr.GetInt32(rdr.GetOrdinal("SpaceId")), rdr.GetInt32(rdr.GetOrdinal("WorkflowId")));
        }

        public async Task<(bool exists, int ruleId, int spaceId)> TryGetActionContextAsync(int actionId)
        {
            await using var conn = await _db.OpenConnectionAsync();
            await using var cmd = _db.CreateCommand(conn, RulesSql.GetActionContext);
            cmd.Parameters.Add(_db.CreateParameter("@Id", actionId));

            await using var rdr = await cmd.ExecuteReaderAsync();
            if (!await rdr.ReadAsync())
                return (false, 0, 0);

            return (true, rdr.GetInt32(rdr.GetOrdinal("RuleId")), rdr.GetInt32(rdr.GetOrdinal("SpaceId")));
        }

        public async Task<(bool exists, int ruleId, int spaceId)> TryGetConditionGroupContextAsync(int groupId)
        {
            await using var conn = await _db.OpenConnectionAsync();
            await using var cmd = _db.CreateCommand(conn, RulesSql.GetConditionGroupContext);
            cmd.Parameters.Add(_db.CreateParameter("@Id", groupId));

            await using var rdr = await cmd.ExecuteReaderAsync();
            if (!await rdr.ReadAsync())
                return (false, 0, 0);

            return (true, rdr.GetInt32(rdr.GetOrdinal("RuleId")), rdr.GetInt32(rdr.GetOrdinal("SpaceId")));
        }

        public async Task<bool> ConditionGroupBelongsToRuleAsync(int groupId, int ruleId)
        {
            await using var conn = await _db.OpenConnectionAsync();
            await using var cmd = _db.CreateCommand(conn, RulesSql.ConditionGroupBelongsToRule);
            cmd.Parameters.Add(_db.CreateParameter("@GroupId", groupId));
            cmd.Parameters.Add(_db.CreateParameter("@RuleId", ruleId));

            var obj = await cmd.ExecuteScalarAsync();
            return obj is not null;
        }

        public async Task<bool> ActionBelongsToRuleAsync(int actionId, int ruleId)
        {
            await using var conn = await _db.OpenConnectionAsync();
            await using var cmd = _db.CreateCommand(conn, RulesSql.ActionBelongsToRule);
            cmd.Parameters.Add(_db.CreateParameter("@ActionId", actionId));
            cmd.Parameters.Add(_db.CreateParameter("@RuleId", ruleId));

            var obj = await cmd.ExecuteScalarAsync();
            return obj is not null;
        }

        public async Task<int?> GetStateTypeIdByNameAsync(string type)
        {
            await using var conn = await _db.OpenConnectionAsync();
            await using var cmd = _db.CreateCommand(conn, RulesSql.GetStateTypeIdByName);
            cmd.Parameters.Add(_db.CreateParameter("@Type", type));

            var obj = await cmd.ExecuteScalarAsync();
            return obj is null ? null : Convert.ToInt32(obj);
        }

        public async Task<int> CreateWorkflowAsync(WorkflowCreateDto dto, int stateTypeId)
        {
            await using var conn = await _db.OpenConnectionAsync();
            await using var cmd = _db.CreateCommand(conn, RulesSql.InsertWorkflow);

            cmd.Parameters.Add(_db.CreateParameter("@SpaceId", dto.SpaceId));
            cmd.Parameters.Add(_db.CreateParameter("@Name", dto.Name));
            cmd.Parameters.Add(_db.CreateParameter("@StateTypeId", stateTypeId));

            var obj = await cmd.ExecuteScalarAsync();
            return Convert.ToInt32(obj);
        }

        public async Task<int> CreateRuleAsync(RuleCreateDto dto, int stateTypeId)
        {
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
            return Convert.ToInt32(obj);
        }

        public async Task<IReadOnlyList<int>> InsertConditionGroupsAsync(IEnumerable<ConditionGroupCreateDto> dtos)
        {
            await using var conn = await _db.OpenConnectionAsync();
            await using var tx = await conn.BeginTransactionAsync();

            var ids = new List<int>();
            try
            {
                foreach (var dto in dtos)
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

            return ids;
        }

        public async Task<IReadOnlyList<int>> InsertConditionsAsync(IEnumerable<ConditionCreateDto> dtos)
        {
            await using var conn = await _db.OpenConnectionAsync();
            await using var tx = await conn.BeginTransactionAsync();

            var ids = new List<int>();
            try
            {
                foreach (var dto in dtos)
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

            return ids;
        }

        public async Task<int> CreateRuleActionAsync(RuleActionCreateDto dto)
        {
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
            return Convert.ToInt32(obj);
        }

        public async Task BindRewardToActionAsync(int actionId, int rewardId)
        {
            await using var conn = await _db.OpenConnectionAsync();
            await using var cmd = _db.CreateCommand(conn, RulesSql.UpsertRuleActionReward);

            cmd.Parameters.Add(_db.CreateParameter("@ActionId", actionId));
            cmd.Parameters.Add(_db.CreateParameter("@RewardId", rewardId));

            await cmd.ExecuteNonQueryAsync();
        }

        public async Task<IReadOnlyList<RuntimeWorkflowDefinition>> GetRuntimeWorkflowsAsync(int spaceId)
        {
            await using var conn = await _db.OpenConnectionAsync();

            var builders = new Dictionary<int, RuntimeWorkflowBuilder>();
            var eventTypeIds = new HashSet<int>();

            await using (var cmd = _db.CreateCommand(conn, RulesSql.GetRuntimeWorkflows))
            {
                cmd.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));

                await using var rdr = await cmd.ExecuteReaderAsync();
                if (!rdr.HasRows)
                {
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
                }
            }

            if (builders.Count == 0)
            {
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
            }

            foreach (var builder in builders.Values)
            {
                if (builder.EventTypeId.HasValue &&
                    parameterCache.TryGetValue(builder.EventTypeId.Value, out var parameters))
                {
                    builder.SetParameters(parameters);
                }
            }

            return builders.Values
                .Select(b => b.ToDefinition())
                .ToList();
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
