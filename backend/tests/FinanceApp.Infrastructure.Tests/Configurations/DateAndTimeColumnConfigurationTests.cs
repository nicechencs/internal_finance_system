using FluentAssertions;
using FinanceApp.Domain.Entities;
using FinanceApp.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FinanceApp.Infrastructure.Tests.Configurations;

public class DateAndTimeColumnConfigurationTests : IDisposable
{
    private readonly AppDbContext _context;

    public DateAndTimeColumnConfigurationTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql("Host=localhost;Database=finance_test;Username=postgres;Password=postgres")
            .Options;

        _context = new AppDbContext(options);
    }

    [Theory]
    [InlineData(typeof(Project), nameof(Project.StartDate))]
    [InlineData(typeof(Project), nameof(Project.EndDate))]
    [InlineData(typeof(Transaction), nameof(Transaction.TransactionDate))]
    [InlineData(typeof(BankTransaction), nameof(BankTransaction.TransactionDate))]
    [InlineData(typeof(Receivable), nameof(Receivable.DueDate))]
    [InlineData(typeof(Payable), nameof(Payable.DueDate))]
    [InlineData(typeof(ReceivableDetail), nameof(ReceivableDetail.PaymentDate))]
    [InlineData(typeof(PayableDetail), nameof(PayableDetail.PaymentDate))]
    [InlineData(typeof(Account), nameof(Account.InterestStartDate))]
    [InlineData(typeof(Account), nameof(Account.MaturityDate))]
    [InlineData(typeof(Person), nameof(Person.JoinDate))]
    [InlineData(typeof(Person), nameof(Person.LeaveDate))]
    [InlineData(typeof(FixedDepositRecord), nameof(FixedDepositRecord.DepositDate))]
    [InlineData(typeof(FixedDepositRecord), nameof(FixedDepositRecord.MaturityDate))]
    [InlineData(typeof(FixedDepositRecord), nameof(FixedDepositRecord.WithdrawalDate))]
    [InlineData(typeof(TagDailySummary), nameof(TagDailySummary.SummaryDate))]
    public void BusinessDateColumns_ShouldUseDateType(Type entityClrType, string propertyName)
    {
        var entityType = _context.Model.FindEntityType(entityClrType);
        var property = entityType!.FindProperty(propertyName);

        property.Should().NotBeNull();
        property!.GetColumnType().Should().Be("date");
    }

    [Theory]
    [InlineData(typeof(Project), nameof(Project.CreatedAt))]
    [InlineData(typeof(Project), nameof(Project.UpdatedAt))]
    [InlineData(typeof(Project), nameof(Project.DeletedAt))]
    [InlineData(typeof(Receivable), nameof(Receivable.CreatedAt))]
    [InlineData(typeof(Receivable), nameof(Receivable.UpdatedAt))]
    [InlineData(typeof(Receivable), nameof(Receivable.SettledAt))]
    [InlineData(typeof(Payable), nameof(Payable.CreatedAt))]
    [InlineData(typeof(Payable), nameof(Payable.UpdatedAt))]
    [InlineData(typeof(Payable), nameof(Payable.SettledAt))]
    [InlineData(typeof(ImportBatch), nameof(ImportBatch.ImportDate))]
    [InlineData(typeof(User), nameof(User.LastLoginAt))]
    [InlineData(typeof(User), nameof(User.LockoutEndAt))]
    [InlineData(typeof(User), nameof(User.PasswordChangedAt))]
    public void TimestampColumns_ShouldUseTimestampWithTimeZone(Type entityClrType, string propertyName)
    {
        var entityType = _context.Model.FindEntityType(entityClrType);
        var property = entityType!.FindProperty(propertyName);

        property.Should().NotBeNull();
        property!.GetColumnType().Should().Be("timestamp with time zone");
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
