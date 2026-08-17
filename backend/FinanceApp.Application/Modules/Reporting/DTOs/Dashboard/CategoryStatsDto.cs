namespace FinanceApp.Application.Modules.Reporting.DTOs.Dashboard;

public class CategoryStatsDto
{
    public string CategoryName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal Percentage { get; set; }
}
