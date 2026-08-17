namespace FinanceApp.Application.Modules.FinanceSettlement.DTOs.Payable;

public class CreatePayableRequest
{
    public long? PayableTypeId { get; set; }
    public long? SupplierId { get; set; }
    public long? CustomerId { get; set; }
    public long? PersonId { get; set; }
    public long? ProjectId { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTime? DueDate { get; set; }
    public string? Description { get; set; }
}
