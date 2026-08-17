using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FinanceApp.Application.Common;
using FinanceApp.Application.Modules.MasterData.DTOs.FixedDeposit;
using FinanceApp.Application.Modules.MasterData.Interfaces;
using FinanceApp.Application.Modules.TransactionProcessing.DTOs;
using FinanceApp.Api.Controllers;

namespace FinanceApp.Api.Controllers.MasterData;

[ApiController]
[Route("api/fixed-deposits")]
[Authorize]
public class FixedDepositsController : BaseApiController
{
    private readonly IFixedDepositService _fixedDepositService;
    private readonly ILogger<FixedDepositsController> _logger;

    public FixedDepositsController(IFixedDepositService fixedDepositService, ILogger<FixedDepositsController> logger)
    {
        _fixedDepositService = fixedDepositService;
        _logger = logger;
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Accountant")]
    public async Task<ActionResult<ApiResponse<FixedDepositDto>>> Create([FromBody] CreateFixedDepositRequest request)
    {
        _logger.LogInformation("[FixedDepositsController.Create] AccountId={AccountId}, Principal={Principal}, TermMonths={TermMonths}",
            request.AccountId, request.Principal, request.TermMonths);

        var result = await _fixedDepositService.CreateAsync(request);
        return Ok(ApiResponse<FixedDepositDto>.SuccessResponse(result, "定期存款创建成功"));
    }

    [HttpPut("{id:long}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<FixedDepositDto>>> Update(long id, [FromBody] UpdateFixedDepositRequest request)
    {
        _logger.LogInformation("[FixedDepositsController.Update] Id={Id}, AccountId={AccountId}, Principal={Principal}, TermMonths={TermMonths}",
            id, request.AccountId, request.Principal, request.TermMonths);

        var result = await _fixedDepositService.UpdateAsync(id, request);
        return Ok(ApiResponse<FixedDepositDto>.SuccessResponse(result, "定期存款更新成功"));
    }

    [HttpGet]
    [Authorize(Roles = "Admin,Accountant,Viewer")]
    public async Task<ActionResult<ApiResponse<List<FixedDepositDto>>>> GetAll([FromQuery] GetFixedDepositsRequest request)
    {
        _logger.LogInformation("[FixedDepositsController.GetAll] AccountIds={AccountIds}, Status={Status}, IncludeWithdrawn={IncludeWithdrawn}",
            request.AccountIds != null ? string.Join(",", request.AccountIds) : "null",
            request.Status ?? "null",
            request.IncludeWithdrawn);

        var result = await _fixedDepositService.GetAllAsync(request);
        return Ok(ApiResponse<List<FixedDepositDto>>.SuccessResponse(result));
    }

    [HttpGet("account/{accountId:long}")]
    [Authorize(Roles = "Admin,Accountant,Viewer")]
    public async Task<ActionResult<ApiResponse<List<FixedDepositDto>>>> GetByAccount(long accountId)
    {
        _logger.LogInformation("[FixedDepositsController.GetByAccount] AccountId={AccountId}", accountId);

        var result = await _fixedDepositService.GetByAccountAsync(accountId);
        return Ok(ApiResponse<List<FixedDepositDto>>.SuccessResponse(result));
    }

    [HttpGet("{id:long}")]
    [Authorize(Roles = "Admin,Accountant,Viewer")]
    public async Task<ActionResult<ApiResponse<FixedDepositDto>>> GetById(long id)
    {
        _logger.LogInformation("[FixedDepositsController.GetById] Id={Id}", id);

        var result = await _fixedDepositService.GetByIdAsync(id);
        return Ok(ApiResponse<FixedDepositDto>.SuccessResponse(result));
    }

    [HttpPost("{id:long}/withdraw")]
    [Authorize(Roles = "Admin,Accountant")]
    public async Task<ActionResult<ApiResponse<FixedDepositDto>>> Withdraw(long id, [FromBody] WithdrawFixedDepositRequest request)
    {
        _logger.LogInformation("[FixedDepositsController.Withdraw] Id={Id}, WithdrawalDate={WithdrawalDate}, ActualInterest={ActualInterest}",
            id, request.WithdrawalDate, request.ActualInterest);

        var result = await _fixedDepositService.WithdrawAsync(id, request);
        return Ok(ApiResponse<FixedDepositDto>.SuccessResponse(result, "定期存款支取成功"));
    }

    [HttpGet("maturing")]
    [Authorize(Roles = "Admin,Accountant,Viewer")]
    public async Task<ActionResult<ApiResponse<List<FixedDepositDto>>>> GetMaturing([FromQuery] int days = 30)
    {
        _logger.LogInformation("[FixedDepositsController.GetMaturing] Days={Days}", days);

        var result = await _fixedDepositService.GetMaturingAsync(days);
        return Ok(ApiResponse<List<FixedDepositDto>>.SuccessResponse(result));
    }

    [HttpGet("statistics")]
    [Authorize(Roles = "Admin,Accountant,Viewer")]
    public async Task<ActionResult<ApiResponse<FixedDepositStatisticsDto>>> GetStatistics([FromQuery] GetFixedDepositsRequest request)
    {
        _logger.LogInformation("[FixedDepositsController.GetStatistics] AccountIds={AccountIds}, Status={Status}",
            request.AccountIds != null ? string.Join(",", request.AccountIds) : "全部",
            request.Status ?? "全部");

        var result = await _fixedDepositService.GetStatisticsAsync(request);
        return Ok(ApiResponse<FixedDepositStatisticsDto>.SuccessResponse(result));
    }

    [HttpDelete("{id:long}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(long id)
    {
        _logger.LogInformation("[FixedDepositsController.Delete] Id={Id}", id);

        await _fixedDepositService.DeleteAsync(id);
        return Ok(ApiResponse<object>.SuccessResponse(null, "删除定期存款成功"));
    }

    [HttpGet("{id:long}/withdrawal-candidates")]
    [Authorize(Roles = "Admin,Accountant")]
    public async Task<ActionResult<ApiResponse<List<TransactionDto>>>> GetWithdrawalCandidates(long id)
    {
        _logger.LogInformation("[FixedDepositsController.GetWithdrawalCandidates] Id={Id}", id);

        var result = await _fixedDepositService.GetWithdrawalCandidatesAsync(id);
        return Ok(ApiResponse<List<TransactionDto>>.SuccessResponse(result));
    }
}
