using FluentAssertions;
using FinanceApp.Application.Modules.Reporting.Services;
using FinanceApp.Application.Tests.Helpers;
using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Enums;
using FinanceApp.Domain.Interfaces;
using FinanceApp.Infrastructure.Data;
using FinanceApp.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace FinanceApp.Application.Tests.Services;

public class DashboardServiceTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly DashboardService _service;

    public DashboardServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AppDbContext(options);
        var loggerMock = new Mock<ILogger<DashboardService>>();
        var currentUserMock = new Mock<ICurrentUserService>();
        currentUserMock.Setup(x => x.IsAdmin).Returns(true);
        currentUserMock.Setup(x => x.Role).Returns(UserRole.Admin);
        currentUserMock.Setup(x => x.UserId).Returns(1L);

        var transactionRepo = new Repository<Transaction>(_context, Mock.Of<ILogger<Repository<Transaction>>>());
        var accountRepo = new Repository<Account>(_context, Mock.Of<ILogger<Repository<Account>>>());
        var projectRepo = new Repository<Project>(_context, Mock.Of<ILogger<Repository<Project>>>());

        _service = new DashboardService(
            transactionRepo,
            accountRepo,
            projectRepo,
            loggerMock.Object,
            currentUserMock.Object,
            new AllPermissionsDataPermissionService());
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    [Fact]
    public async Task GetSummaryAsync_WithNoData_ShouldReturnZeroValues()
    {
        // Act
        var result = await _service.GetSummaryAsync();

        // Assert
        result.Should().NotBeNull();
        result.TotalIncome.Should().Be(0);
        result.TotalExpense.Should().Be(0);
        result.NetProfit.Should().Be(0);
        result.TotalBalance.Should().Be(0);
        result.AccountCount.Should().Be(0);
        result.TransactionCount.Should().Be(0);
        result.ProjectCount.Should().Be(0);
    }

    [Fact]
    public async Task GetSummaryAsync_WithTransactionsAndAccounts_ShouldCalculateCorrectly()
    {
        // Arrange
        var account1 = new Account { Id = 1, Name = "账户1", AccountType = AccountType.Bank, CurrentBalance = 10000m, IsActive = true, CreatedAt = DateTime.UtcNow };
        var account2 = new Account { Id = 2, Name = "账户2", AccountType = AccountType.Alipay, CurrentBalance = 5000m, IsActive = true, CreatedAt = DateTime.UtcNow };
        var account3 = new Account { Id = 3, Name = "停用账户", AccountType = AccountType.Bank, CurrentBalance = 3000m, IsActive = false, CreatedAt = DateTime.UtcNow };
        var project = new Project { Id = 1, Name = "项目1", ProjectCode = "P001", Status = ProjectStatus.Active, CreatedAt = DateTime.UtcNow };

        _context.Accounts.AddRange(account1, account2, account3);
        _context.Projects.Add(project);
        await _context.SaveChangesAsync();

        var transaction1 = new Transaction { Id = 1, TransactionDate = DateTime.UtcNow, TransactionType = TransactionType.Income, Amount = 8000m, AccountId = 1, Description = "收入1", CreatedAt = DateTime.UtcNow };
        var transaction2 = new Transaction { Id = 2, TransactionDate = DateTime.UtcNow, TransactionType = TransactionType.Income, Amount = 2000m, AccountId = 2, Description = "收入2", CreatedAt = DateTime.UtcNow };
        var transaction3 = new Transaction { Id = 3, TransactionDate = DateTime.UtcNow, TransactionType = TransactionType.Expense, Amount = 3000m, AccountId = 1, Description = "支出1", CreatedAt = DateTime.UtcNow };
        var transaction4 = new Transaction { Id = 4, TransactionDate = DateTime.UtcNow, TransactionType = TransactionType.Expense, Amount = 1500m, AccountId = 2, Description = "支出2", CreatedAt = DateTime.UtcNow };

        _context.Transactions.AddRange(transaction1, transaction2, transaction3, transaction4);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetSummaryAsync();

        // Assert
        result.Should().NotBeNull();
        result.TotalIncome.Should().Be(10000m);
        result.TotalExpense.Should().Be(4500m);
        result.NetProfit.Should().Be(5500m);
        result.TotalBalance.Should().Be(15000m);
        result.AccountCount.Should().Be(2);
        result.TransactionCount.Should().Be(4);
        result.ProjectCount.Should().Be(1);
    }

    [Fact]
    public async Task GetMonthlyStatsAsync_WithNoData_ShouldReturnEmptyMonths()
    {
        // Act
        var result = await _service.GetMonthlyStatsAsync(6);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(6);
        result.All(m => m.Income == 0 && m.Expense == 0 && m.Net == 0).Should().BeTrue();
    }

    [Fact]
    public async Task GetMonthlyStatsAsync_WithTransactions_ShouldGroupByMonth()
    {
        // Arrange
        var account = new Account { Id = 1, Name = "测试账户", AccountType = AccountType.Bank, CurrentBalance = 10000m, IsActive = true, CreatedAt = DateTime.UtcNow };
        _context.Accounts.Add(account);
        await _context.SaveChangesAsync();

        var now = DateTime.UtcNow;
        var currentMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var lastMonth = currentMonth.AddMonths(-1);

        var t1 = new Transaction { Id = 1, TransactionDate = currentMonth.AddDays(5), TransactionType = TransactionType.Income, Amount = 5000m, AccountId = 1, CreatedAt = DateTime.UtcNow };
        var t2 = new Transaction { Id = 2, TransactionDate = currentMonth.AddDays(10), TransactionType = TransactionType.Expense, Amount = 2000m, AccountId = 1, CreatedAt = DateTime.UtcNow };
        var t3 = new Transaction { Id = 3, TransactionDate = lastMonth.AddDays(15), TransactionType = TransactionType.Income, Amount = 3000m, AccountId = 1, CreatedAt = DateTime.UtcNow };

        _context.Transactions.AddRange(t1, t2, t3);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetMonthlyStatsAsync(2);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);

        var currentMonthStats = result.FirstOrDefault(m => m.Month == currentMonth.ToString("yyyy-MM"));
        currentMonthStats.Should().NotBeNull();
        currentMonthStats!.Income.Should().Be(5000m);
        currentMonthStats.Expense.Should().Be(2000m);
        currentMonthStats.Net.Should().Be(3000m);

        var lastMonthStats = result.FirstOrDefault(m => m.Month == lastMonth.ToString("yyyy-MM"));
        lastMonthStats.Should().NotBeNull();
        lastMonthStats!.Income.Should().Be(3000m);
        lastMonthStats.Expense.Should().Be(0m);
        lastMonthStats.Net.Should().Be(3000m);
    }

    [Fact]
    public async Task GetExpenseByCategoryAsync_WithNoData_ShouldReturnEmptyList()
    {
        // Act
        var result = await _service.GetExpenseByCategoryAsync(null, null);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetExpenseByCategoryAsync_WithCategories_ShouldGroupAndCalculatePercentage()
    {
        // Arrange
        var account = new Account { Id = 1, Name = "测试账户", AccountType = AccountType.Bank, CurrentBalance = 10000m, IsActive = true, CreatedAt = DateTime.UtcNow };
        var category1 = new Category { Id = 1, Name = "办公费用", CategoryType = CategoryType.Expense, CreatedAt = DateTime.UtcNow };
        var category2 = new Category { Id = 2, Name = "差旅费", CategoryType = CategoryType.Expense, CreatedAt = DateTime.UtcNow };

        _context.Accounts.Add(account);
        _context.Categories.AddRange(category1, category2);
        await _context.SaveChangesAsync();

        var t1 = new Transaction { Id = 1, TransactionDate = DateTime.UtcNow, TransactionType = TransactionType.Expense, Amount = 6000m, AccountId = 1, CategoryId = 1, CreatedAt = DateTime.UtcNow };
        var t2 = new Transaction { Id = 2, TransactionDate = DateTime.UtcNow, TransactionType = TransactionType.Expense, Amount = 4000m, AccountId = 1, CategoryId = 2, CreatedAt = DateTime.UtcNow };

        _context.Transactions.AddRange(t1, t2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetExpenseByCategoryAsync(null, null);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result[0].CategoryName.Should().Be("办公费用");
        result[0].Amount.Should().Be(6000m);
        result[0].Percentage.Should().Be(60m);
        result[1].CategoryName.Should().Be("差旅费");
        result[1].Amount.Should().Be(4000m);
        result[1].Percentage.Should().Be(40m);
    }

    [Fact]
    public async Task GetExpenseByCategoryAsync_WithDateRange_ShouldFilterCorrectly()
    {
        // Arrange
        var account = new Account { Id = 1, Name = "测试账户", AccountType = AccountType.Bank, CurrentBalance = 10000m, IsActive = true, CreatedAt = DateTime.UtcNow };
        var category = new Category { Id = 1, Name = "办公费用", CategoryType = CategoryType.Expense, CreatedAt = DateTime.UtcNow };

        _context.Accounts.Add(account);
        _context.Categories.Add(category);
        await _context.SaveChangesAsync();

        var startDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var endDate = new DateTime(2026, 1, 31, 23, 59, 59, DateTimeKind.Utc);

        var t1 = new Transaction { Id = 1, TransactionDate = new DateTime(2025, 12, 15, 0, 0, 0, DateTimeKind.Utc), TransactionType = TransactionType.Expense, Amount = 1000m, AccountId = 1, CategoryId = 1, CreatedAt = DateTime.UtcNow };
        var t2 = new Transaction { Id = 2, TransactionDate = new DateTime(2026, 1, 15, 0, 0, 0, DateTimeKind.Utc), TransactionType = TransactionType.Expense, Amount = 3000m, AccountId = 1, CategoryId = 1, CreatedAt = DateTime.UtcNow };
        var t3 = new Transaction { Id = 3, TransactionDate = new DateTime(2026, 2, 15, 0, 0, 0, DateTimeKind.Utc), TransactionType = TransactionType.Expense, Amount = 2000m, AccountId = 1, CategoryId = 1, CreatedAt = DateTime.UtcNow };

        _context.Transactions.AddRange(t1, t2, t3);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetExpenseByCategoryAsync(startDate, endDate);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result[0].Amount.Should().Be(3000m);
    }

    [Fact]
    public async Task GetIncomeByCategoryAsync_WithCategories_ShouldGroupCorrectly()
    {
        // Arrange
        var account = new Account { Id = 1, Name = "测试账户", AccountType = AccountType.Bank, CurrentBalance = 10000m, IsActive = true, CreatedAt = DateTime.UtcNow };
        var category1 = new Category { Id = 1, Name = "销售收入", CategoryType = CategoryType.Income, CreatedAt = DateTime.UtcNow };
        var category2 = new Category { Id = 2, Name = "服务收入", CategoryType = CategoryType.Income, CreatedAt = DateTime.UtcNow };

        _context.Accounts.Add(account);
        _context.Categories.AddRange(category1, category2);
        await _context.SaveChangesAsync();

        var t1 = new Transaction { Id = 1, TransactionDate = DateTime.UtcNow, TransactionType = TransactionType.Income, Amount = 7000m, AccountId = 1, CategoryId = 1, CreatedAt = DateTime.UtcNow };
        var t2 = new Transaction { Id = 2, TransactionDate = DateTime.UtcNow, TransactionType = TransactionType.Income, Amount = 3000m, AccountId = 1, CategoryId = 2, CreatedAt = DateTime.UtcNow };

        _context.Transactions.AddRange(t1, t2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetIncomeByCategoryAsync(null, null);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result[0].CategoryName.Should().Be("销售收入");
        result[0].Amount.Should().Be(7000m);
        result[0].Percentage.Should().Be(70m);
        result[1].CategoryName.Should().Be("服务收入");
        result[1].Amount.Should().Be(3000m);
        result[1].Percentage.Should().Be(30m);
    }

    [Fact]
    public async Task GetRecentTransactionsAsync_WithNoData_ShouldReturnEmptyList()
    {
        // Act
        var result = await _service.GetRecentTransactionsAsync(10);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetRecentTransactionsAsync_ShouldReturnOrderedByDateDesc()
    {
        // Arrange
        var account = new Account { Id = 1, Name = "测试账户", AccountType = AccountType.Bank, CurrentBalance = 10000m, IsActive = true, CreatedAt = DateTime.UtcNow };
        var category = new Category { Id = 1, Name = "办公费用", CategoryType = CategoryType.Expense, CreatedAt = DateTime.UtcNow };
        var customer = new Customer { Id = 1, Name = "客户A", CreatedAt = DateTime.UtcNow };

        _context.Accounts.Add(account);
        _context.Categories.Add(category);
        _context.Customers.Add(customer);
        await _context.SaveChangesAsync();

        var t1 = new Transaction { Id = 1, TransactionDate = DateTime.UtcNow.AddDays(-2), TransactionType = TransactionType.Income, Amount = 1000m, AccountId = 1, CategoryId = 1, CustomerId = 1, Description = "交易1", CreatedAt = DateTime.UtcNow.AddDays(-2) };
        var t2 = new Transaction { Id = 2, TransactionDate = DateTime.UtcNow.AddDays(-1), TransactionType = TransactionType.Expense, Amount = 500m, AccountId = 1, CategoryId = 1, Description = "交易2", CreatedAt = DateTime.UtcNow.AddDays(-1) };
        var t3 = new Transaction { Id = 3, TransactionDate = DateTime.UtcNow, TransactionType = TransactionType.Income, Amount = 2000m, AccountId = 1, CategoryId = 1, Description = "交易3", CreatedAt = DateTime.UtcNow };

        _context.Transactions.AddRange(t1, t2, t3);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetRecentTransactionsAsync(10);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(3);
        result[0].Id.Should().Be(3);
        result[1].Id.Should().Be(2);
        result[2].Id.Should().Be(1);
        result[0].AccountName.Should().Be("测试账户");
        result[0].CategoryName.Should().Be("办公费用");
        result[2].CounterpartyName.Should().Be("客户A");
    }

    [Fact]
    public async Task GetRecentTransactionsAsync_ShouldLimitResults()
    {
        // Arrange
        var account = new Account { Id = 1, Name = "测试账户", AccountType = AccountType.Bank, CurrentBalance = 10000m, IsActive = true, CreatedAt = DateTime.UtcNow };
        _context.Accounts.Add(account);
        await _context.SaveChangesAsync();

        for (int i = 1; i <= 20; i++)
        {
            var t = new Transaction { Id = i, TransactionDate = DateTime.UtcNow.AddDays(-i), TransactionType = TransactionType.Income, Amount = 1000m, AccountId = 1, CreatedAt = DateTime.UtcNow.AddDays(-i) };
            _context.Transactions.Add(t);
        }
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetRecentTransactionsAsync(5);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(5);
    }

    [Fact]
    public async Task GetExpenseByCategoryAsync_WithUncategorizedTransactions_ShouldShowUncategorized()
    {
        // Arrange
        var account = new Account { Id = 1, Name = "测试账户", AccountType = AccountType.Bank, CurrentBalance = 10000m, IsActive = true, CreatedAt = DateTime.UtcNow };
        var category = new Category { Id = 1, Name = "办公费用", CategoryType = CategoryType.Expense, CreatedAt = DateTime.UtcNow };

        _context.Accounts.Add(account);
        _context.Categories.Add(category);
        await _context.SaveChangesAsync();

        var t1 = new Transaction { Id = 1, TransactionDate = DateTime.UtcNow, TransactionType = TransactionType.Expense, Amount = 3000m, AccountId = 1, CategoryId = 1, CreatedAt = DateTime.UtcNow };
        var t2 = new Transaction { Id = 2, TransactionDate = DateTime.UtcNow, TransactionType = TransactionType.Expense, Amount = 2000m, AccountId = 1, CategoryId = null, CreatedAt = DateTime.UtcNow };

        _context.Transactions.AddRange(t1, t2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetExpenseByCategoryAsync(null, null);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.Any(r => r.CategoryName == "未分类").Should().BeTrue();
        var uncategorized = result.First(r => r.CategoryName == "未分类");
        uncategorized.Amount.Should().Be(2000m);
        uncategorized.Percentage.Should().Be(40m);
    }

    [Fact]
    public async Task GetMonthlyStatsAsync_WithOldTransactions_ShouldOnlyIncludeRecentMonths()
    {
        // Arrange
        var account = new Account { Id = 1, Name = "测试账户", AccountType = AccountType.Bank, CurrentBalance = 10000m, IsActive = true, CreatedAt = DateTime.UtcNow };
        _context.Accounts.Add(account);
        await _context.SaveChangesAsync();

        var now = DateTime.UtcNow;
        var currentMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var veryOldDate = currentMonth.AddMonths(-24);

        var t1 = new Transaction { Id = 1, TransactionDate = currentMonth.AddDays(5), TransactionType = TransactionType.Income, Amount = 5000m, AccountId = 1, CreatedAt = DateTime.UtcNow };
        var t2 = new Transaction { Id = 2, TransactionDate = veryOldDate, TransactionType = TransactionType.Income, Amount = 10000m, AccountId = 1, CreatedAt = DateTime.UtcNow };

        _context.Transactions.AddRange(t1, t2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetMonthlyStatsAsync(3);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(3);
        var totalIncome = result.Sum(m => m.Income);
        totalIncome.Should().Be(5000m);
    }

    [Fact]
    public async Task GetMonthlyStatsAsync_ShouldIgnoreTransfers()
    {
        var account = new Account { Id = 1, Name = "测试账户", AccountType = AccountType.Bank, CurrentBalance = 10000m, IsActive = true, CreatedAt = DateTime.UtcNow };
        _context.Accounts.Add(account);
        await _context.SaveChangesAsync();

        var now = DateTime.UtcNow;
        var currentMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        _context.Transactions.AddRange(
            new Transaction { Id = 1, TransactionDate = currentMonth.AddDays(2), TransactionType = TransactionType.Income, Amount = 4000m, AccountId = 1, CreatedAt = DateTime.UtcNow },
            new Transaction { Id = 2, TransactionDate = currentMonth.AddDays(3), TransactionType = TransactionType.Expense, Amount = 1000m, AccountId = 1, CreatedAt = DateTime.UtcNow },
            new Transaction { Id = 3, TransactionDate = currentMonth.AddDays(4), TransactionType = TransactionType.Transfer, Amount = 2500m, AccountId = 1, Description = "转账至账户B", CreatedAt = DateTime.UtcNow });
        await _context.SaveChangesAsync();

        var result = await _service.GetMonthlyStatsAsync(1);

        result.Should().ContainSingle();
        result[0].Income.Should().Be(4000m);
        result[0].Expense.Should().Be(1000m);
        result[0].Net.Should().Be(3000m);
    }
}
