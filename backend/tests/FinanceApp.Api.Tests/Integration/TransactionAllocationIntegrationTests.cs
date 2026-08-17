using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using FinanceApp.Application.Common;
using FinanceApp.Application.Modules.TransactionProcessing.DTOs;
using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace FinanceApp.Api.Tests.Integration;

public class TransactionAllocationIntegrationTests : IntegrationTestBase
{
    public TransactionAllocationIntegrationTests(IntegrationTestFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task CreateTransaction_WithMultipleAllocations_ShouldDistributeAmountCorrectly()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        SetAuthToken(token);

        var account = new Account
        {
            Name = "公司账户",
            AccountNumber = "1234567890",
            BankName = "测试银行",
            AccountType = AccountType.Bank,
            Currency = "CNY",
            OpeningBalance = 100000m,
            CurrentBalance = 100000m,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        DbContext.Accounts.Add(account);

        var category = new Category
        {
            Name = "项目费用",
            CategoryType = CategoryType.Expense,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        DbContext.Categories.Add(category);

        var project1 = new Project
        {
            Name = "项目A",
            ProjectCode = "PROJ-A",
            Status = ProjectStatus.Active,
            CreatedAt = DateTime.UtcNow
        };
        var project2 = new Project
        {
            Name = "项目B",
            ProjectCode = "PROJ-B",
            Status = ProjectStatus.Active,
            CreatedAt = DateTime.UtcNow
        };
        var project3 = new Project
        {
            Name = "项目C",
            ProjectCode = "PROJ-C",
            Status = ProjectStatus.Active,
            CreatedAt = DateTime.UtcNow
        };
        DbContext.Projects.AddRange(project1, project2, project3);
        await DbContext.SaveChangesAsync();

        // Act - 创建带分摊的交易
        var createRequest = new CreateTransactionRequest
        {
            TransactionDate = DateTime.UtcNow,
            TransactionType = "Expense",
            Amount = 30000m,
            AccountId = account.Id,
            CategoryId = category.Id,
            Description = "多项目共享费用",
            Allocations = new List<CreateAllocationRequest>
            {
                new() { ProjectId = project1.Id, Amount = 10000m, Description = "项目A分摊" },
                new() { ProjectId = project2.Id, Amount = 12000m, Description = "项目B分摊" },
                new() { ProjectId = project3.Id, Amount = 8000m, Description = "项目C分摊" }
            }
        };

        var response = await PostAsync("/api/transactions", createRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<TransactionDto>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();

        // 验证交易记录
        var transaction = await DbContext.Transactions
            .Include(t => t.Allocations)
            .FirstOrDefaultAsync(t => t.Id == result.Data!.Id);

        transaction.Should().NotBeNull();
        transaction!.Amount.Should().Be(30000m);
        transaction.Allocations.Should().HaveCount(3);

        // 验证分摊金额总和
        var totalAllocated = transaction.Allocations.Sum(a => a.Amount);
        totalAllocated.Should().Be(30000m);

        // 验证每个项目的分摊
        transaction.Allocations.Should().Contain(a => a.ProjectId == project1.Id && a.Amount == 10000m);
        transaction.Allocations.Should().Contain(a => a.ProjectId == project2.Id && a.Amount == 12000m);
        transaction.Allocations.Should().Contain(a => a.ProjectId == project3.Id && a.Amount == 8000m);
    }

    [Fact]
    public async Task CreateTransaction_WithAllocationRates_ShouldCalculateAmountsCorrectly()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        SetAuthToken(token);

        var account = new Account
        {
            Name = "测试账户",
            AccountNumber = "9876543210",
            BankName = "测试银行",
            AccountType = AccountType.Bank,
            Currency = "CNY",
            OpeningBalance = 50000m,
            CurrentBalance = 50000m,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        DbContext.Accounts.Add(account);

        var category = new Category
        {
            Name = "人力成本",
            CategoryType = CategoryType.Expense,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        DbContext.Categories.Add(category);

        var project1 = new Project
        {
            Name = "项目X",
            ProjectCode = "PROJ-X",
            Status = ProjectStatus.Active,
            CreatedAt = DateTime.UtcNow
        };
        var project2 = new Project
        {
            Name = "项目Y",
            ProjectCode = "PROJ-Y",
            Status = ProjectStatus.Active,
            CreatedAt = DateTime.UtcNow
        };
        DbContext.Projects.AddRange(project1, project2);
        await DbContext.SaveChangesAsync();

        // Act - 使用分摊比例创建交易
        var createRequest = new CreateTransactionRequest
        {
            TransactionDate = DateTime.UtcNow,
            TransactionType = "Expense",
            Amount = 20000m,
            AccountId = account.Id,
            CategoryId = category.Id,
            Description = "按比例分摊的人力成本",
            Allocations = new List<CreateAllocationRequest>
            {
                new() { ProjectId = project1.Id, AllocationRate = 60m, Description = "项目X 60%" },
                new() { ProjectId = project2.Id, AllocationRate = 40m, Description = "项目Y 40%" }
            }
        };

        var response = await PostAsync("/api/transactions", createRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<TransactionDto>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();

        // 验证分摊金额计算
        var transaction = await DbContext.Transactions
            .Include(t => t.Allocations)
            .FirstOrDefaultAsync(t => t.Id == result.Data!.Id);

        transaction.Should().NotBeNull();
        transaction!.Allocations.Should().HaveCount(2);

        var allocation1 = transaction.Allocations.First(a => a.ProjectId == project1.Id);
        var allocation2 = transaction.Allocations.First(a => a.ProjectId == project2.Id);

        allocation1.Amount.Should().Be(12000m); // 20000 * 60 / 100
        allocation1.AllocationRate.Should().Be(60m);

        allocation2.Amount.Should().Be(8000m); // 20000 * 40 / 100
        allocation2.AllocationRate.Should().Be(40m);
    }

    [Fact]
    public async Task GetTransactionsByProject_ShouldIncludeAllocatedTransactions()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        SetAuthToken(token);

        var account = new Account
        {
            Name = "测试账户",
            AccountNumber = "1111222233",
            BankName = "测试银行",
            AccountType = AccountType.Bank,
            Currency = "CNY",
            OpeningBalance = 100000m,
            CurrentBalance = 100000m,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        DbContext.Accounts.Add(account);

        var category = new Category
        {
            Name = "测试分类",
            CategoryType = CategoryType.Expense,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        DbContext.Categories.Add(category);

        var targetProject = new Project
        {
            Name = "目标项目",
            ProjectCode = "TARGET",
            Status = ProjectStatus.Active,
            CreatedAt = DateTime.UtcNow
        };
        var otherProject = new Project
        {
            Name = "其他项目",
            ProjectCode = "OTHER",
            Status = ProjectStatus.Active,
            CreatedAt = DateTime.UtcNow
        };
        DbContext.Projects.AddRange(targetProject, otherProject);
        await DbContext.SaveChangesAsync();

        // 创建直接关联到目标项目的交易
        var directTransaction = new Transaction
        {
            TransactionDate = DateTime.UtcNow,
            TransactionType = TransactionType.Expense,
            Amount = 5000m,
            AccountId = account.Id,
            CategoryId = category.Id,
            ProjectId = targetProject.Id,
            Description = "直接关联交易",
            CreatedAt = DateTime.UtcNow
        };
        DbContext.Transactions.Add(directTransaction);

        // 创建分摊到目标项目的交易
        var allocatedTransaction = new Transaction
        {
            TransactionDate = DateTime.UtcNow,
            TransactionType = TransactionType.Expense,
            Amount = 10000m,
            AccountId = account.Id,
            CategoryId = category.Id,
            IsAllocated = true,
            Description = "分摊交易",
            CreatedAt = DateTime.UtcNow
        };
        DbContext.Transactions.Add(allocatedTransaction);

        var allocation1 = new TransactionAllocation
        {
            Transaction = allocatedTransaction,
            ProjectId = targetProject.Id,
            Amount = 6000m,
            AllocationRate = 0.6m,
            Description = "目标项目分摊"
        };
        var allocation2 = new TransactionAllocation
        {
            Transaction = allocatedTransaction,
            ProjectId = otherProject.Id,
            Amount = 4000m,
            AllocationRate = 0.4m,
            Description = "其他项目分摊"
        };
        DbContext.TransactionAllocations.AddRange(allocation1, allocation2);
        await DbContext.SaveChangesAsync();

        // Act
        var response = await GetAsync<ApiResponse<List<TransactionDto>>>($"/api/transactions/by-project/{targetProject.Id}");

        // Assert
        response.Should().NotBeNull();
        response!.Success.Should().BeTrue();
        response.Data.Should().NotBeNull();
        response.Data!.Should().HaveCountGreaterThanOrEqualTo(2);

        // 应该包含直接关联的交易
        response.Data.Should().Contain(t => t.Id == directTransaction.Id);

        // 应该包含有分摊到该项目的交易
        var allocatedTx = response.Data.FirstOrDefault(t => t.Id == allocatedTransaction.Id);
        allocatedTx.Should().NotBeNull();
        allocatedTx!.Allocations.Should().NotBeNull();
        allocatedTx.Allocations!.Should().Contain(a => a.ProjectId == targetProject.Id && a.Amount == 6000m);
    }
}
