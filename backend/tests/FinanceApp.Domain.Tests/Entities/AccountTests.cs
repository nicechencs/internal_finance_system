using FluentAssertions;
using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Enums;

namespace FinanceApp.Domain.Tests.Entities;

public class AccountTests
{
    [Fact]
    public void Account_ShouldInitializeWithDefaultValues()
    {
        // Arrange & Act
        var account = new Account();

        // Assert
        account.Name.Should().BeEmpty();
        account.Currency.Should().Be("CNY");
        account.IsActive.Should().BeTrue();
        account.OpeningBalance.Should().Be(0);
        account.CurrentBalance.Should().Be(0);
        account.Balance.Should().Be(0);
        account.IsDeleted.Should().BeFalse();
        account.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        account.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Account_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var account = new Account
        {
            Name = "工商银行对公账户",
            AccountType = AccountType.Bank,
            AccountNumber = "6222021234567890",
            BankName = "工商银行",
            OpeningBalance = 100000m,
            CurrentBalance = 150000m,
            Currency = "CNY",
            Description = "公司主账户",
            IsActive = true
        };

        // Assert
        account.Name.Should().Be("工商银行对公账户");
        account.AccountType.Should().Be(AccountType.Bank);
        account.AccountNumber.Should().Be("6222021234567890");
        account.BankName.Should().Be("工商银行");
        account.OpeningBalance.Should().Be(100000m);
        account.CurrentBalance.Should().Be(150000m);
        account.Balance.Should().Be(150000m);
        account.Currency.Should().Be("CNY");
        account.Description.Should().Be("公司主账户");
        account.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Account_Balance_ShouldReturnCurrentBalance()
    {
        // Arrange
        var account = new Account
        {
            CurrentBalance = 50000m
        };

        // Act & Assert
        account.Balance.Should().Be(50000m);
        account.Balance.Should().Be(account.CurrentBalance);
    }

    [Theory]
    [InlineData(AccountType.Bank)]
    [InlineData(AccountType.Alipay)]
    public void Account_ShouldSupportDifferentAccountTypes(AccountType accountType)
    {
        // Arrange & Act
        var account = new Account
        {
            AccountType = accountType
        };

        // Assert
        account.AccountType.Should().Be(accountType);
    }

    [Fact]
    public void Account_ShouldSupportSoftDelete()
    {
        // Arrange
        var account = new Account
        {
            Name = "测试账户",
            IsActive = true
        };

        // Act
        account.IsDeleted = true;
        account.DeletedAt = DateTime.UtcNow;

        // Assert
        account.IsDeleted.Should().BeTrue();
        account.DeletedAt.Should().NotBeNull();
        account.DeletedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Account_ShouldAllowNullableFields()
    {
        // Arrange & Act
        var account = new Account
        {
            Name = "现金账户",
            AccountNumber = null,
            BankName = null,
            Description = null
        };

        // Assert
        account.AccountNumber.Should().BeNull();
        account.BankName.Should().BeNull();
        account.Description.Should().BeNull();
    }
}
