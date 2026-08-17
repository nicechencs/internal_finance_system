using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using FinanceApp.Application.Common;
using FinanceApp.Application.Modules.MasterData.DTOs.Account;
using FinanceApp.Application.Modules.TransactionProcessing.DTOs;
using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace FinanceApp.Api.Tests.Integration;

/// <summary>
/// 事务原子性集成测试
/// 验证 TransactionService 的 CreateAsync 和 CreateTransferAsync 在异常场景下能正确回滚数据
/// </summary>
public class TransactionAtomicityTests : IntegrationTestBase
{
    public TransactionAtomicityTests(IntegrationTestFactory factory) : base(factory)
    {
    }

    /// <summary>
    /// 测试1：创建带无效分摊的交易时，不应留下孤立的交易记录
    /// 场景：分摊金额之和不等于交易金额，验证失败应阻止任何数据写入
    /// 预期：交易和分摊都不会被创建，账户余额不变
    /// </summary>
    [Fact]
    public async Task CreateTransaction_WithInvalidAllocation_ShouldNotLeaveOrphanTransaction()
    {
        // Arrange - 准备测试数据
        var token = await GetAuthTokenAsync();
        SetAuthToken(token);

        // 创建一个账户，初始余额 50000
        var account = new Account
        {
            Name = "原子性测试账户",
            AccountNumber = "ATOM-001",
            BankName = "测试银行",
            AccountType = AccountType.Bank,
            Currency = "CNY",
            OpeningBalance = 50000m,
            CurrentBalance = 50000m,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        DbContext.Accounts.Add(account);

        // 创建分类（交易必填字段）
        var category = new Category
        {
            Name = "测试费用",
            CategoryType = CategoryType.Expense,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        DbContext.Categories.Add(category);

        // 创建两个项目用于分摊
        var project1 = new Project
        {
            Name = "项目一",
            ProjectCode = "PROJ-001",
            Status = ProjectStatus.Active,
            CreatedAt = DateTime.UtcNow
        };
        var project2 = new Project
        {
            Name = "项目二",
            ProjectCode = "PROJ-002",
            Status = ProjectStatus.Active,
            CreatedAt = DateTime.UtcNow
        };
        DbContext.Projects.AddRange(project1, project2);
        await DbContext.SaveChangesAsync();

        // 记录操作前的账户余额
        var initialBalance = account.CurrentBalance;

        // Act - 创建带无效分摊的交易
        // 交易金额 = 10000，但分摊总金额 = 6000 + 5000 = 11000（不等于交易金额）
        // ValidateAllocations 应抛出 ValidationException，阻止任何数据写入
        var createRequest = new CreateTransactionRequest
        {
            TransactionDate = DateTime.UtcNow,
            TransactionType = "Expense",
            Amount = 10000m,
            AccountId = account.Id,
            CategoryId = category.Id,
            Description = "分摊金额不匹配的交易",
            Allocations = new List<CreateAllocationRequest>
            {
                new() { ProjectId = project1.Id, Amount = 6000m, Description = "项目一分摊" },
                new() { ProjectId = project2.Id, Amount = 5000m, Description = "项目二分摊（金额故意多出1000）" }
            }
        };

        var response = await PostAsync("/api/transactions", createRequest);

        // Assert - 请求应该失败（分摊金额之和不等于交易金额）
        response.StatusCode.Should().NotBe(HttpStatusCode.OK,
            "分摊金额之和（11000）不等于交易金额（10000），请求不应成功");

        // 清除 EF Core 跟踪缓存，确保从数据库重新读取
        DbContext.ChangeTracker.Clear();

        // 验证没有孤立的交易记录被创建
        var orphanTransactions = await DbContext.Transactions
            .IgnoreQueryFilters()
            .Where(t => t.Description == "分摊金额不匹配的交易")
            .ToListAsync();
        orphanTransactions.Should().BeEmpty("验证失败后不应存在孤立的交易记录");

        // 验证没有孤立的分摊记录被创建
        var orphanAllocations = await DbContext.TransactionAllocations
            .IgnoreQueryFilters()
            .Where(a => a.Description!.Contains("项目一分摊") || a.Description!.Contains("项目二分摊"))
            .ToListAsync();
        orphanAllocations.Should().BeEmpty("验证失败后不应存在孤立的分摊记录");

        // 验证账户余额没有被修改
        var accountAfter = await DbContext.Accounts
            .IgnoreQueryFilters()
            .FirstAsync(a => a.Id == account.Id);
        accountAfter.CurrentBalance.Should().Be(initialBalance,
            "操作失败后账户余额应保持不变");
    }

    /// <summary>
    /// 测试2：转账操作应原子性地更新两个账户余额
    /// 场景：从账户A转账到账户B，两个账户余额都应正确更新
    /// 预期：转出账户余额减少，转入账户余额增加，且金额一致
    /// </summary>
    [Fact]
    public async Task CreateTransfer_ShouldUpdateBothAccountsAtomically()
    {
        // Arrange - 准备两个账户
        var token = await GetAuthTokenAsync();
        SetAuthToken(token);

        // 转出账户，初始余额 100000
        var fromAccount = new Account
        {
            Name = "转出账户",
            AccountNumber = "FROM-001",
            BankName = "测试银行A",
            AccountType = AccountType.Bank,
            Currency = "CNY",
            OpeningBalance = 100000m,
            CurrentBalance = 100000m,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        // 转入账户，初始余额 20000
        var toAccount = new Account
        {
            Name = "转入账户",
            AccountNumber = "TO-001",
            BankName = "测试银行B",
            AccountType = AccountType.Bank,
            Currency = "CNY",
            OpeningBalance = 20000m,
            CurrentBalance = 20000m,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        DbContext.Accounts.AddRange(fromAccount, toAccount);
        await DbContext.SaveChangesAsync();

        var transferAmount = 30000m;

        // Act - 执行转账
        var transferRequest = new CreateTransferRequest
        {
            FromAccountId = fromAccount.Id,
            ToAccountId = toAccount.Id,
            Amount = transferAmount,
            TransactionDate = DateTime.UtcNow,
            Description = "原子性测试转账"
        };

        var response = await PostAsync("/api/transactions/transfer", transferRequest);

        // Assert - 转账应成功
        response.StatusCode.Should().Be(HttpStatusCode.OK, "正常转账请求应成功");

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<TransferResultDto>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue("转账应成功完成");
        result.Data.Should().NotBeNull();

        // 验证创建了两笔配对交易（转出和转入）
        result.Data!.OutTransaction.Should().NotBeNull("应创建转出交易");
        result.Data.InTransaction.Should().NotBeNull("应创建转入交易");
        result.Data.OutTransaction.Amount.Should().Be(transferAmount, "转出金额应等于转账金额");
        result.Data.InTransaction.Amount.Should().Be(transferAmount, "转入金额应等于转账金额");

        // 清除跟踪缓存，从数据库重新读取账户余额
        DbContext.ChangeTracker.Clear();

        // 验证转出账户余额正确减少
        var fromAccountAfter = await DbContext.Accounts
            .IgnoreQueryFilters()
            .FirstAsync(a => a.Id == fromAccount.Id);
        fromAccountAfter.CurrentBalance.Should().Be(100000m - transferAmount,
            "转出账户余额应减少转账金额");

        // 验证转入账户余额正确增加
        var toAccountAfter = await DbContext.Accounts
            .IgnoreQueryFilters()
            .FirstAsync(a => a.Id == toAccount.Id);
        toAccountAfter.CurrentBalance.Should().Be(20000m + transferAmount,
            "转入账户余额应增加转账金额");

        // 验证两笔交易通过 RelatedTransactionId 互相关联
        var outTx = await DbContext.Transactions
            .IgnoreQueryFilters()
            .FirstAsync(t => t.Id == result.Data.OutTransaction.Id);
        var inTx = await DbContext.Transactions
            .IgnoreQueryFilters()
            .FirstAsync(t => t.Id == result.Data.InTransaction.Id);

        outTx.RelatedTransactionId.Should().Be(inTx.Id, "转出交易应关联转入交易");
        inTx.RelatedTransactionId.Should().Be(outTx.Id, "转入交易应关联转出交易");
    }

    /// <summary>
    /// 测试3：创建带分摊的交易，验证账户余额正确更新
    /// 场景：验证创建带分摊的交易对账户余额的影响
    /// 预期：创建交易后余额正确更新，分摊记录正确创建
    /// 注意：已分摊的交易不允许删除（业务规则），因此本测试不包含删除步骤
    /// </summary>
    [Fact]
    public async Task CreateTransaction_WithAllocations_ShouldMaintainAccountBalance()
    {
        // Arrange - 准备测试数据
        var token = await GetAuthTokenAsync();
        SetAuthToken(token);

        // 创建账户，初始余额 80000
        var account = new Account
        {
            Name = "余额一致性账户",
            AccountNumber = "BAL-001",
            BankName = "测试银行",
            AccountType = AccountType.Bank,
            Currency = "CNY",
            OpeningBalance = 80000m,
            CurrentBalance = 80000m,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        DbContext.Accounts.Add(account);

        var category = new Category
        {
            Name = "项目支出",
            CategoryType = CategoryType.Expense,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        DbContext.Categories.Add(category);

        var project1 = new Project
        {
            Name = "项目甲",
            ProjectCode = "PRJ-A",
            Status = ProjectStatus.Active,
            CreatedAt = DateTime.UtcNow
        };
        var project2 = new Project
        {
            Name = "项目乙",
            ProjectCode = "PRJ-B",
            Status = ProjectStatus.Active,
            CreatedAt = DateTime.UtcNow
        };
        DbContext.Projects.AddRange(project1, project2);
        await DbContext.SaveChangesAsync();

        // 记录初始余额
        var initialBalance = account.CurrentBalance;

        // Act 1 - 创建带分摊的支出交易
        var transactionAmount = 15000m;
        var createRequest = new CreateTransactionRequest
        {
            TransactionDate = DateTime.UtcNow,
            TransactionType = "Expense",
            Amount = transactionAmount,
            AccountId = account.Id,
            CategoryId = category.Id,
            Description = "带分摊的项目支出",
            Allocations = new List<CreateAllocationRequest>
            {
                new() { ProjectId = project1.Id, Amount = 9000m, Description = "项目甲分摊 60%" },
                new() { ProjectId = project2.Id, Amount = 6000m, Description = "项目乙分摊 40%" }
            }
        };

        var createResponse = await PostAsync("/api/transactions", createRequest);

        // Assert 1 - 交易创建成功
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created, "带有效分摊的交易应创建成功");
        var createResult = await createResponse.Content.ReadFromJsonAsync<ApiResponse<TransactionDto>>();
        createResult.Should().NotBeNull();
        createResult!.Success.Should().BeTrue();
        createResult.Data.Should().NotBeNull();

        var transactionId = createResult.Data!.Id;

        // 清除跟踪缓存
        DbContext.ChangeTracker.Clear();

        // 验证账户余额已更新（支出应减少余额）
        var accountAfterCreate = await DbContext.Accounts
            .IgnoreQueryFilters()
            .FirstAsync(a => a.Id == account.Id);
        accountAfterCreate.CurrentBalance.Should().Be(initialBalance - transactionAmount,
            "支出交易创建后，账户余额应减少相应金额");

        // 验证分摊记录已创建
        var allocations = await DbContext.TransactionAllocations
            .IgnoreQueryFilters()
            .Where(a => a.TransactionId == transactionId)
            .ToListAsync();
        allocations.Should().HaveCount(2, "应创建两条分摊记录");
        allocations.Sum(a => a.Amount).Should().Be(transactionAmount, "分摊总金额应等于交易金额");

        // 验证交易标记为已分摊
        var transaction = await DbContext.Transactions
            .IgnoreQueryFilters()
            .FirstAsync(t => t.Id == transactionId);
        transaction.IsAllocated.Should().BeTrue("交易应标记为已分摊");
    }

    [Fact]
    public async Task ConvertImportedExpenseToTransfer_WithMatchedTransaction_ShouldConvertBothTransactionsWithoutChangingBalances()
    {
        var token = await GetAuthTokenAsync();
        SetAuthToken(token);

        var fromAccount = new Account
        {
            Name = "活期账户",
            AccountNumber = "CHK-001",
            BankName = "测试银行A",
            AccountType = AccountType.Bank,
            Currency = "CNY",
            OpeningBalance = 100000m,
            CurrentBalance = 80000m,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var toAccount = new Account
        {
            Name = "定期账户",
            AccountNumber = "FD-001",
            BankName = "测试银行A",
            AccountType = AccountType.FixedDeposit,
            Currency = "CNY",
            OpeningBalance = 0m,
            CurrentBalance = 20000m,
            IsActive = true,
            InterestStartDate = DateTime.UtcNow.Date,
            MaturityDate = DateTime.UtcNow.Date.AddMonths(3),
            InterestRate = 1.8m,
            CreatedAt = DateTime.UtcNow
        };

        DbContext.Accounts.AddRange(fromAccount, toAccount);
        await DbContext.SaveChangesAsync();

        var sourceBankTransaction = new BankTransaction
        {
            AccountId = fromAccount.Id,
            TransactionDate = DateTime.UtcNow.Date,
            Amount = 20000m,
            Direction = BankTransactionDirection.Out,
            Counterparty = "定期存款",
            Memo = "转存定期",
            UniqueHash = Guid.NewGuid().ToString("N"),
            IsProcessed = true
        };

        var targetBankTransaction = new BankTransaction
        {
            AccountId = toAccount.Id,
            TransactionDate = DateTime.UtcNow.Date,
            Amount = 20000m,
            Direction = BankTransactionDirection.In,
            Counterparty = "活期账户",
            Memo = "转入定期",
            UniqueHash = Guid.NewGuid().ToString("N"),
            IsProcessed = true
        };

        DbContext.BankTransactions.AddRange(sourceBankTransaction, targetBankTransaction);
        await DbContext.SaveChangesAsync();

        var sourceTransaction = new Transaction
        {
            BankTransactionId = sourceBankTransaction.Id,
            TransactionDate = sourceBankTransaction.TransactionDate,
            Amount = 20000m,
            TransactionType = TransactionType.Expense,
            AccountId = fromAccount.Id,
            Description = "转存定期",
            Status = TransactionStatus.Confirmed,
            IsAllocated = false,
            CreatedAt = DateTime.UtcNow
        };

        var targetTransaction = new Transaction
        {
            BankTransactionId = targetBankTransaction.Id,
            TransactionDate = targetBankTransaction.TransactionDate,
            Amount = 20000m,
            TransactionType = TransactionType.Income,
            AccountId = toAccount.Id,
            Description = "转入定期",
            Status = TransactionStatus.Confirmed,
            IsAllocated = false,
            CreatedAt = DateTime.UtcNow
        };

        DbContext.Transactions.AddRange(sourceTransaction, targetTransaction);
        await DbContext.SaveChangesAsync();
        DbContext.ChangeTracker.Clear();

        var response = await PostAsync($"/api/transactions/{sourceTransaction.Id}/convert-to-transfer", new ConvertTransactionToTransferRequest
        {
            TargetAccountId = toAccount.Id,
            MatchedTransactionId = targetTransaction.Id
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        DbContext.ChangeTracker.Clear();

        var sourceAfter = await DbContext.Transactions.IgnoreQueryFilters().FirstAsync(t => t.Id == sourceTransaction.Id);
        var targetAfter = await DbContext.Transactions.IgnoreQueryFilters().FirstAsync(t => t.Id == targetTransaction.Id);
        var fromAccountAfter = await DbContext.Accounts.IgnoreQueryFilters().FirstAsync(a => a.Id == fromAccount.Id);
        var toAccountAfter = await DbContext.Accounts.IgnoreQueryFilters().FirstAsync(a => a.Id == toAccount.Id);

        sourceAfter.TransactionType.Should().Be(TransactionType.Transfer);
        sourceAfter.TransferDirection.Should().Be(TransferDirection.Out);
        sourceAfter.RelatedTransactionId.Should().Be(targetTransaction.Id);
        sourceAfter.CategoryId.Should().BeNull();

        targetAfter.TransactionType.Should().Be(TransactionType.Transfer);
        targetAfter.TransferDirection.Should().Be(TransferDirection.In);
        targetAfter.RelatedTransactionId.Should().Be(sourceTransaction.Id);
        targetAfter.CategoryId.Should().BeNull();

        fromAccountAfter.CurrentBalance.Should().Be(80000m);
        toAccountAfter.CurrentBalance.Should().Be(20000m);
    }

    [Fact]
    public async Task ConvertImportedExpenseToTransfer_WithoutMatchedTransaction_ShouldCreateCounterTransactionAndUpdateTargetBalance()
    {
        var token = await GetAuthTokenAsync();
        SetAuthToken(token);

        var fromAccount = new Account
        {
            Name = "活期账户",
            AccountNumber = "CHK-002",
            BankName = "测试银行B",
            AccountType = AccountType.Bank,
            Currency = "CNY",
            OpeningBalance = 50000m,
            CurrentBalance = 30000m,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var toAccount = new Account
        {
            Name = "定期账户",
            AccountNumber = "FD-002",
            BankName = "测试银行B",
            AccountType = AccountType.FixedDeposit,
            Currency = "CNY",
            OpeningBalance = 0m,
            CurrentBalance = 0m,
            IsActive = true,
            InterestStartDate = DateTime.UtcNow.Date,
            MaturityDate = DateTime.UtcNow.Date.AddMonths(6),
            InterestRate = 2.0m,
            CreatedAt = DateTime.UtcNow
        };

        DbContext.Accounts.AddRange(fromAccount, toAccount);
        await DbContext.SaveChangesAsync();

        var sourceBankTransaction = new BankTransaction
        {
            AccountId = fromAccount.Id,
            TransactionDate = DateTime.UtcNow.Date,
            Amount = 20000m,
            Direction = BankTransactionDirection.Out,
            Counterparty = "定期存款",
            Memo = "转存定期",
            UniqueHash = Guid.NewGuid().ToString("N"),
            IsProcessed = true
        };

        DbContext.BankTransactions.Add(sourceBankTransaction);
        await DbContext.SaveChangesAsync();

        var sourceTransaction = new Transaction
        {
            BankTransactionId = sourceBankTransaction.Id,
            TransactionDate = sourceBankTransaction.TransactionDate,
            Amount = 20000m,
            TransactionType = TransactionType.Expense,
            AccountId = fromAccount.Id,
            Description = "转存定期",
            Status = TransactionStatus.Confirmed,
            IsAllocated = false,
            CreatedAt = DateTime.UtcNow
        };

        DbContext.Transactions.Add(sourceTransaction);
        await DbContext.SaveChangesAsync();
        DbContext.ChangeTracker.Clear();

        var response = await PostAsync($"/api/transactions/{sourceTransaction.Id}/convert-to-transfer", new ConvertTransactionToTransferRequest
        {
            TargetAccountId = toAccount.Id
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        DbContext.ChangeTracker.Clear();

        var transferTransactions = await DbContext.Transactions
            .IgnoreQueryFilters()
            .Where(t => t.TransactionType == TransactionType.Transfer)
            .ToListAsync();

        transferTransactions.Should().HaveCount(2);
        transferTransactions.Should().Contain(t => t.AccountId == fromAccount.Id && t.TransferDirection == TransferDirection.Out);
        transferTransactions.Should().Contain(t => t.AccountId == toAccount.Id && t.TransferDirection == TransferDirection.In);

        var fromAccountAfter = await DbContext.Accounts.IgnoreQueryFilters().FirstAsync(a => a.Id == fromAccount.Id);
        var toAccountAfter = await DbContext.Accounts.IgnoreQueryFilters().FirstAsync(a => a.Id == toAccount.Id);

        fromAccountAfter.CurrentBalance.Should().Be(30000m);
        toAccountAfter.CurrentBalance.Should().Be(20000m);
    }
}
