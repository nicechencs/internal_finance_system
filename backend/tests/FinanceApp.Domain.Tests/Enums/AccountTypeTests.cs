using FluentAssertions;
using FinanceApp.Domain.Enums;

namespace FinanceApp.Domain.Tests.Enums;

public class AccountTypeTests
{
    [Fact]
    public void AccountType_ShouldHaveBankValue()
    {
        // Arrange & Act
        var accountType = AccountType.Bank;

        // Assert
        accountType.Should().Be(AccountType.Bank);
        ((int)accountType).Should().Be(0);
    }

    [Fact]
    public void AccountType_ShouldHaveAlipayValue()
    {
        // Arrange & Act
        var accountType = AccountType.Alipay;

        // Assert
        accountType.Should().Be(AccountType.Alipay);
        ((int)accountType).Should().Be(1);
    }

    [Fact]
    public void AccountType_ShouldHaveExactlyThreeValues()
    {
        // Arrange & Act
        var values = Enum.GetValues<AccountType>();

        // Assert
        values.Should().HaveCount(3);
        values.Should().Contain(AccountType.Bank);
        values.Should().Contain(AccountType.Alipay);
        values.Should().Contain(AccountType.FixedDeposit);
    }

    [Theory]
    [InlineData("Bank", AccountType.Bank)]
    [InlineData("Alipay", AccountType.Alipay)]
    public void AccountType_ShouldParseFromString(string input, AccountType expected)
    {
        // Act
        var result = Enum.Parse<AccountType>(input);

        // Assert
        result.Should().Be(expected);
    }
}
