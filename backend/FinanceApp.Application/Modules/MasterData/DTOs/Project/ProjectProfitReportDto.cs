namespace FinanceApp.Application.Modules.MasterData.DTOs.Project;

public class ProjectProfitReportDto
{
    public long ProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public string? ProjectCode { get; set; }
    public string? CustomerName { get; set; }
    public decimal ContractAmount { get; set; }
    public decimal ReceivedAmount { get; set; }
    public decimal ReceivableAmount { get; set; }
    public decimal TotalCost { get; set; }
    public decimal DirectCost { get; set; }
    public decimal AllocatedCost { get; set; }
    public decimal ProfitAmount { get; set; }
    public decimal ProfitRate { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}
