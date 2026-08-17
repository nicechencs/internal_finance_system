using FluentAssertions;
using FinanceApp.Application.Common;
using FinanceApp.Application.Modules.Identity.DTOs;
using FinanceApp.Application.Modules.Identity.Services;
using FinanceApp.Application.Tests.Helpers;
using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;

namespace FinanceApp.Application.Tests.Services;

public class AuditLogServiceTests : TestBase
{
    private readonly Mock<IRepository<AuditLog>> _auditLogRepositoryMock;
    private readonly Mock<IRepository<User>> _userRepositoryMock;
    private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
    private readonly AuditLogService _service;

    public AuditLogServiceTests()
    {
        _auditLogRepositoryMock = new Mock<IRepository<AuditLog>>();
        _userRepositoryMock = new Mock<IRepository<User>>();
        _httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        var loggerMock = new Mock<ILogger<AuditLogService>>();
        _service = new AuditLogService(
            _auditLogRepositoryMock.Object,
            _userRepositoryMock.Object,
            _httpContextAccessorMock.Object,
            UnitOfWorkMock.Object,
            loggerMock.Object);
    }

    [Fact]
    public async Task GetPagedAsync_ShouldReturnPagedResult()
    {
        var auditLogs = new List<AuditLog>
        {
            new()
            {
                Id = 1,
                EntityType = "Customer",
                EntityId = 100,
                Action = "Create",
                OldValue = null,
                NewValue = "{\"Name\":\"客户1\"}",
                IpAddress = "192.168.1.1",
                CreatedAt = DateTime.UtcNow,
                User = new User { Id = 1, Username = "admin" }
            },
            new()
            {
                Id = 2,
                EntityType = "Transaction",
                EntityId = 200,
                Action = "Update",
                OldValue = "{\"Amount\":100}",
                NewValue = "{\"Amount\":200}",
                IpAddress = "192.168.1.2",
                CreatedAt = DateTime.UtcNow.AddMinutes(-10),
                User = new User { Id = 2, Username = "user1" }
            }
        };

        var queryableMock = auditLogs.AsQueryable().BuildMock();
        _auditLogRepositoryMock.Setup(r => r.GetQueryable()).Returns(queryableMock.Object);

        var request = new AuditLogPageRequest { Page = 1, PageSize = 10 };
        var result = await _service.GetPagedAsync(request);

        result.Should().NotBeNull();
        result.Total.Should().Be(2);
        result.Items.Should().HaveCount(2);
        result.Items[0].EntityType.Should().Be("Customer");
        result.Items[0].OperatorName.Should().Be("admin");
        result.Items[1].EntityType.Should().Be("Transaction");
        result.Items[1].OperatorName.Should().Be("user1");
    }

    [Fact]
    public async Task GetPagedAsync_WithPagination_ShouldReturnCorrectPage()
    {
        var auditLogs = Enumerable.Range(1, 25).Select(i => new AuditLog
        {
            Id = i,
            EntityType = "Test",
            EntityId = i,
            Action = "Create",
            CreatedAt = DateTime.UtcNow.AddMinutes(-i),
            User = new User { Id = 1, Username = "admin" }
        }).ToList();

        var queryableMock = auditLogs.AsQueryable().BuildMock();
        _auditLogRepositoryMock.Setup(r => r.GetQueryable()).Returns(queryableMock.Object);

        var request = new AuditLogPageRequest { Page = 2, PageSize = 10 };
        var result = await _service.GetPagedAsync(request);

        result.Should().NotBeNull();
        result.Total.Should().Be(25);
        result.Items.Should().HaveCount(10);
        result.Page.Should().Be(2);
        result.PageSize.Should().Be(10);
    }

    [Fact]
    public async Task GetPagedAsync_WithSystemUser_ShouldShowSystemAsOperator()
    {
        var auditLogs = new List<AuditLog>
        {
            new()
            {
                Id = 1,
                EntityType = "Account",
                EntityId = 10,
                Action = "Create",
                CreatedAt = DateTime.UtcNow,
                User = null
            }
        };

        var queryableMock = auditLogs.AsQueryable().BuildMock();
        _auditLogRepositoryMock.Setup(r => r.GetQueryable()).Returns(queryableMock.Object);

        var request = new AuditLogPageRequest { Page = 1, PageSize = 10 };
        var result = await _service.GetPagedAsync(request);

        result.Should().NotBeNull();
        result.Items.Should().HaveCount(1);
        result.Items[0].OperatorName.Should().Be("System");
    }

    [Fact]
    public async Task LogAsync_WithAuthenticatedUser_ShouldCreateAuditLog()
    {
        var userId = 123L;
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString())
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);

        var httpContext = new DefaultHttpContext
        {
            User = claimsPrincipal,
            Connection = { RemoteIpAddress = System.Net.IPAddress.Parse("192.168.1.100") }
        };
        httpContext.Request.Headers["User-Agent"] = "Mozilla/5.0";

        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);

        AuditLog? capturedLog = null;
        _auditLogRepositoryMock.Setup(r => r.AddAsync(It.IsAny<AuditLog>()))
            .Callback<AuditLog>(log => capturedLog = log)
            .ReturnsAsync((AuditLog log) => log);

        await _service.LogAsync("Create", "Customer", 100, null, "{\"Name\":\"新客户\"}");

        capturedLog.Should().NotBeNull();
        capturedLog!.UserId.Should().Be(userId);
        capturedLog.Action.Should().Be("Create");
        capturedLog.EntityType.Should().Be("Customer");
        capturedLog.EntityId.Should().Be(100);
        capturedLog.OldValue.Should().BeNull();
        capturedLog.NewValue.Should().Be("{\"Name\":\"新客户\"}");
        capturedLog.IpAddress.Should().Be("192.168.1.100");
        capturedLog.UserAgent.Should().Be("Mozilla/5.0");
        _auditLogRepositoryMock.Verify(r => r.AddAsync(It.IsAny<AuditLog>()), Times.Once);
    }

    [Fact]
    public async Task LogAsync_WithUnauthenticatedUser_ShouldCreateAuditLogWithNullUserId()
    {
        var httpContext = new DefaultHttpContext
        {
            Connection = { RemoteIpAddress = System.Net.IPAddress.Parse("10.0.0.1") }
        };

        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);

        AuditLog? capturedLog = null;
        _auditLogRepositoryMock.Setup(r => r.AddAsync(It.IsAny<AuditLog>()))
            .Callback<AuditLog>(log => capturedLog = log)
            .ReturnsAsync((AuditLog log) => log);

        await _service.LogAsync("Delete", "Transaction", 200, "{\"Amount\":100}", null);

        capturedLog.Should().NotBeNull();
        capturedLog!.UserId.Should().BeNull();
        capturedLog.Action.Should().Be("Delete");
        capturedLog.EntityType.Should().Be("Transaction");
        capturedLog.EntityId.Should().Be(200);
        capturedLog.IpAddress.Should().Be("10.0.0.1");
    }

    [Fact]
    public async Task LogAsync_WithoutHttpContext_ShouldCreateAuditLogWithNullIpAddress()
    {
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns((HttpContext?)null);

        AuditLog? capturedLog = null;
        _auditLogRepositoryMock.Setup(r => r.AddAsync(It.IsAny<AuditLog>()))
            .Callback<AuditLog>(log => capturedLog = log)
            .ReturnsAsync((AuditLog log) => log);

        await _service.LogAsync("Update", "Project", 50, "{\"Name\":\"旧名称\"}", "{\"Name\":\"新名称\"}");

        capturedLog.Should().NotBeNull();
        capturedLog!.UserId.Should().BeNull();
        capturedLog.IpAddress.Should().BeNull();
        capturedLog.UserAgent.Should().BeNull();
        capturedLog.Action.Should().Be("Update");
        capturedLog.EntityType.Should().Be("Project");
        capturedLog.EntityId.Should().Be(50);
    }

    [Fact]
    public async Task LogAsync_WithDifferentActions_ShouldLogCorrectly()
    {
        var httpContext = new DefaultHttpContext();
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);

        var actions = new[] { "Create", "Update", "Delete", "Import", "Export" };
        var capturedLogs = new List<AuditLog>();

        _auditLogRepositoryMock.Setup(r => r.AddAsync(It.IsAny<AuditLog>()))
            .Callback<AuditLog>(log => capturedLogs.Add(log))
            .ReturnsAsync((AuditLog log) => log);

        foreach (var action in actions)
        {
            await _service.LogAsync(action, "TestEntity", 1, null, null);
        }

        capturedLogs.Should().HaveCount(5);
        capturedLogs.Select(l => l.Action).Should().BeEquivalentTo(actions);
    }

    [Fact]
    public async Task LogAsync_WithDifferentEntityTypes_ShouldLogCorrectly()
    {
        var httpContext = new DefaultHttpContext();
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);

        var entityTypes = new[] { "Customer", "Supplier", "Transaction", "Account", "Project" };
        var capturedLogs = new List<AuditLog>();

        _auditLogRepositoryMock.Setup(r => r.AddAsync(It.IsAny<AuditLog>()))
            .Callback<AuditLog>(log => capturedLogs.Add(log))
            .ReturnsAsync((AuditLog log) => log);

        for (int i = 0; i < entityTypes.Length; i++)
        {
            await _service.LogAsync("Create", entityTypes[i], i + 1, null, null);
        }

        capturedLogs.Should().HaveCount(5);
        capturedLogs.Select(l => l.EntityType).Should().BeEquivalentTo(entityTypes);
    }

    [Fact]
    public async Task GetPagedAsync_ShouldOrderByCreatedAtDescending()
    {
        var now = DateTime.UtcNow;
        var auditLogs = new List<AuditLog>
        {
            new() { Id = 1, EntityType = "Test", Action = "Create", CreatedAt = now.AddHours(-2), User = new User { Username = "user1" } },
            new() { Id = 2, EntityType = "Test", Action = "Create", CreatedAt = now, User = new User { Username = "user2" } },
            new() { Id = 3, EntityType = "Test", Action = "Create", CreatedAt = now.AddHours(-1), User = new User { Username = "user3" } }
        };

        var queryableMock = auditLogs.AsQueryable().BuildMock();
        _auditLogRepositoryMock.Setup(r => r.GetQueryable()).Returns(queryableMock.Object);

        var request = new AuditLogPageRequest { Page = 1, PageSize = 10 };
        var result = await _service.GetPagedAsync(request);

        result.Items.Should().HaveCount(3);
        result.Items[0].Id.Should().Be(2);
        result.Items[1].Id.Should().Be(3);
        result.Items[2].Id.Should().Be(1);
    }

    [Fact]
    public async Task GetPagedAsync_WithUsernameFilter_ShouldReturnMatchingRecords()
    {
        var auditLogs = new List<AuditLog>
        {
            new() { Id = 1, EntityType = "Test", Action = "Create", CreatedAt = DateTime.UtcNow, User = new User { Username = "admin" } },
            new() { Id = 2, EntityType = "Test", Action = "Create", CreatedAt = DateTime.UtcNow.AddMinutes(-1), User = new User { Username = "accountant" } },
            new() { Id = 3, EntityType = "Test", Action = "Create", CreatedAt = DateTime.UtcNow.AddMinutes(-2), User = null }
        };

        var queryableMock = auditLogs.AsQueryable().BuildMock();
        _auditLogRepositoryMock.Setup(r => r.GetQueryable()).Returns(queryableMock.Object);

        var request = new AuditLogPageRequest { Page = 1, PageSize = 10, Username = "count" };
        var result = await _service.GetPagedAsync(request);

        result.Total.Should().Be(1);
        result.Items.Should().HaveCount(1);
        result.Items[0].OperatorName.Should().Be("accountant");
    }

    [Fact]
    public async Task GetPagedAsync_WithTooLongUsername_ShouldThrowValidationException()
    {
        // Arrange
        var longUsername = new string('a', 51); // 51 个字符，超过限制
        var request = new AuditLogPageRequest { Page = 1, PageSize = 10, Username = longUsername };

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() => _service.GetPagedAsync(request));
    }

    [Fact]
    public async Task GetPagedAsync_WithExactly50CharUsername_ShouldSucceed()
    {
        // Arrange
        var username50 = new string('a', 50); // 正好 50 个字符
        var auditLogs = new List<AuditLog>
        {
            new() { Id = 1, EntityType = "Test", Action = "Create", CreatedAt = DateTime.UtcNow, User = new User { Username = username50 } }
        };

        var queryableMock = auditLogs.AsQueryable().BuildMock();
        _auditLogRepositoryMock.Setup(r => r.GetQueryable()).Returns(queryableMock.Object);

        var request = new AuditLogPageRequest { Page = 1, PageSize = 10, Username = username50 };

        // Act
        var result = await _service.GetPagedAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Total.Should().Be(1);
    }

    [Fact]
    public async Task LogAsync_WithInvalidUserIdClaim_ShouldCreateAuditLogWithNullUserId()
    {
        var claims = new List<Claim>
        {
            new Claim("userId", "invalid_number")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);

        var httpContext = new DefaultHttpContext
        {
            User = claimsPrincipal
        };

        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);

        AuditLog? capturedLog = null;
        _auditLogRepositoryMock.Setup(r => r.AddAsync(It.IsAny<AuditLog>()))
            .Callback<AuditLog>(log => capturedLog = log)
            .ReturnsAsync((AuditLog log) => log);

        await _service.LogAsync("Create", "Test", 1, null, null);

        capturedLog.Should().NotBeNull();
        capturedLog!.UserId.Should().BeNull();
    }

    [Fact]
    public async Task GetPagedAsync_WithEmptyResult_ShouldReturnEmptyList()
    {
        var auditLogs = new List<AuditLog>();
        var queryableMock = auditLogs.AsQueryable().BuildMock();
        _auditLogRepositoryMock.Setup(r => r.GetQueryable()).Returns(queryableMock.Object);

        var request = new AuditLogPageRequest { Page = 1, PageSize = 10 };
        var result = await _service.GetPagedAsync(request);

        result.Should().NotBeNull();
        result.Total.Should().Be(0);
        result.Items.Should().BeEmpty();
    }

    #region GetByIdAsync 测试

    [Fact]
    public async Task GetByIdAsync_ExistingId_ShouldReturnDto()
    {
        var auditLog = new AuditLog
        {
            Id = 42,
            EntityType = "Transaction",
            EntityId = 100,
            Action = "Update",
            OldValue = "{\"Amount\":500}",
            NewValue = "{\"Amount\":800}",
            IpAddress = "10.0.0.1",
            CreatedAt = DateTime.UtcNow,
            User = new User { Id = 1, Username = "admin" }
        };

        var queryableMock = new List<AuditLog> { auditLog }.AsQueryable().BuildMock();
        _auditLogRepositoryMock.Setup(r => r.GetQueryable()).Returns(queryableMock.Object);

        var result = await _service.GetByIdAsync(42);

        result.Should().NotBeNull();
        result!.Id.Should().Be(42);
        result.EntityType.Should().Be("Transaction");
        result.EntityId.Should().Be(100);
        result.Action.Should().Be("Update");
        result.OldValue.Should().Be("{\"Amount\":500}");
        result.NewValue.Should().Be("{\"Amount\":800}");
        result.OperatorName.Should().Be("admin");
        result.IpAddress.Should().Be("10.0.0.1");
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingId_ShouldThrowNotFoundException()
    {
        var queryableMock = new List<AuditLog>().AsQueryable().BuildMock();
        _auditLogRepositoryMock.Setup(r => r.GetQueryable()).Returns(queryableMock.Object);

        Func<Task> act = async () => await _service.GetByIdAsync(999);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task GetByIdAsync_NullUser_ShouldReturnSystemAsOperator()
    {
        var auditLog = new AuditLog
        {
            Id = 10,
            EntityType = "Account",
            EntityId = 5,
            Action = "Create",
            CreatedAt = DateTime.UtcNow,
            User = null
        };

        var queryableMock = new List<AuditLog> { auditLog }.AsQueryable().BuildMock();
        _auditLogRepositoryMock.Setup(r => r.GetQueryable()).Returns(queryableMock.Object);

        var result = await _service.GetByIdAsync(10);

        result.Should().NotBeNull();
        result!.OperatorName.Should().Be("System");
    }

    #endregion

    #region EnsureJsonValue 测试

    [Fact]
    public async Task LogAsync_WithPlainTextNewValue_ShouldWrapAsJsonString()
    {
        // Arrange - 传入非 JSON 的纯文本（这是导致 jsonb 字段插入失败的根因）
        var httpContext = new DefaultHttpContext();
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);

        AuditLog? capturedLog = null;
        _auditLogRepositoryMock.Setup(r => r.AddAsync(It.IsAny<AuditLog>()))
            .Callback<AuditLog>(log => capturedLog = log)
            .ReturnsAsync((AuditLog log) => log);

        // Act
        await _service.LogAsync("Confirm", "ImportBatch", 1, null,
            "确认导入: 成功 5 条, 失败 2 条, 重复 1 条");

        // Assert - 纯文本应被包装为 JSON 字符串
        capturedLog.Should().NotBeNull();
        capturedLog!.NewValue.Should().Be("\"确认导入: 成功 5 条, 失败 2 条, 重复 1 条\"");
    }

    [Fact]
    public async Task LogAsync_WithValidJsonNewValue_ShouldPassThrough()
    {
        // Arrange - 传入合法 JSON
        var httpContext = new DefaultHttpContext();
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);

        AuditLog? capturedLog = null;
        _auditLogRepositoryMock.Setup(r => r.AddAsync(It.IsAny<AuditLog>()))
            .Callback<AuditLog>(log => capturedLog = log)
            .ReturnsAsync((AuditLog log) => log);

        // Act
        var jsonValue = "{\"success\":5,\"error\":2}";
        await _service.LogAsync("Confirm", "ImportBatch", 1, null, jsonValue);

        // Assert - 合法 JSON 应原样通过
        capturedLog.Should().NotBeNull();
        capturedLog!.NewValue.Should().Be(jsonValue);
    }

    [Fact]
    public async Task LogAsync_WithNullValues_ShouldSetNull()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);

        AuditLog? capturedLog = null;
        _auditLogRepositoryMock.Setup(r => r.AddAsync(It.IsAny<AuditLog>()))
            .Callback<AuditLog>(log => capturedLog = log)
            .ReturnsAsync((AuditLog log) => log);

        // Act
        await _service.LogAsync("Delete", "ImportBatch", 1, null, null);

        // Assert
        capturedLog.Should().NotBeNull();
        capturedLog!.OldValue.Should().BeNull();
        capturedLog.NewValue.Should().BeNull();
    }

    [Fact]
    public async Task LogAsync_WithPlainTextOldAndNewValue_ShouldWrapBoth()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);

        AuditLog? capturedLog = null;
        _auditLogRepositoryMock.Setup(r => r.AddAsync(It.IsAny<AuditLog>()))
            .Callback<AuditLog>(log => capturedLog = log)
            .ReturnsAsync((AuditLog log) => log);

        // Act
        await _service.LogAsync("BatchLink", "Transaction", 100,
            "旧关联记录", "批量关联 5 条交易记录");

        // Assert
        capturedLog.Should().NotBeNull();
        capturedLog!.OldValue.Should().Be("\"旧关联记录\"");
        capturedLog.NewValue.Should().Be("\"批量关联 5 条交易记录\"");
    }

    #endregion
}
