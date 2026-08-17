using System.Globalization;
using System.Text.RegularExpressions;
using FinanceApp.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace FinanceApp.Application.Common;

/// <summary>
/// 一条待匹配记录的原始字段集合，供标签规则 / 分类规则共用
/// </summary>
public readonly record struct RuleMatchInput(
    string Counterparty,
    string Description,
    decimal Amount,
    string? Memo);

/// <summary>
/// 标签规则与分类规则共享的匹配逻辑
/// </summary>
/// <remarks>
/// 语义约定：
/// - 字符串字段（CounterpartyName/Counterparty/Description/Memo）：Contains/Equals/StartsWith/EndsWith/Regex，OrdinalIgnoreCase
/// - Amount 字段：Equals（数值相等）或 Range（闭区间 [matchValue, matchValueMax]，上限为空表示仅约束下限）
/// </remarks>
public static class RuleMatchingHelper
{
    public static bool Match(
        RuleMatchField field,
        RuleMatchOperator op,
        string matchValue,
        string? matchValueMax,
        RuleMatchInput input,
        ILogger? logger = null,
        long? ruleId = null)
    {
        if (field == RuleMatchField.Amount)
        {
            return MatchAmount(op, input.Amount, matchValue, matchValueMax);
        }

        var valueToMatch = GetStringValue(field, input);
        return MatchString(op, valueToMatch, matchValue, logger, ruleId);
    }

    /// <summary>
    /// 校验字段与操作符的组合合法性（及 Range 的数值/区间），不通过抛 ArgumentException。
    /// 调用方通常包装为 ValidationException。
    /// </summary>
    public static void ValidateFieldOperatorCombination(
        RuleMatchField field,
        RuleMatchOperator op,
        string matchValue,
        string? matchValueMax)
    {
        if (field == RuleMatchField.Amount)
        {
            if (op != RuleMatchOperator.Equals && op != RuleMatchOperator.Range)
            {
                throw new ArgumentException($"Amount 字段仅支持 Equals 或 Range 操作符，不支持 {op}");
            }

            if (!TryParseInvariant(matchValue, out var min))
            {
                throw new ArgumentException($"Amount 字段的匹配值必须为数字: {matchValue}");
            }

            if (op == RuleMatchOperator.Range && !string.IsNullOrWhiteSpace(matchValueMax))
            {
                if (!TryParseInvariant(matchValueMax, out var max))
                {
                    throw new ArgumentException($"Amount Range 的上限值必须为数字: {matchValueMax}");
                }
                if (max < min)
                {
                    throw new ArgumentException("Amount Range 的上限必须大于或等于下限");
                }
            }
        }
        else
        {
            if (op == RuleMatchOperator.Range)
            {
                throw new ArgumentException($"字符串字段 {field} 不支持 Range 操作符");
            }
        }
    }

    // ──────────────────────── 内部实现 ────────────────────────

    private static string GetStringValue(RuleMatchField field, RuleMatchInput input)
    {
        return field switch
        {
            RuleMatchField.CounterpartyName => input.Counterparty ?? string.Empty,
            RuleMatchField.Counterparty => input.Counterparty ?? string.Empty,
            RuleMatchField.Description => input.Description ?? string.Empty,
            RuleMatchField.Memo => input.Memo ?? string.Empty,
            _ => string.Empty
        };
    }

    private static bool MatchString(
        RuleMatchOperator op,
        string valueToMatch,
        string matchValue,
        ILogger? logger,
        long? ruleId)
    {
        return op switch
        {
            RuleMatchOperator.Contains => valueToMatch.Contains(matchValue, StringComparison.OrdinalIgnoreCase),
            RuleMatchOperator.Equals => valueToMatch.Equals(matchValue, StringComparison.OrdinalIgnoreCase),
            RuleMatchOperator.StartsWith => valueToMatch.StartsWith(matchValue, StringComparison.OrdinalIgnoreCase),
            RuleMatchOperator.EndsWith => valueToMatch.EndsWith(matchValue, StringComparison.OrdinalIgnoreCase),
            RuleMatchOperator.Regex => IsRegexMatch(valueToMatch, matchValue, logger, ruleId),
            _ => false // Range 等非字符串 operator 用于字符串字段视为不匹配（由组合校验在入库时拦截）
        };
    }

    private static bool MatchAmount(
        RuleMatchOperator op,
        decimal amount,
        string matchValue,
        string? matchValueMax)
    {
        return op switch
        {
            RuleMatchOperator.Equals => TryParseInvariant(matchValue, out var v) && amount == v,
            RuleMatchOperator.Range => IsInAmountRange(amount, matchValue, matchValueMax),
            _ => false
        };
    }

    private static bool IsInAmountRange(decimal amount, string minStr, string? maxStr)
    {
        if (!TryParseInvariant(minStr, out var min)) return false;
        if (amount < min) return false;

        if (!string.IsNullOrWhiteSpace(maxStr))
        {
            if (!TryParseInvariant(maxStr, out var max)) return false;
            if (amount > max) return false;
        }
        return true;
    }

    private static bool IsRegexMatch(string input, string pattern, ILogger? logger, long? ruleId)
    {
        try
        {
            return Regex.IsMatch(input, pattern, RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1));
        }
        catch (RegexMatchTimeoutException)
        {
            logger?.LogWarning(
                "正则匹配超时，视为不匹配, RuleId={RuleId}, Pattern={Pattern}, InputLength={InputLength}",
                ruleId, pattern, input?.Length ?? 0);
            return false;
        }
        catch (ArgumentException ex)
        {
            logger?.LogWarning(
                "正则匹配失败，视为不匹配, RuleId={RuleId}, Pattern={Pattern}, Error={Error}",
                ruleId, pattern, ex.Message);
            return false;
        }
    }

    private static bool TryParseInvariant(string s, out decimal value)
    {
        return decimal.TryParse(s, NumberStyles.Number, CultureInfo.InvariantCulture, out value);
    }
}
