namespace FinanceApp.Application.Modules.Identity.DTOs;

public class UserAdminDto : UserDto
{
    public int AccessFailedCount { get; set; }
    public bool IsLocked { get; set; }
    public DateTime? LockoutEndAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public DateTime PasswordChangedAt { get; set; }
    public bool MustChangePassword { get; set; }
}
