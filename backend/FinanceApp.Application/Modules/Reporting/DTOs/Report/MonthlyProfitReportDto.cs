namespace FinanceApp.Application.Modules.Reporting.DTOs.Report;

public class MonthlyProfitReportDto
{
    public int Year { get; set; }
    public int Month { get; set; }
    public decimal TotalIncome { get; set; }
    public decimal TotalExpense { get; set; }
    public decimal NetProfit { get; set; }
    public decimal ProfitRate { get; set; }
    public List<CategoryAmountDto> IncomeByCategory { get; set; } = new();
    public List<CategoryAmountDto> ExpenseByCategory { get; set; } = new();
}

public class CategoryAmountDto
{
    public string CategoryName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}
