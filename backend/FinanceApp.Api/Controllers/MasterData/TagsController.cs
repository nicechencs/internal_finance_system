using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using FinanceApp.Application.Common;
using FinanceApp.Application.Modules.MasterData.DTOs.Tag;
using FinanceApp.Application.Modules.MasterData.DTOs.Tag.Analytics;
using FinanceApp.Application.Modules.MasterData.Interfaces;

namespace FinanceApp.Api.Controllers.MasterData;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TagsController : BaseApiController
{
    private readonly ITagService _tagService;
    private readonly ITagAnalyticsService _tagAnalyticsService;
    private readonly ILogger<TagsController> _logger;

    public TagsController(ITagService tagService, ITagAnalyticsService tagAnalyticsService, ILogger<TagsController> logger)
    {
        _tagService = tagService;
        _tagAnalyticsService = tagAnalyticsService;
        _logger = logger;
    }

    // ─── 标签定义 CRUD ───────────────────────────────────────────────

    [HttpGet]
    [Authorize(Roles = "Admin,Accountant,Viewer")]
    public async Task<ActionResult<ApiResponse<List<TagDto>>>> GetTags(
        [FromQuery] string scope,
        [FromQuery] bool? isActive = null)
    {
        _logger.LogInformation("[TagsController.GetTags] scope={Scope}, isActive={IsActive}", scope, isActive);

        var result = await _tagService.GetTagsAsync(scope, isActive);
        return Ok(ApiResponse<List<TagDto>>.SuccessResponse(result));
    }

    [HttpGet("{id:long}")]
    [Authorize(Roles = "Admin,Accountant,Viewer")]
    public async Task<ActionResult<ApiResponse<TagDto>>> GetById(long id)
    {
        _logger.LogInformation("[TagsController.GetById] id={Id}", id);

        var result = await _tagService.GetByIdAsync(id);
        return Ok(ApiResponse<TagDto>.SuccessResponse(result));
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Accountant")]
    public async Task<ActionResult<ApiResponse<TagDto>>> Create([FromBody] CreateTagRequest request)
    {
        _logger.LogInformation("[TagsController.Create] scope={Scope}, name={Name}", request.Scope, request.Name);

        var result = await _tagService.CreateAsync(request);
        return Ok(ApiResponse<TagDto>.SuccessResponse(result, "创建标签成功"));
    }

    [HttpPut("{id:long}")]
    [Authorize(Roles = "Admin,Accountant")]
    public async Task<ActionResult<ApiResponse<TagDto>>> Update(long id, [FromBody] UpdateTagRequest request)
    {
        _logger.LogInformation("[TagsController.Update] id={Id}", id);

        var result = await _tagService.UpdateAsync(id, request);
        return Ok(ApiResponse<TagDto>.SuccessResponse(result, "更新标签成功"));
    }

    [HttpDelete("{id:long}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(long id)
    {
        _logger.LogInformation("[TagsController.Delete] id={Id}", id);

        await _tagService.DeleteAsync(id);
        return Ok(ApiResponse<object>.SuccessResponse(null, "删除标签成功"));
    }

    // ─── 标签绑定管理 ────────────────────────────────────────────────

    [HttpGet("bindings")]
    [Authorize(Roles = "Admin,Accountant,Viewer")]
    public async Task<ActionResult<ApiResponse<List<TagBindingDto>>>> GetBindings(
        [FromQuery] string ownerType,
        [FromQuery] long ownerId)
    {
        _logger.LogInformation("[TagsController.GetBindings] ownerType={OwnerType}, ownerId={OwnerId}", ownerType, ownerId);

        var result = await _tagService.GetBindingsAsync(ownerType, ownerId);
        return Ok(ApiResponse<List<TagBindingDto>>.SuccessResponse(result));
    }

    [HttpPut("bindings/set")]
    [Authorize(Roles = "Admin,Accountant")]
    public async Task<ActionResult<ApiResponse<object>>> SetBindings([FromBody] SetBindingsRequest request)
    {
        _logger.LogInformation("[TagsController.SetBindings] ownerType={OwnerType}, ownerId={OwnerId}, tagCount={Count}",
            request.OwnerType, request.OwnerId, request.TagIds.Count);

        await _tagService.SetBindingsAsync(request);
        return Ok(ApiResponse<object>.SuccessResponse(null, "设置标签成功"));
    }

    [HttpPost("bindings")]
    [Authorize(Roles = "Admin,Accountant")]
    public async Task<ActionResult<ApiResponse<object>>> AddBinding([FromBody] AddBindingRequest request)
    {
        _logger.LogInformation("[TagsController.AddBinding] ownerType={OwnerType}, ownerId={OwnerId}, tagId={TagId}",
            request.OwnerType, request.OwnerId, request.TagId);

        await _tagService.AddBindingAsync(request.OwnerType, request.OwnerId, request.TagId);
        return Ok(ApiResponse<object>.SuccessResponse(null, "添加标签成功"));
    }

    [HttpDelete("bindings")]
    [Authorize(Roles = "Admin,Accountant")]
    public async Task<ActionResult<ApiResponse<object>>> RemoveBinding([FromBody] RemoveBindingRequest request)
    {
        _logger.LogInformation("[TagsController.RemoveBinding] ownerType={OwnerType}, ownerId={OwnerId}, tagId={TagId}",
            request.OwnerType, request.OwnerId, request.TagId);

        await _tagService.RemoveBindingAsync(request.OwnerType, request.OwnerId, request.TagId);
        return Ok(ApiResponse<object>.SuccessResponse(null, "移除标签成功"));
    }

    [HttpPut("bindings/batch")]
    [Authorize(Roles = "Admin,Accountant")]
    public async Task<ActionResult<ApiResponse<object>>> BatchSetBindings([FromBody] BatchSetBindingsRequest request)
    {
        _logger.LogInformation("[TagsController.BatchSetBindings] ownerType={OwnerType}, ownerCount={OwnerCount}, tagCount={TagCount}",
            request.OwnerType, request.OwnerIds.Count, request.TagIds.Count);

        await _tagService.BatchSetBindingsAsync(request);
        return Ok(ApiResponse<object>.SuccessResponse(null, "批量设置标签成功"));
    }

    // ─── 标签分析统计 ────────────────────────────────────────────────

    [HttpGet("analytics/summary")]
    [Authorize(Roles = "Admin,Accountant,Viewer")]
    public async Task<ActionResult<ApiResponse<TagSummaryDto>>> GetTagSummary(
        [FromQuery] string scope,
        [FromQuery] DateTime? dateFrom = null,
        [FromQuery] DateTime? dateTo = null)
    {
        _logger.LogInformation("[TagsController.GetTagSummary] scope={Scope}, dateFrom={DateFrom}, dateTo={DateTo}",
            scope, dateFrom, dateTo);

        var result = await _tagAnalyticsService.GetTagSummaryAsync(scope, dateFrom, dateTo);
        return Ok(ApiResponse<TagSummaryDto>.SuccessResponse(result));
    }

    [HttpGet("analytics/cross")]
    [Authorize(Roles = "Admin,Accountant,Viewer")]
    public async Task<ActionResult<ApiResponse<TagCrossAnalysisDto>>> GetTagCrossAnalysis(
        [FromQuery] string rowScope,
        [FromQuery] string colScope,
        [FromQuery] DateTime? dateFrom = null,
        [FromQuery] DateTime? dateTo = null)
    {
        _logger.LogInformation("[TagsController.GetTagCrossAnalysis] rowScope={RowScope}, colScope={ColScope}",
            rowScope, colScope);

        var result = await _tagAnalyticsService.GetTagCrossAnalysisAsync(rowScope, colScope, dateFrom, dateTo);
        return Ok(ApiResponse<TagCrossAnalysisDto>.SuccessResponse(result));
    }
}
