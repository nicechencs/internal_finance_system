using FinanceApp.Application.Modules.MasterData.DTOs.Tag;

namespace FinanceApp.Application.Modules.MasterData.DTOs.Customer;

public class CustomerDto
{
    public long Id { get; set; }
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
    public bool IsActive { get; set; }
    public List<TagItemDto> Tags { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
