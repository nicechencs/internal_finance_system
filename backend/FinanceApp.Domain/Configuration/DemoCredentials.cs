namespace FinanceApp.Domain.Configuration;

/// <summary>
/// Published local-demo credentials. Allowed only in Development.
/// Production/testing bootstrap must use a unique password.
/// </summary>
public static class DemoCredentials
{
    public const string Username = "admin";
    public const string Password = "DemoOnly_ChangeMe!";

    private static readonly string[] PublishedPasswords =
    [
        Password,
        "admin123456",
        "admin123"
    ];

    public static bool IsPublishedDemoPassword(string? password)
    {
        return !string.IsNullOrEmpty(password)
               && PublishedPasswords.Contains(password, StringComparer.Ordinal);
    }
}
