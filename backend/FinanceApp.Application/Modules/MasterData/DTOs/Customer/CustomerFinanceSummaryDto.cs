namespace FinanceApp.Application.Modules.MasterData.DTOs.Customer;

public class CustomerFinanceSummaryDto
{
    // Receivable
    public decimal TotalReceivable { get; set; }
    public decimal TotalReceived { get; set; }
    public decimal ReceivableRemaining { get; set; }
    public int ReceivableOverdueCount { get; set; }
    public decimal ReceivableOverdueAmount { get; set; }

    // Payable
    public decimal TotalPayable { get; set; }
    public decimal TotalPaid { get; set; }
    public decimal PayableRemaining { get; set; }
    public int PayableOverdueCount { get; set; }
    public decimal PayableOverdueAmount { get; set; }

    public int ProjectCount { get; set; }
}
