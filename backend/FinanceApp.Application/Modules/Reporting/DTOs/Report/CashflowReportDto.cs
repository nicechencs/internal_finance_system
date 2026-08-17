namespace FinanceApp.Application.Modules.Reporting.DTOs.Report;

public class CashflowReportDto
{
    public string StartDate { get; set; } = string.Empty;
    public string EndDate { get; set; } = string.Empty;
    public decimal OpeningBalance { get; set; }
    public decimal TotalIncome { get; set; }
    public decimal TotalExpense { get; set; }
    public decimal ClosingBalance { get; set; }
    public List<MonthlyDetailDto> MonthlyDetail { get; set; } = new();
}

public class MonthlyDetailDto
{
    public string Month { get; set; } = string.Empty;
    public decimal OpeningBalance { get; set; }
    public decimal Income { get; set; }
    public decimal Expense { get; set; }
    public decimal ClosingBalance { get; set; }
}
