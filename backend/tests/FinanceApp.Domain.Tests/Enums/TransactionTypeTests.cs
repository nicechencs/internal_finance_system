using FluentAssertions;
using FinanceApp.Domain.Enums;

namespace FinanceApp.Domain.Tests.Enums;

public class TransactionTypeTests
{
    [Fact]
    public void TransactionType_ShouldHaveIncomeValue()
    {
        // Arrange & Act
        var transactionType = TransactionType.Income;

        // Assert
        transactionType.Should().Be(TransactionType.Income);
        ((int)transactionType).Should().Be(0);
    }

    [Fact]
    public void TransactionType_ShouldHaveExpenseValue()
    {
        // Arrange & Act
        var transactionType = TransactionType.Expense;

        // Assert
        transactionType.Should().Be(TransactionType.Expense);
        ((int)transactionType).Should().Be(1);
    }

    [Fact]
    public void TransactionType_ShouldHaveTransferValue()
    {
        // Arrange & Act
        var transactionType = TransactionType.Transfer;

        // Assert
        transactionType.Should().Be(TransactionType.Transfer);
        ((int)transactionType).Should().Be(2);
    }

    [Fact]
    public void TransactionType_ShouldHaveExactlyThreeValues()
    {
        // Arrange & Act
        var values = Enum.GetValues<TransactionType>();

        // Assert
        values.Should().HaveCount(3);
        values.Should().Contain(TransactionType.Income);
        values.Should().Contain(TransactionType.Expense);
        values.Should().Contain(TransactionType.Transfer);
    }

    [Theory]
    [InlineData("Income", TransactionType.Income)]
    [InlineData("Expense", TransactionType.Expense)]
    [InlineData("Transfer", TransactionType.Transfer)]
    public void TransactionType_ShouldParseFromString(string input, TransactionType expected)
    {
        // Act
        var result = Enum.Parse<TransactionType>(input);

        // Assert
        result.Should().Be(expected);
    }
}
