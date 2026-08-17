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
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace FinanceApp.Application.Tests.Services;

public class AuthServiceTests : TestBase
{
    private readonly Mock<IRepository<User>> _userRepositoryMock;
    private readonly Mock<IPasswordService> _passwordServiceMock;
    private readonly Mock<ILogger<AuthService>> _loggerMock;
    private readonly Mock<IAuditLogService> _auditLogServiceMock;
    private readonly AuthService _service;

    public AuthServiceTests()
    {
        _userRepositoryMock = new Mock<IRepository<User>>();
        _passwordServiceMock = new Mock<IPasswordService>();
        _loggerMock = new Mock<ILogger<AuthService>>();
        _auditLogServiceMock = new Mock<IAuditLogService>();

        var authOptions = Options.Create(new AuthOptions());

        _service = new AuthService(
            _userRepositoryMock.Object,
            _passwordServiceMock.Object,
            Mapper,
            _loggerMock.Object,
            UnitOfWorkMock.Object,
            authOptions,
            _auditLogServiceMock.Object);
    }

    #region UpdateProfileAsync Tests

    [Fact]
    public async Task UpdateProfileAsync_ValidRequest_UpdatesAndReturnsUserDto()
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
            Role = UserRole.Admin,
            IsActive = true,
            SecurityStamp = "stamp",
            PasswordChangedAt = DateTime.UtcNow
        };

        _userRepositoryMock
            .Setup(r => r.GetByIdAsync(userId))
            .ReturnsAsync(user);

        var request = new UpdateProfileRequest
        {
            FullName = "New Name",
            Email = "new@example.com"
        };

        // Act
        var result = await _service.UpdateProfileAsync(userId, request);

        // Assert
        result.Should().NotBeNull();
        result.FullName.Should().Be("New Name");
        result.Email.Should().Be("new@example.com");
        result.Id.Should().Be(userId);

        _userRepositoryMock.Verify(r => r.Update(It.Is<User>(u => u.FullName == "New Name" && u.Email == "new@example.com")), Times.Once);
        UnitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateProfileAsync_UserNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var userId = 999L;
        _userRepositoryMock
            .Setup(r => r.GetByIdAsync(userId))
            .ReturnsAsync((User?)null);

        var request = new UpdateProfileRequest
        {
            FullName = "New Name",
            Email = "new@example.com"
        };

        // Act
        var act = () => _service.UpdateProfileAsync(userId, request);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("*用户不存在*");
    }

    [Fact]
    public async Task UpdateProfileAsync_EmptyFullName_ThrowsValidationException()
    {
        // Arrange
        var request = new UpdateProfileRequest
        {
            FullName = "   ",
            Email = "test@example.com"
        };

        // Act
        var act = () => _service.UpdateProfileAsync(1L, request);

        // Assert
        await act.Should().ThrowAsync<ValidationException>();
    }

    #endregion
}
