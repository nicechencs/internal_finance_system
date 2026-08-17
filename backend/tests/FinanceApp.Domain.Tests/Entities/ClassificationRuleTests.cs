using FluentAssertions;
using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Enums;

namespace FinanceApp.Domain.Tests.Entities;

public class ClassificationRuleTests
{
    [Fact]
    public void ClassificationRule_ShouldInitializeWithDefaultValues()
    {
        // Arrange & Act
        var rule = new ClassificationRule();

        // Assert
        rule.RuleName.Should().BeEmpty();
        rule.MatchValue.Should().BeEmpty();
        rule.IsActive.Should().BeTrue();
        rule.IsDeleted.Should().BeFalse();
        rule.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void ClassificationRule_ShouldSetPropertiesCorrectly()
    {
        // Arrange & Act
        var rule = new ClassificationRule
        {
            RuleName = "工资发放规则",
            Priority = 10,
            MatchField = RuleMatchField.Description,
            MatchOperator = RuleMatchOperator.Contains,
            MatchValue = "工资",
            CategoryId = 1,
            ProjectId = null,
            CustomerId = null,
            SupplierId = null,
            PersonId = 1,
            IsActive = true
        };

        // Assert
        rule.RuleName.Should().Be("工资发放规则");
        rule.Priority.Should().Be(10);
        rule.MatchField.Should().Be(RuleMatchField.Description);
        rule.MatchOperator.Should().Be(RuleMatchOperator.Contains);
        rule.MatchValue.Should().Be("工资");
        rule.CategoryId.Should().Be(1);
        rule.ProjectId.Should().BeNull();
        rule.CustomerId.Should().BeNull();
        rule.SupplierId.Should().BeNull();
        rule.PersonId.Should().Be(1);
        rule.IsActive.Should().BeTrue();
    }

    [Theory]
    [InlineData(RuleMatchField.Counterparty)]
    [InlineData(RuleMatchField.CounterpartyName)]
    [InlineData(RuleMatchField.Memo)]
    [InlineData(RuleMatchField.Description)]
    [InlineData(RuleMatchField.Amount)]
    public void ClassificationRule_ShouldSupportDifferentMatchFields(RuleMatchField matchField)
    {
        // Arrange & Act
        var rule = new ClassificationRule
        {
            MatchField = matchField,
            MatchOperator = RuleMatchOperator.Equals,
            MatchValue = "test"
        };

        // Assert
        rule.MatchField.Should().Be(matchField);
    }

    [Theory]
    [InlineData(RuleMatchOperator.Equals)]
    [InlineData(RuleMatchOperator.Contains)]
    [InlineData(RuleMatchOperator.StartsWith)]
    [InlineData(RuleMatchOperator.EndsWith)]
    [InlineData(RuleMatchOperator.Regex)]
    [InlineData(RuleMatchOperator.Range)]
    public void ClassificationRule_ShouldSupportDifferentMatchOperators(RuleMatchOperator matchOperator)
    {
        // Arrange & Act
        var rule = new ClassificationRule
        {
            MatchField = RuleMatchField.Description,
            MatchOperator = matchOperator,
            MatchValue = "test"
        };

        // Assert
        rule.MatchOperator.Should().Be(matchOperator);
    }

    [Fact]
    public void ClassificationRule_ShouldSupportPriorityOrdering()
    {
        // Arrange
        var rule1 = new ClassificationRule { Priority = 1, RuleName = "高优先级" };
        var rule2 = new ClassificationRule { Priority = 10, RuleName = "中优先级" };
        var rule3 = new ClassificationRule { Priority = 100, RuleName = "低优先级" };

        // Assert
        rule1.Priority.Should().BeLessThan(rule2.Priority);
        rule2.Priority.Should().BeLessThan(rule3.Priority);
    }

    [Fact]
    public void ClassificationRule_ShouldAllowNullableRelationships()
    {
        // Arrange & Act
        var rule = new ClassificationRule
        {
            RuleName = "测试规则",
            MatchField = RuleMatchField.Description,
            MatchOperator = RuleMatchOperator.Contains,
            MatchValue = "test",
            CategoryId = null,
            ProjectId = null,
            CustomerId = null,
            SupplierId = null,
            PersonId = null
        };

        // Assert
        rule.CategoryId.Should().BeNull();
        rule.ProjectId.Should().BeNull();
        rule.CustomerId.Should().BeNull();
        rule.SupplierId.Should().BeNull();
        rule.PersonId.Should().BeNull();
    }

    [Fact]
    public void ClassificationRule_ShouldSupportSoftDelete()
    {
        // Arrange
        var rule = new ClassificationRule
        {
            RuleName = "测试规则",
            IsActive = true
        };

        // Act
        rule.IsDeleted = true;
        rule.DeletedAt = DateTime.UtcNow;

        // Assert
        rule.IsDeleted.Should().BeTrue();
        rule.DeletedAt.Should().NotBeNull();
    }

    [Fact]
    public void ClassificationRule_ShouldSupportDeactivation()
    {
        // Arrange
        var rule = new ClassificationRule
        {
            RuleName = "测试规则",
            IsActive = true
        };

        // Act
        rule.IsActive = false;

        // Assert
        rule.IsActive.Should().BeFalse();
        rule.IsDeleted.Should().BeFalse(); // 停用不等于删除
    }
}
