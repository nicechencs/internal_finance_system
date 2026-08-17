using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FinanceApp.Application.Common;
using FinanceApp.Application.Modules.Reporting.DTOs.Report;
using FinanceApp.Application.Modules.Reporting.Interfaces;

using FinanceApp.Api.Controllers;

namespace FinanceApp.Api.Controllers.Reporting;

[ApiController]
[Route("api/reports")]
[Authorize]
public class ReportController : BaseApiController
{
    private readonly IReportService _reportService;
    private readonly ILogger<ReportController> _logger;

    public ReportController(IReportService reportService, ILogger<ReportController> logger)
    {
        _reportService = reportService;
        _logger = logger;
    }

    [HttpGet("monthly-profit")]
    [Authorize(Roles = "Admin,Accountant,Viewer")]
    public async Task<ActionResult<ApiResponse<MonthlyProfitReportDto>>> GetMonthlyProfitReport([FromQuery] int year, [FromQuery] int month)
    {
        _logger.LogInformation("[ReportController.GetMonthlyProfitReport] Year={Year}, Month={Month}", year, month);

        var result = await _reportService.GetMonthlyProfitReportAsync(year, month);
        return Ok(ApiResponse<MonthlyProfitReportDto>.SuccessResponse(result));
    }

    [HttpGet("cashflow")]
    [Authorize(Roles = "Admin,Accountant,Viewer")]
    public async Task<ActionResult<ApiResponse<CashflowReportDto>>> GetCashflowReport([FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
    {
        _logger.LogInformation("[ReportController.GetCashflowReport] StartDate={StartDate}, EndDate={EndDate}",
            startDate.ToString("yyyy-MM-dd"), endDate.ToString("yyyy-MM-dd"));

        var result = await _reportService.GetCashflowReportAsync(startDate, endDate);
        return Ok(ApiResponse<CashflowReportDto>.SuccessResponse(result));
    }

    [HttpGet("project-profit")]
    [Authorize(Roles = "Admin,Accountant,Viewer")]
    public async Task<ActionResult<ApiResponse<ProjectProfitReportDto>>> GetProjectProfitReport()
    {
        _logger.LogInformation("[ReportController.GetProjectProfitReport]");

        var result = await _reportService.GetProjectProfitReportAsync();
        return Ok(ApiResponse<ProjectProfitReportDto>.SuccessResponse(result));
    }

    [HttpGet("person-cost")]
    [Authorize(Roles = "Admin,Accountant,Viewer")]
    public async Task<ActionResult<ApiResponse<PersonCostReportDto>>> GetPersonCostReport()
    {
        _logger.LogInformation("[ReportController.GetPersonCostReport]");

        var result = await _reportService.GetPersonCostReportAsync();
        return Ok(ApiResponse<PersonCostReportDto>.SuccessResponse(result));
    }

    [HttpGet("supplier-expense")]
    [Authorize(Roles = "Admin,Accountant,Viewer")]
    public async Task<ActionResult<ApiResponse<SupplierExpenseReportDto>>> GetSupplierExpenseReport()
    {
        _logger.LogInformation("[ReportController.GetSupplierExpenseReport]");

        var result = await _reportService.GetSupplierExpenseReportAsync();
        return Ok(ApiResponse<SupplierExpenseReportDto>.SuccessResponse(result));
    }

    [HttpGet("annual-overview")]
    [Authorize(Roles = "Admin,Accountant,Viewer")]
    public async Task<ActionResult<ApiResponse<AnnualOverviewReportDto>>> GetAnnualOverviewReport([FromQuery] int year)
    {
        _logger.LogInformation("[ReportController.GetAnnualOverviewReport] Year={Year}", year);

        var result = await _reportService.GetAnnualOverviewReportAsync(year);
        return Ok(ApiResponse<AnnualOverviewReportDto>.SuccessResponse(result));
    }
}
