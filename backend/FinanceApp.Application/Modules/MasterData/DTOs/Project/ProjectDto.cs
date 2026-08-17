using FinanceApp.Application.Modules.MasterData.DTOs.Tag;

namespace FinanceApp.Application.Modules.MasterData.DTOs.Project;

public class ProjectDto
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ProjectCode { get; set; } = string.Empty;
    public CustomerInfo Customer { get; set; } = new();
    public decimal ContractAmount { get; set; }
    public decimal ReceivedAmount { get; set; }
    public decimal ReceivableAmount { get; set; }
    public decimal TotalCost { get; set; }
    public decimal ProfitAmount { get; set; }
    public decimal ProfitRate { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? Description { get; set; }
    public List<CostBreakdownItem> CostBreakdown { get; set; } = new();
    public List<TagItemDto> Tags { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CustomerInfo
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class CostBreakdownItem
{
    public string CategoryName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}
