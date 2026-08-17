using FluentAssertions;
using FinanceApp.Domain.Enums;

namespace FinanceApp.Domain.Tests.Enums;

public class CategoryTypeTests
{
    [Fact]
    public void CategoryType_ShouldHaveIncomeValue()
    {
        // Arrange & Act
        var categoryType = CategoryType.Income;

        // Assert
        categoryType.Should().Be(CategoryType.Income);
        ((int)categoryType).Should().Be(0);
    }

    [Fact]
    public void CategoryType_ShouldHaveExpenseValue()
    {
        // Arrange & Act
        var categoryType = CategoryType.Expense;

        // Assert
        categoryType.Should().Be(CategoryType.Expense);
        ((int)categoryType).Should().Be(1);
    }

    [Fact]
    public void CategoryType_ShouldHaveExactlyTwoValues()
    {
        // Arrange & Act
        var values = Enum.GetValues<CategoryType>();

        // Assert
        values.Should().HaveCount(2);
        values.Should().Contain(CategoryType.Income);
        values.Should().Contain(CategoryType.Expense);
    }

    [Theory]
    [InlineData("Income", CategoryType.Income)]
    [InlineData("Expense", CategoryType.Expense)]
    public void CategoryType_ShouldParseFromString(string input, CategoryType expected)
    {
        // Act
        var result = Enum.Parse<CategoryType>(input);

        // Assert
        result.Should().Be(expected);
    }
}
