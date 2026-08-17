using FinanceApp.Application.Modules.MasterData.DTOs.Tag;

namespace FinanceApp.Application.Modules.FinanceSettlement.DTOs.Payable;

public class PayableDto
{
    public long Id { get; set; }
    public long? SupplierId { get; set; }
    public string? SupplierName { get; set; }
    public long? CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public long? PersonId { get; set; }
    public string? PersonName { get; set; }
    public long? ProjectId { get; set; }
    public string? ProjectName { get; set; }
    public long? PayableTypeId { get; set; }
    public string? PayableTypeName { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal RemainingAmount { get; set; }
    public DateTime? DueDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime? SettledAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<PayableDetailDto> Details { get; set; } = new();
    public List<TagItemDto> Tags { get; set; } = new();
}
