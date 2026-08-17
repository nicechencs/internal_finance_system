namespace FinanceApp.Application.Modules.Identity.DTOs;

public class UpdateUserRequest
{
    public string FullName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string Role { get; set; } = "Viewer";
}
