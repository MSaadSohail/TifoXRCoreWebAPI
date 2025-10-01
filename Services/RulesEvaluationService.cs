using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading;
using RulesEngine.Models;
using RulesEngineCore = RulesEngine.RulesEngine;
using GMS.TifoXRCoreWebAPI.Models;
using GMS.TifoXRCoreWebAPI.Repositories;

namespace GMS.TifoXRCoreWebAPI.Services;

public sealed class RulesEvaluationService : IRulesEvaluationService
{
    private readonly ILogger<RulesEvaluationService> _logger;
    private readonly IRulesRepository _rulesRepository;
    private readonly ConcurrentDictionary<int, WorkflowCache> _workflowCaches = new();

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public RulesEvaluationService(IRulesRepository rulesRepository, ILogger<RulesEvaluationService> logger)
    {
        _rulesRepository = rulesRepository ?? throw new ArgumentNullException(nameof(rulesRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    private static string Serialize(object? value)
        => JsonSerializer.Serialize(value, JsonOptions);

    public async Task<RulesEngineEvaluationResponse> EvaluateAsync(
        RulesEngineEvaluationRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request is null) throw new ArgumentNullException(nameof(request));

        _logger.LogInformation(
            "Starting rules evaluation for space {SpaceId} and event {EventType} with payload {Payload}.",
            request.SpaceId,
            request.EventType,
            Serialize(request));

        if (request.SpaceId <= 0)
        {
            throw new ArgumentException("SpaceId must be provided on the evaluation request.", nameof(request));
        }

        var cache = await GetWorkflowCacheAsync(request.SpaceId, cancellationToken);

        _logger.LogDebug(
            "Workflow cache for space {SpaceId} contains {WorkflowCount} workflows.",
            request.SpaceId,
            cache.WorkflowLookup.Count);

        if (cache.Engine is null || cache.WorkflowLookup.Count == 0)
        {
            _logger.LogInformation("No workflows configured in database for space {SpaceId}.", request.SpaceId);
            return new RulesEngineEvaluationResponse
            {
                AnyRuleMatched = false,
                Messages = new[] { $"No workflows configured for space '{request.SpaceId}'." }
            };
        }

        if (!cache.WorkflowLookup.TryGetValue(request.EventType, out var workflowName))
        {
            _logger.LogInformation(
                "No workflow configured for event type {EventType} in space {SpaceId}.",
                request.EventType,
                request.SpaceId);

            return new RulesEngineEvaluationResponse
            {
                AnyRuleMatched = false,
                Messages = new[] { $"No workflow configured for event type '{request.EventType}'." }
            };
        }

        cache.ParameterMap.TryGetValue(workflowName, out var parameterDefinitions);

        var inputBag = BuildPropertyBag(request, parameterDefinitions);
        _logger.LogDebug(
            "Built input bag for workflow {WorkflowName}: {Payload}.",
            workflowName,
            Serialize(inputBag));
        var parameters = new List<RuleParameter>
        {
            new("input1", inputBag),
            new("event", request)
        };

        if (parameterDefinitions is not null && parameterDefinitions.Count > 0)
        {
            var inputDictionary = (IDictionary<string, object?>)inputBag;

            foreach (var (alias, definition) in parameterDefinitions)
            {
                if (string.IsNullOrWhiteSpace(alias) ||
                    string.Equals(alias, "input1", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(alias, "event", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                object? value = null;

                if (!string.IsNullOrWhiteSpace(alias) && inputDictionary.TryGetValue(alias, out var aliasValue))
                {
                    value = aliasValue;
                }
                else if (!string.IsNullOrWhiteSpace(definition?.Key) &&
                         !string.Equals(definition.Key, alias, StringComparison.OrdinalIgnoreCase) &&
                         inputDictionary.TryGetValue(definition.Key, out var keyValue))
                {
                    value = keyValue;
                }

                parameters.Add(new RuleParameter(alias, value));
            }
        }

        List<RuleResultTree> results;

        var parameterArray = parameters.ToArray();

        _logger.LogInformation(
            "Executing workflow {WorkflowName} with parameters {Parameters}.",
            workflowName,
            Serialize(parameterArray.Select(p => new { p.Name, p.Value })));

        try
        {
            results = await cache.Engine.ExecuteAllRulesAsync(workflowName, parameterArray);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute rules workflow {WorkflowName}.", workflowName);
            throw;
        }

        _logger.LogInformation(
            "Workflow {WorkflowName} execution produced {ResultCount} results: {Results}.",
            workflowName,
            results.Count,
            Serialize(results.Select(r => new
            {
                r.Rule.RuleName,
                r.IsSuccess,
                r.ExceptionMessage,
                r.Rule.SuccessEvent
            })));

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

        _logger.LogInformation(
            "Workflow {WorkflowName} outcomes: {Outcomes}.",
            workflowName,
            Serialize(outcomes));

        return new RulesEngineEvaluationResponse
        {
            AnyRuleMatched = outcomes.Any(o => o.IsSuccess),
            Outcomes = outcomes,
            RewardDecisions = rewardDecisions
        };
    }

    public void Invalidate(int spaceId)
    {
        if (spaceId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(spaceId));
        }

        _workflowCaches.TryRemove(spaceId, out _);
    }

    private async Task<WorkflowCache> GetWorkflowCacheAsync(int spaceId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (_workflowCaches.TryGetValue(spaceId, out var cached))
        {
            _logger.LogDebug(
                "Workflow cache hit for space {SpaceId} with {WorkflowCount} workflows.",
                spaceId,
                cached.WorkflowLookup.Count);
            return cached;
        }

        IReadOnlyList<RuntimeWorkflowDefinition> definitions;

        try
        {
            _logger.LogDebug("Cache miss for space {SpaceId}; loading workflows from repository.", spaceId);
            definitions = await _rulesRepository.GetRuntimeWorkflowsAsync(spaceId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load workflow definitions for space {SpaceId}.", spaceId);
            throw;
        }

        _logger.LogDebug(
            "Loaded {DefinitionCount} workflow definitions from repository for space {SpaceId}.",
            definitions.Count,
            spaceId);

        if (definitions.Count == 0)
        {
            _workflowCaches[spaceId] = WorkflowCache.Empty;
            return WorkflowCache.Empty;
        }

        var validDefinitions = definitions
            .Where(d => d.Rules.Count > 0)
            .ToList();

        if (validDefinitions.Count == 0)
        {
            _workflowCaches[spaceId] = WorkflowCache.Empty;
            return WorkflowCache.Empty;
        }

        var workflows = validDefinitions
            .Select(d => new Workflow
            {
                WorkflowName = d.EventType,
                Rules = d.Rules.Select(r => new Rule
                {
                    RuleName = r.RuleName,
                    Expression = NormalizeExpression(r.Expression, d.Parameters),
                    SuccessEvent = r.SuccessEvent,
                    RuleExpressionType = RuleExpressionType.LambdaExpression,
                    Enabled = true
                }).ToList()
            })
            .ToArray();

        if (workflows.Length == 0)
        {
            _workflowCaches[spaceId] = WorkflowCache.Empty;
            return WorkflowCache.Empty;
        }

        var engine = new RulesEngineCore(workflows);

        var lookup = validDefinitions
            .Where(d => !string.IsNullOrWhiteSpace(d.EventType))
            .ToDictionary(d => d.EventType, d => d.EventType, StringComparer.OrdinalIgnoreCase);

        var parameterMap = validDefinitions
            .Where(d => !string.IsNullOrWhiteSpace(d.EventType))
            .ToDictionary(
                d => d.EventType,
                d => d.Parameters,
                StringComparer.OrdinalIgnoreCase);

        var cache = new WorkflowCache(engine, lookup, parameterMap);
        _workflowCaches[spaceId] = cache;
        return cache;
    }

    private static string NormalizeExpression(
        string expression,
        IReadOnlyDictionary<string, RuntimeParameterDefinition>? parameters)
    {
        if (string.IsNullOrWhiteSpace(expression))
        {
            return expression;
        }

        if (expression.Contains("input1.", StringComparison.Ordinal))
        {
            return expression;
        }

        if (parameters is null || parameters.Count == 0)
        {
            return expression;
        }

        var identifiers = parameters.Keys
            .Where(static key => !string.IsNullOrWhiteSpace(key))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(static key => key.Length)
            .ToList();

        if (identifiers.Count == 0)
        {
            return expression;
        }

        foreach (var identifier in identifiers)
        {
            var pattern = $"(?<![\\w.]){Regex.Escape(identifier)}\\b";
            expression = Regex.Replace(
                expression,
                pattern,
                static match => $"input1.{match.Value}",
                RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
        }

        return expression;
    }

    private static ExpandoObject BuildPropertyBag(
        RulesEngineEvaluationRequest request,
        IReadOnlyDictionary<string, RuntimeParameterDefinition>? parameters)
    {
        var expando = new ExpandoObject();
        var dict = (IDictionary<string, object?>)expando;

        if (request.Properties is not null)
        {
            foreach (var (key, value) in request.Properties)
            {
                dict[key] = ConvertJsonElement(value);
            }
        }

        if (parameters is not null)
        {
            foreach (var parameter in parameters.Values)
            {
                var resolved = ResolveParameterValue(parameter, request);

                if (resolved is not null)
                {
                    dict[parameter.Key] = resolved;
                }
                else if (!dict.ContainsKey(parameter.Key))
                {
                    dict[parameter.Key] = resolved;
                }
            }
        }

        return expando;
    }

    private static object? ResolveParameterValue(
        RuntimeParameterDefinition definition,
        RulesEngineEvaluationRequest request)
    {
        object? value = null;

        if (string.Equals(definition.Source, "event", StringComparison.OrdinalIgnoreCase))
        {
            value = ResolveEventValue(definition.Path, request.Properties);
        }

        if (value is null && !string.IsNullOrWhiteSpace(definition.DefaultValueJson))
        {
            value = ParseDefaultValue(definition.DefaultValueJson);
        }

        return value;
    }

    private static object? ResolveEventValue(string path, Dictionary<string, JsonElement>? properties)
    {
        if (properties is null || string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        var segments = ParsePathSegments(path);
        if (segments.Count == 0)
        {
            return null;
        }

        if (!TryGetValueCaseInsensitive(properties, segments[0], out var current))
        {
            return null;
        }

        for (var i = 1; i < segments.Count; i++)
        {
            if (current.ValueKind != JsonValueKind.Object ||
                !TryGetPropertyCaseInsensitive(current, segments[i], out current))
            {
                return null;
            }
        }

        return ConvertJsonElement(current);
    }

    private static bool TryGetValueCaseInsensitive(
        IReadOnlyDictionary<string, JsonElement> dictionary,
        string key,
        out JsonElement value)
    {
        if (dictionary.TryGetValue(key, out value))
        {
            return true;
        }

        foreach (var pair in dictionary)
        {
            if (string.Equals(pair.Key, key, StringComparison.OrdinalIgnoreCase))
            {
                value = pair.Value;
                return true;
            }
        }

        value = default;
        return false;
    }

    private static bool TryGetPropertyCaseInsensitive(JsonElement element, string propertyName, out JsonElement value)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            value = default;
            return false;
        }

        if (element.TryGetProperty(propertyName, out value))
        {
            return true;
        }

        foreach (var property in element.EnumerateObject())
        {
            if (string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase))
            {
                value = property.Value;
                return true;
            }
        }

        value = default;
        return false;
    }

    private static IReadOnlyList<string> ParsePathSegments(string path)
    {
        var trimmed = path.Trim();

        if (trimmed.StartsWith("$.", StringComparison.Ordinal))
        {
            trimmed = trimmed[2..];
        }
        else if (trimmed.StartsWith("$", StringComparison.Ordinal))
        {
            trimmed = trimmed[1..];
        }

        return trimmed.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    private static object? ParseDefaultValue(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            return ConvertJsonElement(doc.RootElement);
        }
        catch (JsonException)
        {
            return json;
        }
    }

    private static RuleEvaluationOutcome ToOutcome(RuleResultTree result)
    {
        var successEvent = result.Rule.SuccessEvent;
        var rewardCode = TryParseRewardCode(successEvent);

        var errorMessage = string.IsNullOrWhiteSpace(result.ExceptionMessage)
            ? result.Rule.ErrorMessage
            : result.ExceptionMessage;

        return new RuleEvaluationOutcome
        {
            RuleName = result.Rule.RuleName,
            IsSuccess = result.IsSuccess,
            SuccessEvent = successEvent,
            ErrorMessage = errorMessage,
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

    private sealed record WorkflowCache(
        RulesEngineCore? Engine,
        IReadOnlyDictionary<string, string> WorkflowLookup,
        IReadOnlyDictionary<string, IReadOnlyDictionary<string, RuntimeParameterDefinition>> ParameterMap)
    {
        public static readonly WorkflowCache Empty = new(
            null,
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase),
            new Dictionary<string, IReadOnlyDictionary<string, RuntimeParameterDefinition>>(StringComparer.OrdinalIgnoreCase));
    }
}
