namespace FinanceApp.Application.Modules.FinanceSettlement.DTOs.Payable;

public class PayableDetailDto
{
    public long Id { get; set; }
    public long PayableId { get; set; }
    public long TransactionId { get; set; }
    public DateTime PaymentDate { get; set; }
    public decimal Amount { get; set; }
    public string? PaymentMethod { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
}
