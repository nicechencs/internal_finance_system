namespace FinanceApp.Application.Modules.Identity.DTOs;

public class SetUserPasswordRequest
{
    public string NewPassword { get; set; } = string.Empty;
    public bool MustChangePassword { get; set; }
}
