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
                .Setup(r => r.UpdateRuleDefinitionAsync(
                    It.IsAny<int>(),
                    It.IsAny<RuleDefinitionUpdateDto>(),
                    It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            var service = new RulesAuthoringService(
                repo.Object,
                rewardRepo.Object,
                evaluation.Object,
                metadata.Object);

            var dto = new RuleDefinitionUpdateDto
            {
                RuleName = "Test Rule",
                Priority = 1,
                RuleCooldownSeconds = 0
            };

            var result = await service.UpdateRuleDefinitionAsync(42, 1, dto);

            result.Expression.Should().Be("ScoreValue >= 50");
            repo.Verify(r => r.UpdateRuleDefinitionAsync(1, dto, "ScoreValue >= 50"), Times.Once);
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

        [Fact]
        public async Task AddConditionGroupsAsync_UsesRouteRuleId()
        {
            var repo = new Mock<IRulesRepository>();
            var rewardRepo = new Mock<IRewardRepository>();
            var evaluation = new Mock<IRulesEvaluationService>();
            var metadata = new Mock<IRulesMetadataRepository>();

            repo
                .Setup(r => r.TryGetRuleContextAsync(5))
                .ReturnsAsync((true, 42, 9));

            var expectedIds = new List<int> { 11, 12 };
            repo
                .Setup(r => r.InsertConditionGroupsAsync(5, It.IsAny<IEnumerable<ConditionGroupCreateDto>>()))
                .ReturnsAsync(expectedIds);

            var service = new RulesAuthoringService(
                repo.Object,
                rewardRepo.Object,
                evaluation.Object,
                metadata.Object);

            var payload = new[]
            {
                new ConditionGroupCreateDto
                {
                    ParentGroupId = null,
                    LogicalOperatorId = 1,
                    OrderIndex = 1,
                    NameKey = "root",
                    DescriptionKey = null
                }
            };

            var ids = await service.AddConditionGroupsAsync(5, payload);

            ids.Should().BeEquivalentTo(expectedIds);
            repo.Verify(
                r => r.InsertConditionGroupsAsync(
                    5,
                    It.Is<IEnumerable<ConditionGroupCreateDto>>(g => ReferenceEquals(g, payload))),
                Times.Once);
            evaluation.Verify(e => e.Invalidate(42), Times.Once);
        }

        [Fact]
        public async Task AddConditionsAsync_UsesRouteGroupId()
        {
            var repo = new Mock<IRulesRepository>();
            var rewardRepo = new Mock<IRewardRepository>();
            var evaluation = new Mock<IRulesEvaluationService>();
            var metadata = new Mock<IRulesMetadataRepository>();

            repo
                .Setup(r => r.TryGetConditionGroupContextAsync(15))
                .ReturnsAsync((true, 5, 42));

            var expectedIds = new List<int> { 21 };
            repo
                .Setup(r => r.InsertConditionsAsync(15, It.IsAny<IEnumerable<ConditionCreateDto>>()))
                .ReturnsAsync(expectedIds);

            repo
                .Setup(r => r.GetRuleConditionGroupsAsync(5))
                .ReturnsAsync(new List<ConditionGroupDetailView>());

            repo
                .Setup(r => r.GetRuleConditionsAsync(5))
                .ReturnsAsync(new List<ConditionDetailView>
                {
                    new()
                    {
                        Id = 21,
                        GroupId = 15,
                        ParameterId = 99,
                        ComparatorId = 7,
                        ComparatorCode = ComparatorCodes.Eq,
                        ComparatorFormat = "{0} == {1}",
                        ParameterKey = "Score",
                        ParameterSource = "event",
                        ParameterPath = "$.Score",
                        Negate = false,
                        RightValueKind = RightValueKinds.Literal,
                        RightValueJson = "10",
                        OrderIndex = 1
                    }
                });

            repo
                .Setup(r => r.UpdateRuleExpressionAsync(5, It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            var service = new RulesAuthoringService(
                repo.Object,
                rewardRepo.Object,
                evaluation.Object,
                metadata.Object);

            var payload = new[]
            {
                new ConditionCreateDto
                {
                    ParameterId = 99,
                    ComparatorId = 7,
                    Negate = false,
                    RightValueKind = RightValueKinds.Literal,
                    RightValueJson = "{\"value\":10}",
                    OrderIndex = 1
                }
            };

            var ids = await service.AddConditionsAsync(15, payload);

            ids.Should().BeEquivalentTo(expectedIds);
            repo.Verify(
                r => r.InsertConditionsAsync(
                    15,
                    It.Is<IEnumerable<ConditionCreateDto>>(c => ReferenceEquals(c, payload))),
                Times.Once);
            repo.Verify(r => r.GetRuleConditionGroupsAsync(5), Times.Once);
            repo.Verify(r => r.GetRuleConditionsAsync(5), Times.Once);
            repo.Verify(r => r.UpdateRuleExpressionAsync(5, "Score == 10"), Times.Once);
            evaluation.Verify(e => e.Invalidate(42), Times.Once);
        }

        [Fact]
        public async Task AddConditionsAsync_UsesLiteralValueFromJsonObject()
        {
            var repo = new Mock<IRulesRepository>();
            var rewardRepo = new Mock<IRewardRepository>();
            var evaluation = new Mock<IRulesEvaluationService>();
            var metadata = new Mock<IRulesMetadataRepository>();

            repo
                .Setup(r => r.TryGetConditionGroupContextAsync(15))
                .ReturnsAsync((true, 5, 42));

            repo
                .Setup(r => r.InsertConditionsAsync(15, It.IsAny<IEnumerable<ConditionCreateDto>>()))
                .ReturnsAsync(new List<int> { 21 });

            repo
                .Setup(r => r.GetRuleConditionGroupsAsync(5))
                .ReturnsAsync(new List<ConditionGroupDetailView>());

            repo
                .Setup(r => r.GetRuleConditionsAsync(5))
                .ReturnsAsync(new List<ConditionDetailView>
                {
                    new()
                    {
                        Id = 21,
                        GroupId = 15,
                        ParameterId = 99,
                        ComparatorId = 7,
                        ComparatorCode = ComparatorCodes.Eq,
                        ComparatorFormat = "{0} == {1}",
                        ParameterKey = "Score",
                        ParameterSource = "event",
                        ParameterPath = "$.Score",
                        Negate = false,
                        RightValueKind = RightValueKinds.Literal,
                        RightValueJson = "{\"value\":10}",
                        OrderIndex = 1
                    }
                });

            repo
                .Setup(r => r.UpdateRuleExpressionAsync(5, It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            var service = new RulesAuthoringService(
                repo.Object,
                rewardRepo.Object,
                evaluation.Object,
                metadata.Object);

            var payload = new[]
            {
                new ConditionCreateDto
                {
                    ParameterId = 99,
                    ComparatorId = 7,
                    Negate = false,
                    RightValueKind = RightValueKinds.Literal,
                    RightValueJson = "{\"value\":10}",
                    OrderIndex = 1
                }
            };

            var ids = await service.AddConditionsAsync(15, payload);

            ids.Should().BeEquivalentTo(new List<int> { 21 });
            repo.Verify(
                r => r.UpdateRuleExpressionAsync(5, "Score == 10"),
                Times.Once);
        }

        [Fact]
        public async Task AddConditionsAsync_FormatsStringLiteralFromJsonObject()
        {
            var repo = new Mock<IRulesRepository>();
            var rewardRepo = new Mock<IRewardRepository>();
            var evaluation = new Mock<IRulesEvaluationService>();
            var metadata = new Mock<IRulesMetadataRepository>();

            repo
                .Setup(r => r.TryGetConditionGroupContextAsync(15))
                .ReturnsAsync((true, 5, 42));

            repo
                .Setup(r => r.InsertConditionsAsync(15, It.IsAny<IEnumerable<ConditionCreateDto>>()))
                .ReturnsAsync(new List<int> { 21 });

            repo
                .Setup(r => r.GetRuleConditionGroupsAsync(5))
                .ReturnsAsync(new List<ConditionGroupDetailView>());

            repo
                .Setup(r => r.GetRuleConditionsAsync(5))
                .ReturnsAsync(new List<ConditionDetailView>
                {
                    new()
                    {
                        Id = 21,
                        GroupId = 15,
                        ParameterId = 99,
                        ComparatorId = 7,
                        ComparatorCode = ComparatorCodes.Eq,
                        ComparatorFormat = "{0} == {1}",
                        ParameterKey = "Tier",
                        ParameterSource = "event",
                        ParameterPath = "$.Tier",
                        Negate = false,
                        RightValueKind = RightValueKinds.Literal,
                        RightValueJson = "{\"value\":\"Bronze\"}",
                        OrderIndex = 1
                    }
                });

            repo
                .Setup(r => r.UpdateRuleExpressionAsync(5, It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            var service = new RulesAuthoringService(
                repo.Object,
                rewardRepo.Object,
                evaluation.Object,
                metadata.Object);

            var payload = new[]
            {
                new ConditionCreateDto
                {
                    ParameterId = 99,
                    ComparatorId = 7,
                    Negate = false,
                    RightValueKind = RightValueKinds.Literal,
                    RightValueJson = "{\"value\":\"Bronze\"}",
                    OrderIndex = 1
                }
            };

            var ids = await service.AddConditionsAsync(15, payload);

            ids.Should().BeEquivalentTo(new List<int> { 21 });
            repo.Verify(
                r => r.UpdateRuleExpressionAsync(5, "Tier == \"Bronze\""),
                Times.Once);
        }
    }
}
