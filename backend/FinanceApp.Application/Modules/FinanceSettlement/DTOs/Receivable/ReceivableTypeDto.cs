namespace FinanceApp.Application.Modules.FinanceSettlement.DTOs.Receivable;

public class ReceivableTypeDto
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public int SortOrder { get; set; }
}
