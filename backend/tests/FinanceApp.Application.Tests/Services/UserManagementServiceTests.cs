using FluentAssertions;
using FinanceApp.Application.Common;
using FinanceApp.Application.Modules.Identity.DTOs;
using FinanceApp.Application.Modules.Identity.Interfaces;
using FinanceApp.Application.Modules.Identity.Services;
using FinanceApp.Application.Tests.Helpers;
using FinanceApp.Domain.Configuration;
using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Enums;
using FinanceApp.Domain.Interfaces;
using Microsoft.Extensions.Options;
using Moq;

namespace FinanceApp.Application.Tests.Services;

public class UserManagementServiceTests : TestBase
{
    private readonly Mock<IRepository<User>> _userRepositoryMock;
    private readonly Mock<IPasswordService> _passwordServiceMock;
    private readonly Mock<IAuditLogService> _auditLogServiceMock;
    private readonly UserManagementService _service;

    public UserManagementServiceTests()
    {
        _userRepositoryMock = new Mock<IRepository<User>>();
        _passwordServiceMock = new Mock<IPasswordService>();
        _auditLogServiceMock = new Mock<IAuditLogService>();

        var authOptions = Options.Create(new AuthOptions());

        _service = new UserManagementService(
            _userRepositoryMock.Object,
            _passwordServiceMock.Object,
            UnitOfWorkMock.Object,
            Mapper,
            authOptions,
            _auditLogServiceMock.Object);
    }

    #region UpdateUserAsync Tests

    [Fact]
    public async Task UpdateUserAsync_ValidRequest_UpdatesAndReturnsDto()
    {
        // Arrange
        var userId = 1L;
        var user = new User
        {
            Id = userId,
            Username = "testuser",
            NormalizedUsername = "TESTUSER",
            FullName = "Old Name",
            Email = "old@example.com",
            Role = UserRole.Accountant,
            IsActive = true,
            SecurityStamp = "stamp",
            PasswordChangedAt = DateTime.UtcNow
        };

        _userRepositoryMock
            .Setup(r => r.GetByIdAsync(userId))
            .ReturnsAsync(user);

        var request = new UpdateUserRequest
        {
            FullName = "New Name",
            Email = "new@example.com",
            Role = "Accountant"
        };

        // Act
        var result = await _service.UpdateUserAsync(userId, request, 2L);

        // Assert
        result.Should().NotBeNull();
        result.FullName.Should().Be("New Name");
        result.Email.Should().Be("new@example.com");
        result.Role.Should().Be("Accountant");
        result.Id.Should().Be(userId);

        _userRepositoryMock.Verify(r => r.Update(It.Is<User>(u => u.FullName == "New Name" && u.Email == "new@example.com")), Times.Once);
        UnitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateUserAsync_UserNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var userId = 999L;
        _userRepositoryMock
            .Setup(r => r.GetByIdAsync(userId))
            .ReturnsAsync((User?)null);

        var request = new UpdateUserRequest
        {
            FullName = "New Name",
            Email = "new@example.com",
            Role = "Viewer"
        };

        // Act
        var act = () => _service.UpdateUserAsync(userId, request, 1L);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("*用户不存在*");
    }

    [Fact]
    public async Task UpdateUserAsync_DemoteLastAdmin_ThrowsValidationException()
    {
        // Arrange
        var userId = 1L;
        var user = new User
        {
            Id = userId,
            Username = "admin",
            NormalizedUsername = "ADMIN",
            FullName = "Admin User",
            Role = UserRole.Admin,
            IsActive = true,
            SecurityStamp = "stamp",
            PasswordChangedAt = DateTime.UtcNow
        };

        _userRepositoryMock
            .Setup(r => r.GetByIdAsync(userId))
            .ReturnsAsync(user);

        // No other active admins
        var users = new List<User> { user };
        var queryableMock = users.AsQueryable().BuildMock();
        _userRepositoryMock.Setup(r => r.GetQueryable()).Returns(queryableMock.Object);

        var request = new UpdateUserRequest
        {
            FullName = "Admin User",
            Role = "Viewer"
        };

        // Act
        var act = () => _service.UpdateUserAsync(userId, request, 2L);

        // Assert
        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*至少保留一个启用的管理员账号*");
    }

    [Fact]
    public async Task UpdateUserAsync_RoleChanged_UpdatesSecurityStamp()
    {
        // Arrange
        var userId = 1L;
        var originalStamp = "original-stamp";
        var user = new User
        {
            Id = userId,
            Username = "testuser",
            NormalizedUsername = "TESTUSER",
            FullName = "Test User",
            Role = UserRole.Viewer,
            IsActive = true,
            SecurityStamp = originalStamp,
            PasswordChangedAt = DateTime.UtcNow
        };

        _userRepositoryMock
            .Setup(r => r.GetByIdAsync(userId))
            .ReturnsAsync(user);

        var request = new UpdateUserRequest
        {
            FullName = "Test User",
            Role = "Accountant"
        };

        // Act
        await _service.UpdateUserAsync(userId, request, 2L);

        // Assert
        user.SecurityStamp.Should().NotBe(originalStamp);
        user.Role.Should().Be(UserRole.Accountant);

        _userRepositoryMock.Verify(r => r.Update(It.Is<User>(u => u.SecurityStamp != originalStamp)), Times.Once);
    }

    #endregion
}
