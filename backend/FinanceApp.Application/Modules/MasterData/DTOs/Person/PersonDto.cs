using FinanceApp.Application.Modules.MasterData.DTOs.Tag;

namespace FinanceApp.Application.Modules.MasterData.DTOs.Person;

public class PersonDto
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string PersonType { get; set; } = string.Empty;
    public string? Department { get; set; }
    public string? Position { get; set; }
    public string? IdNumber { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? BankAccount { get; set; }
    public string? BankName { get; set; }
    public DateTime? JoinDate { get; set; }
    public DateTime? LeaveDate { get; set; }
    public bool IsActive { get; set; }
    public List<TagItemDto> Tags { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
