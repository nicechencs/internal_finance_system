namespace FinanceApp.Domain.Entities;

public class PayableDetail : BaseEntity
{
    public long PayableId { get; set; }
    public long TransactionId { get; set; }
    public DateTime PaymentDate { get; set; }
    public decimal Amount { get; set; }
    public string? PaymentMethod { get; set; }
    public string? Description { get; set; }

    // Navigation properties
    public Payable Payable { get; set; } = null!;
    public Transaction? Transaction { get; set; }
}
