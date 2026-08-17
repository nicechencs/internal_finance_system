using Microsoft.EntityFrameworkCore;
using FinanceApp.Infrastructure.Data;

namespace FinanceApp.Infrastructure.Tests.Helpers;

public static class TestDbContextFactory
{
    public static AppDbContext CreateInMemoryContext(string databaseName = "TestDb")
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: databaseName)
            .Options;

        var context = new AppDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    public static AppDbContext CreateInMemoryContextWithData(string databaseName = "TestDb")
    {
        var context = CreateInMemoryContext(databaseName);
        // 可以在这里添加测试数据
        return context;
    }
}
