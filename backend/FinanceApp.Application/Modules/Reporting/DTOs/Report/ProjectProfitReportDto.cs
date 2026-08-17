namespace FinanceApp.Application.Modules.Reporting.DTOs.Report;

public class ProjectProfitReportDto
{
    public List<ProjectProfitItemDto> Projects { get; set; } = new();
    public ProjectProfitSummaryDto Summary { get; set; } = new();
}

public class ProjectProfitItemDto
{
    public long ProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public decimal ContractAmount { get; set; }
    public decimal ReceivedAmount { get; set; }
    public decimal TotalCost { get; set; }
    public decimal ProfitAmount { get; set; }
    public decimal ProfitRate { get; set; }
}

public class ProjectProfitSummaryDto
{
    public decimal TotalContract { get; set; }
    public decimal TotalReceived { get; set; }
    public decimal TotalCost { get; set; }
    public decimal TotalProfit { get; set; }
    public decimal AvgProfitRate { get; set; }
}
