using System.Diagnostics;
using System.Security.Claims;

namespace FinanceApp.Api.Middleware;

public class PerformanceLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<PerformanceLoggingMiddleware> _logger;

    public PerformanceLoggingMiddleware(RequestDelegate next, ILogger<PerformanceLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Method == "OPTIONS")
        {
            await _next(context);
            return;
        }

        var sw = Stopwatch.StartNew();
        await _next(context);
        sw.Stop();

        if (sw.ElapsedMilliseconds > 1000)
        {
            var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "anonymous";
            var username = context.User.Identity?.Name ?? "anonymous";
            var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

            _logger.LogWarning(
                "Slow request detected: {Method} {Path}, ElapsedMs={ElapsedMs}, StatusCode={StatusCode}, UserId={UserId}, Username={Username}, ClientIp={ClientIp}",
                context.Request.Method,
                context.Request.Path,
                sw.ElapsedMilliseconds,
                context.Response.StatusCode,
                userId,
                username,
                clientIp);
        }
    }
}
