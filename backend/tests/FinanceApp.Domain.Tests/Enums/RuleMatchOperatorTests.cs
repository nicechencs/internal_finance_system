using FluentAssertions;
using FinanceApp.Domain.Enums;

namespace FinanceApp.Domain.Tests.Enums;

public class RuleMatchOperatorTests
{
    [Fact]
    public void RuleMatchOperator_ShouldHaveAllExpectedValues()
    {
        // Arrange & Act
        var values = Enum.GetValues<RuleMatchOperator>();

        // Assert
        values.Should().HaveCount(6);
        values.Should().Contain(RuleMatchOperator.Equals);
        values.Should().Contain(RuleMatchOperator.Contains);
        values.Should().Contain(RuleMatchOperator.StartsWith);
        values.Should().Contain(RuleMatchOperator.EndsWith);
        values.Should().Contain(RuleMatchOperator.Regex);
        values.Should().Contain(RuleMatchOperator.Range);
    }

    [Theory]
    [InlineData("Equals", RuleMatchOperator.Equals)]
    [InlineData("Contains", RuleMatchOperator.Contains)]
    [InlineData("StartsWith", RuleMatchOperator.StartsWith)]
    [InlineData("EndsWith", RuleMatchOperator.EndsWith)]
    [InlineData("Regex", RuleMatchOperator.Regex)]
    [InlineData("Range", RuleMatchOperator.Range)]
    public void RuleMatchOperator_ShouldParseFromString(string input, RuleMatchOperator expected)
    {
        // Act
        var result = Enum.Parse<RuleMatchOperator>(input);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void RuleMatchOperator_ShouldHaveCorrectEnumValues()
    {
        // Assert
        ((int)RuleMatchOperator.Equals).Should().Be(0);
        ((int)RuleMatchOperator.Contains).Should().Be(1);
        ((int)RuleMatchOperator.StartsWith).Should().Be(2);
        ((int)RuleMatchOperator.EndsWith).Should().Be(3);
        ((int)RuleMatchOperator.Regex).Should().Be(4);
        ((int)RuleMatchOperator.Range).Should().Be(5);
    }
}
