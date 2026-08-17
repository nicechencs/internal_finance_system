namespace FinanceApp.Application.Modules.MasterData.DTOs.Supplier;

public class UpdateSupplierRequest
{
    public string Name { get; set; } = string.Empty;
    public string? ShortName { get; set; }
    public string? ContactPerson { get; set; }
    public string? ContactPhone { get; set; }
    public string? ContactEmail { get; set; }
    public string? Address { get; set; }
    public string? TaxNumber { get; set; }
    public string? BankAccount { get; set; }
    public string? BankName { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
}
