using Microsoft.EntityFrameworkCore;
using FinanceApp.Infrastructure.Data;

namespace FinanceApp.Infrastructure.Tests;

/// <summary>
/// 测试基类，提供通用测试设置
/// </summary>
public abstract class TestBase : IDisposable
{
    protected AppDbContext Context { get; }

    protected TestBase()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        Context = new AppDbContext(options);
    }

    public void Dispose()
    {
        Context?.Dispose();
    }
}
