namespace FinanceApp.Application.Modules.Reporting.Models;

public class ProjectFinancialSummary
{
    public long ProjectId { get; set; }
    public decimal ContractAmount { get; set; }
    public decimal ReceivedAmount { get; set; }
    public decimal ReceivableAmount { get; set; }
    public decimal DirectCost { get; set; }
    public decimal AllocatedCost { get; set; }
    public decimal TotalCost { get; set; }
    public decimal ProfitAmount { get; set; }
    public decimal ProfitRate { get; set; }
}
