namespace FinanceApp.Application.Modules.Identity.DTOs;

public class UpdateProfileRequest
{
    public string FullName { get; set; } = string.Empty;
    public string? Email { get; set; }
}
