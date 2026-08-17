using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Enums;

namespace FinanceApp.Infrastructure.Tests;

/// <summary>
/// 测试数据构建器
/// </summary>
public static class TestDataBuilder
{
    private static long _idCounter = 1;

    /// <summary>
    /// 创建测试用户
    /// </summary>
    public static User CreateUser(
        long? id = null,
        string username = "testuser",
        string email = "test@example.com",
        string passwordHash = "hashedpassword",
        UserRole role = UserRole.Viewer)
    {
        return new User
        {
            Id = id ?? _idCounter++,
            Username = username,
            Email = email,
            PasswordHash = passwordHash,
            Role = role,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// 创建测试账户
    /// </summary>
    public static Account CreateAccount(
        long? id = null,
        string name = "测试账户",
        AccountType accountType = AccountType.Bank,
        string currency = "CNY",
        decimal openingBalance = 0,
        decimal currentBalance = 0)
    {
        return new Account
        {
            Id = id ?? _idCounter++,
            Name = name,
            AccountType = accountType,
            Currency = currency,
            OpeningBalance = openingBalance,
            CurrentBalance = currentBalance,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// 重置 ID 计数器
    /// </summary>
    public static void ResetIdCounter()
    {
        _idCounter = 1;
    }
}
