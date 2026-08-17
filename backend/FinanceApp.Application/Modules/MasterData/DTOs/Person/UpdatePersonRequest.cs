namespace FinanceApp.Application.Modules.MasterData.DTOs.Person;

public class UpdatePersonRequest
{
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
    public bool IsActive { get; set; } = true;
}
