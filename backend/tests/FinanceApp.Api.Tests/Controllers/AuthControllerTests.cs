using System.Security.Claims;
using FluentAssertions;
using FinanceApp.Api.Controllers.Identity;
using FinanceApp.Application.Common;
using FinanceApp.Application.Modules.Identity.DTOs;
using FinanceApp.Application.Modules.Identity.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace FinanceApp.Api.Tests.Controllers;

public class AuthControllerTests
{
    private readonly Mock<IAuthService> _authServiceMock;
    private readonly Mock<IAuthSessionService> _authSessionServiceMock;
    private readonly Mock<IAuditLogService> _auditLogServiceMock;
    private readonly AuthController _controller;

    public AuthControllerTests()
    {
        _authServiceMock = new Mock<IAuthService>();
        _authSessionServiceMock = new Mock<IAuthSessionService>();
        _auditLogServiceMock = new Mock<IAuditLogService>();
        _controller = new AuthController(
            _authServiceMock.Object,
            _authSessionServiceMock.Object,
            _auditLogServiceMock.Object,
            new Mock<ILogger<AuthController>>().Object);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "1"),
            new(ClaimTypes.Name, "testuser")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsPrincipal }
        };
    }

    [Fact]
    public async Task Login_ValidCredentials_ReturnsOkWithUser()
    {
        var request = new LoginRequest
        {
            Username = "testuser",
            Password = "Test@123456"
        };

        var expectedResponse = new AuthenticatedUserDto
        {
            UserId = 1,
            Username = "testuser",
            FullName = "Test User",
            Email = "test@example.com",
            Role = "Admin",
            SecurityStamp = "stamp",
            MustChangePassword = true,
            User = new UserDto
            {
                Id = 1,
                Username = "testuser",
                FullName = "Test User",
                Email = "test@example.com",
                Role = "Admin",
                IsActive = true
            }
        };

        _authServiceMock
            .Setup(x => x.LoginAsync(It.IsAny<LoginRequest>()))
            .ReturnsAsync(expectedResponse);
        _authSessionServiceMock
            .Setup(x => x.SignInAsync(It.IsAny<HttpContext>(), 1, "testuser", "test@example.com", "Admin", "stamp"))
            .Returns(Task.CompletedTask);

        var result = await _controller.Login(request);

        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<LoginResponse>>().Subject;
        apiResponse.Success.Should().BeTrue();
        apiResponse.Data.Should().NotBeNull();
        apiResponse.Data!.User.Username.Should().Be("testuser");
        apiResponse.Data.MustChangePassword.Should().BeTrue();

        _authServiceMock.Verify(x => x.LoginAsync(It.IsAny<LoginRequest>()), Times.Once);
        _authSessionServiceMock.Verify(
            x => x.SignInAsync(It.IsAny<HttpContext>(), 1, "testuser", "test@example.com", "Admin", "stamp"),
            Times.Once);
    }

    [Fact]
    public async Task Logout_ReturnsOk()
    {
        _authSessionServiceMock
            .Setup(x => x.SignOutAsync(It.IsAny<HttpContext>()))
            .Returns(Task.CompletedTask);

        var result = await _controller.Logout();

        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<object>>().Subject;
        apiResponse.Success.Should().BeTrue();

        _auditLogServiceMock.Verify(
            x => x.LogAsync(
                "Logout",
                "User",
                1,
                null,
                It.Is<string>(payload => payload.Contains("testuser"))),
            Times.Once);
        _authSessionServiceMock.Verify(x => x.SignOutAsync(It.IsAny<HttpContext>()), Times.Once);
    }

    [Fact]
    public async Task GetCurrentUser_AuthenticatedUser_ReturnsOkWithUser()
    {
        const long userId = 1;
        var expectedUser = new UserDto
        {
            Id = userId,
            Username = "testuser",
            FullName = "Test User",
            Email = "test@example.com",
            Role = "Admin",
            IsActive = true
        };

        _authServiceMock
            .Setup(x => x.GetCurrentUserAsync(userId))
            .ReturnsAsync(expectedUser);

        var result = await _controller.GetCurrentUser();

        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<UserDto>>().Subject;
        apiResponse.Success.Should().BeTrue();
        apiResponse.Data.Should().NotBeNull();
        apiResponse.Data!.Id.Should().Be(userId);
    }

    [Fact]
    public async Task ChangePassword_ReturnsOkAndRefreshesSession()
    {
        var request = new ChangePasswordRequest
        {
            CurrentPassword = "OldPassword123!",
            NewPassword = "NewPassword123!"
        };

        var resultDto = new AuthenticatedUserDto
        {
            UserId = 1,
            Username = "testuser",
            FullName = "Test User",
            Email = "test@example.com",
            Role = "Admin",
            SecurityStamp = "new-stamp",
            MustChangePassword = false,
            User = new UserDto
            {
                Id = 1,
                Username = "testuser",
                FullName = "Test User",
                Email = "test@example.com",
                Role = "Admin",
                IsActive = true
            }
        };

        _authServiceMock
            .Setup(x => x.ChangePasswordAsync(1, It.IsAny<ChangePasswordRequest>()))
            .ReturnsAsync(resultDto);
        _authSessionServiceMock
            .Setup(x => x.SignInAsync(It.IsAny<HttpContext>(), 1, "testuser", "test@example.com", "Admin", "new-stamp"))
            .Returns(Task.CompletedTask);

        var result = await _controller.ChangePassword(request);

        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<object>>().Subject;
        apiResponse.Success.Should().BeTrue();

        _authSessionServiceMock.Verify(
            x => x.SignInAsync(It.IsAny<HttpContext>(), 1, "testuser", "test@example.com", "Admin", "new-stamp"),
            Times.Once);
    }

    [Fact]
    public async Task UpdateProfile_ReturnsOk_WithUpdatedUser()
    {
        // Arrange
        var request = new UpdateProfileRequest
        {
            FullName = "Updated Name",
            Email = "updated@example.com"
        };

        var expectedUser = new UserDto
        {
            Id = 1,
            Username = "testuser",
            FullName = "Updated Name",
            Email = "updated@example.com",
            Role = "Admin",
            IsActive = true
        };

        _authServiceMock
            .Setup(x => x.UpdateProfileAsync(1L, It.IsAny<UpdateProfileRequest>()))
            .ReturnsAsync(expectedUser);

        // Act
        var result = await _controller.UpdateProfile(request);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<UserDto>>().Subject;
        apiResponse.Success.Should().BeTrue();
        apiResponse.Data.Should().NotBeNull();
        apiResponse.Data!.FullName.Should().Be("Updated Name");
        apiResponse.Data.Email.Should().Be("updated@example.com");
        apiResponse.Message.Should().Be("个人资料更新成功");

        _authServiceMock.Verify(x => x.UpdateProfileAsync(1L, It.IsAny<UpdateProfileRequest>()), Times.Once);
    }
}
