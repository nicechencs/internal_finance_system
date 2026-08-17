namespace FinanceApp.Application.Modules.FinanceSettlement.DTOs.Receivable;

public class CreateReceivableTypeRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }
}
