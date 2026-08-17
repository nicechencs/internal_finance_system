using FinanceApp.Application.Common;
using FinanceApp.Application.Modules.Identity.DTOs;
using FinanceApp.Application.Modules.Identity.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Text.Json;

using FinanceApp.Api.Controllers;

namespace FinanceApp.Api.Controllers.Identity;

[ApiController]
[Route("api/[controller]")]
public class AuthController : BaseApiController
{
    private readonly IAuthService _authService;
    private readonly IAuthSessionService _authSessionService;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        IAuthService authService,
        IAuthSessionService authSessionService,
        IAuditLogService auditLogService,
        ILogger<AuthController> logger)
    {
        _authService = authService;
        _authSessionService = authSessionService;
        _auditLogService = auditLogService;
        _logger = logger;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("auth-login")]
    public async Task<ActionResult<ApiResponse<LoginResponse>>> Login([FromBody] LoginRequest request)
    {
        var result = await _authService.LoginAsync(request);

        await _authSessionService.SignInAsync(
            HttpContext,
            result.UserId,
            result.Username,
            result.Email,
            result.Role,
            result.SecurityStamp);

        return Ok(ApiResponse<LoginResponse>.SuccessResponse(new LoginResponse
        {
            User = result.User,
            MustChangePassword = result.MustChangePassword
        }, "登录成功"));
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<object>>> Logout()
    {
        var userId = GetCurrentUserId();
        var username = User.Identity?.Name ?? "unknown";

        await _auditLogService.LogAsync("Logout", "User", userId, null,
            JsonSerializer.Serialize(new { username }));
        await _authSessionService.SignOutAsync(HttpContext);
        _logger.LogInformation("User logged out, UserId={UserId}, Username={Username}", userId, username);

        return Ok(ApiResponse<object>.SuccessResponse(null, "退出成功"));
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<UserDto>>> GetCurrentUser()
    {
        var userId = GetCurrentUserId();
        var user = await _authService.GetCurrentUserAsync(userId);
        return Ok(ApiResponse<UserDto>.SuccessResponse(user));
    }

    [HttpPost("change-password")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<object>>> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var userId = GetCurrentUserId();
        var result = await _authService.ChangePasswordAsync(userId, request);

        await _authSessionService.SignInAsync(
            HttpContext,
            result.UserId,
            result.Username,
            result.Email,
            result.Role,
            result.SecurityStamp);

        return Ok(ApiResponse<object>.SuccessResponse(null, "密码修改成功"));
    }

    [HttpPut("profile")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<UserDto>>> UpdateProfile([FromBody] UpdateProfileRequest request)
    {
        var userId = GetCurrentUserId();
        var user = await _authService.UpdateProfileAsync(userId, request);
        return Ok(ApiResponse<UserDto>.SuccessResponse(user, "个人资料更新成功"));
    }
}
