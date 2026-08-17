using FluentAssertions;
using FinanceApp.Application.Common;
using FinanceApp.Application.Modules.Identity.Interfaces;
using FinanceApp.Application.Modules.MasterData.Services;
using FinanceApp.Application.Tests.Helpers;
using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Enums;
using FinanceApp.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using MockQueryable.Moq;
using Moq;

namespace FinanceApp.Application.Tests.Services;

public class TagServiceTests : TestBase
{
    private readonly Mock<IRepository<Tag>> _tagRepositoryMock;
    private readonly Mock<IRepository<TagBinding>> _tagBindingRepositoryMock;
    private readonly Mock<IRepository<Transaction>> _transactionRepositoryMock;
    private readonly Mock<IRepository<Project>> _projectRepositoryMock;
    private readonly Mock<IRepository<Person>> _personRepositoryMock;
    private readonly Mock<IRepository<Customer>> _customerRepositoryMock;
    private readonly Mock<IRepository<Supplier>> _supplierRepositoryMock;
    private readonly Mock<IRepository<Receivable>> _receivableRepositoryMock;
    private readonly Mock<IRepository<Payable>> _payableRepositoryMock;
    private readonly Mock<ILogger<TagService>> _loggerMock;
    private readonly Mock<IAuditLogService> _auditLogServiceMock;
    private readonly Mock<ICurrentUserService> _currentUserServiceMock;
    private readonly Mock<IDataPermissionService> _permissionServiceMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly IMemoryCache _cache;
    private readonly TagService _service;

    public TagServiceTests()
    {
        _tagRepositoryMock = new Mock<IRepository<Tag>>();
        _tagBindingRepositoryMock = new Mock<IRepository<TagBinding>>();
        _transactionRepositoryMock = new Mock<IRepository<Transaction>>();
        _projectRepositoryMock = new Mock<IRepository<Project>>();
        _personRepositoryMock = new Mock<IRepository<Person>>();
        _customerRepositoryMock = new Mock<IRepository<Customer>>();
        _supplierRepositoryMock = new Mock<IRepository<Supplier>>();
        _receivableRepositoryMock = new Mock<IRepository<Receivable>>();
        _payableRepositoryMock = new Mock<IRepository<Payable>>();
        _loggerMock = new Mock<ILogger<TagService>>();
        _auditLogServiceMock = new Mock<IAuditLogService>();
        _currentUserServiceMock = new Mock<ICurrentUserService>();
        _permissionServiceMock = new Mock<IDataPermissionService>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _cache = new MemoryCache(new MemoryCacheOptions());

        // Admin 用户设置
        _currentUserServiceMock.Setup(x => x.UserId).Returns(1L);
        _currentUserServiceMock.Setup(x => x.Username).Returns("admin");
        _currentUserServiceMock.Setup(x => x.Role).Returns(UserRole.Admin);
        _currentUserServiceMock.Setup(x => x.IsAdmin).Returns(true);

        // Admin 权限服务：不过滤任何数据
        _permissionServiceMock.Setup(x => x.ApplyPermissionFilter(It.IsAny<IQueryable<Transaction>>()))
            .Returns((IQueryable<Transaction> query) => query);
        _permissionServiceMock.Setup(x => x.ApplyPermissionFilter(It.IsAny<IQueryable<Project>>()))
            .Returns((IQueryable<Project> query) => query);
        _permissionServiceMock.Setup(x => x.ApplyPermissionFilter(It.IsAny<IQueryable<Person>>()))
            .Returns((IQueryable<Person> query) => query);
        _permissionServiceMock.Setup(x => x.ApplyPermissionFilter(It.IsAny<IQueryable<Customer>>()))
            .Returns((IQueryable<Customer> query) => query);
        _permissionServiceMock.Setup(x => x.ApplyPermissionFilter(It.IsAny<IQueryable<Supplier>>()))
            .Returns((IQueryable<Supplier> query) => query);
        _permissionServiceMock.Setup(x => x.ApplyPermissionFilter(It.IsAny<IQueryable<Receivable>>()))
            .Returns((IQueryable<Receivable> query) => query);
        _permissionServiceMock.Setup(x => x.ApplyPermissionFilter(It.IsAny<IQueryable<Payable>>()))
            .Returns((IQueryable<Payable> query) => query);

        _service = new TagService(
            _tagRepositoryMock.Object,
            _tagBindingRepositoryMock.Object,
            _loggerMock.Object,
            _auditLogServiceMock.Object,
            _currentUserServiceMock.Object,
            _permissionServiceMock.Object,
            _unitOfWorkMock.Object,
            _cache,
            _transactionRepositoryMock.Object,
            _projectRepositoryMock.Object,
            _personRepositoryMock.Object,
            _customerRepositoryMock.Object,
            _supplierRepositoryMock.Object,
            _receivableRepositoryMock.Object,
            _payableRepositoryMock.Object);
    }

    [Fact]
    public async Task GetBindingsAsync_WithDeletedTag_ShouldReturnBindingWithDeletedFlag()
    {
        // Arrange
        var activeTag = new Tag { Id = 1, Name = "活跃标签", Color = "#FF0000", IsDeleted = false };
        var deletedTag = new Tag { Id = 2, Name = "已删除标签", Color = "#00FF00", IsDeleted = true };

        var bindings = new List<TagBinding>
        {
            new() { Id = 1, TagId = 1, Tag = activeTag, OwnerType = TagScope.Transaction, OwnerId = 100, IsDeleted = false },
            new() { Id = 2, TagId = 2, Tag = deletedTag, OwnerType = TagScope.Transaction, OwnerId = 100, IsDeleted = false }
        };

        var bindingsQueryableMock = bindings.AsQueryable().BuildMock();
        _tagBindingRepositoryMock.Setup(r => r.GetQueryable()).Returns(bindingsQueryableMock.Object);

        var transactions = new List<Transaction>
        {
            new() { Id = 100, CreatedBy = 1 }
        };
        var transactionsQueryableMock = transactions.AsQueryable().BuildMock();
        _transactionRepositoryMock.Setup(r => r.GetQueryable()).Returns(transactionsQueryableMock.Object);

        // Act
        var result = await _service.GetBindingsAsync("transaction", 100);

        // Assert
        result.Should().HaveCount(2);

        var activeBinding = result.First(b => b.TagId == 1);
        activeBinding.TagName.Should().Be("活跃标签");
        activeBinding.TagColor.Should().Be("#FF0000");
        activeBinding.TagIsDeleted.Should().BeFalse();

        var deletedBinding = result.First(b => b.TagId == 2);
        deletedBinding.TagName.Should().Be("已删除标签");
        deletedBinding.TagColor.Should().Be("#00FF00");
        deletedBinding.TagIsDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task GetBindingsAsync_WithNullTag_ShouldLogWarningAndReturnFallbackName()
    {
        // Arrange
        var bindings = new List<TagBinding>
        {
            new() { Id = 1, TagId = 999, Tag = null!, OwnerType = TagScope.Transaction, OwnerId = 100, IsDeleted = false }
        };

        var bindingsQueryableMock = bindings.AsQueryable().BuildMock();
        _tagBindingRepositoryMock.Setup(r => r.GetQueryable()).Returns(bindingsQueryableMock.Object);

        var transactions = new List<Transaction>
        {
            new() { Id = 100, CreatedBy = 1 }
        };
        var transactionsQueryableMock = transactions.AsQueryable().BuildMock();
        _transactionRepositoryMock.Setup(r => r.GetQueryable()).Returns(transactionsQueryableMock.Object);

        // Act
        var result = await _service.GetBindingsAsync("transaction", 100);

        // Assert
        result.Should().HaveCount(1);
        result[0].TagId.Should().Be(999);
        result[0].TagName.Should().Be("标签#999");
        result[0].TagIsDeleted.Should().BeTrue();

        // 验证日志记录
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("TagBinding 1 引用了不存在的 Tag 999")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}