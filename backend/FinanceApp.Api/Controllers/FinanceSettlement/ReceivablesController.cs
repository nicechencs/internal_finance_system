using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using FinanceApp.Application.Common;
using FinanceApp.Application.Modules.FinanceSettlement.DTOs.Receivable;
using FinanceApp.Application.Modules.FinanceSettlement.Interfaces;
using FinanceApp.Api.Controllers.Base;

namespace FinanceApp.Api.Controllers.FinanceSettlement;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ReceivablesController : CrudControllerBase<ReceivableDto, CreateReceivableRequest, UpdateReceivableRequest>
{
    private readonly IReceivableService _receivableService;
    private readonly ISettlementCandidateService _settlementCandidateService;

    public ReceivablesController(
        IReceivableService receivableService,
        ISettlementCandidateService settlementCandidateService,
        ILogger<ReceivablesController> logger)
        : base(receivableService, logger)
    {
        _receivableService = receivableService;
        _settlementCandidateService = settlementCandidateService;
    }

    protected override string ControllerName => "ReceivablesController";
    protected override string EntityName => "Receivable";

    [HttpPost("{id:long}/receive")]
    [Authorize(Roles = "Admin,Accountant")]
    public async Task<ActionResult<ApiResponse<ReceivableDto>>> ReceivePayment(long id, [FromBody] ReceivePaymentRequest request)
    {
        Logger.LogInformation("[ReceivablesController.ReceivePayment] ReceivableId={ReceivableId}, Amount={Amount}",
            id, request.Amount);

        var result = await _receivableService.ReceivePaymentAsync(id, request);
        return Ok(ApiResponse<ReceivableDto>.SuccessResponse(result, "收款登记成功"));
    }

    [HttpGet("summary")]
    [Authorize(Roles = "Admin,Accountant,Viewer")]
    public async Task<ActionResult<ApiResponse<ReceivableSummaryDto>>> GetSummary()
    {
        Logger.LogInformation("[ReceivablesController.GetSummary]");

        var result = await _receivableService.GetReceivableSummaryAsync();
        return Ok(ApiResponse<ReceivableSummaryDto>.SuccessResponse(result));
    }

    [HttpGet("trend")]
    [Authorize(Roles = "Admin,Accountant,Viewer")]
    public async Task<ActionResult<ApiResponse<ReceivableTrendDto>>> GetTrend(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate)
    {
        Logger.LogInformation("[ReceivablesController.GetTrend] StartDate={StartDate}, EndDate={EndDate}", startDate, endDate);

        var result = await _receivableService.GetTrendAsync(startDate, endDate);
        return Ok(ApiResponse<ReceivableTrendDto>.SuccessResponse(result));
    }

    [HttpGet("aging")]
    [Authorize(Roles = "Admin,Accountant,Viewer")]
    public async Task<ActionResult<ApiResponse<ReceivableAgingDto>>> GetAging()
    {
        Logger.LogInformation("[ReceivablesController.GetAging]");

        var result = await _receivableService.GetAgingAsync();
        return Ok(ApiResponse<ReceivableAgingDto>.SuccessResponse(result));
    }

    [HttpGet("project/{projectId:long}")]
    [Authorize(Roles = "Admin,Accountant,Viewer")]
    public async Task<ActionResult<ApiResponse<List<ReceivableDto>>>> GetByProject(long projectId)
    {
        Logger.LogInformation("[ReceivablesController.GetByProject] ProjectId={ProjectId}", projectId);

        var result = await _receivableService.GetByProjectIdAsync(projectId);
        return Ok(ApiResponse<List<ReceivableDto>>.SuccessResponse(result));
    }

    [HttpGet("customer/{customerId:long}")]
    [Authorize(Roles = "Admin,Accountant,Viewer")]
    public async Task<ActionResult<ApiResponse<List<ReceivableDto>>>> GetByCustomer(long customerId)
    {
        Logger.LogInformation("[ReceivablesController.GetByCustomer] CustomerId={CustomerId}", customerId);
        var result = await _receivableService.GetByCustomerIdAsync(customerId);
        return Ok(ApiResponse<List<ReceivableDto>>.SuccessResponse(result));
    }

    [HttpGet("supplier/{supplierId:long}")]
    [Authorize(Roles = "Admin,Accountant,Viewer")]
    public async Task<ActionResult<ApiResponse<List<ReceivableDto>>>> GetBySupplier(long supplierId)
    {
        Logger.LogInformation("[ReceivablesController.GetBySupplier] SupplierId={SupplierId}", supplierId);
        var result = await _receivableService.GetBySupplierIdAsync(supplierId);
        return Ok(ApiResponse<List<ReceivableDto>>.SuccessResponse(result));
    }

    [HttpGet("person/{personId:long}")]
    [Authorize(Roles = "Admin,Accountant,Viewer")]
    public async Task<ActionResult<ApiResponse<List<ReceivableDto>>>> GetByPerson(long personId)
    {
        Logger.LogInformation("[ReceivablesController.GetByPerson] PersonId={PersonId}", personId);
        var result = await _receivableService.GetByPersonIdAsync(personId);
        return Ok(ApiResponse<List<ReceivableDto>>.SuccessResponse(result));
    }

    [HttpGet("available-for-transaction")]
    [Authorize(Roles = "Admin,Accountant,Viewer")]
    public async Task<ActionResult<ApiResponse<List<ReceivableDto>>>> GetAvailableForTransaction(
        [FromQuery] long transactionId,
        [FromQuery] string? keyword = null)
    {
        Logger.LogInformation(
            "[ReceivablesController.GetAvailableForTransaction] TransactionId={TransactionId}, Keyword={Keyword}",
            transactionId, keyword);

        var result = await _settlementCandidateService.GetAvailableReceivablesForTransactionAsync(transactionId, keyword);
        return Ok(ApiResponse<List<ReceivableDto>>.SuccessResponse(result));
    }

    [HttpGet("statistics")]
    [Authorize(Roles = "Admin,Accountant,Viewer")]
    public async Task<ActionResult<ApiResponse<ReceivableStatisticsDto>>> GetStatistics([FromQuery] PageRequest request)
    {
        Logger.LogInformation("[ReceivablesController.GetStatistics]");

        var result = await _receivableService.GetStatisticsAsync(request);
        return Ok(ApiResponse<ReceivableStatisticsDto>.SuccessResponse(result));
    }
}
