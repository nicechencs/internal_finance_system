namespace FinanceApp.Application.Modules.Reporting.DTOs.Report;

public class AnnualOverviewReportDto
{
    public int Year { get; set; }
    public decimal TotalIncome { get; set; }
    public decimal TotalExpense { get; set; }
    public decimal NetProfit { get; set; }
    public decimal ProfitRate { get; set; }
    public decimal TotalReceivable { get; set; }
    public decimal TotalPayable { get; set; }
    public List<MonthlyTrendDto> MonthlyTrend { get; set; } = new();
    public List<TopItemDto> TopProjects { get; set; } = new();
    public List<TopItemDto> TopCustomers { get; set; } = new();
    public List<TopItemDto> TopSuppliers { get; set; } = new();
}

public class MonthlyTrendDto
{
    public int Month { get; set; }
    public decimal Income { get; set; }
    public decimal Expense { get; set; }
    public decimal Profit { get; set; }
}

public class TopItemDto
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}
