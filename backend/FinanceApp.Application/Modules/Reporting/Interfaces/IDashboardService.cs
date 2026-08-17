using FinanceApp.Application.Modules.Reporting.DTOs.Dashboard;

namespace FinanceApp.Application.Modules.Reporting.Interfaces;

public interface IDashboardService
{
    Task<DashboardSummaryDto> GetSummaryAsync();
    Task<List<MonthlyStatsDto>> GetMonthlyStatsAsync(int months = 12);
    Task<List<CategoryStatsDto>> GetExpenseByCategoryAsync(DateTime? startDate, DateTime? endDate);
    Task<List<CategoryStatsDto>> GetIncomeByCategoryAsync(DateTime? startDate, DateTime? endDate);
    Task<List<RecentTransactionDto>> GetRecentTransactionsAsync(int count = 10);
}
