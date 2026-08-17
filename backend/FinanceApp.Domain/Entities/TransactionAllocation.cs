namespace FinanceApp.Domain.Entities;

public class TransactionAllocation : BaseEntity
{
    public long TransactionId { get; set; }
    public long? ProjectId { get; set; }
    public long? PersonId { get; set; }
    public decimal Amount { get; set; }
    public decimal? AllocationRate { get; set; }
    public string? Description { get; set; }

    // Navigation properties
    public Transaction Transaction { get; set; } = null!;
    public Project? Project { get; set; }
    public Person? Person { get; set; }
}
