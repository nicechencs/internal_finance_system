using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Enums;

namespace FinanceApp.Domain.Tests;

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
    /// 创建测试分类
    /// </summary>
    public static Category CreateCategory(
        long? id = null,
        string name = "测试分类",
        CategoryType categoryType = CategoryType.Expense,
        long? parentId = null,
        int level = 1)
    {
        return new Category
        {
            Id = id ?? _idCounter++,
            Name = name,
            CategoryType = categoryType,
            ParentId = parentId,
            Level = level,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// 创建测试项目
    /// </summary>
    public static Project CreateProject(
        long? id = null,
        string name = "测试项目",
        string code = "TEST001",
        ProjectStatus status = ProjectStatus.Active)
    {
        return new Project
        {
            Id = id ?? _idCounter++,
            Name = name,
            ProjectCode = code,
            Status = status,
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// 创建测试交易
    /// </summary>
    public static Transaction CreateTransaction(
        long? id = null,
        long accountId = 1,
        decimal amount = 1000m,
        TransactionType transactionType = TransactionType.Expense,
        DateTime? transactionDate = null,
        TransactionStatus status = TransactionStatus.Confirmed)
    {
        return new Transaction
        {
            Id = id ?? _idCounter++,
            AccountId = accountId,
            Amount = amount,
            TransactionType = transactionType,
            TransactionDate = transactionDate ?? DateTime.UtcNow,
            Status = status,
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// 创建测试分类规则
    /// </summary>
    public static ClassificationRule CreateClassificationRule(
        long? id = null,
        string ruleName = "测试规则",
        int priority = 10,
        RuleMatchField matchField = RuleMatchField.Description,
        RuleMatchOperator matchOperator = RuleMatchOperator.Contains,
        string matchValue = "test")
    {
        return new ClassificationRule
        {
            Id = id ?? _idCounter++,
            RuleName = ruleName,
            Priority = priority,
            MatchField = matchField,
            MatchOperator = matchOperator,
            MatchValue = matchValue,
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
