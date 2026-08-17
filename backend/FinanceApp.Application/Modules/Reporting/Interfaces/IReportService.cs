using FinanceApp.Application.Modules.Reporting.DTOs.Report;

namespace FinanceApp.Application.Modules.Reporting.Interfaces;

public interface IReportService
{
    Task<MonthlyProfitReportDto> GetMonthlyProfitReportAsync(int year, int month);
    Task<CashflowReportDto> GetCashflowReportAsync(DateTime startDate, DateTime endDate);
    Task<ProjectProfitReportDto> GetProjectProfitReportAsync();
    Task<PersonCostReportDto> GetPersonCostReportAsync();
    Task<SupplierExpenseReportDto> GetSupplierExpenseReportAsync();
    Task<AnnualOverviewReportDto> GetAnnualOverviewReportAsync(int year);
}
