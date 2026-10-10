namespace FinanceApp.Application.Modules.MasterData.DTOs.Tag.Analytics;

public class TagDailySummaryDto
{
    public long Id { get; set; }
    public DateTime SummaryDate { get; set; }
    public long TagId { get; set; }
    public string TagName { get; set; } = string.Empty;
    public string? TagColor { get; set; }
    public string MetricScope { get; set; } = string.Empty;
    public decimal IncomeAmount { get; set; }
    public decimal ExpenseAmount { get; set; }
    public decimal NetAmount { get; set; }
    public int TransactionCount { get; set; }
    public int Version { get; set; }
}

public class TagDailySummaryRebuildResult
{
    public DateTime SummaryDate { get; set; }
    public int RowCount { get; set; }
    public int Version { get; set; }
}
