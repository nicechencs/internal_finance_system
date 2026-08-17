using System.Security.Claims;
using FluentAssertions;
using FinanceApp.Api.Controllers.Identity;
using FinanceApp.Application.Common;
using FinanceApp.Application.Modules.Identity.DTOs;
using FinanceApp.Application.Modules.Identity.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace FinanceApp.Api.Tests.Controllers;

public class UsersControllerTests
{
    private readonly Mock<IUserManagementService> _userManagementServiceMock;
    private readonly UsersController _controller;

    public UsersControllerTests()
    {
        _userManagementServiceMock = new Mock<IUserManagementService>();
        _controller = new UsersController(_userManagementServiceMock.Object);

        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, "1") };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsPrincipal }
        };
    }

    [Fact]
    public async Task UpdateUser_ReturnsOk_WithUpdatedUser()
    {
        // Arrange
        var userId = 2L;
        var request = new UpdateUserRequest
        {
            FullName = "Updated User",
            Email = "updated@example.com",
            Role = "Accountant"
        };

        var expectedDto = new UserAdminDto
        {
            Id = userId,
            Username = "testuser",
            FullName = "Updated User",
            Email = "updated@example.com",
            Role = "Accountant",
            IsActive = true,
            IsLocked = false
        };

        _userManagementServiceMock
            .Setup(x => x.UpdateUserAsync(userId, It.IsAny<UpdateUserRequest>(), 1L))
            .ReturnsAsync(expectedDto);

        // Act
        var result = await _controller.UpdateUser(userId, request);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<UserAdminDto>>().Subject;
        apiResponse.Success.Should().BeTrue();
        apiResponse.Data.Should().NotBeNull();
        apiResponse.Data!.Id.Should().Be(userId);
        apiResponse.Data.FullName.Should().Be("Updated User");
        apiResponse.Message.Should().Be("用户信息更新成功");

        _userManagementServiceMock.Verify(x => x.UpdateUserAsync(userId, It.IsAny<UpdateUserRequest>(), 1L), Times.Once);
    }
}
