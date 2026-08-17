namespace FinanceApp.Application.Modules.MasterData.DTOs.Tag.Analytics;

public class TagCrossAnalysisCellDto
{
    public long RowTagId { get; set; }
    public long ColTagId { get; set; }
    public int TransactionCount { get; set; }
    public decimal IncomeAmount { get; set; }
    public decimal ExpenseAmount { get; set; }
    public decimal NetAmount { get; set; }
}
