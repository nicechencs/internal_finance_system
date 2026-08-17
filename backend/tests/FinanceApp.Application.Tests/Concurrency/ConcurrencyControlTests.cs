using FluentAssertions;
using FinanceApp.Application.Common;
using FinanceApp.Application.Modules.Identity.Interfaces;
using FinanceApp.Application.Modules.MasterData.Interfaces;
using FinanceApp.Application.Modules.TransactionProcessing.DTOs;
using FinanceApp.Application.Modules.TransactionProcessing.Interfaces;
using FinanceApp.Application.Modules.TransactionProcessing.Services;
using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Enums;
using FinanceApp.Domain.Interfaces;
using FinanceApp.Infrastructure.Data;
using FinanceApp.Infrastructure.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace FinanceApp.Application.Tests.Concurrency;

/// <summary>
/// 乐观并发控制测试。
///
/// 现有集成测试基建使用 EF Core InMemory 提供商，InMemory 不支持真正的
/// 并发令牌 WHERE 语义（无法在 UPDATE 时基于 OriginalValue 判断行是否被抢先修改），
/// 因此这里改用 SQLite 内存模式（共享连接）来真实验证并发令牌行为，保持"提供商无关"的方案一致。
/// </summary>
public class ConcurrencyControlTests
{
    /// <summary>
    /// 共享同一条 SQLite 内存连接的测试库封装。
    /// 连接保持打开，内存数据库在多个 DbContext 之间保持存活，从而可以模拟"多个上下文"并发场景。
    /// </summary>
    private sealed class ConcurrencyTestDatabase : IDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly DbContextOptions<AppDbContext> _options;

        public ConcurrencyTestDatabase()
        {
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            _options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(_connection)
                .Options;

            using var ctx = new AppDbContext(_options);
            ctx.Database.EnsureCreated();
        }

        public AppDbContext NewContext() => new(_options);

        public void Dispose() => _connection.Dispose();
    }

    // ---------------------------------------------------------------------
    // 测试 1：并发令牌基础行为 —— 两个独立上下文修改同一 Account 余额
    // ---------------------------------------------------------------------
    [Fact]
    public async Task Account_TwoContextsUpdateBalance_SecondSaveThrowsAndVersionIncrements()
    {
        using var db = new ConcurrencyTestDatabase();

        long accountId;
        await using (var seed = db.NewContext())
        {
            var account = new Account
            {
                Name = "并发测试账户",
                AccountType = AccountType.Bank,
                Currency = "CNY",
                OpeningBalance = 1000m,
                CurrentBalance = 1000m,
                IsActive = true
            };
            seed.Accounts.Add(account);
            await seed.SaveChangesAsync();
            accountId = account.Id;
        }

        await using var ctxA = db.NewContext();
        await using var ctxB = db.NewContext();

        var accountA = await ctxA.Accounts.FirstAsync(a => a.Id == accountId);
        var accountB = await ctxB.Accounts.FirstAsync(a => a.Id == accountId);

        accountA.Version.Should().Be(0, "初始版本应为 0");
        accountB.Version.Should().Be(0);

        // 上下文 A 先提交
        accountA.CurrentBalance -= 100m;
        await ctxA.SaveChangesAsync();

        // 上下文 B 基于旧版本提交，必须触发并发冲突
        accountB.CurrentBalance -= 50m;
        var act = async () => await ctxB.SaveChangesAsync();
        await act.Should().ThrowAsync<DbUpdateConcurrencyException>("后保存者持有过期版本，应被并发令牌拦截");

        await using var verify = db.NewContext();
        var final = await verify.Accounts.FirstAsync(a => a.Id == accountId);
        final.Version.Should().Be(1, "成功保存后版本应自增为 1");
        final.CurrentBalance.Should().Be(900m, "只有上下文 A 的修改应落库");
    }

    // ---------------------------------------------------------------------
    // 测试 2：无项目应付款的并发付款 —— 第二次（持旧数据）失败，RemainingAmount 不为负
    // ---------------------------------------------------------------------
    [Fact]
    public async Task Payable_WithoutProject_ConcurrentPayment_SecondFailsAndRemainingNotNegative()
    {
        using var db = new ConcurrencyTestDatabase();

        long payableId;
        await using (var seed = db.NewContext())
        {
            var supplier = new Supplier { Name = "供应商甲", IsActive = true };
            seed.Suppliers.Add(supplier);
            await seed.SaveChangesAsync();

            var payable = new Payable
            {
                SupplierId = supplier.Id,   // 恰好一个对方，且 ProjectId 保持 null（无项目）
                ProjectId = null,
                TotalAmount = 1000m,
                PaidAmount = 0m,
                RemainingAmount = 1000m,
                Status = PayableStatus.Pending
            };
            seed.Payables.Add(payable);
            await seed.SaveChangesAsync();
            payableId = payable.Id;
        }

        await using var ctxA = db.NewContext();
        await using var ctxB = db.NewContext();

        var payableA = await ctxA.Payables.FirstAsync(p => p.Id == payableId);
        var payableB = await ctxB.Payables.FirstAsync(p => p.Id == payableId);

        // 两个上下文都尝试付款 600（合计 1200 > 1000，若无保护第二次会把剩余打成负数）
        payableA.PaidAmount += 600m;
        payableA.RemainingAmount -= 600m;
        await ctxA.SaveChangesAsync();

        payableB.PaidAmount += 600m;
        payableB.RemainingAmount -= 600m;
        var act = async () => await ctxB.SaveChangesAsync();
        await act.Should().ThrowAsync<DbUpdateConcurrencyException>("无项目应付款现在由自身版本令牌保护");

        await using var verify = db.NewContext();
        var final = await verify.Payables.FirstAsync(p => p.Id == payableId);
        final.PaidAmount.Should().Be(600m, "仅第一次付款生效");
        final.RemainingAmount.Should().Be(400m);
        final.RemainingAmount.Should().BeGreaterThanOrEqualTo(0m, "剩余应付金额不得为负");
        final.Version.Should().Be(1);
    }

    // ---------------------------------------------------------------------
    // 测试 3：应收款并发收款关键场景 —— 第二次（持旧数据）失败，RemainingAmount 不为负
    // ---------------------------------------------------------------------
    [Fact]
    public async Task Receivable_ConcurrentReceipt_SecondFailsAndRemainingNotNegative()
    {
        using var db = new ConcurrencyTestDatabase();

        long receivableId;
        await using (var seed = db.NewContext())
        {
            var customer = new Customer { Name = "客户甲", IsActive = true };
            seed.Customers.Add(customer);
            var project = new Project { Name = "项目甲", Status = ProjectStatus.Active };
            seed.Projects.Add(project);
            await seed.SaveChangesAsync();

            var receivable = new Receivable
            {
                ProjectId = project.Id,     // Receivable.ProjectId 必填
                CustomerId = customer.Id,
                TotalAmount = 1000m,
                ReceivedAmount = 0m,
                RemainingAmount = 1000m,
                Status = ReceivableStatus.Pending
            };
            seed.Receivables.Add(receivable);
            await seed.SaveChangesAsync();
            receivableId = receivable.Id;
        }

        await using var ctxA = db.NewContext();
        await using var ctxB = db.NewContext();

        var receivableA = await ctxA.Receivables.FirstAsync(r => r.Id == receivableId);
        var receivableB = await ctxB.Receivables.FirstAsync(r => r.Id == receivableId);

        receivableA.ReceivedAmount += 600m;
        receivableA.RemainingAmount -= 600m;
        await ctxA.SaveChangesAsync();

        receivableB.ReceivedAmount += 600m;
        receivableB.RemainingAmount -= 600m;
        var act = async () => await ctxB.SaveChangesAsync();
        await act.Should().ThrowAsync<DbUpdateConcurrencyException>();

        await using var verify = db.NewContext();
        var final = await verify.Receivables.FirstAsync(r => r.Id == receivableId);
        final.ReceivedAmount.Should().Be(600m);
        final.RemainingAmount.Should().Be(400m);
        final.RemainingAmount.Should().BeGreaterThanOrEqualTo(0m, "剩余应收金额不得为负");
        final.Version.Should().Be(1);
    }

    // ---------------------------------------------------------------------
    // 测试 4：TransferService 冲突路径 —— 抛友好业务异常且事务回滚（余额与交易均未落库）
    // ---------------------------------------------------------------------
    [Fact]
    public async Task Transfer_WhenAccountConcurrentlyModified_ThrowsFriendlyExceptionAndRollsBack()
    {
        using var db = new ConcurrencyTestDatabase();

        // transferCtx 是 TransferService 使用的上下文（仓储与工作单元共享它）
        await using var transferCtx = db.NewContext();

        var fromAccount = new Account
        {
            Name = "转出账户",
            AccountType = AccountType.Bank,
            Currency = "CNY",
            OpeningBalance = 1000m,
            CurrentBalance = 1000m,
            IsActive = true
        };
        var toAccount = new Account
        {
            Name = "转入账户",
            AccountType = AccountType.Bank,
            Currency = "CNY",
            OpeningBalance = 500m,
            CurrentBalance = 500m,
            IsActive = true
        };
        transferCtx.Accounts.AddRange(fromAccount, toAccount);
        await transferCtx.SaveChangesAsync();

        // 另一个上下文抢先修改转出账户（版本 0 -> 1），模拟并发更新
        await using (var otherCtx = db.NewContext())
        {
            var other = await otherCtx.Accounts.FirstAsync(a => a.Id == fromAccount.Id);
            other.CurrentBalance = 700m;
            await otherCtx.SaveChangesAsync();
        }

        var transferService = new TransferService(
            new Repository<Transaction>(transferCtx, NullLogger<Repository<Transaction>>.Instance),
            new Repository<Account>(transferCtx, NullLogger<Repository<Account>>.Instance),
            new UnitOfWork(transferCtx),
            Mock.Of<ITransactionQueryService>(),
            Mock.Of<IAuditLogService>(),
            Mock.Of<IFixedDepositService>(),
            NullLogger<TransferService>.Instance);

        var request = new CreateTransferRequest
        {
            FromAccountId = fromAccount.Id,
            ToAccountId = toAccount.Id,
            Amount = 100m,
            TransactionDate = DateTime.UtcNow,
            Description = "并发冲突测试转账"
        };

        // TransferService 在事务内加载到的是本上下文已跟踪的旧实例（版本 0），提交时应发生冲突
        var act = async () => await transferService.CreateTransferAsync(request);
        var ex = await act.Should().ThrowAsync<ValidationException>("并发冲突应被转换为对用户友好的业务异常");
        ex.Which.Message.Should().Contain("账户正在被其他操作更新");

        // 校验事务已回滚：余额保持另一上下文写入的值，且没有任何交易记录落库
        await using var verify = db.NewContext();
        var fromAfter = await verify.Accounts.FirstAsync(a => a.Id == fromAccount.Id);
        var toAfter = await verify.Accounts.FirstAsync(a => a.Id == toAccount.Id);
        fromAfter.CurrentBalance.Should().Be(700m, "转账回滚后转出账户余额应保持并发写入值，未被转账扣减");
        toAfter.CurrentBalance.Should().Be(500m, "转入账户余额不应变化");

        var anyTransaction = await verify.Transactions.IgnoreQueryFilters().AnyAsync();
        anyTransaction.Should().BeFalse("转账失败后不应残留任何交易记录");
    }
}
