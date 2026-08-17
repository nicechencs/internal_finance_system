namespace FinanceApp.Application.Modules.Reporting.DTOs.Dashboard;

public class MonthlyStatsDto
{
    public string Month { get; set; } = string.Empty; // "2026-01"
    public decimal Income { get; set; }
    public decimal Expense { get; set; }
    public decimal Net { get; set; }
}
