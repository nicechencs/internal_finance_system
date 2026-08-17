namespace FinanceApp.Application.Modules.Identity.DTOs;

public class LoginResponse
{
    public UserDto User { get; set; } = null!;
    public bool MustChangePassword { get; set; }
}
