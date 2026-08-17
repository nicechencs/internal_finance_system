namespace FinanceApp.Application.Modules.MasterData.DTOs.Project;

public class MonthlyProfitDto
{
    public string Month { get; set; } = string.Empty;
    public decimal Income { get; set; }
    public decimal Expense { get; set; }
    public decimal Profit { get; set; }
}

public class ExpenseCategoryDto
{
    public string CategoryName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal Percentage { get; set; }
}

public class ProfitAnalysisResponse
{
    public List<MonthlyProfitDto> MonthlyData { get; set; } = new();
    public List<ExpenseCategoryDto> ExpenseCategories { get; set; } = new();
    public decimal TotalIncome { get; set; }
    public decimal TotalExpense { get; set; }
    public decimal TotalProfit { get; set; }
}
