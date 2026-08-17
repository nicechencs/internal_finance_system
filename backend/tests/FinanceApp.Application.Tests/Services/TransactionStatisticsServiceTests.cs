using FluentAssertions;
using FinanceApp.Application.Common;
using FinanceApp.Application.Modules.TransactionProcessing.DTOs;
using FinanceApp.Application.Modules.TransactionProcessing.Interfaces;
using FinanceApp.Application.Modules.TransactionProcessing.Services;
using FinanceApp.Application.Tests.Helpers;
using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Enums;
using FinanceApp.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;

namespace FinanceApp.Application.Tests.Services;

public class TransactionStatisticsServiceTests : TestBase
{
    private readonly Mock<IRepository<Transaction>> _transactionRepositoryMock;
    private readonly Mock<IRepository<ReceivableDetail>> _receivableDetailRepositoryMock;
    private readonly Mock<IRepository<PayableDetail>> _payableDetailRepositoryMock;
    private readonly Mock<IRepository<TagBinding>> _tagBindingRepositoryMock;
    private readonly Mock<ILogger<TransactionStatisticsService>> _loggerMock;
    private readonly Mock<ICurrentUserService> _currentUserServiceMock;
    private readonly Mock<IDataPermissionService> _permissionServiceMock;
    private readonly TransactionStatisticsService _service;

    public TransactionStatisticsServiceTests()
    {
        _transactionRepositoryMock = new Mock<IRepository<Transaction>>();
        _receivableDetailRepositoryMock = new Mock<IRepository<ReceivableDetail>>();
        _payableDetailRepositoryMock = new Mock<IRepository<PayableDetail>>();
        _tagBindingRepositoryMock = new Mock<IRepository<TagBinding>>();
        _loggerMock = new Mock<ILogger<TransactionStatisticsService>>();
        _currentUserServiceMock = new Mock<ICurrentUserService>();
        _permissionServiceMock = new Mock<IDataPermissionService>();

        // 默认设置为 Admin 用户
        _currentUserServiceMock.Setup(x => x.UserId).Returns(1L);
        _currentUserServiceMock.Setup(x => x.Username).Returns("admin");
        _currentUserServiceMock.Setup(x => x.Role).Returns(UserRole.Admin);
        _currentUserServiceMock.Setup(x => x.IsAdmin).Returns(true);

        // 默认权限服务不过滤数据（Admin 权限）
        _permissionServiceMock.Setup(x => x.ApplyPermissionFilter(It.IsAny<IQueryable<Transaction>>()))
            .Returns((IQueryable<Transaction> query) => query);

        _service = new TransactionStatisticsService(
            _transactionRepositoryMock.Object,
            _receivableDetailRepositoryMock.Object,
            _payableDetailRepositoryMock.Object,
            _tagBindingRepositoryMock.Object,
            _loggerMock.Object,
            _currentUserServiceMock.Object,
            _permissionServiceMock.Object
        );
    }


    [Fact]
    public async Task GetStatisticsAsync_WithTagFiltersAndAccountFilter_ShouldApplyIntersection()
    {
        var transactions = new List<Transaction>
        {
            new() { Id = 1, AccountId = 10L, TransactionType = TransactionType.Expense, Amount = 100m, TransactionDate = new DateTime(2026, 3, 26), CreatedBy = 1L },
            new() { Id = 2, AccountId = 20L, TransactionType = TransactionType.Expense, Amount = 200m, TransactionDate = new DateTime(2026, 3, 26), CreatedBy = 1L },
            new() { Id = 3, AccountId = 10L, TransactionType = TransactionType.Expense, Amount = 300m, TransactionDate = new DateTime(2026, 3, 26), CreatedBy = 1L }
        };

        var tagBindings = new List<TagBinding>
        {
            new() { Id = 1, OwnerType = TagScope.Transaction, OwnerId = 1, TagId = 1001L },
            new() { Id = 2, OwnerType = TagScope.Transaction, OwnerId = 2, TagId = 1001L }
        };

        var request = new PageRequest
        {
            AccountId = 10L,
            TagFilters = new List<TagFilterGroup>
            {
                new()
                {
                    Scope = TagScope.Transaction,
                    TagIds = new List<long> { 1001L },
                    MatchMode = TagMatchMode.Or
                }
            }
        };

        var transactionQueryableMock = transactions.AsQueryable().BuildMock();
        _transactionRepositoryMock.Setup(x => x.GetQueryable())
            .Returns(transactionQueryableMock.Object);

        var tagBindingQueryableMock = tagBindings.AsQueryable().BuildMock();
        _tagBindingRepositoryMock.Setup(x => x.GetQueryable())
            .Returns(tagBindingQueryableMock.Object);

        var result = await _service.GetStatisticsAsync(request);

        result.TotalCount.Should().Be(1);
        result.ExpenseCount.Should().Be(1);
        result.TotalExpense.Should().Be(100m);
        result.TotalIncome.Should().Be(0m);
    }

    [Fact]
    public async Task GetStatisticsAsync_WithoutFilter_ShouldAggregateIncomeExpenseAndOutboundTransfers()
    {
        var transactions = new List<Transaction>
        {
            new() { Id = 1, TransactionType = TransactionType.Income, Amount = 1000m, CreatedBy = 1L },
            new() { Id = 2, TransactionType = TransactionType.Expense, Amount = 300m, CreatedBy = 1L },
            new() { Id = 3, TransactionType = TransactionType.Transfer, Amount = 200m, Description = "转账至账户B", CreatedBy = 1L },
            new() { Id = 4, TransactionType = TransactionType.Transfer, Amount = 80m, Description = "转账自账户A", CreatedBy = 1L },
            new() { Id = 5, TransactionType = TransactionType.Transfer, Amount = 50m, TransferDirection = TransferDirection.Out, CreatedBy = 1L }
        };

        _transactionRepositoryMock.Setup(x => x.GetQueryable())
            .Returns(transactions.AsQueryable().BuildMock().Object);

        var result = await _service.GetStatisticsAsync();

        result.TotalIncome.Should().Be(1000m);
        result.TotalExpense.Should().Be(300m);
        result.NetProfit.Should().Be(700m);
        result.IncomeCount.Should().Be(1);
        result.ExpenseCount.Should().Be(1);
        result.TotalTransfer.Should().Be(250m);
        result.TransferCount.Should().Be(2);
        result.TotalCount.Should().Be(5);
    }

    #region GetAccountStatisticsAsync 测试

    [Fact(DisplayName = "GetAccountStatisticsAsync - 正常场景：包含收入、支出、转账交易")]
    public async Task GetAccountStatisticsAsync_正常场景_返回正确的统计数据()
    {
        // Arrange
        var accountId = 1L;
        var transactions = new List<Transaction>
        {
            new() { Id = 1, AccountId = accountId, TransactionType = TransactionType.Income, Amount = 1000m, CreatedBy = 1L },
            new() { Id = 2, AccountId = accountId, TransactionType = TransactionType.Income, Amount = 2000m, CreatedBy = 1L },
            new() { Id = 3, AccountId = accountId, TransactionType = TransactionType.Expense, Amount = 500m, CreatedBy = 1L },
            new() { Id = 4, AccountId = accountId, TransactionType = TransactionType.Expense, Amount = 300m, CreatedBy = 1L },
            new() { Id = 5, AccountId = accountId, TransactionType = TransactionType.Transfer, Amount = 200m, Description = "转账至账户B", CreatedBy = 1L },
            new() { Id = 6, AccountId = 2L, TransactionType = TransactionType.Income, Amount = 5000m, CreatedBy = 1L } // 不同账户，应被过滤
        };

        var queryableMock = transactions.AsQueryable().BuildMock();
        _transactionRepositoryMock.Setup(x => x.GetQueryable())
            .Returns(queryableMock.Object);

        // Act
        var result = await _service.GetAccountStatisticsAsync(accountId);

        // Assert
        result.Should().NotBeNull();
        result.TotalIncome.Should().Be(3000m); // 1000 + 2000
        result.TotalExpense.Should().Be(800m); // 500 + 300
        result.NetProfit.Should().Be(2200m); // 3000 - 800
        result.TotalTransfer.Should().Be(200m);
        result.IncomeCount.Should().Be(2);
        result.ExpenseCount.Should().Be(2);
        result.TransferCount.Should().Be(1);
        result.TotalCount.Should().Be(5);
    }

    [Fact(DisplayName = "GetAccountStatisticsAsync - 边界场景：空数据")]
    public async Task GetAccountStatisticsAsync_空数据_返回零值统计()
    {
        // Arrange
        var accountId = 1L;
        var transactions = new List<Transaction>();

        var queryableMock = transactions.AsQueryable().BuildMock();
        _transactionRepositoryMock.Setup(x => x.GetQueryable())
            .Returns(queryableMock.Object);

        // Act
        var result = await _service.GetAccountStatisticsAsync(accountId);

        // Assert
        result.Should().NotBeNull();
        result.TotalIncome.Should().Be(0m);
        result.TotalExpense.Should().Be(0m);
        result.NetProfit.Should().Be(0m);
        result.TotalTransfer.Should().Be(0m);
        result.IncomeCount.Should().Be(0);
        result.ExpenseCount.Should().Be(0);
        result.TransferCount.Should().Be(0);
        result.TotalCount.Should().Be(0);
    }

    [Fact(DisplayName = "GetAccountStatisticsAsync - 边界场景：只有收入")]
    public async Task GetAccountStatisticsAsync_只有收入_返回正确统计()
    {
        // Arrange
        var accountId = 1L;
        var transactions = new List<Transaction>
        {
            new() { Id = 1, AccountId = accountId, TransactionType = TransactionType.Income, Amount = 1000m, CreatedBy = 1L },
            new() { Id = 2, AccountId = accountId, TransactionType = TransactionType.Income, Amount = 1500m, CreatedBy = 1L }
        };

        var queryableMock = transactions.AsQueryable().BuildMock();
        _transactionRepositoryMock.Setup(x => x.GetQueryable())
            .Returns(queryableMock.Object);

        // Act
        var result = await _service.GetAccountStatisticsAsync(accountId);

        // Assert
        result.Should().NotBeNull();
        result.TotalIncome.Should().Be(2500m);
        result.TotalExpense.Should().Be(0m);
        result.NetProfit.Should().Be(2500m);
        result.IncomeCount.Should().Be(2);
        result.ExpenseCount.Should().Be(0);
        result.TotalCount.Should().Be(2);
    }

    [Fact(DisplayName = "GetAccountStatisticsAsync - 边界场景：只有支出")]
    public async Task GetAccountStatisticsAsync_只有支出_返回正确统计()
    {
        // Arrange
        var accountId = 1L;
        var transactions = new List<Transaction>
        {
            new() { Id = 1, AccountId = accountId, TransactionType = TransactionType.Expense, Amount = 500m, CreatedBy = 1L },
            new() { Id = 2, AccountId = accountId, TransactionType = TransactionType.Expense, Amount = 800m, CreatedBy = 1L }
        };

        var queryableMock = transactions.AsQueryable().BuildMock();
        _transactionRepositoryMock.Setup(x => x.GetQueryable())
            .Returns(queryableMock.Object);

        // Act
        var result = await _service.GetAccountStatisticsAsync(accountId);

        // Assert
        result.Should().NotBeNull();
        result.TotalIncome.Should().Be(0m);
        result.TotalExpense.Should().Be(1300m);
        result.NetProfit.Should().Be(-1300m); // 负利润
        result.ExpenseCount.Should().Be(2);
        result.IncomeCount.Should().Be(0);
        result.TotalCount.Should().Be(2);
    }

    [Fact(DisplayName = "GetAccountStatisticsAsync - 数据权限：Viewer 只能看到自己创建的数据")]
    public async Task GetAccountStatisticsAsync_Viewer权限_只返回自己创建的数据统计()
    {
        // Arrange
        var accountId = 1L;
        var viewerUserId = 2L;
        var transactions = new List<Transaction>
        {
            new() { Id = 1, AccountId = accountId, TransactionType = TransactionType.Income, Amount = 1000m, CreatedBy = viewerUserId },
            new() { Id = 2, AccountId = accountId, TransactionType = TransactionType.Income, Amount = 2000m, CreatedBy = 1L }, // 其他用户创建
            new() { Id = 3, AccountId = accountId, TransactionType = TransactionType.Expense, Amount = 500m, CreatedBy = viewerUserId }
        };

        var queryableMock = transactions.AsQueryable().BuildMock();
        _transactionRepositoryMock.Setup(x => x.GetQueryable())
            .Returns(queryableMock.Object);

        // 模拟 Viewer 权限过滤
        _permissionServiceMock.Setup(x => x.ApplyPermissionFilter(It.IsAny<IQueryable<Transaction>>()))
            .Returns((IQueryable<Transaction> query) => query.Where(t => t.CreatedBy == viewerUserId));

        // Act
        var result = await _service.GetAccountStatisticsAsync(accountId);

        // Assert
        result.Should().NotBeNull();
        result.TotalIncome.Should().Be(1000m); // 只统计 Viewer 自己创建的收入
        result.TotalExpense.Should().Be(500m);
        result.NetProfit.Should().Be(500m);
        result.IncomeCount.Should().Be(1);
        result.ExpenseCount.Should().Be(1);
        result.TotalCount.Should().Be(2);
    }

    #endregion

    #region GetCustomerStatisticsAsync 测试

    [Fact(DisplayName = "GetCustomerStatisticsAsync - 正常场景：包含收入、支出、转账交易")]
    public async Task GetCustomerStatisticsAsync_正常场景_返回正确的统计数据()
    {
        // Arrange
        var customerId = 1L;
        var transactions = new List<Transaction>
        {
            new() { Id = 1, CustomerId = customerId, TransactionType = TransactionType.Income, Amount = 5000m, CreatedBy = 1L },
            new() { Id = 2, CustomerId = customerId, TransactionType = TransactionType.Income, Amount = 3000m, CreatedBy = 1L },
            new() { Id = 3, CustomerId = customerId, TransactionType = TransactionType.Expense, Amount = 1000m, CreatedBy = 1L },
            new() { Id = 4, CustomerId = customerId, TransactionType = TransactionType.Transfer, Amount = 500m, Description = "转账至账户C", CreatedBy = 1L },
            new() { Id = 5, CustomerId = 2L, TransactionType = TransactionType.Income, Amount = 10000m, CreatedBy = 1L } // 不同客户，应被过滤
        };

        var queryableMock = transactions.AsQueryable().BuildMock();
        _transactionRepositoryMock.Setup(x => x.GetQueryable())
            .Returns(queryableMock.Object);

        // Act
        var result = await _service.GetCustomerStatisticsAsync(customerId);

        // Assert
        result.Should().NotBeNull();
        result.TotalIncome.Should().Be(8000m); // 5000 + 3000
        result.TotalExpense.Should().Be(1000m);
        result.NetProfit.Should().Be(7000m); // 8000 - 1000
        result.TotalTransfer.Should().Be(500m);
        result.IncomeCount.Should().Be(2);
        result.ExpenseCount.Should().Be(1);
        result.TransferCount.Should().Be(1);
        result.TotalCount.Should().Be(4);
    }

    [Fact(DisplayName = "GetCustomerStatisticsAsync - 边界场景：空数据")]
    public async Task GetCustomerStatisticsAsync_空数据_返回零值统计()
    {
        // Arrange
        var customerId = 1L;
        var transactions = new List<Transaction>();

        var queryableMock = transactions.AsQueryable().BuildMock();
        _transactionRepositoryMock.Setup(x => x.GetQueryable())
            .Returns(queryableMock.Object);

        // Act
        var result = await _service.GetCustomerStatisticsAsync(customerId);

        // Assert
        result.Should().NotBeNull();
        result.TotalIncome.Should().Be(0m);
        result.TotalExpense.Should().Be(0m);
        result.NetProfit.Should().Be(0m);
        result.TotalTransfer.Should().Be(0m);
        result.IncomeCount.Should().Be(0);
        result.ExpenseCount.Should().Be(0);
        result.TransferCount.Should().Be(0);
        result.TotalCount.Should().Be(0);
    }

    [Fact(DisplayName = "GetCustomerStatisticsAsync - 边界场景：只有收入")]
    public async Task GetCustomerStatisticsAsync_只有收入_返回正确统计()
    {
        // Arrange
        var customerId = 1L;
        var transactions = new List<Transaction>
        {
            new() { Id = 1, CustomerId = customerId, TransactionType = TransactionType.Income, Amount = 3000m, CreatedBy = 1L },
            new() { Id = 2, CustomerId = customerId, TransactionType = TransactionType.Income, Amount = 4500m, CreatedBy = 1L }
        };

        var queryableMock = transactions.AsQueryable().BuildMock();
        _transactionRepositoryMock.Setup(x => x.GetQueryable())
            .Returns(queryableMock.Object);

        // Act
        var result = await _service.GetCustomerStatisticsAsync(customerId);

        // Assert
        result.Should().NotBeNull();
        result.TotalIncome.Should().Be(7500m);
        result.TotalExpense.Should().Be(0m);
        result.NetProfit.Should().Be(7500m);
        result.IncomeCount.Should().Be(2);
        result.ExpenseCount.Should().Be(0);
        result.TotalCount.Should().Be(2);
    }

    [Fact(DisplayName = "GetCustomerStatisticsAsync - 边界场景：只有支出")]
    public async Task GetCustomerStatisticsAsync_只有支出_返回正确统计()
    {
        // Arrange
        var customerId = 1L;
        var transactions = new List<Transaction>
        {
            new() { Id = 1, CustomerId = customerId, TransactionType = TransactionType.Expense, Amount = 1200m, CreatedBy = 1L },
            new() { Id = 2, CustomerId = customerId, TransactionType = TransactionType.Expense, Amount = 800m, CreatedBy = 1L }
        };

        var queryableMock = transactions.AsQueryable().BuildMock();
        _transactionRepositoryMock.Setup(x => x.GetQueryable())
            .Returns(queryableMock.Object);

        // Act
        var result = await _service.GetCustomerStatisticsAsync(customerId);

        // Assert
        result.Should().NotBeNull();
        result.TotalIncome.Should().Be(0m);
        result.TotalExpense.Should().Be(2000m);
        result.NetProfit.Should().Be(-2000m); // 负利润
        result.ExpenseCount.Should().Be(2);
        result.IncomeCount.Should().Be(0);
        result.TotalCount.Should().Be(2);
    }

    [Fact(DisplayName = "GetCustomerStatisticsAsync - 数据权限：Viewer 只能看到自己创建的数据")]
    public async Task GetCustomerStatisticsAsync_Viewer权限_只返回自己创建的数据统计()
    {
        // Arrange
        var customerId = 1L;
        var viewerUserId = 3L;
        var transactions = new List<Transaction>
        {
            new() { Id = 1, CustomerId = customerId, TransactionType = TransactionType.Income, Amount = 2000m, CreatedBy = viewerUserId },
            new() { Id = 2, CustomerId = customerId, TransactionType = TransactionType.Income, Amount = 5000m, CreatedBy = 1L }, // 其他用户创建
            new() { Id = 3, CustomerId = customerId, TransactionType = TransactionType.Expense, Amount = 800m, CreatedBy = viewerUserId },
            new() { Id = 4, CustomerId = customerId, TransactionType = TransactionType.Expense, Amount = 1200m, CreatedBy = 1L } // 其他用户创建
        };

        var queryableMock = transactions.AsQueryable().BuildMock();
        _transactionRepositoryMock.Setup(x => x.GetQueryable())
            .Returns(queryableMock.Object);

        // 模拟 Viewer 权限过滤
        _permissionServiceMock.Setup(x => x.ApplyPermissionFilter(It.IsAny<IQueryable<Transaction>>()))
            .Returns((IQueryable<Transaction> query) => query.Where(t => t.CreatedBy == viewerUserId));

        // Act
        var result = await _service.GetCustomerStatisticsAsync(customerId);

        // Assert
        result.Should().NotBeNull();
        result.TotalIncome.Should().Be(2000m); // 只统计 Viewer 自己创建的收入
        result.TotalExpense.Should().Be(800m); // 只统计 Viewer 自己创建的支出
        result.NetProfit.Should().Be(1200m);
        result.IncomeCount.Should().Be(1);
        result.ExpenseCount.Should().Be(1);
        result.TotalCount.Should().Be(2);
    }

    [Fact(DisplayName = "GetCustomerStatisticsAsync - 转账交易：只统计包含'转账至'描述的转账")]
    public async Task GetCustomerStatisticsAsync_转账交易_只统计符合条件的转账()
    {
        // Arrange
        var customerId = 1L;
        var transactions = new List<Transaction>
        {
            new() { Id = 1, CustomerId = customerId, TransactionType = TransactionType.Transfer, Amount = 500m, Description = "转账至账户A", CreatedBy = 1L },
            new() { Id = 2, CustomerId = customerId, TransactionType = TransactionType.Transfer, Amount = 300m, Description = "普通转账", CreatedBy = 1L }, // 不包含"转账至"
            new() { Id = 3, CustomerId = customerId, TransactionType = TransactionType.Transfer, Amount = 200m, Description = null, CreatedBy = 1L } // Description 为 null
        };

        var queryableMock = transactions.AsQueryable().BuildMock();
        _transactionRepositoryMock.Setup(x => x.GetQueryable())
            .Returns(queryableMock.Object);

        // Act
        var result = await _service.GetCustomerStatisticsAsync(customerId);

        // Assert
        result.Should().NotBeNull();
        result.TotalTransfer.Should().Be(500m); // 只统计包含"转账至"的转账
        result.TransferCount.Should().Be(1);
        result.TotalCount.Should().Be(3); // 总数包含所有转账
    }

    #endregion

    #region GetSupplierStatisticsAsync 测试

    [Fact(DisplayName = "GetSupplierStatisticsAsync - 正常场景：返回正确的统计数据")]
    public async Task GetSupplierStatisticsAsync_正常场景_返回正确的统计数据()
    {
        var supplierId = 1L;
        var transactions = new List<Transaction>
        {
            new() { Id = 1, SupplierId = supplierId, TransactionType = TransactionType.Income, Amount = 1000m, CreatedBy = 1L },
            new() { Id = 2, SupplierId = supplierId, TransactionType = TransactionType.Expense, Amount = 2500m, CreatedBy = 1L },
            new() { Id = 3, SupplierId = supplierId, TransactionType = TransactionType.Transfer, Amount = 300m, Description = "转账至账户A", CreatedBy = 1L },
            new() { Id = 4, SupplierId = supplierId, TransactionType = TransactionType.Transfer, Amount = 400m, Description = "转账自账户B", CreatedBy = 1L },
            new() { Id = 5, SupplierId = 2L, TransactionType = TransactionType.Expense, Amount = 999m, CreatedBy = 1L }
        };

        var queryableMock = transactions.AsQueryable().BuildMock();
        _transactionRepositoryMock.Setup(x => x.GetQueryable()).Returns(queryableMock.Object);

        var result = await _service.GetSupplierStatisticsAsync(supplierId);

        result.TotalIncome.Should().Be(1000m);
        result.TotalExpense.Should().Be(2500m);
        result.NetProfit.Should().Be(-1500m);
        result.TotalTransfer.Should().Be(300m);
        result.TransferCount.Should().Be(1);
        result.TotalCount.Should().Be(4);
    }

    [Fact(DisplayName = "GetSupplierStatisticsAsync - 边界场景：空数据")]
    public async Task GetSupplierStatisticsAsync_空数据_返回零值统计()
    {
        var supplierId = 1L;
        var queryableMock = new List<Transaction>().AsQueryable().BuildMock();
        _transactionRepositoryMock.Setup(x => x.GetQueryable()).Returns(queryableMock.Object);

        var result = await _service.GetSupplierStatisticsAsync(supplierId);

        result.TotalIncome.Should().Be(0m);
        result.TotalExpense.Should().Be(0m);
        result.NetProfit.Should().Be(0m);
        result.TotalTransfer.Should().Be(0m);
        result.TotalCount.Should().Be(0);
    }

    [Fact(DisplayName = "GetSupplierStatisticsAsync - 数据权限：Viewer 只能看到自己创建的数据")]
    public async Task GetSupplierStatisticsAsync_Viewer权限_只返回自己创建的数据统计()
    {
        var supplierId = 1L;
        var viewerUserId = 4L;
        var transactions = new List<Transaction>
        {
            new() { Id = 1, SupplierId = supplierId, TransactionType = TransactionType.Income, Amount = 1000m, CreatedBy = viewerUserId },
            new() { Id = 2, SupplierId = supplierId, TransactionType = TransactionType.Expense, Amount = 800m, CreatedBy = 1L },
            new() { Id = 3, SupplierId = supplierId, TransactionType = TransactionType.Expense, Amount = 200m, CreatedBy = viewerUserId }
        };

        var queryableMock = transactions.AsQueryable().BuildMock();
        _transactionRepositoryMock.Setup(x => x.GetQueryable()).Returns(queryableMock.Object);
        _permissionServiceMock.Setup(x => x.ApplyPermissionFilter(It.IsAny<IQueryable<Transaction>>()))
            .Returns((IQueryable<Transaction> query) => query.Where(t => t.CreatedBy == viewerUserId));

        var result = await _service.GetSupplierStatisticsAsync(supplierId);

        result.TotalIncome.Should().Be(1000m);
        result.TotalExpense.Should().Be(200m);
        result.NetProfit.Should().Be(800m);
        result.TotalCount.Should().Be(2);
    }

    #endregion

    #region GetPersonStatisticsAsync 测试

    [Fact(DisplayName = "GetPersonStatisticsAsync - 正常场景：直接关联和分摊都应计入")]
    public async Task GetPersonStatisticsAsync_正常场景_返回正确的统计数据()
    {
        var personId = 1L;
        var transactions = new List<Transaction>
        {
            new() { Id = 1, PersonId = personId, IsAllocated = false, TransactionType = TransactionType.Expense, Amount = 1500m, CreatedBy = 1L },
            new()
            {
                Id = 2,
                IsAllocated = true,
                TransactionType = TransactionType.Income,
                Amount = 3000m,
                CreatedBy = 1L,
                Allocations = new List<TransactionAllocation>
                {
                    new() { PersonId = personId, Amount = 3000m }
                }
            },
            new() { Id = 3, PersonId = personId, IsAllocated = false, TransactionType = TransactionType.Transfer, Amount = 200m, Description = "转账至账户A", CreatedBy = 1L },
            new() { Id = 4, PersonId = personId, IsAllocated = false, TransactionType = TransactionType.Transfer, Amount = 100m, Description = "转账自账户B", CreatedBy = 1L },
            new() { Id = 5, PersonId = 2L, IsAllocated = false, TransactionType = TransactionType.Income, Amount = 999m, CreatedBy = 1L }
        };

        var queryableMock = transactions.AsQueryable().BuildMock();
        _transactionRepositoryMock.Setup(x => x.GetQueryable()).Returns(queryableMock.Object);

        var result = await _service.GetPersonStatisticsAsync(personId);

        result.TotalIncome.Should().Be(3000m);
        result.TotalExpense.Should().Be(1500m);
        result.NetProfit.Should().Be(1500m);
        result.TotalTransfer.Should().Be(200m);
        result.TransferCount.Should().Be(1);
        result.TotalCount.Should().Be(4);
    }

    [Fact(DisplayName = "GetPersonStatisticsAsync - 边界场景：空数据")]
    public async Task GetPersonStatisticsAsync_空数据_返回零值统计()
    {
        var personId = 1L;
        var queryableMock = new List<Transaction>().AsQueryable().BuildMock();
        _transactionRepositoryMock.Setup(x => x.GetQueryable()).Returns(queryableMock.Object);

        var result = await _service.GetPersonStatisticsAsync(personId);

        result.TotalIncome.Should().Be(0m);
        result.TotalExpense.Should().Be(0m);
        result.NetProfit.Should().Be(0m);
        result.TotalTransfer.Should().Be(0m);
        result.TotalCount.Should().Be(0);
    }

    [Fact(DisplayName = "GetPersonStatisticsAsync - 数据权限：Viewer 只能看到自己创建的数据")]
    public async Task GetPersonStatisticsAsync_Viewer权限_只返回自己创建的数据统计()
    {
        var personId = 1L;
        var viewerUserId = 5L;
        var transactions = new List<Transaction>
        {
            new() { Id = 1, PersonId = personId, IsAllocated = false, TransactionType = TransactionType.Income, Amount = 1200m, CreatedBy = viewerUserId },
            new() { Id = 2, PersonId = personId, IsAllocated = false, TransactionType = TransactionType.Expense, Amount = 300m, CreatedBy = 1L },
            new()
            {
                Id = 3,
                IsAllocated = true,
                TransactionType = TransactionType.Expense,
                Amount = 200m,
                CreatedBy = viewerUserId,
                Allocations = new List<TransactionAllocation>
                {
                    new() { PersonId = personId, Amount = 200m }
                }
            }
        };

        var queryableMock = transactions.AsQueryable().BuildMock();
        _transactionRepositoryMock.Setup(x => x.GetQueryable()).Returns(queryableMock.Object);
        _permissionServiceMock.Setup(x => x.ApplyPermissionFilter(It.IsAny<IQueryable<Transaction>>()))
            .Returns((IQueryable<Transaction> query) => query.Where(t => t.CreatedBy == viewerUserId));

        var result = await _service.GetPersonStatisticsAsync(personId);

        result.TotalIncome.Should().Be(1200m);
        result.TotalExpense.Should().Be(200m);
        result.NetProfit.Should().Be(1000m);
        result.TotalCount.Should().Be(2);
    }

    #endregion
}
