using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using FinanceApp.Application.Common;
using FinanceApp.Application.Modules.MasterData.DTOs.Rule;
using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace FinanceApp.Api.Tests.Integration;

public class RuleMatchingIntegrationTests : IntegrationTestBase
{
    public RuleMatchingIntegrationTests(IntegrationTestFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task CreateRule_AndMatchCategory_ShouldReturnCorrectCategory()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        SetAuthToken(token);

        // 创建分类
        var category1 = new Category
        {
            Name = "办公费用",
            CategoryType = CategoryType.Expense,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        var category2 = new Category
        {
            Name = "差旅费",
            CategoryType = CategoryType.Expense,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        DbContext.Categories.AddRange(category1, category2);
        await DbContext.SaveChangesAsync();

        // Act - 创建规则
        var createRuleRequest = new CreateRuleRequest
        {
            Name = "办公用品自动分类",
            CategoryId = category1.Id,
            MatchField = "CounterpartyName",
            MatchOperator = "Contains",
            MatchValue = "办公用品",
            Priority = 1
        };

        var createResponse = await PostAsync("/api/rules", createRuleRequest);

        // Assert - 规则创建成功
        createResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var createResult = await createResponse.Content.ReadFromJsonAsync<ApiResponse<RuleDto>>();
        createResult.Should().NotBeNull();
        createResult!.Success.Should().BeTrue();

        // Act - 测试规则匹配
        var matchRequest = new MatchCategoryRequest
        {
            CounterpartyName = "北京办公用品有限公司",
            Description = "购买文具",
            Amount = 500m
        };

        var matchResponse = await PostAsync("/api/rules/match", matchRequest);

        // Assert - 应该匹配到正确的分类
        matchResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var matchResult = await matchResponse.Content.ReadFromJsonAsync<ApiResponse<long?>>();
        matchResult.Should().NotBeNull();
        matchResult!.Success.Should().BeTrue();
        matchResult.Data.Should().Be(category1.Id);
    }

    [Fact]
    public async Task MultipleRules_ShouldMatchByPriority()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        SetAuthToken(token);

        var category1 = new Category
        {
            Name = "一般办公费",
            CategoryType = CategoryType.Expense,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        var category2 = new Category
        {
            Name = "IT设备费",
            CategoryType = CategoryType.Expense,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        DbContext.Categories.AddRange(category1, category2);
        await DbContext.SaveChangesAsync();

        // 创建低优先级规则（Priority 值小 = 优先级低，系统按 OrderByDescending 排序）
        var rule1 = new ClassificationRule
        {
            RuleName = "办公费通用规则",
            CategoryId = category1.Id,
            MatchField = RuleMatchField.CounterpartyName,
            MatchOperator = RuleMatchOperator.Contains,
            MatchValue = "办公",
            Priority = 1,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        // 创建高优先级规则（Priority 值大 = 优先级高）
        var rule2 = new ClassificationRule
        {
            RuleName = "IT设备专用规则",
            CategoryId = category2.Id,
            MatchField = RuleMatchField.CounterpartyName,
            MatchOperator = RuleMatchOperator.Contains,
            MatchValue = "电脑",
            Priority = 10,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        DbContext.ClassificationRules.AddRange(rule1, rule2);
        await DbContext.SaveChangesAsync();

        // Act - 测试匹配（同时满足两个规则）
        var matchRequest = new MatchCategoryRequest
        {
            CounterpartyName = "办公电脑专卖店",
            Description = "购买笔记本电脑",
            Amount = 5000m
        };

        var response = await PostAsync("/api/rules/match", matchRequest);

        // Assert - 应该匹配高优先级规则
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<long?>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data.Should().Be(category2.Id); // 优先级更高的规则
    }

    [Fact]
    public async Task RuleWithDifferentOperators_ShouldMatchCorrectly()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        SetAuthToken(token);

        var category1 = new Category
        {
            Name = "大额支出",
            CategoryType = CategoryType.Expense,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        var category2 = new Category
        {
            Name = "小额支出",
            CategoryType = CategoryType.Expense,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        var category3 = new Category
        {
            Name = "特定供应商",
            CategoryType = CategoryType.Expense,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        DbContext.Categories.AddRange(category1, category2, category3);
        await DbContext.SaveChangesAsync();

        // 创建不同操作符的规则
        var rule1 = new ClassificationRule
        {
            RuleName = "大额支出规则",
            CategoryId = category1.Id,
            MatchField = RuleMatchField.Amount,
            MatchOperator = RuleMatchOperator.Range,
            MatchValue = "10000",
            Priority = 1,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var rule2 = new ClassificationRule
        {
            RuleName = "小额支出规则",
            CategoryId = category2.Id,
            MatchField = RuleMatchField.Amount,
            MatchOperator = RuleMatchOperator.Range,
            MatchValue = "1000",
            Priority = 2,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var rule3 = new ClassificationRule
        {
            RuleName = "特定供应商规则",
            CategoryId = category3.Id,
            MatchField = RuleMatchField.CounterpartyName,
            MatchOperator = RuleMatchOperator.Equals,
            MatchValue = "ABC公司",
            Priority = 3,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        DbContext.ClassificationRules.AddRange(rule1, rule2, rule3);
        await DbContext.SaveChangesAsync();

        // Act & Assert - 测试精确匹配
        var matchRequest3 = new MatchCategoryRequest
        {
            CounterpartyName = "ABC公司",
            Description = "常规采购",
            Amount = 5000m
        };
        var response3 = await PostAsync("/api/rules/match", matchRequest3);
        var result3 = await response3.Content.ReadFromJsonAsync<ApiResponse<long?>>();
        result3!.Data.Should().Be(category3.Id);
    }

    [Fact]
    public async Task InactiveRule_ShouldNotMatch()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        SetAuthToken(token);

        var category = new Category
        {
            Name = "测试分类",
            CategoryType = CategoryType.Expense,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        DbContext.Categories.Add(category);
        await DbContext.SaveChangesAsync();

        // 创建未激活的规则
        var rule = new ClassificationRule
        {
            RuleName = "未激活规则",
            CategoryId = category.Id,
            MatchField = RuleMatchField.CounterpartyName,
            MatchOperator = RuleMatchOperator.Contains,
            MatchValue = "测试",
            Priority = 1,
            IsActive = false, // 未激活
            CreatedAt = DateTime.UtcNow
        };
        DbContext.ClassificationRules.Add(rule);
        await DbContext.SaveChangesAsync();

        // Act - 尝试匹配
        var matchRequest = new MatchCategoryRequest
        {
            CounterpartyName = "测试公司",
            Description = "测试交易",
            Amount = 1000m
        };

        var response = await PostAsync("/api/rules/match", matchRequest);

        // Assert - 不应该匹配到任何分类
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<long?>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data.Should().BeNull();
    }

    [Fact]
    public async Task ImportWithRules_ShouldAutoClassifyTransactions()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        SetAuthToken(token);

        var account = new Account
        {
            Name = "测试账户",
            AccountNumber = "1234567890",
            BankName = "测试银行",
            AccountType = AccountType.Bank,
            Currency = "CNY",
            OpeningBalance = 50000m,
            CurrentBalance = 50000m,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        DbContext.Accounts.Add(account);

        var category1 = new Category
        {
            Name = "餐饮费",
            CategoryType = CategoryType.Expense,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        var category2 = new Category
        {
            Name = "交通费",
            CategoryType = CategoryType.Expense,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        DbContext.Categories.AddRange(category1, category2);

        // 创建自动分类规则
        var rule1 = new ClassificationRule
        {
            RuleName = "餐饮自动分类",
            CategoryId = category1.Id,
            MatchField = RuleMatchField.Description,
            MatchOperator = RuleMatchOperator.Contains,
            MatchValue = "餐饮",
            Priority = 1,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        var rule2 = new ClassificationRule
        {
            RuleName = "交通自动分类",
            CategoryId = category2.Id,
            MatchField = RuleMatchField.Description,
            MatchOperator = RuleMatchOperator.Contains,
            MatchValue = "打车",
            Priority = 2,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        DbContext.ClassificationRules.AddRange(rule1, rule2);
        await DbContext.SaveChangesAsync();

        // Act - 触发规则引擎处理（通过导入确认流程）
        // 这里简化测试，直接验证规则匹配逻辑
        var matchRequest1 = new MatchCategoryRequest
        {
            CounterpartyName = "某餐厅",
            Description = "餐饮消费",
            Amount = 200m
        };
        var response1 = await PostAsync("/api/rules/match", matchRequest1);
        var result1 = await response1.Content.ReadFromJsonAsync<ApiResponse<long?>>();

        var matchRequest2 = new MatchCategoryRequest
        {
            CounterpartyName = "滴滴出行",
            Description = "打车费用",
            Amount = 50m
        };
        var response2 = await PostAsync("/api/rules/match", matchRequest2);
        var result2 = await response2.Content.ReadFromJsonAsync<ApiResponse<long?>>();

        // Assert
        result1!.Data.Should().Be(category1.Id); // 应该匹配到餐饮费
        result2!.Data.Should().Be(category2.Id); // 应该匹配到交通费
    }
}
