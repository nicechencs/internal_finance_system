namespace FinanceApp.Application.Modules.Reporting.DTOs.Dashboard;

public class DashboardSummaryDto
{
    public decimal TotalIncome { get; set; }
    public decimal TotalExpense { get; set; }
    public decimal NetProfit { get; set; }
    public decimal TotalBalance { get; set; }
    public int AccountCount { get; set; }
    public int TransactionCount { get; set; }
    public int ProjectCount { get; set; }
}
