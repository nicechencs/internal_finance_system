using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using FinanceApp.Application.Common;
using FinanceApp.Application.Modules.FinanceSettlement.DTOs.Payable;
using FinanceApp.Application.Modules.FinanceSettlement.Interfaces;
using FinanceApp.Api.Controllers.Base;

namespace FinanceApp.Api.Controllers.FinanceSettlement;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PayablesController : CrudControllerBase<PayableDto, CreatePayableRequest, UpdatePayableRequest>
{
    private readonly IPayableService _payableService;
    private readonly ISettlementCandidateService _settlementCandidateService;

    public PayablesController(
        IPayableService payableService,
        ISettlementCandidateService settlementCandidateService,
        ILogger<PayablesController> logger)
        : base(payableService, logger)
    {
        _payableService = payableService;
        _settlementCandidateService = settlementCandidateService;
    }

    protected override string ControllerName => "PayablesController";
    protected override string EntityName => "Payable";

    [HttpPost("{id:long}/pay")]
    [Authorize(Roles = "Admin,Accountant")]
    public async Task<ActionResult<ApiResponse<PayableDto>>> PayPayment(long id, [FromBody] PayPaymentRequest request)
    {
        Logger.LogInformation("[PayablesController.PayPayment] PayableId={PayableId}, Amount={Amount}, PaymentDate={PaymentDate}",
            id, request.Amount, request.PaymentDate);

        var result = await _payableService.PayPaymentAsync(id, request);
        return Ok(ApiResponse<PayableDto>.SuccessResponse(result, "付款登记成功"));
    }

    [HttpGet("summary")]
    [Authorize(Roles = "Admin,Accountant,Viewer")]
    public async Task<ActionResult<ApiResponse<PayableSummaryDto>>> GetSummary()
    {
        Logger.LogInformation("[PayablesController.GetSummary]");

        var result = await _payableService.GetPayableSummaryAsync();
        return Ok(ApiResponse<PayableSummaryDto>.SuccessResponse(result));
    }

    [HttpGet("trend")]
    [Authorize(Roles = "Admin,Accountant,Viewer")]
    public async Task<ActionResult<ApiResponse<PayableTrendDto>>> GetTrend(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate)
    {
        Logger.LogInformation("[PayablesController.GetTrend] StartDate={StartDate}, EndDate={EndDate}", startDate, endDate);

        var result = await _payableService.GetTrendAsync(startDate, endDate);
        return Ok(ApiResponse<PayableTrendDto>.SuccessResponse(result));
    }

    [HttpGet("aging")]
    [Authorize(Roles = "Admin,Accountant,Viewer")]
    public async Task<ActionResult<ApiResponse<PayableAgingDto>>> GetAging()
    {
        Logger.LogInformation("[PayablesController.GetAging]");

        var result = await _payableService.GetAgingAsync();
        return Ok(ApiResponse<PayableAgingDto>.SuccessResponse(result));
    }

    [HttpGet("customer/{customerId:long}")]
    [Authorize(Roles = "Admin,Accountant,Viewer")]
    public async Task<ActionResult<ApiResponse<List<PayableDto>>>> GetByCustomer(long customerId)
    {
        Logger.LogInformation("[PayablesController.GetByCustomer] CustomerId={CustomerId}", customerId);
        var result = await _payableService.GetByCustomerIdAsync(customerId);
        return Ok(ApiResponse<List<PayableDto>>.SuccessResponse(result));
    }

    [HttpGet("supplier/{supplierId:long}")]
    [Authorize(Roles = "Admin,Accountant,Viewer")]
    public async Task<ActionResult<ApiResponse<List<PayableDto>>>> GetBySupplier(long supplierId)
    {
        Logger.LogInformation("[PayablesController.GetBySupplier] SupplierId={SupplierId}", supplierId);
        var result = await _payableService.GetBySupplierIdAsync(supplierId);
        return Ok(ApiResponse<List<PayableDto>>.SuccessResponse(result));
    }

    [HttpGet("person/{personId:long}")]
    [Authorize(Roles = "Admin,Accountant,Viewer")]
    public async Task<ActionResult<ApiResponse<List<PayableDto>>>> GetByPerson(long personId)
    {
        Logger.LogInformation("[PayablesController.GetByPerson] PersonId={PersonId}", personId);
        var result = await _payableService.GetByPersonIdAsync(personId);
        return Ok(ApiResponse<List<PayableDto>>.SuccessResponse(result));
    }

    [HttpGet("project/{projectId:long}")]
    [Authorize(Roles = "Admin,Accountant,Viewer")]
    public async Task<ActionResult<ApiResponse<List<PayableDto>>>> GetByProject(long projectId)
    {
        Logger.LogInformation("[PayablesController.GetByProject] ProjectId={ProjectId}", projectId);
        var result = await _payableService.GetByProjectIdAsync(projectId);
        return Ok(ApiResponse<List<PayableDto>>.SuccessResponse(result));
    }

    [HttpGet("available-for-transaction")]
    [Authorize(Roles = "Admin,Accountant,Viewer")]
    public async Task<ActionResult<ApiResponse<List<PayableDto>>>> GetAvailableForTransaction(
        [FromQuery] long transactionId,
        [FromQuery] string? keyword = null)
    {
        Logger.LogInformation(
            "[PayablesController.GetAvailableForTransaction] TransactionId={TransactionId}, Keyword={Keyword}",
            transactionId, keyword);

        var result = await _settlementCandidateService.GetAvailablePayablesForTransactionAsync(transactionId, keyword);
        return Ok(ApiResponse<List<PayableDto>>.SuccessResponse(result));
    }

    [HttpGet("statistics")]
    [Authorize(Roles = "Admin,Accountant,Viewer")]
    public async Task<ActionResult<ApiResponse<PayableStatisticsDto>>> GetStatistics([FromQuery] PageRequest request)
    {
        Logger.LogInformation("[PayablesController.GetStatistics]");

        var result = await _payableService.GetStatisticsAsync(request);
        return Ok(ApiResponse<PayableStatisticsDto>.SuccessResponse(result));
    }
}
