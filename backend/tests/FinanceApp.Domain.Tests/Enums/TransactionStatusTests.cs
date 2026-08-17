using FluentAssertions;
using FinanceApp.Domain.Enums;

namespace FinanceApp.Domain.Tests.Enums;

public class TransactionStatusTests
{
    [Fact]
    public void TransactionStatus_ShouldHavePendingValue()
    {
        // Arrange & Act
        var status = TransactionStatus.Pending;

        // Assert
        status.Should().Be(TransactionStatus.Pending);
        ((int)status).Should().Be(0);
    }

    [Fact]
    public void TransactionStatus_ShouldHaveConfirmedValue()
    {
        // Arrange & Act
        var status = TransactionStatus.Confirmed;

        // Assert
        status.Should().Be(TransactionStatus.Confirmed);
        ((int)status).Should().Be(1);
    }

    [Fact]
    public void TransactionStatus_ShouldHaveCancelledValue()
    {
        // Arrange & Act
        var status = TransactionStatus.Cancelled;

        // Assert
        status.Should().Be(TransactionStatus.Cancelled);
        ((int)status).Should().Be(2);
    }

    [Fact]
    public void TransactionStatus_ShouldHaveExactlyThreeValues()
    {
        // Arrange & Act
        var values = Enum.GetValues<TransactionStatus>();

        // Assert
        values.Should().HaveCount(3);
        values.Should().Contain(TransactionStatus.Pending);
        values.Should().Contain(TransactionStatus.Confirmed);
        values.Should().Contain(TransactionStatus.Cancelled);
    }

    [Theory]
    [InlineData("Pending", TransactionStatus.Pending)]
    [InlineData("Confirmed", TransactionStatus.Confirmed)]
    [InlineData("Cancelled", TransactionStatus.Cancelled)]
    public void TransactionStatus_ShouldParseFromString(string input, TransactionStatus expected)
    {
        // Act
        var result = Enum.Parse<TransactionStatus>(input);

        // Assert
        result.Should().Be(expected);
    }
}
