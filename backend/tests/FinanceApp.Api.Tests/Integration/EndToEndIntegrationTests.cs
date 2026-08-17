using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using FinanceApp.Application.Common;
using FinanceApp.Application.Modules.MasterData.DTOs.Account;
using FinanceApp.Application.Modules.MasterData.DTOs.Category;
using FinanceApp.Application.Modules.MasterData.DTOs.Project;
using FinanceApp.Application.Modules.MasterData.DTOs.Rule;
using FinanceApp.Application.Modules.TransactionProcessing.DTOs;
using FinanceApp.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace FinanceApp.Api.Tests.Integration;

/// <summary>
/// 端到端集成测试：完整业务流程
/// </summary>
public class EndToEndIntegrationTests : IntegrationTestBase
{
    public EndToEndIntegrationTests(IntegrationTestFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task CompleteBusinessFlow_FromSetupToReporting()
    {
        // ==================== 第一步：用户认证 ====================
        var token = await GetAuthTokenAsync();
        SetAuthToken(token);

        // ==================== 第二步：基础数据设置 ====================

        // 创建账户
        var createAccountRequest = new CreateAccountRequest
        {
            Name = "公司主账户",
            AccountNumber = "6222000012345678",
            BankName = "工商银行",
            AccountType = "Bank",
            Currency = "CNY",
            InitialBalance = 1000000m
        };
        var accountResponse = await PostAsync("/api/accounts", createAccountRequest);
        accountResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var accountResult = await accountResponse.Content.ReadFromJsonAsync<ApiResponse<AccountDto>>();
        var accountId = accountResult!.Data!.Id;

        // 创建分类
        var createCategoryRequest1 = new CreateCategoryRequest
        {
            Name = "项目收入",
            CategoryType = "Income"
        };
        var categoryResponse1 = await PostAsync("/api/categories", createCategoryRequest1);
        var categoryResult1 = await categoryResponse1.Content.ReadFromJsonAsync<ApiResponse<CategoryDto>>();
        var incomeCategoryId = categoryResult1!.Data!.Id;

        var createCategoryRequest2 = new CreateCategoryRequest
        {
            Name = "人力成本",
            CategoryType = "Expense"
        };
        var categoryResponse2 = await PostAsync("/api/categories", createCategoryRequest2);
        var categoryResult2 = await categoryResponse2.Content.ReadFromJsonAsync<ApiResponse<CategoryDto>>();
        var expenseCategoryId = categoryResult2!.Data!.Id;

        // 创建客户（项目需要关联客户）
        var customer = new FinanceApp.Domain.Entities.Customer
        {
            Name = "测试客户",
            ContactPerson = "张三",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        DbContext.Customers.Add(customer);
        await DbContext.SaveChangesAsync();

        // 创建项目
        var createProjectRequest1 = new CreateProjectRequest
        {
            Name = "项目A",
            ProjectCode = "PROJ-A",
            CustomerId = customer.Id,
            ContractAmount = 500000m,
            StartDate = DateTime.UtcNow
        };
        var projectResponse1 = await PostAsync("/api/projects", createProjectRequest1);
        projectResponse1.EnsureSuccessStatusCode();
        var projectResult1 = await projectResponse1.Content.ReadFromJsonAsync<ApiResponse<ProjectDto>>();
        var projectAId = projectResult1!.Data!.Id;

        var createProjectRequest2 = new CreateProjectRequest
        {
            Name = "项目B",
            ProjectCode = "PROJ-B",
            CustomerId = customer.Id,
            ContractAmount = 300000m,
            StartDate = DateTime.UtcNow
        };
        var projectResponse2 = await PostAsync("/api/projects", createProjectRequest2);
        var projectResult2 = await projectResponse2.Content.ReadFromJsonAsync<ApiResponse<ProjectDto>>();
        var projectBId = projectResult2!.Data!.Id;

        // ==================== 第三步：创建分类规则 ====================
        var createRuleRequest = new CreateRuleRequest
        {
            Name = "工资自动分类",
            CategoryId = expenseCategoryId,
            MatchField = "Description",
            MatchOperator = "Contains",
            MatchValue = "工资",
            Priority = 1
        };
        var ruleResponse = await PostAsync("/api/rules", createRuleRequest);
        ruleResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // ==================== 第四步：创建收入交易 ====================
        var createIncomeRequest = new CreateTransactionRequest
        {
            TransactionDate = DateTime.UtcNow.AddDays(-10),
            TransactionType = "Income",
            Amount = 200000m,
            AccountId = accountId,
            CategoryId = incomeCategoryId,
            ProjectId = projectAId,
            Description = "项目A首期款"
        };
        var incomeResponse = await PostAsync("/api/transactions", createIncomeRequest);
        incomeResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        // ==================== 第五步：创建带分摊的支出交易 ====================
        var createExpenseRequest = new CreateTransactionRequest
        {
            TransactionDate = DateTime.UtcNow.AddDays(-5),
            TransactionType = "Expense",
            Amount = 50000m,
            AccountId = accountId,
            CategoryId = expenseCategoryId,
            Description = "团队工资支出",
            Allocations = new List<CreateAllocationRequest>
            {
                new() { ProjectId = projectAId, Amount = 30000m, Description = "项目A人力" },
                new() { ProjectId = projectBId, Amount = 20000m, Description = "项目B人力" }
            }
        };
        var expenseResponse = await PostAsync("/api/transactions", createExpenseRequest);
        expenseResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var expenseResult = await expenseResponse.Content.ReadFromJsonAsync<ApiResponse<TransactionDto>>();
        var expenseTransactionId = expenseResult!.Data!.Id;

        // ==================== 第六步：验证交易分摊 ====================
        var transactionDetailResponse = await GetAsync<ApiResponse<TransactionDto>>($"/api/transactions/{expenseTransactionId}");
        transactionDetailResponse.Should().NotBeNull();
        transactionDetailResponse!.Data.Should().NotBeNull();
        transactionDetailResponse.Data!.Allocations.Should().HaveCount(2);
        transactionDetailResponse.Data.Allocations!.Sum(a => a.Amount).Should().Be(50000m);

        // ==================== 第七步：查询项目交易 ====================
        var projectATransactionsResponse = await GetAsync<ApiResponse<List<TransactionDto>>>($"/api/transactions/by-project/{projectAId}");
        projectATransactionsResponse.Should().NotBeNull();
        projectATransactionsResponse!.Data.Should().NotBeNull();
        projectATransactionsResponse.Data!.Should().HaveCountGreaterThanOrEqualTo(2); // 收入 + 分摊支出

        // ==================== 第八步：验证规则匹配 ====================
        var matchRequest = new MatchCategoryRequest
        {
            CounterpartyName = "某公司",
            Description = "员工工资发放",
            Amount = 10000m
        };
        var matchResponse = await PostAsync("/api/rules/match", matchRequest);
        var matchResult = await matchResponse.Content.ReadFromJsonAsync<ApiResponse<long?>>();
        matchResult!.Data.Should().Be(expenseCategoryId); // 应该匹配到人力成本分类
    }

    [Fact]
    public async Task AccountBalanceTracking_ShouldBeAccurate()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        SetAuthToken(token);

        var createAccountRequest = new CreateAccountRequest
        {
            Name = "测试账户",
            AccountNumber = "1234567890",
            BankName = "测试银行",
            AccountType = "Bank",
            Currency = "CNY",
            InitialBalance = 10000m
        };
        var accountResponse = await PostAsync("/api/accounts", createAccountRequest);
        var accountResult = await accountResponse.Content.ReadFromJsonAsync<ApiResponse<AccountDto>>();
        var accountId = accountResult!.Data!.Id;

        var createCategoryRequest = new CreateCategoryRequest
        {
            Name = "测试分类",
            CategoryType = "Expense"
        };
        var categoryResponse = await PostAsync("/api/categories", createCategoryRequest);
        var categoryResult = await categoryResponse.Content.ReadFromJsonAsync<ApiResponse<CategoryDto>>();
        var categoryId = categoryResult!.Data!.Id;

        // Act - 创建多笔交易
        var transactions = new[]
        {
            new { TransactionType = "Income", Amount = 5000m, Description = "收入1" },
            new { TransactionType = "Expense", Amount = 2000m, Description = "支出1" },
            new { TransactionType = "Income", Amount = 3000m, Description = "收入2" },
            new { TransactionType = "Expense", Amount = 1500m, Description = "支出2" }
        };

        foreach (var tx in transactions)
        {
            var request = new CreateTransactionRequest
            {
                TransactionDate = DateTime.UtcNow,
                TransactionType = tx.TransactionType,
                Amount = tx.Amount,
                AccountId = accountId,
                CategoryId = categoryId,
                Description = tx.Description
            };
            await PostAsync("/api/transactions", request);
        }

        // Assert - 验证最终余额
        var balanceResponse = await GetAsync<ApiResponse<decimal>>($"/api/transactions/account-balance/{accountId}");
        var expectedBalance = 10000m + 5000m - 2000m + 3000m - 1500m;
        balanceResponse!.Data.Should().Be(expectedBalance);
    }
}
