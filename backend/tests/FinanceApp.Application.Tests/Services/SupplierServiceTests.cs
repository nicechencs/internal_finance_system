using FluentAssertions;
using FinanceApp.Application.Common;
using FinanceApp.Application.Modules.MasterData.Interfaces;
using FinanceApp.Application.Modules.MasterData.DTOs.Supplier;
using FinanceApp.Application.Modules.Identity.Interfaces;
using FinanceApp.Application.Modules.MasterData.Services;
using FinanceApp.Application.Tests.Helpers;
using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Enums;
using FinanceApp.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;

namespace FinanceApp.Application.Tests.Services;

public class SupplierServiceTests : TestBase
{
    private readonly Mock<IRepository<Supplier>> _repositoryMock;
    private readonly Mock<IRepository<Payable>> _payableRepositoryMock;
    private readonly Mock<IRepository<Receivable>> _receivableRepositoryMock;
    private readonly Mock<IRepository<TagBinding>> _tagBindingRepositoryMock;
    private readonly Mock<IMasterDataReferenceGuard> _referenceGuardMock;
    private readonly Mock<ILogger<SupplierService>> _loggerMock;
    private readonly SupplierService _service;

    public SupplierServiceTests()
    {
        _repositoryMock = new Mock<IRepository<Supplier>>();
        _payableRepositoryMock = new Mock<IRepository<Payable>>();
        _receivableRepositoryMock = new Mock<IRepository<Receivable>>();
        _tagBindingRepositoryMock = new Mock<IRepository<TagBinding>>();
        _referenceGuardMock = new Mock<IMasterDataReferenceGuard>();
        _loggerMock = new Mock<ILogger<SupplierService>>();
        _referenceGuardMock.Setup(g => g.HasSupplierReferencesAsync(It.IsAny<long>())).ReturnsAsync(false);

        _service = new SupplierService(
            _repositoryMock.Object,
            _payableRepositoryMock.Object,
            _receivableRepositoryMock.Object,
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
        var customers = new List<Supplier>
        {
            new() { Id = 1, Name = "供应商1", IsDeleted = false, CreatedAt = DateTime.UtcNow },
            new() { Id = 2, Name = "供应商2", IsDeleted = false, CreatedAt = DateTime.UtcNow.AddDays(-1) }
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
    public async Task GetByIdAsync_WithExistingId_ShouldReturnSupplier()
    {
        var customer = new Supplier { Id = 1, Name = "供应商1", IsDeleted = false };
        _repositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(customer);

        var result = await _service.GetByIdAsync(1);

        result.Should().NotBeNull();
        result.Id.Should().Be(1);
        result.Name.Should().Be("供应商1");
    }

    [Fact]
    public async Task GetByIdAsync_WithNonExistingId_ShouldThrowNotFoundException()
    {
        _repositoryMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Supplier?)null);
        await Assert.ThrowsAsync<NotFoundException>(() => _service.GetByIdAsync(999));
    }

    [Fact]
    public async Task CreateAsync_WithValidRequest_ShouldCreateSupplier()
    {
        var request = new CreateSupplierRequest { Name = "新供应商", ContactPerson = "张三" };
        _repositoryMock.Setup(r => r.AddAsync(It.IsAny<Supplier>()))
            .ReturnsAsync((Supplier s) => s);

        var result = await _service.CreateAsync(request);

        result.Should().NotBeNull();
        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<Supplier>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WithExistingSupplier_ShouldUpdateSupplier()
    {
        var customer = new Supplier { Id = 1, Name = "旧名称" };
        var request = new UpdateSupplierRequest { Name = "新名称", IsActive = true };

        _repositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(customer);

        var result = await _service.UpdateAsync(1, request);

        customer.Name.Should().Be("新名称");
    }

    [Fact]
    public async Task DeleteAsync_WithExistingSupplier_ShouldDeleteSuccessfully()
    {
        var customer = new Supplier { Id = 1, Name = "供应商1" };
        _repositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(customer);

        await _service.DeleteAsync(1);

        _repositoryMock.Verify(r => r.Delete(customer), Times.Once);
        _repositoryMock.Verify(r => r.Update(It.IsAny<Supplier>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_WithReferencedSupplier_ShouldDeactivateInsteadOfDelete()
    {
        var supplier = new Supplier { Id = 1, Name = "测试供应商", IsActive = true };
        _repositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(supplier);
        _referenceGuardMock.Setup(g => g.HasSupplierReferencesAsync(1)).ReturnsAsync(true);

        await _service.DeleteAsync(1);

        supplier.IsActive.Should().BeFalse();
        _repositoryMock.Verify(r => r.Update(supplier), Times.Once);
        _repositoryMock.Verify(r => r.Delete(It.IsAny<Supplier>()), Times.Never);
        AuditLogServiceMock.Verify(a => a.LogAsync("Archive", "Supplier", 1, It.IsAny<string?>(), It.IsAny<string?>()), Times.Once);
    }

    [Fact]
    public async Task BatchCreateAsync_WithValidItems_ShouldCreateAll()
    {
        var items = new List<CreateSupplierRequest>
        {
            new() { Name = "供应商A", ContactPerson = "张三" },
            new() { Name = "供应商B", ContactPerson = "李四" }
        };

        _repositoryMock.Setup(r => r.AddAsync(It.IsAny<Supplier>()))
            .ReturnsAsync((Supplier s) => s);

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
        var items = new List<CreateSupplierRequest>();

        var result = await _service.BatchCreateAsync(items);

        result.TotalCount.Should().Be(0);
        result.SuccessCount.Should().Be(0);
        result.FailedCount.Should().Be(0);
    }

    [Fact]
    public async Task GetPagedAsync_WithNameFilter_ShouldReturnMatchingSuppliers()
    {
        var suppliers = new List<Supplier>
        {
            new() { Id = 1, Name = "华为供应商", ContactPerson = "张三", IsDeleted = false, CreatedAt = DateTime.UtcNow },
            new() { Id = 2, Name = "腾讯供应商", ContactPerson = "李四", IsDeleted = false, CreatedAt = DateTime.UtcNow.AddDays(-1) },
            new() { Id = 3, Name = "阿里巴巴", ContactPerson = "王五", IsDeleted = false, CreatedAt = DateTime.UtcNow.AddDays(-2) }
        };

        var queryableMock = suppliers.AsQueryable().BuildMock();
        _repositoryMock.Setup(r => r.GetQueryable()).Returns(queryableMock.Object);

        var request = new PageRequest { Page = 1, PageSize = 10, Name = "供应商" };
        var result = await _service.GetPagedAsync(request);

        result.Total.Should().Be(2);
        result.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetPagedAsync_WithContactPersonFilter_ShouldReturnMatchingSuppliers()
    {
        var suppliers = new List<Supplier>
        {
            new() { Id = 1, Name = "供应商1", ContactPerson = "张三", IsDeleted = false, CreatedAt = DateTime.UtcNow },
            new() { Id = 2, Name = "供应商2", ContactPerson = "张伟", IsDeleted = false, CreatedAt = DateTime.UtcNow.AddDays(-1) },
            new() { Id = 3, Name = "供应商3", ContactPerson = "李四", IsDeleted = false, CreatedAt = DateTime.UtcNow.AddDays(-2) }
        };

        var queryableMock = suppliers.AsQueryable().BuildMock();
        _repositoryMock.Setup(r => r.GetQueryable()).Returns(queryableMock.Object);

        var request = new PageRequest { Page = 1, PageSize = 10, ContactPerson = "张" };
        var result = await _service.GetPagedAsync(request);

        result.Total.Should().Be(2);
        result.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetPagedAsync_WithContactPhoneFilter_ShouldReturnMatchingSuppliers()
    {
        var suppliers = new List<Supplier>
        {
            new() { Id = 1, Name = "供应商1", ContactPhone = "13800138000", IsDeleted = false, CreatedAt = DateTime.UtcNow },
            new() { Id = 2, Name = "供应商2", ContactPhone = "13900139000", IsDeleted = false, CreatedAt = DateTime.UtcNow.AddDays(-1) },
            new() { Id = 3, Name = "供应商3", ContactPhone = null, IsDeleted = false, CreatedAt = DateTime.UtcNow.AddDays(-2) }
        };

        var queryableMock = suppliers.AsQueryable().BuildMock();
        _repositoryMock.Setup(r => r.GetQueryable()).Returns(queryableMock.Object);

        var request = new PageRequest { Page = 1, PageSize = 10, ContactPhone = "138" };
        var result = await _service.GetPagedAsync(request);

        result.Total.Should().Be(1);
        result.Items.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetPagedAsync_WithMultipleFilters_ShouldApplyAllFilters()
    {
        var suppliers = new List<Supplier>
        {
            new() { Id = 1, Name = "华为供应商", ContactPerson = "张三", ContactPhone = "13800138000", IsDeleted = false, CreatedAt = DateTime.UtcNow },
            new() { Id = 2, Name = "华为材料", ContactPerson = "李四", ContactPhone = "13900139000", IsDeleted = false, CreatedAt = DateTime.UtcNow.AddDays(-1) },
            new() { Id = 3, Name = "腾讯供应商", ContactPerson = "张伟", ContactPhone = "13700137000", IsDeleted = false, CreatedAt = DateTime.UtcNow.AddDays(-2) }
        };

        var queryableMock = suppliers.AsQueryable().BuildMock();
        _repositoryMock.Setup(r => r.GetQueryable()).Returns(queryableMock.Object);

        var request = new PageRequest { Page = 1, PageSize = 10, Name = "华为", ContactPerson = "张" };
        var result = await _service.GetPagedAsync(request);

        result.Total.Should().Be(1);
        result.Items.Should().HaveCount(1);
        result.Items[0].Name.Should().Be("华为供应商");
    }

    #region GetStatisticsAsync Tests

    [Fact]
    public async Task GetStatisticsAsync_ShouldReturnCorrectStatistics()
    {
        // Arrange
        var suppliers = new List<Supplier>
        {
            new() { Id = 1, Name = "供应商1", IsActive = true, IsDeleted = false, CreatedAt = DateTime.UtcNow },
            new() { Id = 2, Name = "供应商2", IsActive = true, IsDeleted = false, CreatedAt = DateTime.UtcNow },
            new() { Id = 3, Name = "供应商3", IsActive = false, IsDeleted = false, CreatedAt = DateTime.UtcNow.AddMonths(-1) }
        };

        var queryableMock = suppliers.AsQueryable().BuildMock();
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
        var suppliers = new List<Supplier>();
        var queryableMock = suppliers.AsQueryable().BuildMock();
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

    #endregion

    #region GetFinanceSummaryAsync Tests

    [Fact]
    public async Task GetFinanceSummaryAsync_WithExistingSupplier_ShouldReturnCorrectSummary()
    {
        // Arrange
        var supplier = new Supplier { Id = 1, Name = "供应商1", IsDeleted = false };
        _repositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(supplier);

        var today = DateTime.UtcNow.Date;
        var payables = new List<Payable>
        {
            new() { Id = 1, SupplierId = 1, ProjectId = 10, TotalAmount = 5000, PaidAmount = 2000, RemainingAmount = 3000, Status = PayableStatus.Partial, DueDate = today.AddDays(-10) },
            new() { Id = 2, SupplierId = 1, ProjectId = 20, TotalAmount = 3000, PaidAmount = 3000, RemainingAmount = 0, Status = PayableStatus.Settled, DueDate = today.AddDays(-1) },
            new() { Id = 3, SupplierId = 1, ProjectId = null, TotalAmount = 1000, PaidAmount = 0, RemainingAmount = 1000, Status = PayableStatus.Pending, DueDate = today.AddDays(15) },
        };

        _payableRepositoryMock.Setup(r => r.GetQueryable())
            .Returns(payables.AsQueryable().BuildMock().Object);

        _receivableRepositoryMock.Setup(r => r.GetQueryable())
            .Returns(new List<Receivable>().AsQueryable().BuildMock().Object);

        // Act
        var result = await _service.GetFinanceSummaryAsync(1);

        // Assert
        result.Should().NotBeNull();
        result.TotalPayable.Should().Be(9000);
        result.TotalPaid.Should().Be(5000);
        result.PayableRemaining.Should().Be(4000);
        result.PayableOverdueCount.Should().Be(1); // ID=1 逾期且未结清
        result.PayableOverdueAmount.Should().Be(3000);
        result.ProjectCount.Should().Be(2); // ProjectId 10 和 20（null 不计入）
    }

    [Fact]
    public async Task GetFinanceSummaryAsync_WithNoPayables_ShouldReturnZeros()
    {
        // Arrange
        var supplier = new Supplier { Id = 1, Name = "供应商1", IsDeleted = false };
        _repositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(supplier);

        _payableRepositoryMock.Setup(r => r.GetQueryable())
            .Returns(new List<Payable>().AsQueryable().BuildMock().Object);

        _receivableRepositoryMock.Setup(r => r.GetQueryable())
            .Returns(new List<Receivable>().AsQueryable().BuildMock().Object);

        // Act
        var result = await _service.GetFinanceSummaryAsync(1);

        // Assert
        result.Should().NotBeNull();
        result.TotalPayable.Should().Be(0);
        result.TotalPaid.Should().Be(0);
        result.PayableRemaining.Should().Be(0);
        result.PayableOverdueCount.Should().Be(0);
        result.ProjectCount.Should().Be(0);
    }

    [Fact]
    public async Task GetFinanceSummaryAsync_WithNonExistingSupplier_ShouldThrowNotFoundException()
    {
        _repositoryMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Supplier?)null);
        await Assert.ThrowsAsync<NotFoundException>(() => _service.GetFinanceSummaryAsync(999));
    }

    [Fact]
    public async Task GetFinanceSummaryAsync_WithBothReceivablesAndPayables_ShouldReturnCombinedSummary()
    {
        // Arrange
        var supplier = new Supplier { Id = 1, Name = "供应商1", IsDeleted = false };
        _repositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(supplier);

        var today = DateTime.UtcNow.Date;
        var receivables = new List<Receivable>
        {
            new() { Id = 1, SupplierId = 1, ProjectId = 10, TotalAmount = 4000, ReceivedAmount = 1000, RemainingAmount = 3000, Status = ReceivableStatus.Partial, DueDate = today.AddDays(-5) },
            new() { Id = 2, SupplierId = 1, ProjectId = 20, TotalAmount = 2000, ReceivedAmount = 2000, RemainingAmount = 0, Status = ReceivableStatus.Settled, DueDate = today.AddDays(-1) },
        };

        var payables = new List<Payable>
        {
            new() { Id = 1, SupplierId = 1, ProjectId = 20, TotalAmount = 6000, PaidAmount = 2000, RemainingAmount = 4000, Status = PayableStatus.Partial, DueDate = today.AddDays(-3) },
            new() { Id = 2, SupplierId = 1, ProjectId = 30, TotalAmount = 3000, PaidAmount = 0, RemainingAmount = 3000, Status = PayableStatus.Pending, DueDate = today.AddDays(15) },
        };

        _receivableRepositoryMock.Setup(r => r.GetQueryable())
            .Returns(receivables.AsQueryable().BuildMock().Object);
        _payableRepositoryMock.Setup(r => r.GetQueryable())
            .Returns(payables.AsQueryable().BuildMock().Object);

        // Act
        var result = await _service.GetFinanceSummaryAsync(1);

        // Assert — Receivable dimension
        result.TotalReceivable.Should().Be(6000);
        result.TotalReceived.Should().Be(3000);
        result.ReceivableRemaining.Should().Be(3000);
        result.ReceivableOverdueCount.Should().Be(1); // ID=1 逾期且未结清
        result.ReceivableOverdueAmount.Should().Be(3000);

        // Assert — Payable dimension
        result.TotalPayable.Should().Be(9000);
        result.TotalPaid.Should().Be(2000);
        result.PayableRemaining.Should().Be(7000);
        result.PayableOverdueCount.Should().Be(1); // ID=1 逾期且未结清
        result.PayableOverdueAmount.Should().Be(4000);

        // Assert — ProjectCount is union: {10, 20, 30} = 3
        result.ProjectCount.Should().Be(3);
    }

    [Fact]
    public async Task GetFinanceSummaryAsync_WithReceivablesOnly_ShouldReturnReceivableDataAndZeroPayable()
    {
        // Arrange
        var supplier = new Supplier { Id = 1, Name = "供应商1", IsDeleted = false };
        _repositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(supplier);

        var today = DateTime.UtcNow.Date;
        var receivables = new List<Receivable>
        {
            new() { Id = 1, SupplierId = 1, ProjectId = 10, TotalAmount = 8000, ReceivedAmount = 3000, RemainingAmount = 5000, Status = ReceivableStatus.Partial, DueDate = today.AddDays(-2) },
        };

        _receivableRepositoryMock.Setup(r => r.GetQueryable())
            .Returns(receivables.AsQueryable().BuildMock().Object);
        _payableRepositoryMock.Setup(r => r.GetQueryable())
            .Returns(new List<Payable>().AsQueryable().BuildMock().Object);

        // Act
        var result = await _service.GetFinanceSummaryAsync(1);

        // Assert — Receivable should have data
        result.TotalReceivable.Should().Be(8000);
        result.TotalReceived.Should().Be(3000);
        result.ReceivableRemaining.Should().Be(5000);
        result.ReceivableOverdueCount.Should().Be(1);
        result.ReceivableOverdueAmount.Should().Be(5000);

        // Assert — Payable should be zeros
        result.TotalPayable.Should().Be(0);
        result.TotalPaid.Should().Be(0);
        result.PayableRemaining.Should().Be(0);
        result.PayableOverdueCount.Should().Be(0);

        // Assert — ProjectCount from receivables only
        result.ProjectCount.Should().Be(1);
    }

    #endregion
}
