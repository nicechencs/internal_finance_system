namespace FinanceApp.Application.Modules.FinanceSettlement.DTOs.Receivable;

public class ReceivableSummaryDto
{
    public decimal TotalReceivable { get; set; }
    public decimal TotalReceived { get; set; }
    public decimal TotalRemaining { get; set; }
    public int PendingCount { get; set; }
    public int PartialCount { get; set; }
    public int SettledCount { get; set; }
    public int OverdueCount { get; set; }
}
