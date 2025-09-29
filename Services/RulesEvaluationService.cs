// <copyright file="RulesEvaluationService.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved</copyright>
// <author>Saad Sohail</author>
// <date>9/30/2025</date>
// <summary></summary>

using System.Dynamic;
using System.Text.Json;
//
using RulesEngine.Models;
using RulesEngineCore = RulesEngine.RulesEngine;
//
using GMS.TifoXRCoreWebAPI.Models;

namespace GMS.TifoXRCoreWebAPI.Services
{
    public sealed class RulesEvaluationService : IRulesEvaluationService
    {
        private readonly ILogger<RulesEvaluationService> _logger;
        private readonly RulesEngineCore _rulesEngine;
        private readonly Dictionary<string, string> _workflowLookup;

        public RulesEvaluationService(IConfiguration configuration, ILogger<RulesEvaluationService> logger)
        {
            _logger = logger;

            var configured = configuration.GetSection("RulesEngine:Workflows").Get<Workflow[]>();
            Workflow[] workflows;

            if (configured is { Length: > 0 })
            {
                workflows = configured;
            }
            else
            {
                workflows = BuildDefaultWorkflows();
                _logger.LogWarning("RulesEngine configuration not found. Using built-in demonstration workflow definitions.");
            }

            _rulesEngine = new RulesEngineCore(workflows);
            _workflowLookup = workflows
                .Where(w => !string.IsNullOrWhiteSpace(w.WorkflowName))
                .ToDictionary(w => w.WorkflowName!, w => w.WorkflowName!, StringComparer.OrdinalIgnoreCase);
        }

        public async Task<RulesEngineEvaluationResponse> EvaluateAsync(RulesEngineEvaluationRequest request, CancellationToken cancellationToken = default)
        {
            if (request is null) throw new ArgumentNullException(nameof(request));

            if (!_workflowLookup.TryGetValue(request.EventType, out var workflowName))
            {
                _logger.LogInformation("No workflow configured for event type {EventType}.", request.EventType);
                return new RulesEngineEvaluationResponse
                {
                    AnyRuleMatched = false,
                    Messages = new[] { $"No workflow configured for event type '{request.EventType}'." }
                };
            }

            var parameters = new[]
            {
                new RuleParameter("input1", BuildPropertyBag(request.Properties)),
                new RuleParameter("event", request)
            };

            List<RuleResultTree> results;

            try
            {
                results = await _rulesEngine.ExecuteAllRulesAsync(workflowName, parameters);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to execute rules workflow {WorkflowName}.", workflowName);
                throw;
            }

            var outcomes = results.Select(ToOutcome).ToList();
            var rewardDecisions = outcomes
                .Where(o => !string.IsNullOrWhiteSpace(o.RewardCode))
                .Select(o => new RewardDecision
                {
                    RuleName = o.RuleName,
                    Granted = o.IsSuccess,
                    RewardCode = o.RewardCode,
                    Notes = o.IsSuccess ? "Rule matched." : o.ErrorMessage
                })
                .ToList();

            return new RulesEngineEvaluationResponse
            {
                AnyRuleMatched = outcomes.Any(o => o.IsSuccess),
                Outcomes = outcomes,
                RewardDecisions = rewardDecisions
            };
        }

        private static RuleEvaluationOutcome ToOutcome(RuleResultTree result)
        {
            var successEvent = result.Rule.SuccessEvent;
            var rewardCode = TryParseRewardCode(successEvent);

            return new RuleEvaluationOutcome
            {
                RuleName = result.Rule.RuleName,
                IsSuccess = result.IsSuccess,
                SuccessEvent = successEvent,
                ErrorMessage = result.Rule.ErrorMessage,
                RewardCode = rewardCode
            };
        }

        private static string? TryParseRewardCode(string? successEvent)
        {
            if (string.IsNullOrWhiteSpace(successEvent))
            {
                return null;
            }

            const string Prefix = "reward:";

            return successEvent.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase)
                ? successEvent[Prefix.Length..]
                : null;
        }

        private static Workflow[] BuildDefaultWorkflows()
        {
            return new[]
            {
                new Workflow
                {
                    WorkflowName = "game.session.ended",
                    Rules = new List<Rule>
                    {
                        new Rule
                        {
                            RuleName = "Grant reward for high score in hard difficulty",
                            Enabled = true,
                            SuccessEvent = "reward:penalties-hard-champion",
                            ErrorMessage = "Score or difficulty requirements not met.",
                            RuleExpressionType = RuleExpressionType.LambdaExpression,
                            Expression = "event.SpaceId == 42 && input1.score >= 7 && input1.difficulty != null && input1.difficulty.ToString().ToLower() == \"hard\""
                        }
                    }
                }
            };
        }

        private static ExpandoObject BuildPropertyBag(Dictionary<string, JsonElement>? properties)
        {
            var expando = new ExpandoObject();
            var dict = (IDictionary<string, object?>)expando;

            if (properties is null)
            {
                return expando;
            }

            foreach (var (key, value) in properties)
            {
                dict[key] = ConvertJsonElement(value);
            }

            return expando;
        }

        private static object? ConvertJsonElement(JsonElement element)
        {
            return element.ValueKind switch
            {
                JsonValueKind.Object => BuildNestedObject(element),
                JsonValueKind.Array => BuildArray(element),
                JsonValueKind.String => element.GetString(),
                JsonValueKind.Number => element.TryGetInt64(out var l) ? l : element.GetDouble(),
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.Null => null,
                _ => element.ToString()
            };
        }

        private static object BuildArray(JsonElement element)
        {
            var list = new List<object?>();
            foreach (var item in element.EnumerateArray())
            {
                list.Add(ConvertJsonElement(item));
            }

            return list;
        }

        private static ExpandoObject BuildNestedObject(JsonElement element)
        {
            var expando = new ExpandoObject();
            var dict = (IDictionary<string, object?>)expando;

            foreach (var property in element.EnumerateObject())
            {
                dict[property.Name] = ConvertJsonElement(property.Value);
            }

            return expando;
        }
    }
}
