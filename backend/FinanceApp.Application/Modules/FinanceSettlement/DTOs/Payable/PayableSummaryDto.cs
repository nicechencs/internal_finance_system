namespace FinanceApp.Application.Modules.FinanceSettlement.DTOs.Payable;

public class PayableSummaryDto
{
    public decimal TotalPayable { get; set; }
    public decimal TotalPaid { get; set; }
    public decimal TotalRemaining { get; set; }
    public int PendingCount { get; set; }
    public int PartialCount { get; set; }
    public int SettledCount { get; set; }
    public int OverdueCount { get; set; }
}
