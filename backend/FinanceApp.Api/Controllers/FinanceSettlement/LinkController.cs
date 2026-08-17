using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FinanceApp.Application.Common;
using FinanceApp.Application.Modules.FinanceSettlement.DTOs.Link;
using FinanceApp.Application.Modules.FinanceSettlement.Interfaces;

using FinanceApp.Api.Controllers;

namespace FinanceApp.Api.Controllers.FinanceSettlement;

[ApiController]
[Route("api/links")]
[Authorize(Roles = "Admin,Accountant")]
public class LinkController : BaseApiController
{
    private readonly ILinkService _linkService;
    private readonly ILogger<LinkController> _logger;

    public LinkController(ILinkService linkService, ILogger<LinkController> logger)
    {
        _linkService = linkService;
        _logger = logger;
    }

    /// <summary>
    /// 预览一键关联匹配结果
    /// </summary>
    [HttpPost("preview")]
    public async Task<ActionResult<ApiResponse<LinkPreviewResponse>>> Preview([FromBody] LinkPreviewRequest request)
    {
        var userId = GetCurrentUserId();
        _logger.LogInformation("[LinkController.Preview] UserId={UserId}, LinkType={LinkType}, EntityId={EntityId}",
            userId, request.LinkType, request.EntityId);

        var result = await _linkService.PreviewLinkAsync(request);
        return Ok(ApiResponse<LinkPreviewResponse>.SuccessResponse(result));
    }

    /// <summary>
    /// 确认执行一键关联
    /// </summary>
    [HttpPost("confirm")]
    public async Task<ActionResult<ApiResponse<LinkConfirmResponse>>> Confirm([FromBody] LinkConfirmRequest request)
    {
        var userId = GetCurrentUserId();
        _logger.LogInformation("[LinkController.Confirm] UserId={UserId}, LinkType={LinkType}, EntityId={EntityId}, Count={Count}",
            userId, request.LinkType, request.EntityId, request.TransactionIds.Count);

        var result = await _linkService.ConfirmLinkAsync(request);
        return Ok(ApiResponse<LinkConfirmResponse>.SuccessResponse(result, result.Message));
    }

    /// <summary>
    /// 预览规则重跑影响的交易记录
    /// </summary>
    [HttpPost("rule-rerun/preview")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<RuleRerunPreviewResponse>>> RuleRerunPreview([FromBody] RuleRerunPreviewRequest request)
    {
        var userId = GetCurrentUserId();
        _logger.LogInformation("[LinkController.RuleRerunPreview] UserId={UserId}, Strategy={Strategy}",
            userId, request.Strategy);

        var result = await _linkService.PreviewRuleRerunAsync(request);
        return Ok(ApiResponse<RuleRerunPreviewResponse>.SuccessResponse(result));
    }

    /// <summary>
    /// 确认执行规则重跑
    /// </summary>
    [HttpPost("rule-rerun/confirm")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<RuleRerunConfirmResponse>>> RuleRerunConfirm([FromBody] RuleRerunConfirmRequest request)
    {
        var userId = GetCurrentUserId();
        _logger.LogInformation("[LinkController.RuleRerunConfirm] UserId={UserId}, Strategy={Strategy}, Count={Count}",
            userId, request.Strategy, request.TransactionIds?.Count ?? -1);

        var result = await _linkService.ConfirmRuleRerunAsync(request);
        return Ok(ApiResponse<RuleRerunConfirmResponse>.SuccessResponse(result, result.Message));
    }

    /// <summary>
    /// 批量智能关联预览：扫描所有未关联交易，按名称/编号匹配实体
    /// </summary>
    [HttpPost("batch-preview")]
    public async Task<ActionResult<ApiResponse<BatchLinkPreviewResponse>>> BatchPreview()
    {
        var userId = GetCurrentUserId();
        _logger.LogInformation("[LinkController.BatchPreview] UserId={UserId}", userId);

        var result = await _linkService.PreviewBatchLinkAsync();
        return Ok(ApiResponse<BatchLinkPreviewResponse>.SuccessResponse(result));
    }

    /// <summary>
    /// 批量智能关联确认：提交用户选择的关联操作
    /// </summary>
    [HttpPost("batch-confirm")]
    public async Task<ActionResult<ApiResponse<BatchLinkConfirmResponse>>> BatchConfirm([FromBody] BatchLinkConfirmRequest request)
    {
        var userId = GetCurrentUserId();
        _logger.LogInformation("[LinkController.BatchConfirm] UserId={UserId}, Count={Count}",
            userId, request.Items.Count);

        var result = await _linkService.ConfirmBatchLinkAsync(request);
        return Ok(ApiResponse<BatchLinkConfirmResponse>.SuccessResponse(result, result.Message));
    }
}
