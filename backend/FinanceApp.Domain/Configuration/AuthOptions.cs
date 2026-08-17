namespace FinanceApp.Domain.Configuration;

public class AuthOptions
{
    public const string SectionName = "Auth";

    public string CookieName { get; set; } = "finance_auth";
    public string CookieSecurePolicy { get; set; } = "SameAsRequest";
    public int CookieExpirationHours { get; set; } = 12;
    public int MinPasswordLength { get; set; } = 10;
    public int MaxPasswordLength { get; set; } = 128;
    public int MaxFailedAccessAttempts { get; set; } = 5;
    public int LockoutMinutes { get; set; } = 15;
    public int LoginRateLimitPermitLimit { get; set; } = 10;
    public int LoginRateLimitWindowSeconds { get; set; } = 60;
    public BootstrapAdminOptions BootstrapAdmin { get; set; } = new();
}

public class BootstrapAdminOptions
{
    public bool Enabled { get; set; }
    public string Username { get; set; } = "admin";
    public string Password { get; set; } = string.Empty;
    public string FullName { get; set; } = "System Administrator";
    public string? Email { get; set; }
}
