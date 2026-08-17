using System.Net;
using System.Text.Json;
using FinanceApp.Application.Common;

namespace FinanceApp.Api.Middleware;

public class GlobalExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger;

    public GlobalExceptionHandlerMiddleware(RequestDelegate next, ILogger<GlobalExceptionHandlerMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            if (ShouldLogException(ex))
            {
                _logger.LogError(
                    ex,
                    "处理请求时发生未处理异常: {Method} {Path}",
                    context.Request.Method,
                    context.Request.Path);
            }
            else
            {
                // 对于不记录 Error 的异常（如 Validation, Business），记录 Warning 级别，方便追踪客户端传参问题
                _logger.LogWarning(
                    "请求处理受阻 ({ExceptionType}): {Method} {Path}. Message: {Message}",
                    ex.GetType().Name,
                    context.Request.Method,
                    context.Request.Path,
                    ex.Message);
            }

            await HandleExceptionAsync(context, ex);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var statusCode = HttpStatusCode.InternalServerError;
        var message = "服务器处理请求时发生错误，请稍后重试";
        List<string>? errors = null;

        switch (exception)
        {
            case UnauthorizedAccessException:
                statusCode = HttpStatusCode.Unauthorized;
                message = exception.Message;
                break;
            case ForbiddenException:
                statusCode = HttpStatusCode.Forbidden;
                message = exception.Message;
                break;
            case NotFoundException:
            case KeyNotFoundException:
                statusCode = HttpStatusCode.NotFound;
                message = exception.Message;
                break;
            case ValidationException validationException:
                statusCode = HttpStatusCode.BadRequest;
                message = exception.Message;
                errors = validationException.Errors;
                break;
            case BusinessException:
                statusCode = HttpStatusCode.BadRequest;
                message = exception.Message;
                break;
            case OperationCanceledException:
                statusCode = (HttpStatusCode)499; // Client Closed Request
                message = "请求已取消";
                break;
        }

        var response = ApiResponse<object>.ErrorResponse((int)statusCode, message, errors);
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        return context.Response.WriteAsync(JsonSerializer.Serialize(response, options));
    }

    private static bool ShouldLogException(Exception exception)
    {
        if (exception is OperationCanceledException) return false;
        return exception is not UnauthorizedAccessException
            and not ForbiddenException
            and not NotFoundException
            and not KeyNotFoundException
            and not ValidationException
            and not BusinessException;
    }
}
