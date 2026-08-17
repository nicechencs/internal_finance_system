using FluentAssertions;
using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Enums;
using FinanceApp.Infrastructure.Data;
using FinanceApp.Infrastructure.Tests.Helpers;
using Microsoft.EntityFrameworkCore;

namespace FinanceApp.Infrastructure.Tests.Data;

/// <summary>
/// UnitOfWork 测试，重点验证 ClearChangeTracker 对修复"余额脏写"问题的保障
/// </summary>
public class UnitOfWorkTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly UnitOfWork _unitOfWork;

    public UnitOfWorkTests()
    {
        _context = TestDbContextFactory.CreateInMemoryContext(Guid.NewGuid().ToString());
        _unitOfWork = new UnitOfWork(_context);
    }

    // ──────────────────────────────────────────────
    // ClearChangeTracker 基础行为
    // ──────────────────────────────────────────────

    [Fact]
    public async Task ClearChangeTracker_ShouldDetachModifiedEntities()
    {
        // Arrange - 先保存再修改，使实体进入 Modified 状态
        var account = CreateTestAccount("余额测试账户", 10000m);
        _context.Accounts.Add(account);
        await _unitOfWork.SaveChangesAsync();

        account.CurrentBalance = 99999m;
        _context.Entry(account).State.Should().Be(EntityState.Modified, "修改余额后状态应为 Modified");

        // Act
        _unitOfWork.ClearChangeTracker();

        // Assert
        _context.Entry(account).State.Should().Be(EntityState.Detached,
            "ClearChangeTracker 应把 Modified 实体 Detach，防止脏写");
    }

    [Fact]
    public async Task ClearChangeTracker_ShouldDetachAddedEntities()
    {
        // Arrange
        var account = CreateTestAccount("新增账户", 5000m);
        _context.Accounts.Add(account);
        _context.Entry(account).State.Should().Be(EntityState.Added);

        // Act
        _unitOfWork.ClearChangeTracker();

        // Assert
        _context.Entry(account).State.Should().Be(EntityState.Detached,
            "ClearChangeTracker 应同样清理 Added 状态实体");

        // 验证数据库未写入
        var count = await _context.Accounts.CountAsync();
        count.Should().Be(0);
    }

    [Fact]
    public async Task ClearChangeTracker_ShouldLeaveUnchangedEntitiesIntact()
    {
        // Arrange - 保存后不修改，实体处于 Unchanged 状态
        var account = CreateTestAccount("未修改账户", 8000m);
        _context.Accounts.Add(account);
        await _unitOfWork.SaveChangesAsync();
        _context.Entry(account).State.Should().Be(EntityState.Unchanged);

        // Act
        _unitOfWork.ClearChangeTracker();

        // Assert - Unchanged 实体因 EF 内存机制会被 Detach（ChangeTracker.Clear 行为），不影响数据库
        var dbAccount = await _context.Accounts.FindAsync(account.Id);
        dbAccount.Should().NotBeNull("数据库数据应仍存在");
        dbAccount!.Name.Should().Be("未修改账户");
    }

    // ──────────────────────────────────────────────
    // 核心场景：外层异常回滚后余额不被脏写
    // ──────────────────────────────────────────────

    [Fact]
    public async Task ClearChangeTracker_AfterOuterException_ShouldPreventBalanceFromBeingPersisted()
    {
        // 模拟 ImportService.ConfirmAsync 外层 catch 的修复逻辑：
        // 1. 账户余额在内存中被累加
        // 2. 外层异常触发事务回滚
        // 3. 调用 ClearChangeTracker() 清理所有追踪实体
        // 4. 仅保存 batch 状态 → 账户余额不应落库

        // Arrange
        var account = CreateTestAccount("导入测试账户", 50000m);
        _context.Accounts.Add(account);
        await _unitOfWork.SaveChangesAsync();

        var originalBalance = account.CurrentBalance;

        // 模拟：导入循环内累加余额（收入 3000）
        account.CurrentBalance += 3000m;
        _context.Accounts.Update(account);
        _context.Entry(account).State.Should().Be(EntityState.Modified);

        // 模拟：外层 catch - 事务已回滚，调用 ClearChangeTracker
        _unitOfWork.ClearChangeTracker();

        // 模拟：仅更新 batch 状态（不涉及 account）
        await _unitOfWork.SaveChangesAsync();

        // Assert - 重新从数据库读取，余额应保持原值
        _context.ChangeTracker.Clear();
        var reloadedAccount = await _context.Accounts.FindAsync(account.Id);
        reloadedAccount.Should().NotBeNull();
        reloadedAccount!.CurrentBalance.Should().Be(originalBalance,
            "外层异常回滚后，ClearChangeTracker 应阻止余额变化被持久化");
    }

    // ──────────────────────────────────────────────
    // 回归保证：DetachAddedEntities 行为不受影响
    // ──────────────────────────────────────────────

    [Fact]
    public async Task DetachAddedEntities_ShouldOnlyDetachAddedEntities_NotModified()
    {
        // Arrange
        var savedAccount = CreateTestAccount("已保存账户", 20000m);
        _context.Accounts.Add(savedAccount);
        await _unitOfWork.SaveChangesAsync();

        // 修改已保存账户（Modified 状态）
        savedAccount.CurrentBalance = 99m;
        _context.Accounts.Update(savedAccount);

        // 新增一个未保存账户（Added 状态）
        var newAccount = CreateTestAccount("未保存账户", 100m);
        _context.Accounts.Add(newAccount);

        // Act - DetachAddedEntities 只清理 Added
        _unitOfWork.DetachAddedEntities();

        // Assert
        _context.Entry(newAccount).State.Should().Be(EntityState.Detached,
            "DetachAddedEntities 应 Detach Added 实体");
        _context.Entry(savedAccount).State.Should().Be(EntityState.Modified,
            "DetachAddedEntities 不应影响 Modified 实体");
    }

    // ──────────────────────────────────────────────
    // Helper
    // ──────────────────────────────────────────────

    private static Account CreateTestAccount(string name, decimal balance) => new()
    {
        Name = name,
        AccountNumber = $"ACC{Guid.NewGuid():N}",
        BankName = "测试银行",
        AccountType = AccountType.Bank,
        Currency = "CNY",
        OpeningBalance = balance,
        CurrentBalance = balance,
        IsActive = true,
        CreatedAt = DateTime.UtcNow
    };

    public void Dispose()
    {
        _context.Dispose();
    }
}
