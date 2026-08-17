namespace FinanceApp.Application.Modules.Identity.DTOs;

public class CreateUserRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string Role { get; set; } = "Viewer";
    public bool IsActive { get; set; } = true;
    public bool MustChangePassword { get; set; }
}
