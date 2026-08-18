using System.Globalization;
using FinanceApp.Application.Common;
using FinanceApp.Domain.Constants;

namespace FinanceApp.Application.Modules.MasterData.Services;

internal static class SiteBrandValidator
{
    public static string NormalizeRequiredName(string? value, string fieldName, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ValidationException(string.Format(CultureInfo.InvariantCulture, "{0}不能为空", fieldName));
        }

        var normalized = Normalize(value);
        EnsureSafe(normalized, fieldName, maxLength);
        return normalized;
    }

    public static string NormalizeOptionalName(string? value, string fieldName, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var normalized = Normalize(value);
        if (normalized.Length == 0)
        {
            return string.Empty;
        }

        EnsureSafe(normalized, fieldName, maxLength);
        return normalized;
    }

    public static void ValidateBrandValue(string key, string? value)
    {
        if (key == SiteBrandDefaults.SiteNameKey)
        {
            NormalizeRequiredName(value, "站点名称", SiteBrandDefaults.SiteNameMaxLength);
            return;
        }

        if (key == SiteBrandDefaults.SiteNameEnKey)
        {
            NormalizeOptionalName(value, "英文副标题", SiteBrandDefaults.SiteNameEnMaxLength);
        }
    }

    public static bool IsBrandKey(string key)
    {
        return key == SiteBrandDefaults.SiteNameKey || key == SiteBrandDefaults.SiteNameEnKey;
    }

    private static string Normalize(string value)
    {
        return value.Trim();
    }

    private static void EnsureSafe(string value, string fieldName, int maxLength)
    {
        if (value.Length > maxLength)
        {
            throw new ValidationException(
                string.Format(CultureInfo.InvariantCulture, "{0}长度不能超过{1}个字符", fieldName, maxLength));
        }

        if (ContainsUnsafeCharacters(value))
        {
            throw new ValidationException($"{fieldName}不能包含 HTML 标签或控制字符");
        }
    }

    private static bool ContainsUnsafeCharacters(string value)
    {
        foreach (var ch in value)
        {
            if (ch is '<' or '>')
            {
                return true;
            }

            if (char.IsControl(ch))
            {
                return true;
            }
        }

        return false;
    }
}
