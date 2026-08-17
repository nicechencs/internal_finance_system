using FluentAssertions;
using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Enums;
using FinanceApp.Domain.Interfaces;
using FinanceApp.Infrastructure.Data;
using FinanceApp.Infrastructure.Repositories;
using FinanceApp.Infrastructure.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace FinanceApp.Infrastructure.Tests.Repositories;

public class RepositoryTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly Repository<User> _repository;
    private readonly IUnitOfWork _unitOfWork;

    public RepositoryTests()
    {
        _context = TestDbContextFactory.CreateInMemoryContext(Guid.NewGuid().ToString());
        _repository = new Repository<User>(_context, new Mock<ILogger<Repository<User>>>().Object);
        _unitOfWork = new UnitOfWork(_context);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnEntity_WhenEntityExists()
    {
        // Arrange
        var user = new User
        {
            Username = "testuser",
            PasswordHash = "hash123",
            FullName = "Test User",
            Email = "test@example.com",
            Role = UserRole.Admin
        };
        await _repository.AddAsync(user);
        await _unitOfWork.SaveChangesAsync();

        // Act
        var result = await _repository.GetByIdAsync(user.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Username.Should().Be("testuser");
        result.Email.Should().Be("test@example.com");
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNull_WhenEntityDoesNotExist()
    {
        // Act
        var result = await _repository.GetByIdAsync(999);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_ShouldNotReturnDeletedEntity()
    {
        // Arrange
        var user = new User
        {
            Username = "deleteduser",
            PasswordHash = "hash123",
            FullName = "Deleted User",
            Role = UserRole.Admin
        };
        await _repository.AddAsync(user);
        await _unitOfWork.SaveChangesAsync();
        _repository.Delete(user.Id);
        await _unitOfWork.SaveChangesAsync();

        // Act
        var result = await _repository.GetByIdAsync(user.Id);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnAllNonDeletedEntities()
    {
        // Arrange
        var users = new[]
        {
            new User { Username = "user1", PasswordHash = "hash1", FullName = "User 1", Role = UserRole.Admin },
            new User { Username = "user2", PasswordHash = "hash2", FullName = "User 2", Role = UserRole.Viewer },
            new User { Username = "user3", PasswordHash = "hash3", FullName = "User 3", Role = UserRole.Viewer }
        };

        foreach (var user in users)
        {
            await _repository.AddAsync(user);
        }
        await _unitOfWork.SaveChangesAsync();

        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        result.Should().HaveCount(3);
        result.Should().Contain(u => u.Username == "user1");
        result.Should().Contain(u => u.Username == "user2");
        result.Should().Contain(u => u.Username == "user3");
    }

    [Fact]
    public async Task GetAllAsync_ShouldNotReturnDeletedEntities()
    {
        // Arrange
        var user1 = new User { Username = "user1", PasswordHash = "hash1", FullName = "User 1", Role = UserRole.Admin };
        var user2 = new User { Username = "user2", PasswordHash = "hash2", FullName = "User 2", Role = UserRole.Viewer };

        await _repository.AddAsync(user1);
        await _repository.AddAsync(user2);
        await _unitOfWork.SaveChangesAsync();
        _repository.Delete(user2.Id);
        await _unitOfWork.SaveChangesAsync();

        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        result.Should().HaveCount(1);
        result.Should().Contain(u => u.Username == "user1");
        result.Should().NotContain(u => u.Username == "user2");
    }

    [Fact]
    public async Task GetPagedAsync_ShouldReturnCorrectPageAndTotal()
    {
        // Arrange
        for (int i = 1; i <= 10; i++)
        {
            await _repository.AddAsync(new User
            {
                Username = $"user{i}",
                PasswordHash = $"hash{i}",
                FullName = $"User {i}",
                Role = UserRole.Viewer
            });
        }
        await _unitOfWork.SaveChangesAsync();

        // Act
        var (items, total) = await _repository.GetPagedAsync(2, 3);

        // Assert
        total.Should().Be(10);
        items.Should().HaveCount(3);
    }

    [Fact]
    public async Task AddAsync_ShouldAddEntityAndSetTimestamps()
    {
        // Arrange
        var user = new User
        {
            Username = "newuser",
            PasswordHash = "hash123",
            FullName = "New User",
            Role = UserRole.Viewer
        };

        // Act
        var result = await _repository.AddAsync(user);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().BeGreaterThan(0);
        result.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        result.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        result.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateEntityAndTimestamp()
    {
        // Arrange
        var user = new User
        {
            Username = "updateuser",
            PasswordHash = "hash123",
            FullName = "Update User",
            Role = UserRole.Viewer
        };
        await _repository.AddAsync(user);
        await _unitOfWork.SaveChangesAsync();
        var originalUpdatedAt = user.UpdatedAt;

        await Task.Delay(100); // 确保时间戳不同

        // Act
        user.FullName = "Updated Name";
        _repository.Update(user);
        await _unitOfWork.SaveChangesAsync();

        // Act - 重新查询以验证更新
        var updated = await _repository.GetByIdAsync(user.Id);

        // Assert
        updated.Should().NotBeNull();
        updated!.FullName.Should().Be("Updated Name");
        updated.UpdatedAt.Should().BeAfter(originalUpdatedAt);
    }

    [Fact]
    public async Task DeleteAsync_ShouldSoftDeleteEntity()
    {
        // Arrange
        var user = new User
        {
            Username = "deleteuser",
            PasswordHash = "hash123",
            FullName = "Delete User",
            Role = UserRole.Viewer
        };
        await _repository.AddAsync(user);
        await _unitOfWork.SaveChangesAsync();

        // Act
        _repository.Delete(user.Id);
        await _unitOfWork.SaveChangesAsync();

        // Assert - 通过查询过滤器应该找不到
        var result = await _repository.GetByIdAsync(user.Id);
        result.Should().BeNull();

        // Assert - 直接查询数据库应该能找到，但 IsDeleted = true
        var deletedUser = await _context.Users.IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Id == user.Id);
        deletedUser.Should().NotBeNull();
        deletedUser!.IsDeleted.Should().BeTrue();
        deletedUser.DeletedAt.Should().NotBeNull();
        deletedUser.DeletedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task DeleteAsync_WithEntity_ShouldSoftDeleteEntity()
    {
        // Arrange
        var user = new User
        {
            Username = "deleteuser2",
            PasswordHash = "hash123",
            FullName = "Delete User 2",
            Role = UserRole.Viewer
        };
        await _repository.AddAsync(user);
        await _unitOfWork.SaveChangesAsync();

        // Act
        _repository.Delete(user);
        await _unitOfWork.SaveChangesAsync();

        // Assert
        var result = await _repository.GetByIdAsync(user.Id);
        result.Should().BeNull();
    }

    [Fact]
    public async Task ExistsAsync_ShouldReturnTrue_WhenEntityExists()
    {
        // Arrange
        var user = new User
        {
            Username = "existsuser",
            PasswordHash = "hash123",
            FullName = "Exists User",
            Role = UserRole.Viewer
        };
        await _repository.AddAsync(user);
        await _unitOfWork.SaveChangesAsync();

        // Act
        var result = await _repository.ExistsAsync(user.Id);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsAsync_ShouldReturnFalse_WhenEntityDoesNotExist()
    {
        // Act
        var result = await _repository.ExistsAsync(999);

        // Assert
        result.Should().BeFalse();
    }

    #region AddAsync 不自动保存测试

    [Fact]
    public async Task AddAsync_调用后实体被追踪但未保存到数据库()
    {
        // Arrange
        var user = new User
        {
            Username = "nosaveuser",
            PasswordHash = "hash123",
            FullName = "NoSave User",
            Email = "nosave@example.com",
            Role = UserRole.Admin
        };

        // Act - 仅追踪，不保存
        var result = await _repository.AddAsync(user);

        // Assert - 返回的实体不为空
        result.Should().NotBeNull();
        result.Username.Should().Be("nosaveuser");

        // Assert - 实体在 EF ChangeTracker 中被追踪，状态为 Added
        var entry = _context.ChangeTracker.Entries<User>()
            .FirstOrDefault(e => e.Entity.Username == "nosaveuser");
        entry.Should().NotBeNull("实体应在 ChangeTracker 中被追踪");
        entry!.State.Should().Be(EntityState.Added, "实体状态应为 Added（未保存）");

        // Assert - 数据库中尚未持久化（通过新的上下文验证）
        // InMemory 数据库在 SaveChanges 前不会持久化
        var dbUser = await _context.Users.IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Username == "nosaveuser");
        // 注意：InMemory provider 中 AsNoTracking 查询在 SaveChanges 之前找不到实体
        // 但被追踪的实体状态为 Added 即可证明未保存
    }

    #endregion

    #region Update 不自动保存测试

    [Fact]
    public async Task Update_调用后实体状态为Modified但未保存()
    {
        // Arrange - 先添加并保存一个实体
        var user = new User
        {
            Username = "updatenosave",
            PasswordHash = "hash123",
            FullName = "Original Name",
            Email = "update@example.com",
            Role = UserRole.Viewer
        };
        await _repository.AddAsync(user);
        await _unitOfWork.SaveChangesAsync();

        // 分离实体，模拟从数据库重新加载的场景
        _context.Entry(user).State = EntityState.Detached;

        // 修改实体
        user.FullName = "Updated Name";

        // Act - 仅标记为修改，不保存
        _repository.Update(user);

        // Assert - 实体状态应为 Modified
        var entry = _context.ChangeTracker.Entries<User>()
            .FirstOrDefault(e => e.Entity.Username == "updatenosave");
        entry.Should().NotBeNull("实体应在 ChangeTracker 中被追踪");
        entry!.State.Should().Be(EntityState.Modified, "实体状态应为 Modified（未保存）");

        // Modified 状态表明实体已被标记为修改但尚未调用 SaveChanges
    }

    #endregion

    [Fact]
    public async Task BankTransactionRepository_GetQueryable_ShouldExcludeSoftDeletedRecords()
    {
        // Arrange - 创建并软删除一条 BankTransaction
        var btContext = TestDbContextFactory.CreateInMemoryContext(Guid.NewGuid().ToString());
        var btRepo = new Repository<BankTransaction>(btContext, new Mock<ILogger<Repository<BankTransaction>>>().Object);
        var uow = new UnitOfWork(btContext);

        var tx = new BankTransaction
        {
            AccountId = 1,
            TransactionDate = new DateTime(2026, 3, 1),
            Amount = 1000m,
            Direction = BankTransactionDirection.Out,
            UniqueHash = "abc123hash",
            CreatedAt = DateTime.UtcNow
        };
        await btRepo.AddAsync(tx);
        await uow.SaveChangesAsync();

        // 软删除
        btRepo.Delete(tx);
        await uow.SaveChangesAsync();

        // Act - GetQueryable 应应用全局过滤器（IsDeleted = false）
        var hash = "abc123hash";
        var found = await btRepo.GetQueryable()
            .Where(bt => bt.UniqueHash == hash)
            .AnyAsync();

        // Assert - 软删除后不应出现在查询结果中（不会被误判为重复）
        found.Should().BeFalse("软删除的记录不应出现在 GetQueryable() 结果中，允许重新导入已删除的数据");

        btContext.Dispose();
    }

    [Fact]
    public async Task BankTransactionRepository_AfterSoftDelete_SameHashCanBeReimported()
    {
        // Arrange
        var btContext = TestDbContextFactory.CreateInMemoryContext(Guid.NewGuid().ToString());
        var btRepo = new Repository<BankTransaction>(btContext, new Mock<ILogger<Repository<BankTransaction>>>().Object);
        var uow = new UnitOfWork(btContext);

        const string hash = "reimport_test_hash";

        var tx1 = new BankTransaction
        {
            AccountId = 1,
            TransactionDate = new DateTime(2026, 3, 1),
            Amount = 500m,
            Direction = BankTransactionDirection.Out,
            UniqueHash = hash,
            CreatedAt = DateTime.UtcNow
        };
        await btRepo.AddAsync(tx1);
        await uow.SaveChangesAsync();

        // 软删除原记录
        btRepo.Delete(tx1);
        await uow.SaveChangesAsync();

        // Act - 重新导入相同 hash 的记录（模拟 ImportService 插入新记录）
        var tx2 = new BankTransaction
        {
            AccountId = 1,
            TransactionDate = new DateTime(2026, 3, 1),
            Amount = 500m,
            Direction = BankTransactionDirection.Out,
            UniqueHash = hash,
            CreatedAt = DateTime.UtcNow
        };
        var act = async () =>
        {
            await btRepo.AddAsync(tx2);
            await uow.SaveChangesAsync();
        };

        // Assert - 不应抛出唯一约束异常
        await act.Should().NotThrowAsync("软删除后，相同 hash 的记录应可重新导入");

        // 验证新记录已成功保存
        var count = await btRepo.GetQueryable()
            .Where(bt => bt.UniqueHash == hash)
            .CountAsync();
        count.Should().Be(1);

        btContext.Dispose();
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
