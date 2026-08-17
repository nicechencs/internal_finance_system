using FinanceApp.Domain.Enums;

namespace FinanceApp.Domain.Entities;

public class Project : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? ProjectCode { get; set; }
    public long? CustomerId { get; set; }
    public decimal ContractAmount { get; set; }
    public decimal ReceivedAmount { get; set; }
    public decimal ReceivableAmount { get; set; }
    public decimal TotalCost { get; set; }
    public decimal ProfitAmount { get; set; }
    public decimal ProfitRate { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public ProjectStatus Status { get; set; } = ProjectStatus.Active;
    public string? Description { get; set; }

    // Navigation properties
    public Customer? Customer { get; set; }
    public ICollection<TagBinding> TagBindings { get; set; } = new List<TagBinding>();
}
