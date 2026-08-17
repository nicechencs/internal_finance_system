using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FinanceApp.Application.Common;
using FinanceApp.Application.Modules.Reconciliation.DTOs;
using FinanceApp.Application.Modules.Reconciliation.Interfaces;

using FinanceApp.Api.Controllers;

namespace FinanceApp.Api.Controllers.Reconciliation;

[ApiController]
[Route("api/imports")]
[Authorize(Roles = "Admin,Accountant")]
public class ImportController : BaseApiController
{
    private readonly IImportService _importService;
    private readonly ILogger<ImportController> _logger;

    public ImportController(IImportService importService, ILogger<ImportController> logger)
    {
        _importService = importService;
        _logger = logger;
    }

    /// <summary>
    /// 上传 Excel 文件并预览解析结果
    /// </summary>
    [HttpPost("preview")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<ApiResponse<ImportPreviewResponse>>> Preview(
        IFormFile file,
        [FromForm] long accountId)
    {
        var userId = GetCurrentUserId();
        _logger.LogInformation("[ImportController.Preview] UserId={UserId}, AccountId={AccountId}, FileName={FileName}, FileSize={FileSize}",
            userId, accountId, file?.FileName ?? "null", file?.Length ?? 0);

        if (file == null || file.Length == 0)
        {
            _logger.LogWarning("[ImportController.Preview] 参数验证失败: 未上传文件");
            return BadRequest(ApiResponse<ImportPreviewResponse>.ErrorResponse("请上传文件"));
        }

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (extension != ".xlsx")
        {
            _logger.LogWarning("[ImportController.Preview] 参数验证失败: 文件格式不支持, Extension={Extension}", extension);
            return BadRequest(ApiResponse<ImportPreviewResponse>.ErrorResponse("仅支持 .xlsx 格式（Excel 2007 及以上版本），如有 .xls 或 .xml 文件请先转换为 .xlsx"));
        }

        using var stream = file.OpenReadStream();
        // 确保流从开始位置读取
        if (stream.CanSeek)
        {
            stream.Seek(0, SeekOrigin.Begin);
        }
        var result = await _importService.PreviewAsync(stream, file.FileName, accountId);
        return Ok(ApiResponse<ImportPreviewResponse>.SuccessResponse(result));
    }

    /// <summary>
    /// 确认导入选中的记录
    /// </summary>
    [HttpPost("confirm")]
    public async Task<ActionResult<ApiResponse<ImportBatchDto>>> Confirm([FromBody] ImportConfirmRequest request)
    {
        var userId = GetCurrentUserId();
        _logger.LogInformation("[ImportController.Confirm] UserId={UserId}, BatchId={BatchId}, SelectedRowCount={SelectedRowCount}",
            userId, request.BatchId, request.SelectedRowNumbers.Count);

        var result = await _importService.ConfirmAsync(request);
        return Ok(ApiResponse<ImportBatchDto>.SuccessResponse(result, "导入成功"));
    }

    /// <summary>
    /// 获取导入批次列表
    /// </summary>
    [HttpGet("batches")]
    public async Task<ActionResult<ApiResponse<PageResponse<ImportBatchDto>>>> GetBatches([FromQuery] ImportBatchQueryRequest request)
    {
        _logger.LogInformation("[ImportController.GetBatches] Page={Page}, PageSize={PageSize}, AccountId={AccountId}, Status={Status}",
            request.Page, request.PageSize, request.AccountId, request.Status);

        var result = await _importService.GetBatchesAsync(request);
        return Ok(ApiResponse<PageResponse<ImportBatchDto>>.SuccessResponse(result));
    }

    /// <summary>
    /// 获取导入批次详情
    /// </summary>
    [HttpGet("batches/{id:long}")]
    public async Task<ActionResult<ApiResponse<ImportBatchDto>>> GetBatch(long id)
    {
        _logger.LogInformation("[ImportController.GetBatch] BatchId={BatchId}", id);

        var result = await _importService.GetBatchByIdAsync(id);
        return Ok(ApiResponse<ImportBatchDto>.SuccessResponse(result));
    }

    /// <summary>
    /// 删除导入批次（仅限待处理或失败状态）
    /// </summary>
    [HttpDelete("batches/{id:long}")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteBatch(long id)
    {
        var userId = GetCurrentUserId();
        _logger.LogInformation("[ImportController.DeleteBatch] UserId={UserId}, BatchId={BatchId}", userId, id);

        await _importService.DeleteBatchAsync(id);
        return Ok(ApiResponse<object>.SuccessResponse(null, "删除成功"));
    }

    /// <summary>
    /// 获取待处理批次的缓存预览数据（用于继续处理）
    /// </summary>
    [HttpGet("batches/{id:long}/preview")]
    public async Task<ActionResult<ApiResponse<ImportPreviewResponse>>> GetBatchPreview(long id)
    {
        _logger.LogInformation("[ImportController.GetBatchPreview] BatchId={BatchId}", id);

        var result = await _importService.GetCachedPreviewAsync(id);
        return Ok(ApiResponse<ImportPreviewResponse>.SuccessResponse(result));
    }
}
