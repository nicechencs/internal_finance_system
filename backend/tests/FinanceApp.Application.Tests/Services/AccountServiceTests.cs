using FluentAssertions;
using FinanceApp.Application.Common;
using FinanceApp.Application.Modules.MasterData.DTOs.Account;
using FinanceApp.Application.Modules.MasterData.Interfaces;
using FinanceApp.Application.Modules.MasterData.Services;
using FinanceApp.Application.Tests.Helpers;
using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Enums;
using FinanceApp.Application.Modules.Identity.Interfaces;
using FinanceApp.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;
using Transaction = FinanceApp.Domain.Entities.Transaction;

namespace FinanceApp.Application.Tests.Services;

public class AccountServiceTests : TestBase
{
    private readonly Mock<IRepository<Account>> _repositoryMock;
    private readonly Mock<IRepository<Transaction>> _transactionRepositoryMock;
    private readonly Mock<IMasterDataReferenceGuard> _referenceGuardMock;
    private readonly Mock<ILogger<AccountService>> _loggerMock;
    private readonly AccountService _service;

    public AccountServiceTests()
    {
        _repositoryMock = new Mock<IRepository<Account>>();
        _transactionRepositoryMock = new Mock<IRepository<Transaction>>();
        _referenceGuardMock = new Mock<IMasterDataReferenceGuard>();
        _loggerMock = new Mock<ILogger<AccountService>>();
        _referenceGuardMock.Setup(g => g.HasAccountReferencesAsync(It.IsAny<long>())).ReturnsAsync(false);

        _service = new AccountService(
            _repositoryMock.Object,
            _transactionRepositoryMock.Object,
            _referenceGuardMock.Object,
            Mapper,
            AuditLogServiceMock.Object,
            _loggerMock.Object,
            CreateAdminCurrentUserService(),
            CreateAdminDataPermissionService(),
            UnitOfWorkMock.Object);
    }

    #region GetPagedAsync Tests

    [Fact]
    public async Task GetPagedAsync_ShouldReturnPagedResult()
    {
        var accounts = new List<Account>
        {
            new() { Id = 1, Name = "工商银行", AccountType = AccountType.Bank, IsDeleted = false, CreatedAt = DateTime.UtcNow },
            new() { Id = 2, Name = "支付宝", AccountType = AccountType.Alipay, IsDeleted = false, CreatedAt = DateTime.UtcNow.AddDays(-1) }
        };

        var queryableMock = accounts.AsQueryable().BuildMock();
        _repositoryMock.Setup(r => r.GetQueryable()).Returns(queryableMock.Object);

        var request = new PageRequest { Page = 1, PageSize = 10 };
        var result = await _service.GetPagedAsync(request);

        result.Should().NotBeNull();
        result.Total.Should().Be(2);
        result.Items.Should().HaveCount(2);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(10);
    }

    [Fact]
    public async Task GetPagedAsync_WithPagination_ShouldReturnCorrectPage()
    {
        var accounts = new List<Account>
        {
            new() { Id = 1, Name = "账户1", AccountType = AccountType.Bank, IsDeleted = false, CreatedAt = DateTime.UtcNow },
            new() { Id = 2, Name = "账户2", AccountType = AccountType.Bank, IsDeleted = false, CreatedAt = DateTime.UtcNow.AddDays(-1) },
            new() { Id = 3, Name = "账户3", AccountType = AccountType.Alipay, IsDeleted = false, CreatedAt = DateTime.UtcNow.AddDays(-2) }
        };

        var queryableMock = accounts.AsQueryable().BuildMock();
        _repositoryMock.Setup(r => r.GetQueryable()).Returns(queryableMock.Object);

        var request = new PageRequest { Page = 2, PageSize = 2 };
        var result = await _service.GetPagedAsync(request);

        result.Should().NotBeNull();
        result.Total.Should().Be(3);
        result.Items.Should().HaveCount(1);
        result.Page.Should().Be(2);
    }

    [Fact]
    public async Task GetPagedAsync_WithEmptyResult_ShouldReturnEmptyList()
    {
        var accounts = new List<Account>();
        var queryableMock = accounts.AsQueryable().BuildMock();
        _repositoryMock.Setup(r => r.GetQueryable()).Returns(queryableMock.Object);

        var request = new PageRequest { Page = 1, PageSize = 10 };
        var result = await _service.GetPagedAsync(request);

        result.Should().NotBeNull();
        result.Total.Should().Be(0);
        result.Items.Should().BeEmpty();
    }

    #endregion

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_WithExistingId_ShouldReturnAccount()
    {
        var account = new Account
        {
            Id = 1,
            Name = "工商银行",
            AccountType = AccountType.Bank,
            AccountNumber = "6222021234567890",
            BankName = "中国工商银行",
            OpeningBalance = 10000m,
            CurrentBalance = 15000m,
            Currency = "CNY",
            IsActive = true,
            IsDeleted = false
        };
        _repositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(account);

        var result = await _service.GetByIdAsync(1);

        result.Should().NotBeNull();
        result.Id.Should().Be(1);
        result.Name.Should().Be("工商银行");
        result.AccountType.Should().Be("Bank");
    }

    [Fact]
    public async Task GetByIdAsync_WithNonExistingId_ShouldThrowNotFoundException()
    {
        _repositoryMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Account?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => _service.GetByIdAsync(999));
    }

    #endregion

    #region CreateAsync Tests

    [Fact]
    public async Task CreateAsync_WithValidRequest_ShouldCreateAccount()
    {
        var request = new CreateAccountRequest
        {
            Name = "工商银行",
            AccountType = "Bank",
            InitialBalance = 10000m,
            Currency = "CNY",
            BankName = "中国工商银行",
            AccountNumber = "6222021234567890"
        };

        _repositoryMock.Setup(r => r.AddAsync(It.IsAny<Account>()))
            .ReturnsAsync((Account a) =>
            {
                a.Id = 1;
                return a;
            });

        var result = await _service.CreateAsync(request);

        result.Should().NotBeNull();
        result.Name.Should().Be("工商银行");
        result.AccountType.Should().Be("Bank");
        _repositoryMock.Verify(r => r.AddAsync(It.Is<Account>(a =>
            a.Name == "工商银行" &&
            a.AccountType == AccountType.Bank &&
            a.OpeningBalance == 10000m &&
            a.CurrentBalance == 10000m &&
            a.Currency == "CNY" &&
            a.IsActive == true
        )), Times.Once);
        AuditLogServiceMock.Verify(a => a.LogAsync("Create", "Account", It.IsAny<long>(), It.IsAny<string?>(), It.IsAny<string?>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithInvalidAccountType_ShouldThrowValidationException()
    {
        var request = new CreateAccountRequest
        {
            Name = "测试账户",
            AccountType = "InvalidType",
            InitialBalance = 1000m
        };

        await Assert.ThrowsAsync<ValidationException>(() => _service.CreateAsync(request));
        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<Account>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WithAlipayAccount_ShouldCreateSuccessfully()
    {
        var request = new CreateAccountRequest
        {
            Name = "支付宝账户",
            AccountType = "Alipay",
            InitialBalance = 5000m,
            Currency = "CNY"
        };

        _repositoryMock.Setup(r => r.AddAsync(It.IsAny<Account>()))
            .ReturnsAsync((Account a) =>
            {
                a.Id = 2;
                return a;
            });


        var result = await _service.CreateAsync(request);

        result.Should().NotBeNull();
        result.AccountType.Should().Be("Alipay");
        _repositoryMock.Verify(r => r.AddAsync(It.Is<Account>(a => a.AccountType == AccountType.Alipay)), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithZeroInitialBalance_ShouldCreateSuccessfully()
    {
        var request = new CreateAccountRequest
        {
            Name = "新账户",
            AccountType = "Bank",
            InitialBalance = 0m,
            Currency = "CNY"
        };

        _repositoryMock.Setup(r => r.AddAsync(It.IsAny<Account>()))
            .ReturnsAsync((Account a) =>
            {
                a.Id = 3;
                return a;
            });


        var result = await _service.CreateAsync(request);

        result.Should().NotBeNull();
        _repositoryMock.Verify(r => r.AddAsync(It.Is<Account>(a =>
            a.OpeningBalance == 0m &&
            a.CurrentBalance == 0m
        )), Times.Once);
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_WithExistingAccount_ShouldUpdateAccount()
    {
        var account = new Account
        {
            Id = 1,
            Name = "旧名称",
            AccountType = AccountType.Bank,
            IsActive = true,
            BankName = "旧银行",
            AccountNumber = "123456"
        };
        var request = new UpdateAccountRequest
        {
            Name = "新名称",
            IsActive = false,
            BankName = "新银行",
            AccountNumber = "654321"
        };

        _repositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(account);


        var result = await _service.UpdateAsync(1, request);

        account.Name.Should().Be("新名称");
        account.IsActive.Should().BeFalse();
        account.BankName.Should().Be("新银行");
        account.AccountNumber.Should().Be("654321");
        AuditLogServiceMock.Verify(a => a.LogAsync("Update", "Account", 1, It.IsAny<string?>(), It.IsAny<string?>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WithNonExistingAccount_ShouldThrowNotFoundException()
    {
        var request = new UpdateAccountRequest { Name = "新名称", IsActive = true };
        _repositoryMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Account?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => _service.UpdateAsync(999, request));
    }

    [Fact]
    public async Task UpdateAsync_ShouldNotChangeAccountType()
    {
        var account = new Account
        {
            Id = 1,
            Name = "测试账户",
            AccountType = AccountType.Bank,
            OpeningBalance = 10000m,
            CurrentBalance = 15000m
        };
        var request = new UpdateAccountRequest
        {
            Name = "更新后的账户",
            IsActive = true
        };

        _repositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(account);


        await _service.UpdateAsync(1, request);

        account.AccountType.Should().Be(AccountType.Bank);
        account.OpeningBalance.Should().Be(10000m);
        account.CurrentBalance.Should().Be(15000m);
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_WithExistingAccount_ShouldDeleteSuccessfully()
    {
        var account = new Account
        {
            Id = 1,
            Name = "待删除账户",
            AccountType = AccountType.Bank
        };
        _repositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(account);


        await _service.DeleteAsync(1);

        AuditLogServiceMock.Verify(a => a.LogAsync("Delete", "Account", 1, It.IsAny<string?>(), It.IsAny<string?>()), Times.Once);
        _repositoryMock.Verify(r => r.Delete(account), Times.Once);
        _repositoryMock.Verify(r => r.Update(It.IsAny<Account>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_WithReferencedAccount_ShouldDeactivateInsteadOfDelete()
    {
        var account = new Account { Id = 1, Name = "测试账户", AccountType = AccountType.Bank, IsActive = true };
        _repositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(account);
        _referenceGuardMock.Setup(g => g.HasAccountReferencesAsync(1)).ReturnsAsync(true);

        await _service.DeleteAsync(1);

        account.IsActive.Should().BeFalse();
        _repositoryMock.Verify(r => r.Update(account), Times.Once);
        _repositoryMock.Verify(r => r.Delete(It.IsAny<Account>()), Times.Never);
        AuditLogServiceMock.Verify(a => a.LogAsync("Archive", "Account", 1, It.IsAny<string?>(), It.IsAny<string?>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WithNonExistingAccount_ShouldThrowNotFoundException()
    {
        _repositoryMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Account?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => _service.DeleteAsync(999));
    }

    #endregion

    #region GetActiveAccountsAsync Tests

    [Fact]
    public async Task GetActiveAccountsAsync_ShouldReturnOnlyActiveAccounts()
    {
        var accounts = new List<Account>
        {
            new() { Id = 1, Name = "活跃账户1", AccountType = AccountType.Bank, IsActive = true, IsDeleted = false },
            new() { Id = 2, Name = "非活跃账户", AccountType = AccountType.Bank, IsActive = false, IsDeleted = false },
            new() { Id = 3, Name = "活跃账户2", AccountType = AccountType.Alipay, IsActive = true, IsDeleted = false }
        };

        var queryableMock = accounts.AsQueryable().BuildMock();
        _repositoryMock.Setup(r => r.GetQueryable()).Returns(queryableMock.Object);

        var result = await _service.GetActiveAccountsAsync();

        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.Should().OnlyContain(a => a.IsActive);
    }

    [Fact]
    public async Task GetActiveAccountsAsync_ShouldReturnOrderedByName()
    {
        var accounts = new List<Account>
        {
            new() { Id = 1, Name = "C账户", AccountType = AccountType.Bank, IsActive = true, IsDeleted = false },
            new() { Id = 2, Name = "A账户", AccountType = AccountType.Bank, IsActive = true, IsDeleted = false },
            new() { Id = 3, Name = "B账户", AccountType = AccountType.Alipay, IsActive = true, IsDeleted = false }
        };

        var queryableMock = accounts.AsQueryable().BuildMock();
        _repositoryMock.Setup(r => r.GetQueryable()).Returns(queryableMock.Object);

        var result = await _service.GetActiveAccountsAsync();

        result.Should().NotBeNull();
        result.Should().HaveCount(3);
        result[0].Name.Should().Be("A账户");
        result[1].Name.Should().Be("B账户");
        result[2].Name.Should().Be("C账户");
    }

    [Fact]
    public async Task GetActiveAccountsAsync_WithNoActiveAccounts_ShouldReturnEmptyList()
    {
        var accounts = new List<Account>
        {
            new() { Id = 1, Name = "非活跃账户1", AccountType = AccountType.Bank, IsActive = false, IsDeleted = false },
            new() { Id = 2, Name = "非活跃账户2", AccountType = AccountType.Bank, IsActive = false, IsDeleted = false }
        };

        var queryableMock = accounts.AsQueryable().BuildMock();
        _repositoryMock.Setup(r => r.GetQueryable()).Returns(queryableMock.Object);

        var result = await _service.GetActiveAccountsAsync();

        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetActiveAccountsAsync_WithEmptyDatabase_ShouldReturnEmptyList()
    {
        var accounts = new List<Account>();
        var queryableMock = accounts.AsQueryable().BuildMock();
        _repositoryMock.Setup(r => r.GetQueryable()).Returns(queryableMock.Object);

        var result = await _service.GetActiveAccountsAsync();

        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    #endregion

    #region GetStatisticsAsync Tests

    [Fact]
    public async Task GetStatisticsAsync_ShouldReturnCorrectStatistics()
    {
        // Arrange
        var accounts = new List<Account>
        {
            new() { Id = 1, Name = "工商银行", AccountType = AccountType.Bank, IsActive = true, CurrentBalance = 10000m, IsDeleted = false, CreatedAt = DateTime.UtcNow },
            new() { Id = 2, Name = "支付宝", AccountType = AccountType.Alipay, IsActive = true, CurrentBalance = 5000m, IsDeleted = false, CreatedAt = DateTime.UtcNow },
            new() { Id = 3, Name = "定期存款", AccountType = AccountType.FixedDeposit, IsActive = true, CurrentBalance = 50000m, IsDeleted = false, CreatedAt = DateTime.UtcNow },
            new() { Id = 4, Name = "已停用账户", AccountType = AccountType.Bank, IsActive = false, CurrentBalance = 2000m, IsDeleted = false, CreatedAt = DateTime.UtcNow }
        };

        var queryableMock = accounts.AsQueryable().BuildMock();
        _repositoryMock.Setup(r => r.GetQueryable()).Returns(queryableMock.Object);

        // Act
        var result = await _service.GetStatisticsAsync();

        // Assert
        result.Should().NotBeNull();
        result.TotalCount.Should().Be(4);
        result.ActiveCount.Should().Be(3);
        result.TotalBalance.Should().Be(65000m); // 10000 + 5000 + 50000 (仅活跃账户)
        result.FixedDepositCount.Should().Be(1);
    }

    [Fact]
    public async Task GetStatisticsAsync_WithEmptyData_ShouldReturnZeros()
    {
        // Arrange
        var accounts = new List<Account>();
        var queryableMock = accounts.AsQueryable().BuildMock();
        _repositoryMock.Setup(r => r.GetQueryable()).Returns(queryableMock.Object);

        // Act
        var result = await _service.GetStatisticsAsync();

        // Assert
        result.Should().NotBeNull();
        result.TotalCount.Should().Be(0);
        result.ActiveCount.Should().Be(0);
        result.TotalBalance.Should().Be(0m);
        result.FixedDepositCount.Should().Be(0);
    }

    #endregion

    #region 权限过滤测试

    [Fact]
    public async Task GetActiveAccountsAsync_Viewer角色_应只返回自己创建的活跃账户()
    {
        // Arrange - 创建 Viewer 角色的 Service 实例
        var viewerUserId = 42L;
        var viewerCurrentUser = CreateViewerCurrentUserService(viewerUserId);
        var viewerPermissionService = new ViewerDataPermissionService(viewerUserId);
        var viewerService = new AccountService(
            _repositoryMock.Object,
            _transactionRepositoryMock.Object,
            _referenceGuardMock.Object,
            Mapper,
            AuditLogServiceMock.Object,
            _loggerMock.Object,
            viewerCurrentUser,
            viewerPermissionService,
            UnitOfWorkMock.Object);

        var accounts = new List<Account>
        {
            new() { Id = 1, Name = "我的活跃账户", AccountType = AccountType.Bank, IsActive = true, IsDeleted = false, CreatedBy = viewerUserId },
            new() { Id = 2, Name = "他人活跃账户", AccountType = AccountType.Bank, IsActive = true, IsDeleted = false, CreatedBy = 999L },
            new() { Id = 3, Name = "我的非活跃账户", AccountType = AccountType.Alipay, IsActive = false, IsDeleted = false, CreatedBy = viewerUserId },
            new() { Id = 4, Name = "无创建者账户", AccountType = AccountType.Bank, IsActive = true, IsDeleted = false, CreatedBy = null }
        };

        var queryableMock = accounts.AsQueryable().BuildMock();
        _repositoryMock.Setup(r => r.GetQueryable()).Returns(queryableMock.Object);

        // Act
        var result = await viewerService.GetActiveAccountsAsync();

        // Assert - Viewer 只能看到自己创建的活跃账户
        result.Should().HaveCount(1);
        result[0].Name.Should().Be("我的活跃账户");
    }

    [Fact]
    public async Task GetBalanceTrendAsync_Viewer角色访问他人账户_应抛出ForbiddenException()
    {
        // Arrange - 创建 Viewer 角色的 Service 实例
        var viewerUserId = 42L;
        var viewerCurrentUser = CreateViewerCurrentUserService(viewerUserId);
        var viewerPermissionService = new ViewerDataPermissionService(viewerUserId);
        var viewerService = new AccountService(
            _repositoryMock.Object,
            _transactionRepositoryMock.Object,
            _referenceGuardMock.Object,
            Mapper,
            AuditLogServiceMock.Object,
            _loggerMock.Object,
            viewerCurrentUser,
            viewerPermissionService,
            UnitOfWorkMock.Object);

        // 他人创建的账户
        var account = new Account
        {
            Id = 1,
            Name = "他人账户",
            AccountType = AccountType.Bank,
            CurrentBalance = 5000m,
            OpeningBalance = 5000m,
            CreatedBy = 999L // 其他用户创建
        };

        _repositoryMock.Setup(r => r.GetByIdAsync(1L)).ReturnsAsync(account);

        // Act & Assert - Viewer 访问他人账户时应抛出 ForbiddenException
        await Assert.ThrowsAsync<Application.Common.ForbiddenException>(
            () => viewerService.GetBalanceTrendAsync(1L));
    }

    [Fact]
    public async Task GetBalanceTrendAsync_Viewer角色访问自己创建的账户_不应抛异常()
    {
        // Arrange - 创建 Viewer 角色的 Service 实例
        var viewerUserId = 42L;
        var viewerCurrentUser = CreateViewerCurrentUserService(viewerUserId);
        var viewerPermissionService = new ViewerDataPermissionService(viewerUserId);
        var viewerService = new AccountService(
            _repositoryMock.Object,
            _transactionRepositoryMock.Object,
            _referenceGuardMock.Object,
            Mapper,
            AuditLogServiceMock.Object,
            _loggerMock.Object,
            viewerCurrentUser,
            viewerPermissionService,
            UnitOfWorkMock.Object);

        // 自己创建的账户
        var account = new Account
        {
            Id = 1,
            Name = "我的账户",
            AccountType = AccountType.Bank,
            CurrentBalance = 5000m,
            OpeningBalance = 5000m,
            CreatedBy = viewerUserId
        };

        _repositoryMock.Setup(r => r.GetByIdAsync(1L)).ReturnsAsync(account);

        // 空交易列表
        var emptyTransactions = new List<Transaction>().AsQueryable().BuildMock();
        _transactionRepositoryMock.Setup(r => r.GetQueryable()).Returns(emptyTransactions.Object);

        // Act - 不应抛出异常
        var result = await viewerService.GetBalanceTrendAsync(1L);

        // Assert
        result.Should().NotBeNull();
    }

    /// <summary>
    /// 创建 Viewer 角色的 ICurrentUserService Mock
    /// </summary>
    private static ICurrentUserService CreateViewerCurrentUserService(long userId = 1L)
    {
        var mock = new Mock<ICurrentUserService>();
        mock.Setup(x => x.UserId).Returns(userId);
        mock.Setup(x => x.Username).Returns("viewer");
        mock.Setup(x => x.Role).Returns(UserRole.Viewer);
        mock.Setup(x => x.IsAdmin).Returns(false);
        mock.Setup(x => x.IsAccountant).Returns(false);
        mock.Setup(x => x.IsViewer).Returns(true);
        return mock.Object;
    }

    #endregion
}

/// <summary>
/// Viewer 角色的 IDataPermissionService 实现，用于权限过滤测试
/// Viewer 只能看到自己创建的数据
/// </summary>
internal class ViewerDataPermissionService : IDataPermissionService
{
    private readonly long _userId;

    public ViewerDataPermissionService(long userId)
    {
        _userId = userId;
    }

    public bool CanAccess<T>(T entity) where T : BaseEntity
    {
        // Viewer 只能访问自己创建的数据
        if (entity.CreatedBy is long createdById)
            return createdById == _userId;
        return false; // CreatedBy 为 null 时不允许访问
    }

    public bool CanModify<T>(T entity) where T : BaseEntity => false;
    public bool CanDelete<T>(T entity) where T : BaseEntity => false;

    public IQueryable<T> ApplyPermissionFilter<T>(IQueryable<T> query) where T : BaseEntity
    {
        // 使用内存过滤：只返回 CreatedBy == _userId 的数据
        return query.Where(e => e.CreatedBy == _userId);
    }
}
