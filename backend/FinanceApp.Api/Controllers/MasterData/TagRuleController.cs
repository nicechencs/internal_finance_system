using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using FinanceApp.Application.Common;
using FinanceApp.Application.Modules.MasterData.DTOs.TagRule;
using FinanceApp.Application.Modules.MasterData.Interfaces;

using FinanceApp.Api.Controllers;

namespace FinanceApp.Api.Controllers.MasterData;

[ApiController]
[Route("api/tag-rules")]
[Authorize]
public class TagRuleController : BaseApiController
{
    private readonly ITagRuleService _tagRuleService;
    private readonly ILogger<TagRuleController> _logger;

    public TagRuleController(ITagRuleService tagRuleService, ILogger<TagRuleController> logger)
    {
        _tagRuleService = tagRuleService;
        _logger = logger;
    }

    [HttpGet]
    [Authorize(Roles = "Admin,Accountant,Viewer")]
    public async Task<ActionResult<ApiResponse<PageResponse<TagRuleDto>>>> GetPaged([FromQuery] PageRequest request)
    {
        _logger.LogInformation("[TagRuleController.GetPaged] Page={Page}, PageSize={PageSize}",
            request.Page, request.PageSize);

        var result = await _tagRuleService.GetPagedAsync(request);
        return Ok(ApiResponse<PageResponse<TagRuleDto>>.SuccessResponse(result));
    }

    [HttpGet("{id:long}")]
    [Authorize(Roles = "Admin,Accountant,Viewer")]
    public async Task<ActionResult<ApiResponse<TagRuleDto>>> GetById(long id)
    {
        _logger.LogInformation("[TagRuleController.GetById] TagRuleId={TagRuleId}", id);

        var result = await _tagRuleService.GetByIdAsync(id);
        return Ok(ApiResponse<TagRuleDto>.SuccessResponse(result));
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<TagRuleDto>>> Create([FromBody] CreateTagRuleRequest request)
    {
        _logger.LogInformation("[TagRuleController.Create] RuleName={RuleName}", request.RuleName);

        var result = await _tagRuleService.CreateAsync(request);
        return Ok(ApiResponse<TagRuleDto>.SuccessResponse(result));
    }

    [HttpPut("{id:long}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<TagRuleDto>>> Update(long id, [FromBody] UpdateTagRuleRequest request)
    {
        _logger.LogInformation("[TagRuleController.Update] TagRuleId={TagRuleId}, RuleName={RuleName}, IsActive={IsActive}",
            id, request.RuleName, request.IsActive);

        var result = await _tagRuleService.UpdateAsync(id, request);
        return Ok(ApiResponse<TagRuleDto>.SuccessResponse(result));
    }

    [HttpDelete("{id:long}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(long id)
    {
        _logger.LogInformation("[TagRuleController.Delete] TagRuleId={TagRuleId}", id);

        await _tagRuleService.DeleteAsync(id);
        return Ok(ApiResponse<object>.SuccessResponse(null, "删除成功"));
    }

    [HttpPost("run")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<RunTagRulesResult>>> RunRules([FromBody] RunTagRulesRequest request)
    {
        _logger.LogInformation("[TagRuleController.RunRules]");

        var result = await _tagRuleService.RunRulesAsync(request);
        return Ok(ApiResponse<RunTagRulesResult>.SuccessResponse(result,
            $"重跑完成: 扫描 {result.ScannedCount} 条, 新增 {result.AddedCount} 个标签, 跳过 {result.SkippedCount} 个"));
    }

    [HttpPost("rerun/preview")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<RerunPreviewResponse>>> RerunPreview([FromBody] RerunPreviewRequest request)
    {
        _logger.LogInformation("[TagRuleController.RerunPreview] TargetScope={TargetScope}, EntityIdsCount={Count}",
            request.TargetScope, request.EntityIds?.Count ?? 0);

        var result = await _tagRuleService.PreviewRerunAsync(request);
        return Ok(ApiResponse<RerunPreviewResponse>.SuccessResponse(result));
    }

    [HttpPost("rerun/confirm")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<RerunConfirmResponse>>> RerunConfirm([FromBody] RerunConfirmRequest request)
    {
        _logger.LogInformation("[TagRuleController.RerunConfirm] TargetScope={TargetScope}, TransactionCount={Count}",
            request.TargetScope, request.TransactionIds?.Count ?? 0);

        var result = await _tagRuleService.ConfirmRerunAsync(request);
        return Ok(ApiResponse<RerunConfirmResponse>.SuccessResponse(result, result.Message));
    }
}
