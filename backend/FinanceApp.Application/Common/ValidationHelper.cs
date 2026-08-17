using FinanceApp.Domain.Constants;

namespace FinanceApp.Application.Common;

/// <summary>
/// 验证工具类，提供常用的参数验证方法
/// </summary>
public static class ValidationHelper
{
    /// <summary>
    /// 验证 ID 是否有效（大于 0）
    /// </summary>
    /// <param name="id">要验证的 ID</param>
    /// <param name="paramName">参数名称</param>
    /// <exception cref="ValidationException">当 ID 无效时抛出</exception>
    public static void ValidateId(long id, string paramName = "id")
    {
        if (id <= 0)
        {
            throw new ValidationException(
                $"参数 {paramName} 必须大于 0",
                ErrorCodes.Validation.InvalidFormat);
        }
    }

    /// <summary>
    /// 验证字符串不为空
    /// </summary>
    /// <param name="value">要验证的字符串</param>
    /// <param name="paramName">参数名称</param>
    /// <exception cref="ValidationException">当字符串为空时抛出</exception>
    public static void ValidateRequired(string? value, string paramName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ValidationException(
                $"参数 {paramName} 不能为空",
                ErrorCodes.Validation.Required);
        }
    }

    /// <summary>
    /// 验证对象不为 null
    /// </summary>
    /// <typeparam name="T">对象类型</typeparam>
    /// <param name="value">要验证的对象</param>
    /// <param name="paramName">参数名称</param>
    /// <exception cref="ValidationException">当对象为 null 时抛出</exception>
    public static void ValidateNotNull<T>(T? value, string paramName) where T : class
    {
        if (value == null)
        {
            throw new ValidationException(
                $"参数 {paramName} 不能为 null",
                ErrorCodes.Validation.Required);
        }
    }

    /// <summary>
    /// 验证字符串长度
    /// </summary>
    /// <param name="value">要验证的字符串</param>
    /// <param name="maxLength">最大长度</param>
    /// <param name="paramName">参数名称</param>
    /// <exception cref="ValidationException">当字符串长度超过限制时抛出</exception>
    public static void ValidateMaxLength(string? value, int maxLength, string paramName)
    {
        if (!string.IsNullOrEmpty(value) && value.Length > maxLength)
        {
            throw new ValidationException(
                $"参数 {paramName} 长度不能超过 {maxLength} 个字符",
                ErrorCodes.Validation.OutOfRange);
        }
    }

    /// <summary>
    /// 验证数值范围
    /// </summary>
    /// <param name="value">要验证的数值</param>
    /// <param name="min">最小值</param>
    /// <param name="max">最大值</param>
    /// <param name="paramName">参数名称</param>
    /// <exception cref="ValidationException">当数值超出范围时抛出</exception>
    public static void ValidateRange(decimal value, decimal min, decimal max, string paramName)
    {
        if (value < min || value > max)
        {
            throw new ValidationException(
                $"参数 {paramName} 必须在 {min} 到 {max} 之间",
                ErrorCodes.Validation.OutOfRange);
        }
    }

    /// <summary>
    /// 验证分页参数
    /// </summary>
    /// <param name="page">页码</param>
    /// <param name="pageSize">每页大小</param>
    /// <exception cref="ValidationException">当分页参数无效时抛出</exception>
    public static void ValidatePageRequest(int page, int pageSize)
    {
        if (page < 1)
        {
            throw new ValidationException(
                "页码必须大于等于 1",
                ErrorCodes.Validation.OutOfRange);
        }

        if (pageSize < 1 || pageSize > 100)
        {
            throw new ValidationException(
                "每页大小必须在 1 到 100 之间",
                ErrorCodes.Validation.OutOfRange);
        }
    }

    /// <summary>
    /// 验证枚举值是否有效
    /// </summary>
    /// <typeparam name="TEnum">枚举类型</typeparam>
    /// <param name="value">要验证的值</param>
    /// <param name="paramName">参数名称</param>
    /// <exception cref="ValidationException">当枚举值无效时抛出</exception>
    public static void ValidateEnum<TEnum>(string value, string paramName) where TEnum : struct, Enum
    {
        if (!Enum.TryParse<TEnum>(value, true, out _))
        {
            throw new ValidationException(
                $"参数 {paramName} 的值 '{value}' 不是有效的枚举值",
                ErrorCodes.Validation.InvalidFormat);
        }
    }
}
