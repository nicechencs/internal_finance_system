using FluentAssertions;
using FinanceApp.Domain.Configuration;
using FinanceApp.Domain.Interfaces;
using FinanceApp.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace FinanceApp.Infrastructure.Tests.Data;

public class DbInitializerTests
{
    [Fact]
    public async Task SeedAsync_ProductionWithDemoPassword_Throws()
    {
        await using var context = CreateContext();
        var services = BuildServices(context, DemoCredentials.Password, Environments.Production);

        var act = () => DbInitializer.SeedAsync(context, NullLogger.Instance, services);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*published demo password*");
    }

    [Fact]
    public async Task SeedAsync_DevelopmentWithDemoPassword_CreatesAdmin()
    {
        await using var context = CreateContext();
        var services = BuildServices(context, DemoCredentials.Password, Environments.Development);

        await DbInitializer.SeedAsync(context, NullLogger.Instance, services);

        context.Users.Should().ContainSingle(u => u.Username == DemoCredentials.Username);
    }

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private static ServiceProvider BuildServices(AppDbContext context, string password, string environmentName)
    {
        var passwordService = new Mock<IPasswordService>();
        passwordService.Setup(s => s.HashPassword(It.IsAny<string>())).Returns("hashed");

        var environment = new Mock<IHostEnvironment>();
        environment.SetupGet(e => e.EnvironmentName).Returns(environmentName);

        var authOptions = new AuthOptions
        {
            BootstrapAdmin = new BootstrapAdminOptions
            {
                Enabled = true,
                Username = DemoCredentials.Username,
                Password = password,
                FullName = "Demo Administrator"
            }
        };

        return new ServiceCollection()
            .AddSingleton(context)
            .AddSingleton(Options.Create(authOptions))
            .AddSingleton(passwordService.Object)
            .AddSingleton(environment.Object)
            .BuildServiceProvider();
    }
}
