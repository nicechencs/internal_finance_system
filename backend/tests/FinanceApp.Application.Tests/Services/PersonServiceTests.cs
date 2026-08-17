using FluentAssertions;
using FinanceApp.Application.Common;
using FinanceApp.Application.Modules.MasterData.Interfaces;
using FinanceApp.Application.Modules.MasterData.DTOs.Person;
using FinanceApp.Application.Modules.MasterData.Services;
using FinanceApp.Application.Tests.Helpers;
using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Enums;
using FinanceApp.Application.Modules.Identity.Interfaces;
using FinanceApp.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;

namespace FinanceApp.Application.Tests.Services;

public class PersonServiceTests : TestBase
{
    private readonly Mock<IRepository<Person>> _personRepositoryMock;
    private readonly Mock<IRepository<Transaction>> _transactionRepositoryMock;
    private readonly Mock<IRepository<TransactionAllocation>> _allocationRepositoryMock;
    private readonly Mock<IRepository<Payable>> _payableRepositoryMock;
    private readonly Mock<IRepository<Receivable>> _receivableRepositoryMock;
    private readonly Mock<IRepository<TagBinding>> _tagBindingRepositoryMock;
    private readonly Mock<IMasterDataReferenceGuard> _referenceGuardMock;
    private readonly Mock<ILogger<PersonService>> _loggerMock;
    private readonly PersonService _service;

    public PersonServiceTests()
    {
        _personRepositoryMock = new Mock<IRepository<Person>>();
        _transactionRepositoryMock = new Mock<IRepository<Transaction>>();
        _allocationRepositoryMock = new Mock<IRepository<TransactionAllocation>>();
        _payableRepositoryMock = new Mock<IRepository<Payable>>();
        _receivableRepositoryMock = new Mock<IRepository<Receivable>>();
        _tagBindingRepositoryMock = new Mock<IRepository<TagBinding>>();
        _referenceGuardMock = new Mock<IMasterDataReferenceGuard>();
        _loggerMock = new Mock<ILogger<PersonService>>();
        _referenceGuardMock.Setup(g => g.HasPersonReferencesAsync(It.IsAny<long>())).ReturnsAsync(false);

        _service = new PersonService(
            _personRepositoryMock.Object,
            _transactionRepositoryMock.Object,
            _allocationRepositoryMock.Object,
            _payableRepositoryMock.Object,
            _receivableRepositoryMock.Object,
            _tagBindingRepositoryMock.Object,
            _referenceGuardMock.Object,
            Mapper,
            _loggerMock.Object,
            AuditLogServiceMock.Object,
            CreateAdminCurrentUserService(),
            CreateAdminDataPermissionService(),
            UnitOfWorkMock.Object
        );
    }

    [Fact]
    public async Task GetPagedAsync_ShouldReturnPagedResult()
    {
        var persons = new List<Person>
        {
            new() { Id = 1, Name = "张三", CreatedAt = DateTime.UtcNow },
            new() { Id = 2, Name = "李四", CreatedAt = DateTime.UtcNow.AddDays(-1) }
        };

        var queryableMock = persons.AsQueryable().BuildMock();
        _personRepositoryMock.Setup(r => r.GetQueryable()).Returns(queryableMock.Object);

        var request = new PageRequest { Page = 1, PageSize = 10 };
        var result = await _service.GetPagedAsync(request);

        result.Should().NotBeNull();
        result.Total.Should().Be(2);
    }

    [Fact]
    public async Task GetByIdAsync_WithExistingId_ShouldReturnPerson()
    {
        var person = new Person { Id = 1, Name = "张三", IsDeleted = false };
        var queryableMock = new List<Person> { person }.AsQueryable().BuildMock();
        _personRepositoryMock.Setup(r => r.GetQueryable()).Returns(queryableMock.Object);

        var result = await _service.GetByIdAsync(1);

        result.Should().NotBeNull();
        result.Name.Should().Be("张三");
    }

    [Fact]
    public async Task CreateAsync_WithValidRequest_ShouldCreatePerson()
    {
        var request = new CreatePersonRequest { Name = "王五", PersonType = "Employee" };

        Person? createdPerson = null;
        _personRepositoryMock.Setup(r => r.AddAsync(It.IsAny<Person>()))
            .ReturnsAsync((Person p) => { p.Id = 1; createdPerson = p; return p; });

        _personRepositoryMock.Setup(r => r.GetQueryable())
            .Returns(() => new List<Person> { createdPerson! }.AsQueryable().BuildMock().Object);

        var result = await _service.CreateAsync(request);

        _personRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Person>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithInvalidPersonType_ShouldThrowValidationException()
    {
        var request = new CreatePersonRequest { Name = "王五", PersonType = "InvalidType" };

        await Assert.ThrowsAsync<ValidationException>(() => _service.CreateAsync(request));
    }

    [Fact]
    public async Task DeleteAsync_WithExistingPerson_ShouldDeleteSuccessfully()
    {
        var person = new Person { Id = 1, Name = "张三", IsDeleted = false };

        var queryableMock = new List<Person> { person }.AsQueryable().BuildMock();
        _personRepositoryMock.Setup(r => r.GetQueryable()).Returns(queryableMock.Object);

        await _service.DeleteAsync(1);

        _personRepositoryMock.Verify(r => r.Delete(person), Times.Once);
        _personRepositoryMock.Verify(r => r.Update(It.IsAny<Person>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_WithReferencedPerson_ShouldDeactivateInsteadOfDelete()
    {
        var person = new Person { Id = 1, Name = "测试人员", IsActive = true };
        var queryableMock = new List<Person> { person }.AsQueryable().BuildMock();
        _personRepositoryMock.Setup(r => r.GetQueryable()).Returns(queryableMock.Object);
        _referenceGuardMock.Setup(g => g.HasPersonReferencesAsync(1)).ReturnsAsync(true);

        await _service.DeleteAsync(1);

        person.IsActive.Should().BeFalse();
        _personRepositoryMock.Verify(r => r.Update(person), Times.Once);
        _personRepositoryMock.Verify(r => r.Delete(It.IsAny<Person>()), Times.Never);
        AuditLogServiceMock.Verify(a => a.LogAsync("Archive", "Person", 1, It.IsAny<string?>(), It.IsAny<string?>()), Times.Once);
    }

    [Fact]
    public async Task BatchCreateAsync_WithValidItems_ShouldCreateAll()
    {
        var items = new List<CreatePersonRequest>
        {
            new() { Name = "张三", PersonType = "Employee" },
            new() { Name = "李四", PersonType = "Employee" }
        };

        var persons = new List<Person>();
        _personRepositoryMock.Setup(r => r.AddAsync(It.IsAny<Person>()))
            .ReturnsAsync((Person p) =>
            {
                p.Id = persons.Count + 1;
                persons.Add(p);
                return p;
            });
        _personRepositoryMock.Setup(r => r.GetQueryable())
            .Returns(() => persons.AsQueryable().BuildMock().Object);

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
        var items = new List<CreatePersonRequest>();

        var result = await _service.BatchCreateAsync(items);

        result.TotalCount.Should().Be(0);
        result.SuccessCount.Should().Be(0);
        result.FailedCount.Should().Be(0);
    }

    [Fact]
    public async Task GetPagedAsync_WithNameFilter_ShouldReturnMatchingPersons()
    {
        var persons = new List<Person>
        {
            new() { Id = 1, Name = "张三", PersonType = Domain.Enums.PersonType.Employee, CreatedAt = DateTime.UtcNow },
            new() { Id = 2, Name = "张伟", PersonType = Domain.Enums.PersonType.Contractor, CreatedAt = DateTime.UtcNow.AddDays(-1) },
            new() { Id = 3, Name = "李四", PersonType = Domain.Enums.PersonType.Employee, CreatedAt = DateTime.UtcNow.AddDays(-2) }
        };

        var queryableMock = persons.AsQueryable().BuildMock();
        _personRepositoryMock.Setup(r => r.GetQueryable()).Returns(queryableMock.Object);

        var request = new PageRequest { Page = 1, PageSize = 10, Name = "张" };
        var result = await _service.GetPagedAsync(request);

        result.Total.Should().Be(2);
        result.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetPagedAsync_WithPersonTypeFilter_ShouldReturnMatchingPersons()
    {
        var persons = new List<Person>
        {
            new() { Id = 1, Name = "张三", PersonType = Domain.Enums.PersonType.Employee, CreatedAt = DateTime.UtcNow },
            new() { Id = 2, Name = "李四", PersonType = Domain.Enums.PersonType.Contractor, CreatedAt = DateTime.UtcNow.AddDays(-1) },
            new() { Id = 3, Name = "王五", PersonType = Domain.Enums.PersonType.Employee, CreatedAt = DateTime.UtcNow.AddDays(-2) }
        };

        var queryableMock = persons.AsQueryable().BuildMock();
        _personRepositoryMock.Setup(r => r.GetQueryable()).Returns(queryableMock.Object);

        var request = new PageRequest { Page = 1, PageSize = 10, PersonType = "Employee" };
        var result = await _service.GetPagedAsync(request);

        result.Total.Should().Be(2);
        result.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetPagedAsync_WithPhoneFilter_ShouldReturnMatchingPersons()
    {
        var persons = new List<Person>
        {
            new() { Id = 1, Name = "张三", Phone = "13800138000", CreatedAt = DateTime.UtcNow },
            new() { Id = 2, Name = "李四", Phone = "13900139000", CreatedAt = DateTime.UtcNow.AddDays(-1) },
            new() { Id = 3, Name = "王五", Phone = null, CreatedAt = DateTime.UtcNow.AddDays(-2) }
        };

        var queryableMock = persons.AsQueryable().BuildMock();
        _personRepositoryMock.Setup(r => r.GetQueryable()).Returns(queryableMock.Object);

        var request = new PageRequest { Page = 1, PageSize = 10, Phone = "138" };
        var result = await _service.GetPagedAsync(request);

        result.Total.Should().Be(1);
        result.Items.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetPagedAsync_WithMultipleFilters_ShouldApplyAllFilters()
    {
        var persons = new List<Person>
        {
            new() { Id = 1, Name = "张三", PersonType = Domain.Enums.PersonType.Employee, Phone = "13800138000", CreatedAt = DateTime.UtcNow },
            new() { Id = 2, Name = "张伟", PersonType = Domain.Enums.PersonType.Contractor, Phone = "13900139000", CreatedAt = DateTime.UtcNow.AddDays(-1) },
            new() { Id = 3, Name = "李四", PersonType = Domain.Enums.PersonType.Employee, Phone = "13700137000", CreatedAt = DateTime.UtcNow.AddDays(-2) }
        };

        var queryableMock = persons.AsQueryable().BuildMock();
        _personRepositoryMock.Setup(r => r.GetQueryable()).Returns(queryableMock.Object);

        var request = new PageRequest { Page = 1, PageSize = 10, Name = "张", PersonType = "Employee" };
        var result = await _service.GetPagedAsync(request);

        result.Total.Should().Be(1);
        result.Items.Should().HaveCount(1);
        result.Items[0].Name.Should().Be("张三");
    }

    #region DeleteAsync Extended Tests

    [Fact]
    public async Task DeleteAsync_WithReferences_ActivePerson_ShouldDeactivate()
    {
        // Arrange: 有引用 + 活跃 → 改为停用
        var person = new Person { Id = 10, Name = "有引用活跃人员", IsActive = true };
        var queryableMock = new List<Person> { person }.AsQueryable().BuildMock();
        _personRepositoryMock.Setup(r => r.GetQueryable()).Returns(queryableMock.Object);
        _referenceGuardMock.Setup(g => g.HasPersonReferencesAsync(10)).ReturnsAsync(true);

        // Act
        await _service.DeleteAsync(10);

        // Assert
        person.IsActive.Should().BeFalse();
        _personRepositoryMock.Verify(r => r.Update(person), Times.Once);
        UnitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        AuditLogServiceMock.Verify(a => a.LogAsync("Archive", "Person", 10, It.IsAny<string?>(), It.IsAny<string?>()), Times.Once);
        _personRepositoryMock.Verify(r => r.Delete(It.IsAny<Person>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_WithReferences_InactivePerson_ShouldSkip()
    {
        // Arrange: 有引用 + 已停用 → 跳过，不做任何修改
        var person = new Person { Id = 11, Name = "有引用已停用人员", IsActive = false };
        var queryableMock = new List<Person> { person }.AsQueryable().BuildMock();
        _personRepositoryMock.Setup(r => r.GetQueryable()).Returns(queryableMock.Object);
        _referenceGuardMock.Setup(g => g.HasPersonReferencesAsync(11)).ReturnsAsync(true);

        // Act
        await _service.DeleteAsync(11);

        // Assert: 不应调用 Update、Delete、SaveChanges、AuditLog
        _personRepositoryMock.Verify(r => r.Update(It.IsAny<Person>()), Times.Never);
        _personRepositoryMock.Verify(r => r.Delete(It.IsAny<Person>()), Times.Never);
        UnitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        AuditLogServiceMock.Verify(a => a.LogAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<long>(), It.IsAny<string?>(), It.IsAny<string?>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_NoReferences_ShouldDelete()
    {
        // Arrange: 无引用 → 正常物理删除
        var person = new Person { Id = 12, Name = "无引用人员", IsActive = true };
        var queryableMock = new List<Person> { person }.AsQueryable().BuildMock();
        _personRepositoryMock.Setup(r => r.GetQueryable()).Returns(queryableMock.Object);
        _referenceGuardMock.Setup(g => g.HasPersonReferencesAsync(12)).ReturnsAsync(false);

        // Act
        await _service.DeleteAsync(12);

        // Assert
        _personRepositoryMock.Verify(r => r.Delete(person), Times.Once);
        UnitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        AuditLogServiceMock.Verify(a => a.LogAsync("Delete", "Person", 12, It.IsAny<string?>(), null), Times.Once);
        _personRepositoryMock.Verify(r => r.Update(It.IsAny<Person>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_NotFound_ShouldThrowNotFoundException()
    {
        // Arrange: 人员不存在 → 抛出 NotFoundException
        var queryableMock = new List<Person>().AsQueryable().BuildMock();
        _personRepositoryMock.Setup(r => r.GetQueryable()).Returns(queryableMock.Object);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<NotFoundException>(() => _service.DeleteAsync(999));
        ex.Message.Should().Contain("人员不存在");

        // 不应有任何删除/更新操作
        _personRepositoryMock.Verify(r => r.Delete(It.IsAny<Person>()), Times.Never);
        _personRepositoryMock.Verify(r => r.Update(It.IsAny<Person>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_UnexpectedException_ShouldRethrow()
    {
        var person = new Person { Id = 13, Name = "异常测试人员", IsActive = true };
        var queryableMock = new List<Person> { person }.AsQueryable().BuildMock();
        _personRepositoryMock.Setup(r => r.GetQueryable()).Returns(queryableMock.Object);
        _referenceGuardMock.Setup(g => g.HasPersonReferencesAsync(13))
            .ThrowsAsync(new InvalidOperationException("数据库连接失败"));

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _service.DeleteAsync(13));
        ex.Message.Should().Be("数据库连接失败");

        _personRepositoryMock.Verify(r => r.Delete(It.IsAny<Person>()), Times.Never);
        _personRepositoryMock.Verify(r => r.Update(It.IsAny<Person>()), Times.Never);
    }

    #endregion

    #region GetStatisticsAsync Tests

    [Fact]
    public async Task GetStatisticsAsync_ShouldReturnCorrectStatistics()
    {
        // Arrange
        var persons = new List<Person>
        {
            new() { Id = 1, Name = "张三", IsActive = true, IsDeleted = false, CreatedAt = DateTime.UtcNow },
            new() { Id = 2, Name = "李四", IsActive = true, IsDeleted = false, CreatedAt = DateTime.UtcNow },
            new() { Id = 3, Name = "王五", IsActive = false, IsDeleted = false, CreatedAt = DateTime.UtcNow.AddMonths(-1) }
        };

        var queryableMock = persons.AsQueryable().BuildMock();
        _personRepositoryMock.Setup(r => r.GetQueryable()).Returns(queryableMock.Object);

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
        var persons = new List<Person>();
        var queryableMock = persons.AsQueryable().BuildMock();
        _personRepositoryMock.Setup(r => r.GetQueryable()).Returns(queryableMock.Object);

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
    public async Task GetFinanceSummaryAsync_WithExistingPerson_ShouldReturnCorrectSummary()
    {
        // Arrange
        var person = new Person { Id = 1, Name = "张三", IsDeleted = false };
        _personRepositoryMock.Setup(r => r.GetQueryable())
            .Returns(new List<Person> { person }.AsQueryable().BuildMock().Object);

        var transactions = new List<Transaction>
        {
            new() { Id = 1, PersonId = 1, ProjectId = 10, Amount = 3000, TransactionType = TransactionType.Expense, IsAllocated = false },
            new() { Id = 2, PersonId = 1, ProjectId = 20, Amount = 2000, TransactionType = TransactionType.Expense, IsAllocated = false },
        };
        _transactionRepositoryMock.Setup(r => r.GetQueryable())
            .Returns(transactions.AsQueryable().BuildMock().Object);

        var allocations = new List<TransactionAllocation>
        {
            new() { Id = 1, PersonId = 1, Amount = 1500, Transaction = new Transaction { ProjectId = 10, TransactionType = TransactionType.Expense } },
        };
        _allocationRepositoryMock.Setup(r => r.GetQueryable())
            .Returns(allocations.AsQueryable().BuildMock().Object);

        var payables = new List<Payable>
        {
            new() { Id = 1, PersonId = 1, TotalAmount = 1000, PaidAmount = 200, RemainingAmount = 800, Status = PayableStatus.Partial },
        };
        _payableRepositoryMock.Setup(r => r.GetQueryable())
            .Returns(payables.AsQueryable().BuildMock().Object);

        _receivableRepositoryMock.Setup(r => r.GetQueryable())
            .Returns(new List<Receivable>().AsQueryable().BuildMock().Object);

        // Act
        var result = await _service.GetFinanceSummaryAsync(1);

        // Assert
        result.Should().NotBeNull();
        result.DirectCost.Should().Be(5000); // 3000 + 2000
        result.AllocatedCost.Should().Be(1500);
        result.TotalCost.Should().Be(6500); // 5000 + 1500
        result.TotalPayable.Should().Be(1000);
        result.TotalPaid.Should().Be(200);
        result.PayableRemaining.Should().Be(800);
        result.TransactionCount.Should().Be(3); // 2 direct + 1 allocation
        result.ProjectCount.Should().Be(2); // ProjectId 10 和 20
    }

    [Fact]
    public async Task GetFinanceSummaryAsync_WithNonExistingPerson_ShouldThrowNotFoundException()
    {
        _personRepositoryMock.Setup(r => r.GetQueryable())
            .Returns(new List<Person>().AsQueryable().BuildMock().Object);

        await Assert.ThrowsAsync<NotFoundException>(() => _service.GetFinanceSummaryAsync(999));
    }

    [Fact]
    public async Task GetFinanceSummaryAsync_WithBothReceivablesAndPayables_ShouldReturnCombinedSummary()
    {
        // Arrange
        var person = new Person { Id = 1, Name = "张三", IsDeleted = false };
        _personRepositoryMock.Setup(r => r.GetQueryable())
            .Returns(new List<Person> { person }.AsQueryable().BuildMock().Object);

        var today = DateTime.UtcNow.Date;

        // Cost data
        var transactions = new List<Transaction>
        {
            new() { Id = 1, PersonId = 1, ProjectId = 10, Amount = 2000, TransactionType = TransactionType.Expense, IsAllocated = false },
        };
        _transactionRepositoryMock.Setup(r => r.GetQueryable())
            .Returns(transactions.AsQueryable().BuildMock().Object);
        _allocationRepositoryMock.Setup(r => r.GetQueryable())
            .Returns(new List<TransactionAllocation>().AsQueryable().BuildMock().Object);

        // Receivable data
        var receivables = new List<Receivable>
        {
            new() { Id = 1, PersonId = 1, ProjectId = 10, TotalAmount = 5000, ReceivedAmount = 1000, RemainingAmount = 4000, Status = ReceivableStatus.Partial, DueDate = today.AddDays(-3) },
            new() { Id = 2, PersonId = 1, ProjectId = 20, TotalAmount = 3000, ReceivedAmount = 3000, RemainingAmount = 0, Status = ReceivableStatus.Settled, DueDate = today.AddDays(-1) },
        };
        _receivableRepositoryMock.Setup(r => r.GetQueryable())
            .Returns(receivables.AsQueryable().BuildMock().Object);

        // Payable data
        var payables = new List<Payable>
        {
            new() { Id = 1, PersonId = 1, ProjectId = 20, TotalAmount = 4000, PaidAmount = 500, RemainingAmount = 3500, Status = PayableStatus.Partial, DueDate = today.AddDays(-5) },
            new() { Id = 2, PersonId = 1, ProjectId = 30, TotalAmount = 2000, PaidAmount = 0, RemainingAmount = 2000, Status = PayableStatus.Pending, DueDate = today.AddDays(15) },
        };
        _payableRepositoryMock.Setup(r => r.GetQueryable())
            .Returns(payables.AsQueryable().BuildMock().Object);

        // Act
        var result = await _service.GetFinanceSummaryAsync(1);

        // Assert — Cost dimension
        result.DirectCost.Should().Be(2000);
        result.TotalCost.Should().Be(2000);

        // Assert — Receivable dimension
        result.TotalReceivable.Should().Be(8000);
        result.TotalReceived.Should().Be(4000);
        result.ReceivableRemaining.Should().Be(4000);
        result.ReceivableOverdueCount.Should().Be(1); // ID=1 逾期且未结清
        result.ReceivableOverdueAmount.Should().Be(4000);

        // Assert — Payable dimension
        result.TotalPayable.Should().Be(6000);
        result.TotalPaid.Should().Be(500);
        result.PayableRemaining.Should().Be(5500);
        result.PayableOverdueCount.Should().Be(1); // ID=1 逾期且未结清
        result.PayableOverdueAmount.Should().Be(3500);

        // Assert — ProjectCount is union across all sources: {10, 20, 30} = 3
        result.ProjectCount.Should().Be(3);
    }

    [Fact]
    public async Task GetFinanceSummaryAsync_WithReceivablesOnly_ShouldReturnReceivableDataAndZeroPayable()
    {
        // Arrange
        var person = new Person { Id = 1, Name = "张三", IsDeleted = false };
        _personRepositoryMock.Setup(r => r.GetQueryable())
            .Returns(new List<Person> { person }.AsQueryable().BuildMock().Object);

        _transactionRepositoryMock.Setup(r => r.GetQueryable())
            .Returns(new List<Transaction>().AsQueryable().BuildMock().Object);
        _allocationRepositoryMock.Setup(r => r.GetQueryable())
            .Returns(new List<TransactionAllocation>().AsQueryable().BuildMock().Object);

        var today = DateTime.UtcNow.Date;
        var receivables = new List<Receivable>
        {
            new() { Id = 1, PersonId = 1, ProjectId = 10, TotalAmount = 6000, ReceivedAmount = 2000, RemainingAmount = 4000, Status = ReceivableStatus.Partial, DueDate = today.AddDays(-2) },
        };
        _receivableRepositoryMock.Setup(r => r.GetQueryable())
            .Returns(receivables.AsQueryable().BuildMock().Object);

        _payableRepositoryMock.Setup(r => r.GetQueryable())
            .Returns(new List<Payable>().AsQueryable().BuildMock().Object);

        // Act
        var result = await _service.GetFinanceSummaryAsync(1);

        // Assert — Receivable should have data
        result.TotalReceivable.Should().Be(6000);
        result.TotalReceived.Should().Be(2000);
        result.ReceivableRemaining.Should().Be(4000);
        result.ReceivableOverdueCount.Should().Be(1);
        result.ReceivableOverdueAmount.Should().Be(4000);

        // Assert — Payable should be zeros
        result.TotalPayable.Should().Be(0);
        result.TotalPaid.Should().Be(0);
        result.PayableRemaining.Should().Be(0);
        result.PayableOverdueCount.Should().Be(0);

        // Assert — ProjectCount from receivables
        result.ProjectCount.Should().Be(1);
    }

    #endregion
}
