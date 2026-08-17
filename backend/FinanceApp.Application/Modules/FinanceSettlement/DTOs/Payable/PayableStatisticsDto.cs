namespace FinanceApp.Application.Modules.FinanceSettlement.DTOs.Payable;

public class PayableStatisticsDto
{
    public int TotalCount { get; set; }
    public int PendingCount { get; set; }
    public int PartialCount { get; set; }
    public int SettledCount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal RemainingAmount { get; set; }
    public decimal OverdueAmount { get; set; }
}
