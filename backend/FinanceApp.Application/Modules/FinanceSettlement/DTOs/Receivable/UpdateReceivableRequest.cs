namespace FinanceApp.Application.Modules.FinanceSettlement.DTOs.Receivable;

public class UpdateReceivableRequest
{
    public long ProjectId { get; set; }
    public long? CustomerId { get; set; }
    public long? SupplierId { get; set; }
    public long? PersonId { get; set; }
    public long? ReceivableTypeId { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTime? DueDate { get; set; }
    public string? Description { get; set; }
}
