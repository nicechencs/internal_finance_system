using FluentAssertions;
using FinanceApp.Application.Common;
using FinanceApp.Domain.Enums;

namespace FinanceApp.Application.Tests.Common;

public class RuleMatchingHelperTests
{
    // ─────────────────────── 字符串字段 × 各 operator ───────────────────────

    [Theory]
    [InlineData(RuleMatchField.CounterpartyName, RuleMatchOperator.Contains, "ali", "Alibaba Cloud", true)]
    [InlineData(RuleMatchField.CounterpartyName, RuleMatchOperator.Contains, "ALI", "alibaba cloud", true)]   // 大小写不敏感
    [InlineData(RuleMatchField.CounterpartyName, RuleMatchOperator.Contains, "xxx", "Alibaba Cloud", false)]
    [InlineData(RuleMatchField.CounterpartyName, RuleMatchOperator.Equals, "Alibaba", "ALIBABA", true)]
    [InlineData(RuleMatchField.CounterpartyName, RuleMatchOperator.Equals, "Alibaba", "Alibaba Cloud", false)]
    [InlineData(RuleMatchField.CounterpartyName, RuleMatchOperator.StartsWith, "Ali", "Alibaba Cloud", true)]
    [InlineData(RuleMatchField.CounterpartyName, RuleMatchOperator.StartsWith, "Cloud", "Alibaba Cloud", false)]
    [InlineData(RuleMatchField.CounterpartyName, RuleMatchOperator.EndsWith, "Cloud", "Alibaba Cloud", true)]
    [InlineData(RuleMatchField.CounterpartyName, RuleMatchOperator.EndsWith, "Ali", "Alibaba Cloud", false)]
    [InlineData(RuleMatchField.CounterpartyName, RuleMatchOperator.Regex, @"^Ali.*Cloud$", "Alibaba Cloud", true)]
    [InlineData(RuleMatchField.CounterpartyName, RuleMatchOperator.Regex, @"^Xx.*$", "Alibaba Cloud", false)]
    public void Match_StringField_WorksAsExpected(
        RuleMatchField field, RuleMatchOperator op, string ruleValue, string actualCounterparty, bool expected)
    {
        var input = new RuleMatchInput(actualCounterparty, string.Empty, 0m, null);

        var actual = RuleMatchingHelper.Match(field, op, ruleValue, null, input);

        actual.Should().Be(expected);
    }

    [Fact]
    public void Match_Regex_Invalid_ReturnsFalse_WithoutThrow()
    {
        var input = new RuleMatchInput("Alibaba", string.Empty, 0m, null);

        var actual = RuleMatchingHelper.Match(
            RuleMatchField.CounterpartyName, RuleMatchOperator.Regex, "[invalid(regex", null, input);

        actual.Should().BeFalse();
    }

    // ─────────────────────── Amount × Equals（数值比较）───────────────────────

    [Theory]
    [InlineData("100.5", 100.5, true)]
    [InlineData("100.50", 100.5, true)]      // 财务等价但字符串形态不同
    [InlineData("100.00", 100.0, true)]
    [InlineData("100", 100.5, false)]
    [InlineData("abc", 100.5, false)]        // 非数值 matchValue
    public void Match_AmountEquals_UsesNumericEquality(string ruleValue, double amount, bool expected)
    {
        var input = new RuleMatchInput(string.Empty, string.Empty, (decimal)amount, null);

        var actual = RuleMatchingHelper.Match(
            RuleMatchField.Amount, RuleMatchOperator.Equals, ruleValue, null, input);

        actual.Should().Be(expected);
    }

    // ─────────────────────── Amount × Range ───────────────────────

    [Theory]
    [InlineData("1000", "10000", 5000, true)]     // 区间内
    [InlineData("1000", "10000", 1000, true)]     // 下界闭
    [InlineData("1000", "10000", 10000, true)]    // 上界闭
    [InlineData("1000", "10000", 999.99, false)]  // 下界外
    [InlineData("1000", "10000", 10000.01, false)]// 上界外
    [InlineData("1000", null, 5000, true)]        // 开放上限
    [InlineData("1000", null, 500, false)]        // 开放上限，低于下界不命中
    [InlineData("1000", "", 5000, true)]          // 空串等同 null
    [InlineData("abc", "10000", 5000, false)]     // 下界非数值
    [InlineData("1000", "abc", 5000, false)]      // 上界非数值（当存在时）
    public void Match_AmountRange_MatchesByNumericBounds(
        string min, string? max, double amount, bool expected)
    {
        var input = new RuleMatchInput(string.Empty, string.Empty, (decimal)amount, null);

        var actual = RuleMatchingHelper.Match(
            RuleMatchField.Amount, RuleMatchOperator.Range, min, max, input);

        actual.Should().Be(expected);
    }

    // ─────────────────────── 各字段的 Input 映射 ───────────────────────

    [Theory]
    [InlineData(RuleMatchField.CounterpartyName, "ACME", "", "")]
    [InlineData(RuleMatchField.Counterparty, "ACME", "", "")]
    [InlineData(RuleMatchField.Description, "", "desc", "")]
    [InlineData(RuleMatchField.Memo, "", "", "")]
    public void Match_FieldMapping_WorksPerField(
        RuleMatchField field, string counterparty, string description, string memo)
    {
        var input = new RuleMatchInput(
            counterparty,
            description,
            0m,
            string.IsNullOrEmpty(memo) ? null : memo);

        // 用 "ACME" 作为 ruleValue 对字符串字段 Contains 测试
        var hasACME = RuleMatchingHelper.Match(
            field, RuleMatchOperator.Contains, "ACME", null, input);

        if (field == RuleMatchField.CounterpartyName || field == RuleMatchField.Counterparty)
        {
            hasACME.Should().Be(counterparty.Contains("ACME"));
        }
        else if (field == RuleMatchField.Description)
        {
            hasACME.Should().Be(description.Contains("ACME"));
        }
        else // Memo
        {
            hasACME.Should().BeFalse();
        }
    }

    // ─────────────────────── ValidateFieldOperatorCombination ───────────────────────

    [Theory]
    [InlineData(RuleMatchField.CounterpartyName, RuleMatchOperator.Range, "100", null)]  // 字符串字段禁 Range
    [InlineData(RuleMatchField.Description, RuleMatchOperator.Range, "100", null)]
    [InlineData(RuleMatchField.Amount, RuleMatchOperator.Contains, "100", null)]         // Amount 禁 Contains
    [InlineData(RuleMatchField.Amount, RuleMatchOperator.StartsWith, "100", null)]
    [InlineData(RuleMatchField.Amount, RuleMatchOperator.Regex, "100", null)]
    [InlineData(RuleMatchField.Amount, RuleMatchOperator.Equals, "abc", null)]           // Amount 值非数字
    [InlineData(RuleMatchField.Amount, RuleMatchOperator.Range, "100", "abc")]           // max 非数字
    [InlineData(RuleMatchField.Amount, RuleMatchOperator.Range, "100", "50")]            // max < min
    public void Validate_InvalidCombination_Throws(
        RuleMatchField field, RuleMatchOperator op, string matchValue, string? matchValueMax)
    {
        Action act = () => RuleMatchingHelper.ValidateFieldOperatorCombination(field, op, matchValue, matchValueMax);
        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(RuleMatchField.CounterpartyName, RuleMatchOperator.Contains, "abc", null)]
    [InlineData(RuleMatchField.CounterpartyName, RuleMatchOperator.Regex, "^abc$", null)]
    [InlineData(RuleMatchField.Amount, RuleMatchOperator.Equals, "100.50", null)]
    [InlineData(RuleMatchField.Amount, RuleMatchOperator.Range, "100", "200")]
    [InlineData(RuleMatchField.Amount, RuleMatchOperator.Range, "100", null)]           // 开放上限
    [InlineData(RuleMatchField.Amount, RuleMatchOperator.Range, "100", "")]             // 空串等同开放上限
    public void Validate_ValidCombination_DoesNotThrow(
        RuleMatchField field, RuleMatchOperator op, string matchValue, string? matchValueMax)
    {
        Action act = () => RuleMatchingHelper.ValidateFieldOperatorCombination(field, op, matchValue, matchValueMax);
        act.Should().NotThrow();
    }
}
