using FinanceApp.Application.Common;
using FinanceApp.Application.Modules.Identity.DTOs;
using FinanceApp.Application.Modules.Identity.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using FinanceApp.Api.Controllers;

namespace FinanceApp.Api.Controllers.Identity;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class UsersController : BaseApiController
{
    private readonly IUserManagementService _userManagementService;

    public UsersController(IUserManagementService userManagementService)
    {
        _userManagementService = userManagementService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<UserAdminDto>>>> GetUsers()
    {
        var users = await _userManagementService.GetUsersAsync();
        return Ok(ApiResponse<IReadOnlyList<UserAdminDto>>.SuccessResponse(users));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<UserAdminDto>>> CreateUser([FromBody] CreateUserRequest request)
    {
        var user = await _userManagementService.CreateUserAsync(request);
        return Ok(ApiResponse<UserAdminDto>.SuccessResponse(user, "用户创建成功"));
    }

    [HttpPut("{userId:long}/password")]
    public async Task<ActionResult<ApiResponse<object>>> SetPassword(long userId, [FromBody] SetUserPasswordRequest request)
    {
        await _userManagementService.SetUserPasswordAsync(userId, request);
        return Ok(ApiResponse<object>.SuccessResponse(null, "密码设置成功"));
    }

    [HttpPut("{userId:long}/status")]
    public async Task<ActionResult<ApiResponse<object>>> SetStatus(long userId, [FromBody] SetUserStatusRequest request)
    {
        await _userManagementService.SetUserStatusAsync(userId, request, GetCurrentUserId());
        return Ok(ApiResponse<object>.SuccessResponse(null, "用户状态已更新"));
    }

    [HttpPost("{userId:long}/unlock")]
    public async Task<ActionResult<ApiResponse<object>>> Unlock(long userId)
    {
        await _userManagementService.UnlockUserAsync(userId);
        return Ok(ApiResponse<object>.SuccessResponse(null, "用户已解锁"));
    }

    [HttpPut("{userId:long}")]
    public async Task<ActionResult<ApiResponse<UserAdminDto>>> UpdateUser(long userId, [FromBody] UpdateUserRequest request)
    {
        var currentUserId = GetCurrentUserId();
        var user = await _userManagementService.UpdateUserAsync(userId, request, currentUserId);
        return Ok(ApiResponse<UserAdminDto>.SuccessResponse(user, "用户信息更新成功"));
    }
}
