namespace FinanceApp.Application.Modules.FinanceSettlement.DTOs.Payable;

public class PayableTypeDto
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public int SortOrder { get; set; }
}
