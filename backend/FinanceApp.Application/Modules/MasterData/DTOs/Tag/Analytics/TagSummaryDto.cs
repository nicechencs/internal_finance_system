namespace FinanceApp.Application.Modules.MasterData.DTOs.Tag.Analytics;

public class TagSummaryDto
{
    public string Scope { get; set; } = string.Empty;
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
    public int TotalTransactionCount { get; set; }
    public decimal TotalIncomeAmount { get; set; }
    public decimal TotalExpenseAmount { get; set; }
    public decimal TotalNetAmount { get; set; }
    public List<TagSummaryItemDto> Items { get; set; } = new();
}
