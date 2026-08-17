using FinanceApp.Application.Common;
using FinanceApp.Application.Modules.FinanceSettlement.DTOs;
using FinanceApp.Application.Modules.FinanceSettlement.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using FinanceApp.Api.Controllers;

namespace FinanceApp.Api.Controllers.FinanceSettlement;

/// <summary>
/// 数据迁移管理控制器
/// </summary>
[ApiController]
[Route("api/admin/data-migration")]
[Authorize(Roles = "Admin")]
public class DataMigrationController : BaseApiController
{
    private readonly IDataMigrationService _dataMigrationService;
    private readonly ILogger<DataMigrationController> _logger;

    public DataMigrationController(
        IDataMigrationService dataMigrationService,
        ILogger<DataMigrationController> logger)
    {
        _dataMigrationService = dataMigrationService;
        _logger = logger;
    }

    /// <summary>
    /// 获取数据一致性问题报告
    /// </summary>
    [HttpGet("issues")]
    public async Task<ActionResult<ApiResponse<DataMigrationIssuesDto>>> GetDataIssues()
    {
        _logger.LogInformation("[DataMigrationController.GetDataIssues]");

        var issues = await _dataMigrationService.GetDataIssuesAsync();
        return Ok(ApiResponse<DataMigrationIssuesDto>.SuccessResponse(issues));
    }

    /// <summary>
    /// 修复应收款金额不一致
    /// </summary>
    [HttpPost("fix-receivable/{id}")]
    public async Task<ActionResult<ApiResponse<object>>> FixReceivableAmount(long id)
    {
        _logger.LogInformation("[DataMigrationController.FixReceivableAmount] ReceivableId={ReceivableId}", id);

        await _dataMigrationService.FixReceivableAmountAsync(id);
        return Ok(ApiResponse<object>.SuccessResponse(null, "修复成功"));
    }

    /// <summary>
    /// 修复应付款金额不一致
    /// </summary>
    [HttpPost("fix-payable/{id}")]
    public async Task<ActionResult<ApiResponse<object>>> FixPayableAmount(long id)
    {
        _logger.LogInformation("[DataMigrationController.FixPayableAmount] PayableId={PayableId}", id);

        await _dataMigrationService.FixPayableAmountAsync(id);
        return Ok(ApiResponse<object>.SuccessResponse(null, "修复成功"));
    }

    /// <summary>
    /// 批量修复所有金额不一致
    /// </summary>
    [HttpPost("fix-all")]
    public async Task<ActionResult<ApiResponse<object>>> FixAllAmountIssues()
    {
        _logger.LogInformation("[DataMigrationController.FixAllAmountIssues]");

        await _dataMigrationService.FixAllAmountIssuesAsync();
        return Ok(ApiResponse<object>.SuccessResponse(null, "批量修复完成"));
    }
}
