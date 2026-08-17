namespace FinanceApp.Application.Modules.MasterData.DTOs.Tag.Analytics;

public class TagSummaryItemDto
{
    public long TagId { get; set; }
    public string TagName { get; set; } = string.Empty;
    public string? TagColor { get; set; }
    public int TransactionCount { get; set; }
    public decimal IncomeAmount { get; set; }
    public decimal ExpenseAmount { get; set; }
    public decimal NetAmount { get; set; }
    /// <summary>占总收入百分比（0-100）</summary>
    public decimal IncomePercentage { get; set; }
    /// <summary>占总支出百分比（0-100）</summary>
    public decimal ExpensePercentage { get; set; }
}
