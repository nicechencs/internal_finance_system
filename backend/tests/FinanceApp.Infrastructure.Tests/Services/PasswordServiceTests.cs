using FluentAssertions;
using FinanceApp.Infrastructure.Services;

namespace FinanceApp.Infrastructure.Tests.Services;

public class PasswordServiceTests
{
    private readonly PasswordService _passwordService = new();

    [Fact]
    public void HashPassword_AndVerifyPassword_ShouldReturnTrue()
    {
        var password = "VeryStrongPassword123!";

        var hash = _passwordService.HashPassword(password);
        var result = _passwordService.VerifyPassword(password, hash);

        result.Should().BeTrue();
    }

    [Fact]
    public void VerifyPassword_WithWrongPassword_ShouldReturnFalse()
    {
        var hash = _passwordService.HashPassword("CorrectPassword123!");

        var result = _passwordService.VerifyPassword("WrongPassword123!", hash);

        result.Should().BeFalse();
    }

    [Fact]
    public void VerifyAgainstDummyHash_ShouldNotThrow()
    {
        var result = _passwordService.VerifyAgainstDummyHash("AnyPassword123!");

        result.Should().BeFalse();
    }
}
