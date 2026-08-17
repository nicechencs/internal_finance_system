using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using FinanceApp.Application.Common;
using FinanceApp.Application.Modules.Reporting.DTOs.Dashboard;
using FinanceApp.Application.Modules.Reporting.Interfaces;

using FinanceApp.Api.Controllers;

namespace FinanceApp.Api.Controllers.Reporting;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DashboardController : BaseApiController
{
    private readonly IDashboardService _dashboardService;
    private readonly ILogger<DashboardController> _logger;

    public DashboardController(IDashboardService dashboardService, ILogger<DashboardController> logger)
    {
        _dashboardService = dashboardService;
        _logger = logger;
    }

    [HttpGet("summary")]
    [Authorize(Roles = "Admin,Accountant,Viewer")]
    public async Task<ActionResult<ApiResponse<DashboardSummaryDto>>> GetSummary()
    {
        _logger.LogInformation("[DashboardController.GetSummary]");

        var result = await _dashboardService.GetSummaryAsync();
        return Ok(ApiResponse<DashboardSummaryDto>.SuccessResponse(result));
    }

    [HttpGet("monthly-stats")]
    [Authorize(Roles = "Admin,Accountant,Viewer")]
    public async Task<ActionResult<ApiResponse<List<MonthlyStatsDto>>>> GetMonthlyStats([FromQuery] int months = 12)
    {
        _logger.LogInformation("[DashboardController.GetMonthlyStats] Months={Months}", months);

        var result = await _dashboardService.GetMonthlyStatsAsync(months);
        return Ok(ApiResponse<List<MonthlyStatsDto>>.SuccessResponse(result));
    }

    [HttpGet("expense-by-category")]
    [Authorize(Roles = "Admin,Accountant,Viewer")]
    public async Task<ActionResult<ApiResponse<List<CategoryStatsDto>>>> GetExpenseByCategory(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate)
    {
        _logger.LogInformation("[DashboardController.GetExpenseByCategory] StartDate={StartDate}, EndDate={EndDate}",
            startDate?.ToString("yyyy-MM-dd") ?? "null",
            endDate?.ToString("yyyy-MM-dd") ?? "null");

        var result = await _dashboardService.GetExpenseByCategoryAsync(startDate, endDate);
        return Ok(ApiResponse<List<CategoryStatsDto>>.SuccessResponse(result));
    }

    [HttpGet("income-by-category")]
    [Authorize(Roles = "Admin,Accountant,Viewer")]
    public async Task<ActionResult<ApiResponse<List<CategoryStatsDto>>>> GetIncomeByCategory(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate)
    {
        _logger.LogInformation("[DashboardController.GetIncomeByCategory] StartDate={StartDate}, EndDate={EndDate}",
            startDate?.ToString("yyyy-MM-dd") ?? "null",
            endDate?.ToString("yyyy-MM-dd") ?? "null");

        var result = await _dashboardService.GetIncomeByCategoryAsync(startDate, endDate);
        return Ok(ApiResponse<List<CategoryStatsDto>>.SuccessResponse(result));
    }

    [HttpGet("recent-transactions")]
    [Authorize(Roles = "Admin,Accountant,Viewer")]
    public async Task<ActionResult<ApiResponse<List<RecentTransactionDto>>>> GetRecentTransactions([FromQuery] int count = 10)
    {
        _logger.LogInformation("[DashboardController.GetRecentTransactions] Count={Count}", count);

        var result = await _dashboardService.GetRecentTransactionsAsync(count);
        return Ok(ApiResponse<List<RecentTransactionDto>>.SuccessResponse(result));
    }
}
