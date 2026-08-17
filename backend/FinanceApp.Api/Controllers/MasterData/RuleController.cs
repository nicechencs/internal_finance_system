using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using FinanceApp.Application.Common;
using FinanceApp.Application.Modules.MasterData.DTOs.Rule;
using FinanceApp.Application.Modules.MasterData.Interfaces;

using FinanceApp.Api.Controllers;

namespace FinanceApp.Api.Controllers.MasterData;

[ApiController]
[Route("api/rules")]
[Authorize]
public class RuleController : BaseApiController
{
    private readonly IRuleService _ruleService;
    private readonly ILogger<RuleController> _logger;

    public RuleController(IRuleService ruleService, ILogger<RuleController> logger)
    {
        _ruleService = ruleService;
        _logger = logger;
    }

    [HttpGet]
    [Authorize(Roles = "Admin,Accountant,Viewer")]
    public async Task<ActionResult<ApiResponse<PageResponse<RuleDto>>>> GetPaged([FromQuery] PageRequest request)
    {
        _logger.LogInformation("[RuleController.GetPaged] Page={Page}, PageSize={PageSize}",
            request.Page, request.PageSize);

        var result = await _ruleService.GetPagedAsync(request);
        return Ok(ApiResponse<PageResponse<RuleDto>>.SuccessResponse(result));
    }

    [HttpGet("{id:long}")]
    [Authorize(Roles = "Admin,Accountant,Viewer")]
    public async Task<ActionResult<ApiResponse<RuleDto>>> GetById(long id)
    {
        _logger.LogInformation("[RuleController.GetById] RuleId={RuleId}", id);

        var result = await _ruleService.GetByIdAsync(id);
        return Ok(ApiResponse<RuleDto>.SuccessResponse(result));
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<RuleDto>>> Create([FromBody] CreateRuleRequest request)
    {
        _logger.LogInformation("[RuleController.Create] Name={Name}, Priority={Priority}, MatchField={MatchField}, MatchOperator={MatchOperator}",
            request.Name, request.Priority, request.MatchField, request.MatchOperator);

        var result = await _ruleService.CreateAsync(request);
        return Ok(ApiResponse<RuleDto>.SuccessResponse(result));
    }

    [HttpPut("{id:long}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<RuleDto>>> Update(long id, [FromBody] UpdateRuleRequest request)
    {
        _logger.LogInformation("[RuleController.Update] RuleId={RuleId}, Name={Name}, Priority={Priority}, IsActive={IsActive}",
            id, request.Name, request.Priority, request.IsActive);

        var result = await _ruleService.UpdateAsync(id, request);
        return Ok(ApiResponse<RuleDto>.SuccessResponse(result));
    }

    [HttpDelete("{id:long}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(long id)
    {
        _logger.LogInformation("[RuleController.Delete] RuleId={RuleId}", id);

        await _ruleService.DeleteAsync(id);
        return Ok(ApiResponse<object>.SuccessResponse(null, "删除成功"));
    }

    [HttpGet("active")]
    [Authorize(Roles = "Admin,Accountant,Viewer")]
    public async Task<ActionResult<ApiResponse<List<RuleDto>>>> GetActive()
    {
        _logger.LogInformation("[RuleController.GetActive]");

        var result = await _ruleService.GetActiveRulesAsync();
        return Ok(ApiResponse<List<RuleDto>>.SuccessResponse(result));
    }

    [HttpPost("match")]
    [Authorize(Roles = "Admin,Accountant,Viewer")]
    public async Task<ActionResult<ApiResponse<long?>>> MatchCategory([FromBody] MatchCategoryRequest request)
    {
        _logger.LogInformation("[RuleController.MatchCategory] CounterpartyName={CounterpartyName}, Amount={Amount}",
            request.CounterpartyName, request.Amount);

        var result = await _ruleService.MatchCategoryAsync(
            request.CounterpartyName,
            request.Description,
            request.Amount
        );
        return Ok(ApiResponse<long?>.SuccessResponse(result));
    }
}
