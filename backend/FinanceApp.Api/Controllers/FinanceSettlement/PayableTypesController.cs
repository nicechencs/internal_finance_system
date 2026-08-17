using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using FinanceApp.Application.Common;
using FinanceApp.Application.Modules.FinanceSettlement.DTOs.Payable;
using FinanceApp.Application.Modules.FinanceSettlement.Interfaces;
using FinanceApp.Api.Controllers.Base;

namespace FinanceApp.Api.Controllers.FinanceSettlement;

/// <summary>
/// 应付款业务类型控制器
/// </summary>
[ApiController]
[Route("api/payable-types")]
[Authorize]
public class PayableTypesController : CrudControllerBase<PayableTypeDto, CreatePayableTypeRequest, UpdatePayableTypeRequest>
{
    private readonly IPayableTypeService _payableTypeService;

    public PayableTypesController(IPayableTypeService payableTypeService, ILogger<PayableTypesController> logger)
        : base(payableTypeService, logger)
    {
        _payableTypeService = payableTypeService;
    }

    protected override string ControllerName => "PayableTypesController";
    protected override string EntityName => "PayableType";

    /// <summary>
    /// 获取所有启用的应付款类型
    /// </summary>
    [HttpGet("active")]
    [Authorize(Roles = "Admin,Accountant,Viewer")]
    public async Task<ActionResult<ApiResponse<List<PayableTypeDto>>>> GetAllActive()
    {
        Logger.LogInformation("[PayableTypesController.GetAllActive]");

        var result = await _payableTypeService.GetAllActiveAsync();
        return Ok(ApiResponse<List<PayableTypeDto>>.SuccessResponse(result));
    }

    protected override string GetCreateSuccessMessage() => "应付款类型创建成功";
    protected override string GetUpdateSuccessMessage() => "应付款类型更新成功";
    protected override string GetDeleteSuccessMessage() => "应付款类型删除成功";
}
