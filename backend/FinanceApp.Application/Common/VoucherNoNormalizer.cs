using FinanceApp.Domain.Constants;

namespace FinanceApp.Application.Common;

/// <summary>
/// 凭证号规范化：去空白、空值转 null、拒绝控制字符与超长值。
/// </summary>
public static class VoucherNoNormalizer
{
    public static string? Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        if (trimmed.Any(char.IsControl))
        {
            throw new ValidationException("凭证号不能包含控制字符");
        }

        if (trimmed.Length > ValidationConstants.Transaction.VoucherNoMaxLength)
        {
            throw new ValidationException(
                $"凭证号长度不能超过{ValidationConstants.Transaction.VoucherNoMaxLength}个字符");
        }

        return trimmed;
    }
}
