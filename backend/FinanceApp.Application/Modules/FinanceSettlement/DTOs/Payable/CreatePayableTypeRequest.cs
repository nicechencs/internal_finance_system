namespace FinanceApp.Application.Modules.FinanceSettlement.DTOs.Payable;

public class CreatePayableTypeRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
    public string? Description { get; set; }
    public int SortOrder { get; set; }
}
