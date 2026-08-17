using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using FinanceApp.Application.Common;
using FinanceApp.Application.Modules.TransactionProcessing.DTOs;
using FinanceApp.Application.Modules.TransactionProcessing.Interfaces;
using FinanceApp.Api.Controllers.Base;

namespace FinanceApp.Api.Controllers.TransactionProcessing;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TransactionsController : CrudControllerBase<TransactionDto, CreateTransactionRequest, UpdateTransactionRequest>
{
    private readonly ITransactionService _transactionService;

    public TransactionsController(ITransactionService transactionService, ILogger<TransactionsController> logger)
        : base(transactionService, logger)
    {
        _transactionService = transactionService;
    }

    protected override string ControllerName => "TransactionsController";
    protected override string EntityName => "Transaction";

    [HttpGet("by-account/{accountId}")]
    [Authorize(Roles = "Admin,Accountant,Viewer")]
    public async Task<ActionResult<ApiResponse<List<TransactionDto>>>> GetByAccount(long accountId)
    {
        Logger.LogInformation("[TransactionsController.GetByAccount] AccountId={AccountId}", accountId);

        var result = await _transactionService.GetByAccountAsync(accountId);
        return Ok(ApiResponse<List<TransactionDto>>.SuccessResponse(result));
    }

    [HttpGet("by-project/{projectId}")]
    [Authorize(Roles = "Admin,Accountant,Viewer")]
    public async Task<ActionResult<ApiResponse<List<TransactionDto>>>> GetByProject(long projectId)
    {
        Logger.LogInformation("[TransactionsController.GetByProject] ProjectId={ProjectId}", projectId);

        var result = await _transactionService.GetByProjectAsync(projectId);
        return Ok(ApiResponse<List<TransactionDto>>.SuccessResponse(result));
    }

    [HttpGet("by-category/{categoryId}")]
    [Authorize(Roles = "Admin,Accountant,Viewer")]
    public async Task<ActionResult<ApiResponse<List<TransactionDto>>>> GetByCategory(long categoryId)
    {
        Logger.LogInformation("[TransactionsController.GetByCategory] CategoryId={CategoryId}", categoryId);

        var result = await _transactionService.GetByCategoryAsync(categoryId);
        return Ok(ApiResponse<List<TransactionDto>>.SuccessResponse(result));
    }

    [HttpGet("by-customer/{customerId}")]
    [Authorize(Roles = "Admin,Accountant,Viewer")]
    public async Task<ActionResult<ApiResponse<List<TransactionDto>>>> GetByCustomer(long customerId)
    {
        Logger.LogInformation("[TransactionsController.GetByCustomer] CustomerId={CustomerId}", customerId);

        var result = await _transactionService.GetByCustomerAsync(customerId);
        return Ok(ApiResponse<List<TransactionDto>>.SuccessResponse(result));
    }

    [HttpGet("by-supplier/{supplierId}")]
    [Authorize(Roles = "Admin,Accountant,Viewer")]
    public async Task<ActionResult<ApiResponse<List<TransactionDto>>>> GetBySupplier(long supplierId)
    {
        Logger.LogInformation("[TransactionsController.GetBySupplier] SupplierId={SupplierId}", supplierId);

        var result = await _transactionService.GetBySupplierAsync(supplierId);
        return Ok(ApiResponse<List<TransactionDto>>.SuccessResponse(result));
    }

    [HttpGet("by-person/{personId}")]
    [Authorize(Roles = "Admin,Accountant,Viewer")]
    public async Task<ActionResult<ApiResponse<List<TransactionDto>>>> GetByPerson(long personId)
    {
        Logger.LogInformation("[TransactionsController.GetByPerson] PersonId={PersonId}", personId);

        var result = await _transactionService.GetByPersonAsync(personId);
        return Ok(ApiResponse<List<TransactionDto>>.SuccessResponse(result));
    }

    [HttpGet("account-balance/{accountId}")]
    [Authorize(Roles = "Admin,Accountant,Viewer")]
    public async Task<ActionResult<ApiResponse<decimal>>> GetAccountBalance(long accountId)
    {
        Logger.LogInformation("[TransactionsController.GetAccountBalance] AccountId={AccountId}", accountId);

        var result = await _transactionService.GetAccountBalanceAsync(accountId);
        return Ok(ApiResponse<decimal>.SuccessResponse(result));
    }

    [HttpPost("transfer")]
    [Authorize(Roles = "Admin,Accountant")]
    public async Task<ActionResult<ApiResponse<TransferResultDto>>> CreateTransfer([FromBody] CreateTransferRequest request)
    {
        Logger.LogInformation("[TransactionsController.CreateTransfer] FromAccountId={FromAccountId}, ToAccountId={ToAccountId}, Amount={Amount}",
            request.FromAccountId, request.ToAccountId, request.Amount);

        var result = await _transactionService.CreateTransferAsync(request);
        return Ok(ApiResponse<TransferResultDto>.SuccessResponse(result, "转账成功"));
    }

    [HttpGet("{id:long}/transfer-candidates")]
    [Authorize(Roles = "Admin,Accountant")]
    public async Task<ActionResult<ApiResponse<List<TransactionDto>>>> GetTransferCandidates(long id, [FromQuery] long targetAccountId)
    {
        Logger.LogInformation("[TransactionsController.GetTransferCandidates] TransactionId={TransactionId}, TargetAccountId={TargetAccountId}",
            id, targetAccountId);

        var result = await _transactionService.GetTransferCandidatesAsync(id, targetAccountId);
        return Ok(ApiResponse<List<TransactionDto>>.SuccessResponse(result));
    }

    [HttpPost("{id:long}/convert-to-transfer")]
    [Authorize(Roles = "Admin,Accountant")]
    public async Task<ActionResult<ApiResponse<TransferResultDto>>> ConvertToTransfer(long id, [FromBody] ConvertTransactionToTransferRequest request)
    {
        Logger.LogInformation("[TransactionsController.ConvertToTransfer] TransactionId={TransactionId}, TargetAccountId={TargetAccountId}, MatchedTransactionId={MatchedTransactionId}",
            id, request.TargetAccountId, request.MatchedTransactionId);

        var result = await _transactionService.ConvertToTransferAsync(id, request);
        return Ok(ApiResponse<TransferResultDto>.SuccessResponse(result, "识别转账成功"));
    }

    [HttpGet("statistics")]
    [Authorize(Roles = "Admin,Accountant,Viewer")]
    public async Task<ActionResult<ApiResponse<TransactionStatisticsDto>>> GetStatistics([FromQuery] PageRequest request)
    {
        Logger.LogInformation("[TransactionsController.GetStatistics] 获取交易统计数据");

        var result = await _transactionService.GetStatisticsAsync(request);
        return Ok(ApiResponse<TransactionStatisticsDto>.SuccessResponse(result));
    }

    [HttpGet("account/{accountId}/statistics")]
    [Authorize(Roles = "Admin,Accountant,Viewer")]
    public async Task<ActionResult<ApiResponse<TransactionStatisticsDto>>> GetAccountStatistics(long accountId)
    {
        Logger.LogInformation("[TransactionsController.GetAccountStatistics] 获取账户交易统计数据: AccountId={AccountId}", accountId);

        var result = await _transactionService.GetAccountStatisticsAsync(accountId);
        return Ok(ApiResponse<TransactionStatisticsDto>.SuccessResponse(result));
    }

    [HttpGet("customer/{customerId}/statistics")]
    [Authorize(Roles = "Admin,Accountant,Viewer")]
    public async Task<ActionResult<ApiResponse<TransactionStatisticsDto>>> GetCustomerStatistics(long customerId)
    {
        Logger.LogInformation("[TransactionsController.GetCustomerStatistics] 获取客户交易统计数据: CustomerId={CustomerId}", customerId);

        var result = await _transactionService.GetCustomerStatisticsAsync(customerId);
        return Ok(ApiResponse<TransactionStatisticsDto>.SuccessResponse(result));
    }

    [HttpGet("supplier/{supplierId}/statistics")]
    [Authorize(Roles = "Admin,Accountant,Viewer")]
    public async Task<ActionResult<ApiResponse<TransactionStatisticsDto>>> GetSupplierStatistics(long supplierId)
    {
        Logger.LogInformation("[TransactionsController.GetSupplierStatistics] 获取供应商交易统计数据: SupplierId={SupplierId}", supplierId);

        var result = await _transactionService.GetSupplierStatisticsAsync(supplierId);
        return Ok(ApiResponse<TransactionStatisticsDto>.SuccessResponse(result));
    }

    [HttpGet("person/{personId}/statistics")]
    [Authorize(Roles = "Admin,Accountant,Viewer")]
    public async Task<ActionResult<ApiResponse<TransactionStatisticsDto>>> GetPersonStatistics(long personId)
    {
        Logger.LogInformation("[TransactionsController.GetPersonStatistics] 获取人员交易统计数据: PersonId={PersonId}", personId);

        var result = await _transactionService.GetPersonStatisticsAsync(personId);
        return Ok(ApiResponse<TransactionStatisticsDto>.SuccessResponse(result));
    }

    [HttpGet("{id:long}/related")]
    [Authorize(Roles = "Admin,Accountant,Viewer")]
    public async Task<ActionResult<ApiResponse<RelatedFinanceRecordDto>>> GetRelatedFinanceRecords(long id)
    {
        Logger.LogInformation("[TransactionsController.GetRelatedFinanceRecords] TransactionId={TransactionId}", id);

        var result = await _transactionService.GetRelatedFinanceRecordsAsync(id);
        return Ok(ApiResponse<RelatedFinanceRecordDto>.SuccessResponse(result));
    }

    [HttpGet("available-for-receivable")]
    [Authorize(Roles = "Admin,Accountant,Viewer")]
    public async Task<ActionResult<ApiResponse<List<TransactionDto>>>> GetAvailableForReceivable(
        [FromQuery] long? projectId = null,
        [FromQuery] long? customerId = null,
        [FromQuery] long? supplierId = null,
        [FromQuery] long? personId = null,
        [FromQuery] bool showAll = false,
        [FromQuery] string? keyword = null)
    {
        Logger.LogInformation("[TransactionsController.GetAvailableForReceivable] ProjectId={ProjectId}, CustomerId={CustomerId}, SupplierId={SupplierId}, PersonId={PersonId}, ShowAll={ShowAll}, Keyword={Keyword}",
            projectId, customerId, supplierId, personId, showAll, keyword);

        var transactions = await _transactionService.GetAvailableForReceivableAsync(projectId, customerId, supplierId, personId, showAll, keyword);
        return Ok(ApiResponse<List<TransactionDto>>.SuccessResponse(transactions));
    }

    [HttpGet("available-for-payable")]
    [Authorize(Roles = "Admin,Accountant,Viewer")]
    public async Task<ActionResult<ApiResponse<List<TransactionDto>>>> GetAvailableForPayable(
        [FromQuery] long? projectId = null,
        [FromQuery] long? supplierId = null,
        [FromQuery] long? customerId = null,
        [FromQuery] long? personId = null,
        [FromQuery] bool showAll = false,
        [FromQuery] string? keyword = null)
    {
        Logger.LogInformation("[TransactionsController.GetAvailableForPayable] ProjectId={ProjectId}, SupplierId={SupplierId}, CustomerId={CustomerId}, PersonId={PersonId}, ShowAll={ShowAll}, Keyword={Keyword}",
            projectId, supplierId, customerId, personId, showAll, keyword);

        var transactions = await _transactionService.GetAvailableForPayableAsync(projectId, supplierId, customerId, personId, showAll, keyword);
        return Ok(ApiResponse<List<TransactionDto>>.SuccessResponse(transactions));
    }
}
