using FluentAssertions;
using FinanceApp.Application.Common;
using FinanceApp.Application.Modules.MasterData.DTOs.Customer;
using FinanceApp.Application.Modules.MasterData.Interfaces;
using FinanceApp.Application.Modules.MasterData.Services;
using FinanceApp.Application.Tests.Helpers;
using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Enums;
using FinanceApp.Application.Modules.Identity.Interfaces;
using FinanceApp.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;

namespace FinanceApp.Application.Tests.Services;

public class CustomerServiceTests : TestBase
{
    private readonly Mock<IRepository<Customer>> _repositoryMock;
    private readonly Mock<IRepository<Receivable>> _receivableRepositoryMock;
    private readonly Mock<IRepository<Payable>> _payableRepositoryMock;
    private readonly Mock<IRepository<TagBinding>> _tagBindingRepositoryMock;
    private readonly Mock<IMasterDataReferenceGuard> _referenceGuardMock;
    private readonly Mock<ILogger<CustomerService>> _loggerMock;
    private readonly CustomerService _service;

    public CustomerServiceTests()
    {
        _repositoryMock = new Mock<IRepository<Customer>>();
        _receivableRepositoryMock = new Mock<IRepository<Receivable>>();
        _payableRepositoryMock = new Mock<IRepository<Payable>>();
        _tagBindingRepositoryMock = new Mock<IRepository<TagBinding>>();
        _referenceGuardMock = new Mock<IMasterDataReferenceGuard>();
        _loggerMock = new Mock<ILogger<CustomerService>>();
        _referenceGuardMock.Setup(g => g.HasCustomerReferencesAsync(It.IsAny<long>())).ReturnsAsync(false);

        _service = new CustomerService(
            _repositoryMock.Object,
            _receivableRepositoryMock.Object,
            _payableRepositoryMock.Object,
            _tagBindingRepositoryMock.Object,
            _referenceGuardMock.Object,
            Mapper,
            _loggerMock.Object,
            AuditLogServiceMock.Object,
            CreateAdminCurrentUserService(),
            CreateAdminDataPermissionService(),
            UnitOfWorkMock.Object);
    }

    [Fact]
    public async Task GetPagedAsync_ShouldReturnPagedResult()
    {
        var customers = new List<Customer>
        {
            new() { Id = 1, Name = "客户1", IsDeleted = false, CreatedAt = DateTime.UtcNow },
            new() { Id = 2, Name = "客户2", IsDeleted = false, CreatedAt = DateTime.UtcNow.AddDays(-1) }
        };

        var queryableMock = customers.AsQueryable().BuildMock();
        _repositoryMock.Setup(r => r.GetQueryable()).Returns(queryableMock.Object);

        var request = new PageRequest { Page = 1, PageSize = 10 };
        var result = await _service.GetPagedAsync(request);

        result.Should().NotBeNull();
        result.Total.Should().Be(2);
        result.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetByIdAsync_WithExistingId_ShouldReturnCustomer()
    {
        var customer = new Customer { Id = 1, Name = "客户1", IsDeleted = false };
        _repositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(customer);

        var result = await _service.GetByIdAsync(1);

        result.Should().NotBeNull();
        result.Id.Should().Be(1);
        result.Name.Should().Be("客户1");
    }

    [Fact]
    public async Task GetByIdAsync_WithNonExistingId_ShouldThrowNotFoundException()
    {
        _repositoryMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Customer?)null);
        await Assert.ThrowsAsync<NotFoundException>(() => _service.GetByIdAsync(999));
    }

    [Fact]
    public async Task CreateAsync_WithValidRequest_ShouldCreateCustomer()
    {
        var request = new CreateCustomerRequest { Name = "新客户", ContactPerson = "张三" };
        _repositoryMock.Setup(r => r.AddAsync(It.IsAny<Customer>()))
            .ReturnsAsync((Customer c) => c);

        var result = await _service.CreateAsync(request);

        result.Should().NotBeNull();
        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<Customer>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WithExistingCustomer_ShouldUpdateCustomer()
    {
        var customer = new Customer { Id = 1, Name = "旧名称" };
        var request = new UpdateCustomerRequest { Name = "新名称", IsActive = true };

        _repositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(customer);

        var result = await _service.UpdateAsync(1, request);

        customer.Name.Should().Be("新名称");
    }

    [Fact]
    public async Task DeleteAsync_WithExistingCustomer_ShouldDeleteSuccessfully()
    {
        var customer = new Customer { Id = 1, Name = "客户1" };
        _repositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(customer);

        await _service.DeleteAsync(1);

        _repositoryMock.Verify(r => r.Delete(customer), Times.Once);
        _repositoryMock.Verify(r => r.Update(It.IsAny<Customer>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_WithReferencedCustomer_ShouldDeactivateInsteadOfDelete()
    {
        var customer = new Customer { Id = 1, Name = "测试客户", IsActive = true };
        _repositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(customer);
        _referenceGuardMock.Setup(g => g.HasCustomerReferencesAsync(1)).ReturnsAsync(true);

        await _service.DeleteAsync(1);

        customer.IsActive.Should().BeFalse();
        _repositoryMock.Verify(r => r.Update(customer), Times.Once);
        _repositoryMock.Verify(r => r.Delete(It.IsAny<Customer>()), Times.Never);
        AuditLogServiceMock.Verify(a => a.LogAsync("Archive", "Customer", 1, It.IsAny<string?>(), It.IsAny<string?>()), Times.Once);
    }


    [Fact]
    public async Task BatchCreateAsync_WithValidItems_ShouldCreateAll()
    {
        var items = new List<CreateCustomerRequest>
        {
            new() { Name = "客户A", ContactPerson = "张三" },
            new() { Name = "客户B", ContactPerson = "李四" }
        };

        _repositoryMock.Setup(r => r.AddAsync(It.IsAny<Customer>()))
            .ReturnsAsync((Customer c) => c);

        var result = await _service.BatchCreateAsync(items);

        result.TotalCount.Should().Be(2);
        result.SuccessCount.Should().Be(2);
        result.FailedCount.Should().Be(0);
        result.SuccessItems.Should().HaveCount(2);
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task BatchCreateAsync_WithEmptyList_ShouldReturnEmptyResult()
    {
        var items = new List<CreateCustomerRequest>();

        var result = await _service.BatchCreateAsync(items);

        result.TotalCount.Should().Be(0);
        result.SuccessCount.Should().Be(0);
        result.FailedCount.Should().Be(0);
    }

    [Fact]
    public async Task GetPagedAsync_WithNameFilter_ShouldReturnMatchingCustomers()
    {
        var customers = new List<Customer>
        {
            new() { Id = 1, Name = "华为科技", ContactPerson = "张三", IsDeleted = false, CreatedAt = DateTime.UtcNow },
            new() { Id = 2, Name = "腾讯科技", ContactPerson = "李四", IsDeleted = false, CreatedAt = DateTime.UtcNow.AddDays(-1) },
            new() { Id = 3, Name = "阿里巴巴", ContactPerson = "王五", IsDeleted = false, CreatedAt = DateTime.UtcNow.AddDays(-2) }
        };

        var queryableMock = customers.AsQueryable().BuildMock();
        _repositoryMock.Setup(r => r.GetQueryable()).Returns(queryableMock.Object);

        var request = new PageRequest { Page = 1, PageSize = 10, Name = "科技" };
        var result = await _service.GetPagedAsync(request);

        result.Total.Should().Be(2);
        result.Items.Should().HaveCount(2);
        result.Items.Should().AllSatisfy(c => c.Name.Should().Contain("科技"));
    }

    [Fact]
    public async Task GetPagedAsync_WithContactPersonFilter_ShouldReturnMatchingCustomers()
    {
        var customers = new List<Customer>
        {
            new() { Id = 1, Name = "客户1", ContactPerson = "张三", IsDeleted = false, CreatedAt = DateTime.UtcNow },
            new() { Id = 2, Name = "客户2", ContactPerson = "张伟", IsDeleted = false, CreatedAt = DateTime.UtcNow.AddDays(-1) },
            new() { Id = 3, Name = "客户3", ContactPerson = "李四", IsDeleted = false, CreatedAt = DateTime.UtcNow.AddDays(-2) }
        };

        var queryableMock = customers.AsQueryable().BuildMock();
        _repositoryMock.Setup(r => r.GetQueryable()).Returns(queryableMock.Object);

        var request = new PageRequest { Page = 1, PageSize = 10, ContactPerson = "张" };
        var result = await _service.GetPagedAsync(request);

        result.Total.Should().Be(2);
        result.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetPagedAsync_WithContactPhoneFilter_ShouldReturnMatchingCustomers()
    {
        var customers = new List<Customer>
        {
            new() { Id = 1, Name = "客户1", ContactPhone = "13800138000", IsDeleted = false, CreatedAt = DateTime.UtcNow },
            new() { Id = 2, Name = "客户2", ContactPhone = "13900139000", IsDeleted = false, CreatedAt = DateTime.UtcNow.AddDays(-1) },
            new() { Id = 3, Name = "客户3", ContactPhone = null, IsDeleted = false, CreatedAt = DateTime.UtcNow.AddDays(-2) }
        };

        var queryableMock = customers.AsQueryable().BuildMock();
        _repositoryMock.Setup(r => r.GetQueryable()).Returns(queryableMock.Object);

        var request = new PageRequest { Page = 1, PageSize = 10, ContactPhone = "138" };
        var result = await _service.GetPagedAsync(request);

        result.Total.Should().Be(1);
        result.Items.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetPagedAsync_WithMultipleFilters_ShouldApplyAllFilters()
    {
        var customers = new List<Customer>
        {
            new() { Id = 1, Name = "华为科技", ContactPerson = "张三", ContactPhone = "13800138000", IsDeleted = false, CreatedAt = DateTime.UtcNow },
            new() { Id = 2, Name = "华为云", ContactPerson = "李四", ContactPhone = "13900139000", IsDeleted = false, CreatedAt = DateTime.UtcNow.AddDays(-1) },
            new() { Id = 3, Name = "腾讯科技", ContactPerson = "张伟", ContactPhone = "13700137000", IsDeleted = false, CreatedAt = DateTime.UtcNow.AddDays(-2) }
        };

        var queryableMock = customers.AsQueryable().BuildMock();
        _repositoryMock.Setup(r => r.GetQueryable()).Returns(queryableMock.Object);

        var request = new PageRequest { Page = 1, PageSize = 10, Name = "华为", ContactPerson = "张" };
        var result = await _service.GetPagedAsync(request);

        result.Total.Should().Be(1);
        result.Items.Should().HaveCount(1);
        result.Items[0].Name.Should().Be("华为科技");
    }

    [Fact]
    public async Task GetPagedAsync_WithMultipleTagFilterGroups_ShouldApplyGroupIntersection()
    {
        var customers = new List<Customer>
        {
            new() { Id = 1, Name = "客户1", IsActive = true, IsDeleted = false, CreatedAt = DateTime.UtcNow },
            new() { Id = 2, Name = "客户2", IsActive = true, IsDeleted = false, CreatedAt = DateTime.UtcNow.AddDays(-1) },
            new() { Id = 3, Name = "客户3", IsActive = true, IsDeleted = false, CreatedAt = DateTime.UtcNow.AddDays(-2) }
        };

        var bindings = new List<TagBinding>
        {
            new() { Id = 1, OwnerType = Domain.Enums.TagScope.Customer, OwnerId = 1, TagId = 101 },
            new() { Id = 2, OwnerType = Domain.Enums.TagScope.Customer, OwnerId = 1, TagId = 102 },
            new() { Id = 3, OwnerType = Domain.Enums.TagScope.Customer, OwnerId = 2, TagId = 101 },
            new() { Id = 4, OwnerType = Domain.Enums.TagScope.Customer, OwnerId = 3, TagId = 102 }
        };

        _repositoryMock.Setup(r => r.GetQueryable()).Returns(customers.AsQueryable().BuildMock().Object);
        _tagBindingRepositoryMock.Setup(r => r.GetQueryable()).Returns(bindings.AsQueryable().BuildMock().Object);

        var request = new PageRequest
        {
            Page = 1,
            PageSize = 10,
            TagFilters = new List<TagFilterGroup>
            {
                new() { Scope = Domain.Enums.TagScope.Customer, TagIds = new List<long> { 101 }, MatchMode = TagMatchMode.Or },
                new() { Scope = Domain.Enums.TagScope.Customer, TagIds = new List<long> { 102 }, MatchMode = TagMatchMode.Or }
            }
        };

        var result = await _service.GetPagedAsync(request);

        result.Total.Should().Be(1);
        result.Items.Should().ContainSingle();
        result.Items[0].Id.Should().Be(1);
    }

    #region GetStatisticsAsync Tests

    [Fact]
    public async Task GetStatisticsAsync_ShouldReturnCorrectStatistics()
    {
        // Arrange
        var customers = new List<Customer>
        {
            new() { Id = 1, Name = "客户1", IsActive = true, IsDeleted = false, CreatedAt = DateTime.UtcNow },
            new() { Id = 2, Name = "客户2", IsActive = true, IsDeleted = false, CreatedAt = DateTime.UtcNow },
            new() { Id = 3, Name = "客户3", IsActive = false, IsDeleted = false, CreatedAt = DateTime.UtcNow.AddMonths(-1) }
        };

        var queryableMock = customers.AsQueryable().BuildMock();
        _repositoryMock.Setup(r => r.GetQueryable()).Returns(queryableMock.Object);

        // Act
        var result = await _service.GetStatisticsAsync();

        // Assert
        result.Should().NotBeNull();
        result.TotalCount.Should().Be(3);
        result.ActiveCount.Should().Be(2);
        result.InactiveCount.Should().Be(1);
        result.ThisMonthNewCount.Should().Be(2); // 只有本月创建的2条
    }

    [Fact]
    public async Task GetStatisticsAsync_WithEmptyData_ShouldReturnZeros()
    {
        // Arrange
        var customers = new List<Customer>();
        var queryableMock = customers.AsQueryable().BuildMock();
        _repositoryMock.Setup(r => r.GetQueryable()).Returns(queryableMock.Object);

        // Act
        var result = await _service.GetStatisticsAsync();

        // Assert
        result.Should().NotBeNull();
        result.TotalCount.Should().Be(0);
        result.ActiveCount.Should().Be(0);
        result.InactiveCount.Should().Be(0);
        result.ThisMonthNewCount.Should().Be(0);
    }

    [Fact]
    public async Task GetStatisticsAsync_WithTagFilters_ShouldFilterByMatchingCustomers()
    {
        var now = DateTime.UtcNow;
        var customers = new List<Customer>
        {
            new() { Id = 1, Name = "客户1", IsActive = true, IsDeleted = false, CreatedAt = now },
            new() { Id = 2, Name = "客户2", IsActive = true, IsDeleted = false, CreatedAt = now },
            new() { Id = 3, Name = "客户3", IsActive = false, IsDeleted = false, CreatedAt = now.AddMonths(-1) }
        };

        var bindings = new List<TagBinding>
        {
            new() { Id = 1, OwnerType = Domain.Enums.TagScope.Customer, OwnerId = 1, TagId = 101 },
            new() { Id = 2, OwnerType = Domain.Enums.TagScope.Customer, OwnerId = 1, TagId = 102 },
            new() { Id = 3, OwnerType = Domain.Enums.TagScope.Customer, OwnerId = 2, TagId = 101 },
            new() { Id = 4, OwnerType = Domain.Enums.TagScope.Customer, OwnerId = 3, TagId = 102 }
        };

        _repositoryMock.Setup(r => r.GetQueryable()).Returns(customers.AsQueryable().BuildMock().Object);
        _tagBindingRepositoryMock.Setup(r => r.GetQueryable()).Returns(bindings.AsQueryable().BuildMock().Object);

        var request = new PageRequest
        {
            Page = 1,
            PageSize = 10,
            TagFilters = new List<TagFilterGroup>
            {
                new() { Scope = Domain.Enums.TagScope.Customer, TagIds = new List<long> { 101 }, MatchMode = TagMatchMode.Or },
                new() { Scope = Domain.Enums.TagScope.Customer, TagIds = new List<long> { 102 }, MatchMode = TagMatchMode.Or }
            }
        };

        var result = await _service.GetStatisticsAsync(request);

        result.TotalCount.Should().Be(1);
        result.ActiveCount.Should().Be(1);
        result.InactiveCount.Should().Be(0);
        result.ThisMonthNewCount.Should().Be(1);
    }

    #endregion

    #region GetFinanceSummaryAsync Tests

    [Fact]
    public async Task GetFinanceSummaryAsync_WithExistingCustomer_ShouldReturnCorrectSummary()
    {
        // Arrange
        var customer = new Customer { Id = 1, Name = "客户1", IsDeleted = false };
        _repositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(customer);

        var today = DateTime.UtcNow.Date;
        var receivables = new List<Receivable>
        {
            new() { Id = 1, CustomerId = 1, ProjectId = 10, TotalAmount = 1000, ReceivedAmount = 500, RemainingAmount = 500, Status = ReceivableStatus.Partial, DueDate = today.AddDays(-5) },
            new() { Id = 2, CustomerId = 1, ProjectId = 20, TotalAmount = 2000, ReceivedAmount = 2000, RemainingAmount = 0, Status = ReceivableStatus.Settled, DueDate = today.AddDays(-1) },
            new() { Id = 3, CustomerId = 1, ProjectId = 10, TotalAmount = 3000, ReceivedAmount = 0, RemainingAmount = 3000, Status = ReceivableStatus.Pending, DueDate = today.AddDays(30) },
        };

        _receivableRepositoryMock.Setup(r => r.GetQueryable())
            .Returns(receivables.AsQueryable().BuildMock().Object);

        _payableRepositoryMock.Setup(r => r.GetQueryable())
            .Returns(new List<Payable>().AsQueryable().BuildMock().Object);

        // Act
        var result = await _service.GetFinanceSummaryAsync(1);

        // Assert
        result.Should().NotBeNull();
        result.TotalReceivable.Should().Be(6000);
        result.TotalReceived.Should().Be(2500);
        result.ReceivableRemaining.Should().Be(3500);
        result.ReceivableOverdueCount.Should().Be(1); // ID=1 逾期且未结清
        result.ReceivableOverdueAmount.Should().Be(500);
        result.ProjectCount.Should().Be(2); // ProjectId 10 和 20
    }

    [Fact]
    public async Task GetFinanceSummaryAsync_WithNoReceivables_ShouldReturnZeros()
    {
        // Arrange
        var customer = new Customer { Id = 1, Name = "客户1", IsDeleted = false };
        _repositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(customer);

        _receivableRepositoryMock.Setup(r => r.GetQueryable())
            .Returns(new List<Receivable>().AsQueryable().BuildMock().Object);

        _payableRepositoryMock.Setup(r => r.GetQueryable())
            .Returns(new List<Payable>().AsQueryable().BuildMock().Object);

        // Act
        var result = await _service.GetFinanceSummaryAsync(1);

        // Assert
        result.Should().NotBeNull();
        result.TotalReceivable.Should().Be(0);
        result.TotalReceived.Should().Be(0);
        result.ReceivableRemaining.Should().Be(0);
        result.ReceivableOverdueCount.Should().Be(0);
        result.ProjectCount.Should().Be(0);
    }

    [Fact]
    public async Task GetFinanceSummaryAsync_WithNonExistingCustomer_ShouldThrowNotFoundException()
    {
        // Arrange
        _repositoryMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Customer?)null);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => _service.GetFinanceSummaryAsync(999));
    }

    [Fact]
    public async Task GetFinanceSummaryAsync_WithBothReceivablesAndPayables_ShouldReturnCombinedSummary()
    {
        // Arrange
        var customer = new Customer { Id = 1, Name = "客户1", IsDeleted = false };
        _repositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(customer);

        var today = DateTime.UtcNow.Date;
        var receivables = new List<Receivable>
        {
            new() { Id = 1, CustomerId = 1, ProjectId = 10, TotalAmount = 5000, ReceivedAmount = 2000, RemainingAmount = 3000, Status = ReceivableStatus.Partial, DueDate = today.AddDays(-3) },
            new() { Id = 2, CustomerId = 1, ProjectId = 20, TotalAmount = 3000, ReceivedAmount = 0, RemainingAmount = 3000, Status = ReceivableStatus.Pending, DueDate = today.AddDays(30) },
        };

        var payables = new List<Payable>
        {
            new() { Id = 1, CustomerId = 1, ProjectId = 20, TotalAmount = 4000, PaidAmount = 1000, RemainingAmount = 3000, Status = PayableStatus.Partial, DueDate = today.AddDays(-5) },
            new() { Id = 2, CustomerId = 1, ProjectId = 30, TotalAmount = 2000, PaidAmount = 2000, RemainingAmount = 0, Status = PayableStatus.Settled, DueDate = today.AddDays(-1) },
        };

        _receivableRepositoryMock.Setup(r => r.GetQueryable())
            .Returns(receivables.AsQueryable().BuildMock().Object);
        _payableRepositoryMock.Setup(r => r.GetQueryable())
            .Returns(payables.AsQueryable().BuildMock().Object);

        // Act
        var result = await _service.GetFinanceSummaryAsync(1);

        // Assert — Receivable dimension
        result.TotalReceivable.Should().Be(8000);
        result.TotalReceived.Should().Be(2000);
        result.ReceivableRemaining.Should().Be(6000);
        result.ReceivableOverdueCount.Should().Be(1); // ID=1 逾期且未结清
        result.ReceivableOverdueAmount.Should().Be(3000);

        // Assert — Payable dimension
        result.TotalPayable.Should().Be(6000);
        result.TotalPaid.Should().Be(3000);
        result.PayableRemaining.Should().Be(3000);
        result.PayableOverdueCount.Should().Be(1); // ID=1 逾期且未结清
        result.PayableOverdueAmount.Should().Be(3000);

        // Assert — ProjectCount is union of both: {10, 20, 30} = 3
        result.ProjectCount.Should().Be(3);
    }

    [Fact]
    public async Task GetFinanceSummaryAsync_WithPayablesOnly_ShouldReturnPayableDataAndZeroReceivable()
    {
        // Arrange
        var customer = new Customer { Id = 1, Name = "客户1", IsDeleted = false };
        _repositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(customer);

        var today = DateTime.UtcNow.Date;
        var payables = new List<Payable>
        {
            new() { Id = 1, CustomerId = 1, ProjectId = 10, TotalAmount = 7000, PaidAmount = 3000, RemainingAmount = 4000, Status = PayableStatus.Partial, DueDate = today.AddDays(-2) },
        };

        _receivableRepositoryMock.Setup(r => r.GetQueryable())
            .Returns(new List<Receivable>().AsQueryable().BuildMock().Object);
        _payableRepositoryMock.Setup(r => r.GetQueryable())
            .Returns(payables.AsQueryable().BuildMock().Object);

        // Act
        var result = await _service.GetFinanceSummaryAsync(1);

        // Assert — Receivable should be zeros
        result.TotalReceivable.Should().Be(0);
        result.TotalReceived.Should().Be(0);
        result.ReceivableRemaining.Should().Be(0);
        result.ReceivableOverdueCount.Should().Be(0);

        // Assert — Payable should have data
        result.TotalPayable.Should().Be(7000);
        result.TotalPaid.Should().Be(3000);
        result.PayableRemaining.Should().Be(4000);
        result.PayableOverdueCount.Should().Be(1);
        result.PayableOverdueAmount.Should().Be(4000);

        // Assert — ProjectCount from payables only
        result.ProjectCount.Should().Be(1);
    }

    #endregion
}
