namespace FinanceApp.Domain.Entities;

public class ReceivableDetail : BaseEntity
{
    public long ReceivableId { get; set; }
    public long TransactionId { get; set; }
    public DateTime PaymentDate { get; set; }
    public decimal Amount { get; set; }
    public string? PaymentMethod { get; set; }
    public string? Description { get; set; }

    // Navigation properties
    public Receivable Receivable { get; set; } = null!;
    public Transaction? Transaction { get; set; }
}
