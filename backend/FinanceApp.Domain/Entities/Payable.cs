using FinanceApp.Domain.Enums;
using FinanceApp.Domain.Interfaces;

namespace FinanceApp.Domain.Entities;

public class Payable : BaseEntity, IConcurrencyVersioned
{
    /// <summary>乐观并发版本号（由 AppDbContext 自动维护）</summary>
    public long Version { get; set; }

    public long? SupplierId { get; set; }
    public long? CustomerId { get; set; }
    public long? PersonId { get; set; }
    public long? ProjectId { get; set; }
    public long? PayableTypeId { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal RemainingAmount { get; set; }
    public DateTime? DueDate { get; set; }
    public PayableStatus Status { get; set; } = PayableStatus.Pending;
    public string? Description { get; set; }
    public DateTime? SettledAt { get; set; }

    // Navigation properties
    public Supplier? Supplier { get; set; }
    public Customer? Customer { get; set; }
    public Person? Person { get; set; }
    public Project? Project { get; set; }
    public PayableType? PayableType { get; set; }
    public ICollection<PayableDetail> Details { get; set; } = new List<PayableDetail>();
    public ICollection<TagBinding> TagBindings { get; set; } = new List<TagBinding>();
}
