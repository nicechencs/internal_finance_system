using System.Security.Claims;
using System.Text.Json;
using FinanceApp.Domain.Constants;
using FinanceApp.Infrastructure.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace FinanceApp.Infrastructure.Services;

public class CookieSessionValidationEvents : CookieAuthenticationEvents
{
    private readonly AppDbContext _dbContext;

    public CookieSessionValidationEvents(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public override async Task ValidatePrincipal(CookieValidatePrincipalContext context)
    {
        var userIdClaim = context.Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var securityStampClaim = context.Principal?.FindFirst(AppConstants.Auth.SecurityStampClaimType)?.Value;

        if (!long.TryParse(userIdClaim, out var userId) || string.IsNullOrWhiteSpace(securityStampClaim))
        {
            await RejectPrincipalAsync(context);
            return;
        }

        var user = await _dbContext.Users
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null
            || user.IsDeleted
            || !user.IsActive
            || string.IsNullOrWhiteSpace(user.SecurityStamp)
            || !string.Equals(user.SecurityStamp, securityStampClaim, StringComparison.Ordinal))
        {
            await RejectPrincipalAsync(context);
            return;
        }

        await base.ValidatePrincipal(context);
    }

    public override Task RedirectToLogin(RedirectContext<CookieAuthenticationOptions> context)
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        context.Response.ContentType = "application/json";
        var payload = JsonSerializer.Serialize(new ApiErrorResponse<object>
        {
            Success = false,
            Code = 401,
            Message = "未登录或登录已失效",
            Data = default
        });
        return context.Response.WriteAsync(payload);
    }

    public override Task RedirectToAccessDenied(RedirectContext<CookieAuthenticationOptions> context)
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        context.Response.ContentType = "application/json";
        var payload = JsonSerializer.Serialize(new ApiErrorResponse<object>
        {
            Success = false,
            Code = 403,
            Message = "拒绝访问",
            Data = default
        });
        return context.Response.WriteAsync(payload);
    }

    private static async Task RejectPrincipalAsync(CookieValidatePrincipalContext context)
    {
        context.RejectPrincipal();
        await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    }

    private sealed class ApiErrorResponse<T>
    {
        public bool Success { get; init; }
        public int Code { get; init; }
        public string Message { get; init; } = string.Empty;
        public T? Data { get; init; }
    }
}
