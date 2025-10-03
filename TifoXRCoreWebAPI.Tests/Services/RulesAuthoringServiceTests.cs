using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using GMS.TifoXRCoreWebAPI.Models;
using GMS.TifoXRCoreWebAPI.Repositories;
using GMS.TifoXRCoreWebAPI.Services;
using Moq;
using Xunit;

namespace TifoXRCoreWebAPI.Tests.Services
{
    public class RulesAuthoringServiceTests
    {
        [Fact]
        public async Task UpdateRuleDefinitionAsync_ComposesExpressionFromConditions()
        {
            var repo = new Mock<IRulesRepository>();
            var rewardRepo = new Mock<IRewardRepository>();
            var evaluation = new Mock<IRulesEvaluationService>();
            var metadata = new Mock<IRulesMetadataRepository>();

            repo
                .Setup(r => r.TryGetRuleContextAsync(1))
                .ReturnsAsync((true, 42, 7));

            repo
                .Setup(r => r.GetRuleConditionGroupsAsync(1))
                .ReturnsAsync(new List<ConditionGroupDetailView>
                {
                    new()
                    {
                        Id = 100,
                        RuleId = 1,
                        ParentGroupId = null,
                        LogicalOperatorId = 1,
                        LogicalOperatorCode = "and",
                        LogicalOperatorFormat = "({0} && {1})",
                        OrderIndex = 1
                    }
                });

            repo
                .Setup(r => r.GetRuleConditionsAsync(1))
                .ReturnsAsync(new List<ConditionDetailView>
                {
                    new()
                    {
                        Id = 200,
                        GroupId = 100,
                        ParameterId = 10,
                        ComparatorId = 5,
                        ComparatorCode = ComparatorCodes.Gte,
                        ComparatorFormat = "{0} >= {1}",
                        ParameterKey = "ScoreValue",
                        ParameterSource = "event",
                        ParameterPath = "$.ScoreValue",
                        Negate = false,
                        RightValueKind = RightValueKinds.Literal,
                        RightValueJson = "50",
                        OrderIndex = 1
                    }
                });

            repo
                .Setup(r => r.UpdateRuleDefinitionAsync(It.IsAny<RuleDefinitionUpdateDto>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            var service = new RulesAuthoringService(
                repo.Object,
                rewardRepo.Object,
                evaluation.Object,
                metadata.Object);

            var dto = new RuleDefinitionUpdateDto
            {
                RuleId = 1,
                SpaceId = 42,
                RuleName = "Test Rule",
                Priority = 1,
                RuleCooldownSeconds = 0
            };

            var result = await service.UpdateRuleDefinitionAsync(dto);

            result.Expression.Should().Be("ScoreValue >= 50");
            repo.Verify(r => r.UpdateRuleDefinitionAsync(dto, "ScoreValue >= 50"), Times.Once);
            evaluation.Verify(e => e.Invalidate(42), Times.Once);
        }

        [Fact]
        public async Task GetRuleDetailAsync_ReturnsAggregatedView()
        {
            var repo = new Mock<IRulesRepository>();
            var rewardRepo = new Mock<IRewardRepository>();
            var evaluation = new Mock<IRulesEvaluationService>();
            var metadata = new Mock<IRulesMetadataRepository>();

            repo
                .Setup(r => r.GetRuleDetailAsync(1))
                .ReturnsAsync(new RuleDetailRecord
                {
                    RuleId = 1,
                    WorkflowId = 9,
                    SpaceId = 42,
                    RuleName = "Test Rule",
                    WorkflowName = "Workflow",
                    Expression = "ScoreValue >= 50",
                    TargetType = "game.session.ended",
                    SuccessEvent = "reward:granted",
                    Priority = 10,
                    RuleCooldownSeconds = 0,
                    StateTypeId = 3,
                    Version = 1,
                    ParentRuleId = null,
                    EventTypeId = 7
                });

            repo
                .Setup(r => r.GetRuleConditionGroupsAsync(1))
                .ReturnsAsync(new List<ConditionGroupDetailView>
                {
                    new()
                    {
                        Id = 100,
                        RuleId = 1,
                        ParentGroupId = null,
                        LogicalOperatorId = 1,
                        LogicalOperatorCode = "and",
                        LogicalOperatorFormat = "({0} && {1})",
                        OrderIndex = 1
                    }
                });

            repo
                .Setup(r => r.GetRuleConditionsAsync(1))
                .ReturnsAsync(new List<ConditionDetailView>
                {
                    new()
                    {
                        Id = 200,
                        GroupId = 100,
                        ParameterId = 10,
                        ComparatorId = 5,
                        ComparatorCode = ComparatorCodes.Gte,
                        ComparatorFormat = "{0} >= {1}",
                        ParameterKey = "ScoreValue",
                        ParameterSource = "event",
                        ParameterPath = "$.ScoreValue",
                        Negate = false,
                        RightValueKind = RightValueKinds.Literal,
                        RightValueJson = "50",
                        OrderIndex = 1
                    }
                });

            metadata
                .Setup(m => m.GetEventTypeParametersAsync(7))
                .ReturnsAsync(new List<EventTypeParameterView>
                {
                    new()
                    {
                        Id = 300,
                        EventTypeId = 7,
                        ParameterId = 10,
                        ParameterKey = "ScoreValue",
                        IsRequired = true,
                        DefaultValueJson = null
                    }
                });

            var service = new RulesAuthoringService(
                repo.Object,
                rewardRepo.Object,
                evaluation.Object,
                metadata.Object);

            var result = await service.GetRuleDetailAsync(42, 1);

            result.Rule.RuleId.Should().Be(1);
            result.Rule.WorkflowName.Should().Be("Workflow");
            result.ConditionGroups.Should().HaveCount(1);
            result.Conditions.Should().HaveCount(1);
            result.EventTypeParameters.Should().ContainSingle()
                .Which.ParameterKey.Should().Be("ScoreValue");
        }
    }
}
