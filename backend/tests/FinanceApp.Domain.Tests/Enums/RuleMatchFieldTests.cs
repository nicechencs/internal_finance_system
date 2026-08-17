using FluentAssertions;
using FinanceApp.Domain.Enums;

namespace FinanceApp.Domain.Tests.Enums;

public class RuleMatchFieldTests
{
    [Fact]
    public void RuleMatchField_ShouldHaveAllExpectedValues()
    {
        // Arrange & Act
        var values = Enum.GetValues<RuleMatchField>();

        // Assert
        values.Should().HaveCount(5);
        values.Should().Contain(RuleMatchField.Counterparty);
        values.Should().Contain(RuleMatchField.CounterpartyName);
        values.Should().Contain(RuleMatchField.Memo);
        values.Should().Contain(RuleMatchField.Description);
        values.Should().Contain(RuleMatchField.Amount);
    }

    [Theory]
    [InlineData("Counterparty", RuleMatchField.Counterparty)]
    [InlineData("CounterpartyName", RuleMatchField.CounterpartyName)]
    [InlineData("Memo", RuleMatchField.Memo)]
    [InlineData("Description", RuleMatchField.Description)]
    [InlineData("Amount", RuleMatchField.Amount)]
    public void RuleMatchField_ShouldParseFromString(string input, RuleMatchField expected)
    {
        // Act
        var result = Enum.Parse<RuleMatchField>(input);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void RuleMatchField_ShouldHaveCorrectEnumValues()
    {
        // Assert
        ((int)RuleMatchField.Counterparty).Should().Be(0);
        ((int)RuleMatchField.CounterpartyName).Should().Be(1);
        ((int)RuleMatchField.Memo).Should().Be(2);
        ((int)RuleMatchField.Description).Should().Be(3);
        ((int)RuleMatchField.Amount).Should().Be(4);
    }
}
