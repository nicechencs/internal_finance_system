using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FinanceApp.Application.Common;
using FinanceApp.Application.Modules.MasterData.DTOs.Account;
using FinanceApp.Application.Modules.MasterData.Interfaces;
using FinanceApp.Api.Controllers.Base;

namespace FinanceApp.Api.Controllers.MasterData;

[ApiController]
[Route("api/accounts")]
[Authorize]
public class AccountController : CrudControllerBase<AccountDto, CreateAccountRequest, UpdateAccountRequest>
{
    private readonly IAccountService _accountService;

    public AccountController(IAccountService accountService, ILogger<AccountController> logger)
        : base(accountService, logger)
    {
        _accountService = accountService;
    }

    protected override string ControllerName => "AccountController";
    protected override string EntityName => "Account";

    protected override string GetCreateSuccessMessage() => "账户创建成功";
    protected override string GetUpdateSuccessMessage() => "账户更新成功";
    protected override string GetDeleteSuccessMessage() => "账户删除成功";

    [HttpGet("active")]
    [Authorize(Roles = "Admin,Accountant,Viewer")]
    public async Task<ActionResult<ApiResponse<List<AccountDto>>>> GetActive()
    {
        Logger.LogInformation("[AccountController.GetActive]");

        var result = await _accountService.GetActiveAccountsAsync();
        return Ok(ApiResponse<List<AccountDto>>.SuccessResponse(result));
    }

    [HttpGet("maturing")]
    [Authorize(Roles = "Admin,Accountant,Viewer")]
    public async Task<ActionResult<ApiResponse<List<AccountDto>>>> GetMaturing([FromQuery] int days = 30)
    {
        Logger.LogInformation("[AccountController.GetMaturing] Days={Days}", days);

        var result = await _accountService.GetMaturingAccountsAsync(days);
        return Ok(ApiResponse<List<AccountDto>>.SuccessResponse(result));
    }

    [HttpGet("{id:long}/balance-trend")]
    [Authorize(Roles = "Admin,Accountant,Viewer")]
    public async Task<ActionResult<ApiResponse<BalanceTrendResponse>>> GetBalanceTrend(long id, [FromQuery] int months = 6)
    {
        Logger.LogInformation("[AccountController.GetBalanceTrend] AccountId={AccountId}, Months={Months}", id, months);

        var result = await _accountService.GetBalanceTrendAsync(id, months);
        return Ok(ApiResponse<BalanceTrendResponse>.SuccessResponse(result));
    }

    [HttpGet("statistics")]
    [Authorize(Roles = "Admin,Accountant,Viewer")]
    public async Task<ActionResult<ApiResponse<AccountStatisticsDto>>> GetStatistics()
    {
        Logger.LogInformation("[AccountController.GetStatistics]");

        var result = await _accountService.GetStatisticsAsync();
        return Ok(ApiResponse<AccountStatisticsDto>.SuccessResponse(result));
    }
}
