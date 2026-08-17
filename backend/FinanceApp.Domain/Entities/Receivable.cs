using FinanceApp.Domain.Enums;
using FinanceApp.Domain.Interfaces;

namespace FinanceApp.Domain.Entities;

public class Receivable : BaseEntity, IConcurrencyVersioned
{
    /// <summary>乐观并发版本号（由 AppDbContext 自动维护）</summary>
    public long Version { get; set; }

    public long ProjectId { get; set; }
    public long? CustomerId { get; set; }
    public long? SupplierId { get; set; }
    public long? PersonId { get; set; }
    public long? ReceivableTypeId { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal ReceivedAmount { get; set; }
    public decimal RemainingAmount { get; set; }
    public DateTime? DueDate { get; set; }
    public ReceivableStatus Status { get; set; } = ReceivableStatus.Pending;
    public string? Description { get; set; }
    public DateTime? SettledAt { get; set; }

    // Navigation properties
    public Project Project { get; set; } = null!;
    public Customer? Customer { get; set; }
    public Supplier? Supplier { get; set; }
    public Person? Person { get; set; }
    public ReceivableType? ReceivableType { get; set; }
    public ICollection<ReceivableDetail> Details { get; set; } = new List<ReceivableDetail>();
    public ICollection<TagBinding> TagBindings { get; set; } = new List<TagBinding>();
}
