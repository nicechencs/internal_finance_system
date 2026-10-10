using FluentAssertions;
using FinanceApp.Application.Common;
using FinanceApp.Application.Modules.TransactionProcessing.Services;
using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Enums;
using FinanceApp.Infrastructure.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace FinanceApp.Application.Tests.Services;

public class TransactionVoucherNoFilterTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<AppDbContext> _options;

    public TransactionVoucherNoFilterTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        _options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        using var ctx = new AppDbContext(_options);
        ctx.Database.EnsureCreated();
    }

    [Fact]
    public async Task ApplyRequestFilters_VoucherNo_MatchesContainsAndIgnoresBlank()
    {
        await using (var seed = new AppDbContext(_options))
        {
            var account = new Account
            {
                Name = "主账户",
                AccountType = AccountType.Bank,
                Currency = "CNY",
                OpeningBalance = 0,
                CurrentBalance = 0,
                IsActive = true
            };
            seed.Accounts.Add(account);
            await seed.SaveChangesAsync();

            seed.Transactions.AddRange(
                NewTransaction(account.Id, "PZ-2026-001"),
                NewTransaction(account.Id, "PZ-2026-099"),
                NewTransaction(account.Id, null));
            await seed.SaveChangesAsync();
        }

        await using var ctx = new AppDbContext(_options);
        var matched = ctx.Transactions
            .ApplyRequestFilters(new PageRequest { VoucherNo = "  2026-0  " }, () => ctx.TagBindings)
            .Select(t => t.VoucherNo)
            .ToList();

        matched.Should().BeEquivalentTo("PZ-2026-001", "PZ-2026-099");

        var unfiltered = ctx.Transactions
            .ApplyRequestFilters(new PageRequest { VoucherNo = "   " }, () => ctx.TagBindings)
            .Count();
        unfiltered.Should().Be(3);
    }

    public void Dispose() => _connection.Dispose();

    private static Transaction NewTransaction(long accountId, string? voucherNo) => new()
    {
        AccountId = accountId,
        TransactionDate = new DateTime(2026, 10, 1),
        Amount = 10m,
        TransactionType = TransactionType.Expense,
        Description = "test",
        VoucherNo = voucherNo,
        Status = TransactionStatus.Confirmed
    };
}
