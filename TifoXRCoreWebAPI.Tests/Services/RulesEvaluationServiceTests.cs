using System.Text.Json;
using FluentAssertions;
using GMS.TifoXRCoreWebAPI.Models;
using GMS.TifoXRCoreWebAPI.Repositories;
using GMS.TifoXRCoreWebAPI.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace TifoXRCoreWebAPI.Tests.Services;

public sealed class RulesEvaluationServiceTests
{
    [Fact]
    public async Task EvaluateAsync_UsesRequestPropertyWhenParameterResolutionFails()
    {
        // Arrange
        var workflowDefinitions = new List<RuntimeWorkflowDefinition>
        {
            new()
            {
                WorkflowId = 1,
                WorkflowName = "GamePlayed",
                EventType = "GamePlayed",
                Rules = new List<RuntimeRuleDefinition>
                {
                    new()
                    {
                        RuleId = 1,
                        RuleName = "DifficultyMatch",
                        Expression = "input1.Difficulty == 3",
                        SuccessEvent = "reward:matched"
                    }
                },
                Parameters = new Dictionary<string, RuntimeParameterDefinition>
                {
                    ["Difficulty"] = new()
                    {
                        Key = "Difficulty",
                        Source = "event",
                        Path = "$.difficulty",
                        IsRequired = false
                    }
                }
            }
        };

        var repository = new Mock<IRulesRepository>();
        repository
            .Setup(r => r.GetRuntimeWorkflowsAsync(It.IsAny<int>()))
            .ReturnsAsync(workflowDefinitions);

        var logger = new Mock<ILogger<RulesEvaluationService>>();
        var service = new RulesEvaluationService(repository.Object, logger.Object);

        using var doc = JsonDocument.Parse("""{ \"Difficulty\": 3 }""");

        var request = new RulesEngineEvaluationRequest
        {
            SpaceId = 42,
            EventType = "GamePlayed",
            OccurredAt = DateTime.UtcNow,
            Properties = new Dictionary<string, JsonElement>
            {
                ["Difficulty"] = doc.RootElement.GetProperty("Difficulty")
            }
        };

        // Act
        var response = await service.EvaluateAsync(request);

        // Assert
        response.AnyRuleMatched.Should().BeTrue();
        response.Outcomes.Should().ContainSingle();
        response.Outcomes[0].IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task EvaluateAsync_PopulatesErrorMessageWhenRuleThrows()
    {
        // Arrange
        const string ExpectedError = "Boom!";

        var workflowDefinitions = new List<RuntimeWorkflowDefinition>
        {
            new()
            {
                WorkflowId = 1,
                WorkflowName = "GamePlayed",
                EventType = "GamePlayed",
                Rules = new List<RuntimeRuleDefinition>
                {
                    new()
                    {
                        RuleId = 1,
                        RuleName = "Throws",
                        Expression = $"throw new System.Exception(\"{ExpectedError}\")",
                        SuccessEvent = null,
                        ErrorMessage = "Fallback error"
                    }
                }
            }
        };

        var repository = new Mock<IRulesRepository>();
        repository
            .Setup(r => r.GetRuntimeWorkflowsAsync(It.IsAny<int>()))
            .ReturnsAsync(workflowDefinitions);

        var logger = new Mock<ILogger<RulesEvaluationService>>();
        var service = new RulesEvaluationService(repository.Object, logger.Object);

        var request = new RulesEngineEvaluationRequest
        {
            SpaceId = 42,
            EventType = "GamePlayed",
            OccurredAt = DateTime.UtcNow
        };

        // Act
        var response = await service.EvaluateAsync(request);

        // Assert
        response.AnyRuleMatched.Should().BeFalse();
        response.Outcomes.Should().ContainSingle();
        var outcome = response.Outcomes[0];
        outcome.IsSuccess.Should().BeFalse();
        outcome.ErrorMessage.Should().NotBeNull();
        outcome.ErrorMessage.Should().Contain(ExpectedError);
        outcome.ErrorMessage.Should().NotBe("Fallback error");
    }

    [Fact]
    public async Task EvaluateAsync_AllowsParameterReferenceWithoutInputPrefix()
    {
        // Arrange
        var workflowDefinitions = new List<RuntimeWorkflowDefinition>
        {
            new()
            {
                WorkflowId = 1,
                WorkflowName = "GamePlayed",
                EventType = "GamePlayed",
                Rules = new List<RuntimeRuleDefinition>
                {
                    new()
                    {
                        RuleId = 1,
                        RuleName = "ScoreThreshold",
                        Expression = "ScoreValue >= 50",
                        SuccessEvent = "reward:threshold"
                    }
                },
                Parameters = new Dictionary<string, RuntimeParameterDefinition>
                {
                    ["ScoreValue"] = new()
                    {
                        Key = "ScoreValue",
                        Source = "event",
                        Path = "$.ScoreValue",
                        IsRequired = false
                    }
                }
            }
        };

        var repository = new Mock<IRulesRepository>();
        repository
            .Setup(r => r.GetRuntimeWorkflowsAsync(It.IsAny<int>()))
            .ReturnsAsync(workflowDefinitions);

        var logger = new Mock<ILogger<RulesEvaluationService>>();
        var service = new RulesEvaluationService(repository.Object, logger.Object);

        using var doc = JsonDocument.Parse("""{ \"ScoreValue\": 75 }""");

        var request = new RulesEngineEvaluationRequest
        {
            SpaceId = 42,
            EventType = "GamePlayed",
            OccurredAt = DateTime.UtcNow,
            Properties = new Dictionary<string, JsonElement>
            {
                ["ScoreValue"] = doc.RootElement.GetProperty("ScoreValue")
            }
        };

        // Act
        var response = await service.EvaluateAsync(request);

        // Assert
        response.AnyRuleMatched.Should().BeTrue();
        response.Outcomes.Should().ContainSingle();
        response.Outcomes[0].IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task EvaluateAsync_AllowsMultipleParameterReferencesWithoutInputPrefix()
    {
        // Arrange
        var workflowDefinitions = new List<RuntimeWorkflowDefinition>
        {
            new()
            {
                WorkflowId = 1,
                WorkflowName = "GameSessionEnded",
                EventType = "game.session.ended",
                Rules = new List<RuntimeRuleDefinition>
                {
                    new()
                    {
                        RuleId = 1,
                        RuleName = "Task completion reward",
                        Expression = "ScoreValue >= 50 && Difficulty == \"hard\"",
                        SuccessEvent = "reward:granted"
                    }
                },
                Parameters = new Dictionary<string, RuntimeParameterDefinition>
                {
                    ["ScoreValue"] = new()
                    {
                        Key = "ScoreValue",
                        Source = "event",
                        Path = "$.ScoreValue",
                        IsRequired = true
                    },
                    ["Difficulty"] = new()
                    {
                        Key = "Difficulty",
                        Source = "event",
                        Path = "$.Difficulty",
                        IsRequired = true
                    }
                }
            }
        };

        var repository = new Mock<IRulesRepository>();
        repository
            .Setup(r => r.GetRuntimeWorkflowsAsync(It.IsAny<int>()))
            .ReturnsAsync(workflowDefinitions);

        var logger = new Mock<ILogger<RulesEvaluationService>>();
        var service = new RulesEvaluationService(repository.Object, logger.Object);

        using var doc = JsonDocument.Parse("""{ \"ScoreValue\": 123, \"Difficulty\": \"hard\" }""");

        var request = new RulesEngineEvaluationRequest
        {
            SpaceId = 42,
            EventType = "game.session.ended",
            OccurredAt = DateTime.UtcNow,
            Properties = new Dictionary<string, JsonElement>
            {
                ["ScoreValue"] = doc.RootElement.GetProperty("ScoreValue"),
                ["Difficulty"] = doc.RootElement.GetProperty("Difficulty")
            }
        };

        // Act
        var response = await service.EvaluateAsync(request);

        // Assert
        response.AnyRuleMatched.Should().BeTrue();
        response.Outcomes.Should().ContainSingle();
        response.Outcomes[0].IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task EvaluateAsync_ReloadsCacheAfterInvalidation()
    {
        // Arrange
        var initialDefinitions = new List<RuntimeWorkflowDefinition>
        {
            new()
            {
                WorkflowId = 1,
                WorkflowName = "GamePlayed",
                EventType = "GamePlayed",
                Rules = new List<RuntimeRuleDefinition>
                {
                    new()
                    {
                        RuleId = 1,
                        RuleName = "ScoreThreshold",
                        Expression = "ScoreValue >= 50",
                        SuccessEvent = "reward:threshold"
                    }
                },
                Parameters = new Dictionary<string, RuntimeParameterDefinition>
                {
                    ["ScoreValue"] = new()
                    {
                        Key = "ScoreValue",
                        Source = "event",
                        Path = "$.ScoreValue",
                        IsRequired = false
                    }
                }
            }
        };

        var updatedDefinitions = new List<RuntimeWorkflowDefinition>
        {
            new()
            {
                WorkflowId = 1,
                WorkflowName = "GamePlayed",
                EventType = "GamePlayed",
                Rules = new List<RuntimeRuleDefinition>
                {
                    new()
                    {
                        RuleId = 1,
                        RuleName = "ScoreThreshold",
                        Expression = "ScoreValue >= 50",
                        SuccessEvent = "reward:threshold"
                    }
                },
                Parameters = new Dictionary<string, RuntimeParameterDefinition>
                {
                    ["ScoreValue"] = new()
                    {
                        Key = "ScoreValue",
                        Source = "event",
                        Path = "$.NewScore",
                        IsRequired = false
                    }
                }
            }
        };

        var repository = new Mock<IRulesRepository>();
        repository
            .SetupSequence(r => r.GetRuntimeWorkflowsAsync(It.IsAny<int>()))
            .ReturnsAsync(initialDefinitions)
            .ReturnsAsync(updatedDefinitions);

        var logger = new Mock<ILogger<RulesEvaluationService>>();
        var service = new RulesEvaluationService(repository.Object, logger.Object);

        using var firstDoc = JsonDocument.Parse("""{ \"ScoreValue\": 75 }""");
        var firstRequest = new RulesEngineEvaluationRequest
        {
            SpaceId = 42,
            EventType = "GamePlayed",
            OccurredAt = DateTime.UtcNow,
            Properties = new Dictionary<string, JsonElement>
            {
                ["ScoreValue"] = firstDoc.RootElement.GetProperty("ScoreValue")
            }
        };

        // Warm the cache
        var initialResponse = await service.EvaluateAsync(firstRequest);
        initialResponse.AnyRuleMatched.Should().BeTrue();

        service.Invalidate(firstRequest.SpaceId);

        using var secondDoc = JsonDocument.Parse("""{ \"NewScore\": 80 }""");
        var secondRequest = new RulesEngineEvaluationRequest
        {
            SpaceId = 42,
            EventType = "GamePlayed",
            OccurredAt = DateTime.UtcNow,
            Properties = new Dictionary<string, JsonElement>
            {
                ["NewScore"] = secondDoc.RootElement.GetProperty("NewScore")
            }
        };

        // Act
        var updatedResponse = await service.EvaluateAsync(secondRequest);

        // Assert
        updatedResponse.AnyRuleMatched.Should().BeTrue();
        updatedResponse.Outcomes.Should().ContainSingle();
        updatedResponse.Outcomes[0].IsSuccess.Should().BeTrue();
        repository.Verify(r => r.GetRuntimeWorkflowsAsync(firstRequest.SpaceId), Times.Exactly(2));
    }
}
