using FinanceApp.Application.Modules.Identity.DTOs;
using FinanceApp.Domain.Configuration;
using FinanceApp.Domain.Constants;
using FinanceApp.Domain.Enums;

namespace FinanceApp.Application.Common;

public static class AuthValidationHelper
{
    public static string NormalizeUsername(string username)
    {
        return username.Trim().ToUpperInvariant();
    }

    public static void ValidateLoginRequest(LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Username))
        {
            throw new ValidationException("用户名不能为空");
        }

        if (string.IsNullOrWhiteSpace(request.Password))
        {
            throw new ValidationException("密码不能为空");
        }
    }

    public static void ValidatePassword(string password, AuthOptions options, string fieldName = "密码")
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            throw new ValidationException($"{fieldName}不能为空");
        }

        if (password.Length < options.MinPasswordLength)
        {
            throw new ValidationException($"{fieldName}至少 {options.MinPasswordLength} 位");
        }

        if (password.Length > options.MaxPasswordLength)
        {
            throw new ValidationException($"{fieldName}不能超过 {options.MaxPasswordLength} 位");
        }
    }

    public static void ValidateUsername(string username)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            throw new ValidationException("用户名不能为空");
        }

        var trimmedUsername = username.Trim();
        if (trimmedUsername.Length < ValidationConstants.User.UsernameMinLength
            || trimmedUsername.Length > ValidationConstants.User.UsernameMaxLength)
        {
            throw new ValidationException(
                $"用户名长度必须在 {ValidationConstants.User.UsernameMinLength} 到 {ValidationConstants.User.UsernameMaxLength} 位之间");
        }
    }

    public static void ValidateFullName(string fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
        {
            throw new ValidationException("姓名不能为空");
        }

        if (fullName.Trim().Length > ValidationConstants.User.RealNameMaxLength)
        {
            throw new ValidationException($"姓名不能超过 {ValidationConstants.User.RealNameMaxLength} 个字符");
        }
    }

    public static UserRole ParseRole(string role)
    {
        if (!Enum.TryParse<UserRole>(role, true, out var parsedRole))
        {
            throw new ValidationException("无效的用户角色");
        }

        return parsedRole;
    }

    public static string CreateSecurityStamp()
    {
        return Guid.NewGuid().ToString("N");
    }
}
