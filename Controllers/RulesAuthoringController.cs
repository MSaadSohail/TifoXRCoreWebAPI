// <copyright file="RulesAuthoringController.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Saad Sohail</author>
// <date>09/30/2025</date>
// <summary>Rules engine authoring endpoints.</summary>

using System.Collections.Generic;
using GMS.TifoXRCoreWebAPI.Middleware;
using GMS.TifoXRCoreWebAPI.Middleware.Errors;
using GMS.TifoXRCoreWebAPI.Models;
using GMS.TifoXRCoreWebAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace GMS.TifoXRCoreWebAPI.Controllers
{
    [ApiController]
    [Route("api/space/{spaceId}")]
    [Produces("application/json")]
    public sealed class RulesAuthoringController : ControllerBase
    {
        private readonly IRulesAuthoringService _svc;
        public RulesAuthoringController(IRulesAuthoringService svc) => _svc = svc;

        [HttpPost("workflows")]
        [ProducesResponseType(typeof(object), StatusCodes.Status201Created)]
        public async Task<ActionResult<object>> CreateWorkflow(
            int spaceId,
            [FromBody] WorkflowCreateDto dto)
        {
            if (dto is null)
                throw ErrorService.Exception(
                    ErrorType.ArgumentNull,
                    nameof(CreateWorkflow),
                    ErrorMessages.Validation.MissingParameter,
                    paramName: nameof(dto));

            if (spaceId <= 0)
                throw ErrorService.Exception(
                    ErrorType.Argument,
                    nameof(CreateWorkflow),
                    ErrorMessages.Validation.PositiveIntRequired,
                    paramName: nameof(spaceId));

            var id = await _svc.CreateWorkflowAsync(spaceId, dto);
            return CreatedAtAction(nameof(CreateWorkflow), new { id }, new { id });
        }

        [HttpGet("rules/{ruleId:int}")]
        [ProducesResponseType(typeof(RuleDetailView), StatusCodes.Status200OK)]
        public async Task<ActionResult<RuleDetailView>> GetRule(int spaceId, int ruleId)
        {
            var detail = await _svc.GetRuleDetailAsync(spaceId, ruleId);
            return Ok(detail);
        }

        [HttpPut("rules/{ruleId:int}")]
        [ProducesResponseType(typeof(RuleExpressionUpdateResult), StatusCodes.Status200OK)]
        public async Task<ActionResult<RuleExpressionUpdateResult>> UpdateRule(
            int spaceId,
            int ruleId,
            [FromBody] RuleDefinitionUpdateDto dto)
        {
            if (dto is null)
                throw ErrorService.Exception(
                    ErrorType.ArgumentNull,
                    nameof(UpdateRule),
                    ErrorMessages.Validation.MissingParameter,
                    paramName: nameof(dto),
                    parameters: new { spaceId, ruleId });

            var result = await _svc.UpdateRuleDefinitionAsync(spaceId, ruleId, dto);
            return Ok(result);
        }

        [HttpPost("workflows/{workflowId:int}/rules")]
        [ProducesResponseType(typeof(object), StatusCodes.Status201Created)]
        public async Task<ActionResult<object>> CreateRule(
            int spaceId,
            int workflowId,
            [FromBody] RuleCreateDto dto)
        {
            if (dto is null)
                throw ErrorService.Exception(
                    ErrorType.ArgumentNull,
                    nameof(CreateRule),
                    ErrorMessages.Validation.MissingParameter,
                    paramName: nameof(dto),
                    parameters: new { workflowId });

            if (spaceId <= 0)
                throw ErrorService.Exception(
                    ErrorType.Argument,
                    nameof(CreateRule),
                    ErrorMessages.Validation.PositiveIntRequired,
                    paramName: nameof(spaceId),
                    parameters: new { workflowId });

            var id = await _svc.CreateRuleAsync(spaceId, workflowId, dto);
            return CreatedAtAction(nameof(CreateRule), new { workflowId, id }, new { id });
        }

        [HttpPost("rules/{ruleId:int}/condition-groups")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        public async Task<ActionResult<object>> AddConditionGroups(
            int spaceId,
            int ruleId,
            [FromBody] IReadOnlyList<ConditionGroupCreateDto> dtos)
        {
            if (dtos is null)
                throw ErrorService.Exception(
                    ErrorType.ArgumentNull,
                    nameof(AddConditionGroups),
                    ErrorMessages.Validation.MissingParameter,
                    paramName: nameof(dtos),
                    parameters: new { ruleId });

            var ids = await _svc.AddConditionGroupsAsync(ruleId, dtos);
            return Ok(new { ids });
        }

        [HttpPost("condition-groups/{groupId:int}/conditions")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        public async Task<ActionResult<object>> AddConditions(
            int spaceId,
            int groupId,
            [FromBody] IReadOnlyList<ConditionCreateDto> dtos)
        {
            if (dtos is null)
                throw ErrorService.Exception(
                    ErrorType.ArgumentNull,
                    nameof(AddConditions),
                    ErrorMessages.Validation.MissingParameter,
                    paramName: nameof(dtos),
                    parameters: new { groupId });

            var ids = await _svc.AddConditionsAsync(groupId, dtos);
            return Ok(new { ids });
        }

        [HttpPut("rules/{ruleId:int}/condition-groups/{groupId:int}")]
        [ProducesResponseType(typeof(RuleExpressionUpdateResult), StatusCodes.Status200OK)]
        public async Task<ActionResult<RuleExpressionUpdateResult>> UpdateConditionGroup(
            int spaceId,
            int ruleId,
            int groupId,
            [FromBody] ConditionGroupUpdateDto dto)
        {
            if (dto is null)
                throw ErrorService.Exception(
                    ErrorType.ArgumentNull,
                    nameof(UpdateConditionGroup),
                    ErrorMessages.Validation.MissingParameter,
                    paramName: nameof(dto),
                    parameters: new { ruleId, groupId });

            var result = await _svc.UpdateConditionGroupAsync(ruleId, groupId, dto);
            return Ok(result);
        }

        [HttpPut("condition-groups/{groupId:int}/conditions/{conditionId:int}")]
        [ProducesResponseType(typeof(RuleExpressionUpdateResult), StatusCodes.Status200OK)]
        public async Task<ActionResult<RuleExpressionUpdateResult>> UpdateCondition(
            int spaceId,
            int groupId,
            int conditionId,
            [FromBody] ConditionUpdateDto dto)
        {
            if (dto is null)
                throw ErrorService.Exception(
                    ErrorType.ArgumentNull,
                    nameof(UpdateCondition),
                    ErrorMessages.Validation.MissingParameter,
                    paramName: nameof(dto),
                    parameters: new { groupId, conditionId });

            var result = await _svc.UpdateConditionAsync(groupId, conditionId, dto);
            return Ok(result);
        }

        [HttpPost("rules/{ruleId:int}/actions")]
        [ProducesResponseType(typeof(object), StatusCodes.Status201Created)]
        public async Task<ActionResult<object>> CreateRuleAction(
            int spaceId,
            int ruleId,
            [FromBody] RuleActionCreateDto dto)
        {
            if (dto is null)
                throw ErrorService.Exception(
                    ErrorType.ArgumentNull,
                    nameof(CreateRuleAction),
                    ErrorMessages.Validation.MissingParameter,
                    paramName: nameof(dto),
                    parameters: new { ruleId });

            var id = await _svc.CreateRuleActionAsync(ruleId, dto);
            return CreatedAtAction(nameof(CreateRuleAction), new { ruleId, id }, new { id });
        }

        [HttpPost("rule-actions/{actionId:int}/reward")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> BindRewardToAction(
            int spaceId,
            int actionId,
            [FromBody] RuleActionRewardBindDto dto)
        {
            if (dto is null)
                throw ErrorService.Exception(
                    ErrorType.ArgumentNull,
                    nameof(BindRewardToAction),
                    ErrorMessages.Validation.MissingParameter,
                    paramName: nameof(dto),
                    parameters: new { spaceId, actionId });

            if (dto.RewardId <= 0)
                throw ErrorService.Exception(
                    ErrorType.Argument,
                    nameof(BindRewardToAction),
                    ErrorMessages.Validation.PositiveIntRequired,
                    paramName: nameof(dto.RewardId),
                    parameters: new { spaceId, actionId, dto.RewardId });

            await _svc.BindRewardAsync(actionId, dto.RewardId);
            return NoContent();
        }
    }
}
