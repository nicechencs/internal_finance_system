using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using FinanceApp.Application.Common;
using FinanceApp.Application.Modules.FinanceSettlement.DTOs.Receivable;
using FinanceApp.Application.Modules.FinanceSettlement.Interfaces;
using FinanceApp.Api.Controllers.Base;

namespace FinanceApp.Api.Controllers.FinanceSettlement;

/// <summary>
/// 应收款业务类型控制器
/// </summary>
[ApiController]
[Route("api/receivable-types")]
[Authorize]
public class ReceivableTypesController : CrudControllerBase<ReceivableTypeDto, CreateReceivableTypeRequest, UpdateReceivableTypeRequest>
{
    private readonly IReceivableTypeService _receivableTypeService;

    public ReceivableTypesController(IReceivableTypeService receivableTypeService, ILogger<ReceivableTypesController> logger)
        : base(receivableTypeService, logger)
    {
        _receivableTypeService = receivableTypeService;
    }

    protected override string ControllerName => "ReceivableTypesController";
    protected override string EntityName => "ReceivableType";

    /// <summary>
    /// 获取所有启用的应收款类型
    /// </summary>
    [HttpGet("active")]
    [Authorize(Roles = "Admin,Accountant,Viewer")]
    public async Task<ActionResult<ApiResponse<List<ReceivableTypeDto>>>> GetAllActive()
    {
        Logger.LogInformation("[ReceivableTypesController.GetAllActive]");

        var result = await _receivableTypeService.GetAllActiveAsync();
        return Ok(ApiResponse<List<ReceivableTypeDto>>.SuccessResponse(result));
    }

    protected override string GetCreateSuccessMessage() => "应收款类型创建成功";
    protected override string GetUpdateSuccessMessage() => "应收款类型更新成功";
    protected override string GetDeleteSuccessMessage() => "应收款类型删除成功";
}
