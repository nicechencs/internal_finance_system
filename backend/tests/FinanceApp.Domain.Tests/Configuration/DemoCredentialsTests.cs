using FluentAssertions;
using FinanceApp.Domain.Configuration;

namespace FinanceApp.Domain.Tests.Configuration;

public class DemoCredentialsTests
{
    [Theory]
    [InlineData("DemoOnly_ChangeMe!")]
    [InlineData("admin123456")]
    [InlineData("admin123")]
    public void IsPublishedDemoPassword_KnownPlaceholders_ReturnsTrue(string password)
    {
        DemoCredentials.IsPublishedDemoPassword(password).Should().BeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("UniqueProdPass_9xQ!")]
    public void IsPublishedDemoPassword_UniqueOrEmpty_ReturnsFalse(string? password)
    {
        DemoCredentials.IsPublishedDemoPassword(password).Should().BeFalse();
    }
}
