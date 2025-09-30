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
}
