namespace FinanceApp.Application.Modules.Identity.DTOs;

public class AuthenticatedUserDto
{
    public long UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string SecurityStamp { get; set; } = string.Empty;
    public bool MustChangePassword { get; set; }
    public UserDto User { get; set; } = null!;
}
