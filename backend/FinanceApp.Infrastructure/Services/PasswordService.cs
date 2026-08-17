using FinanceApp.Domain.Interfaces;

namespace FinanceApp.Infrastructure.Services;

public class PasswordService : IPasswordService
{
    private static readonly string DummyHash = BCrypt.Net.BCrypt.HashPassword("FinanceApp.Dummy.Password");

    public string HashPassword(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password);
    }

    public bool VerifyPassword(string password, string passwordHash)
    {
        try
        {
            return BCrypt.Net.BCrypt.Verify(password, passwordHash);
        }
        catch (BCrypt.Net.SaltParseException)
        {
            return false;
        }
    }

    public bool VerifyAgainstDummyHash(string password)
    {
        return BCrypt.Net.BCrypt.Verify(password, DummyHash);
    }
}
