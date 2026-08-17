namespace FinanceApp.Application.Common;

public class ApiResponse<T>
{
    /// <summary>
    /// 是否成功
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// HTTP 状态码
    /// </summary>
    public int Code { get; set; }

    /// <summary>
    /// 响应消息
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// 响应数据
    /// </summary>
    public T? Data { get; set; }

    /// <summary>
    /// 时间戳（ISO 8601 格式）
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 错误详情列表（可选）
    /// </summary>
    public List<string>? Errors { get; set; }

    /// <summary>
    /// 成功响应
    /// </summary>
    public static ApiResponse<T> SuccessResponse(T? data, string? message = null)
    {
        return new ApiResponse<T>
        {
            Success = true,
            Code = 200,
            Message = message ?? "操作成功",
            Data = data
        };
    }

    /// <summary>
    /// 创建成功响应（201）
    /// </summary>
    public static ApiResponse<T> CreatedResponse(T? data, string? message = null)
    {
        return new ApiResponse<T>
        {
            Success = true,
            Code = 201,
            Message = message ?? "创建成功",
            Data = data
        };
    }

    /// <summary>
    /// 错误响应（带错误码）
    /// </summary>
    public static ApiResponse<T> ErrorResponse(int code, string message, List<string>? errors = null)
    {
        return new ApiResponse<T>
        {
            Success = false,
            Code = code,
            Message = message,
            Errors = errors
        };
    }

    /// <summary>
    /// 错误响应（仅消息，默认 400 错误码）
    /// </summary>
    public static ApiResponse<T> ErrorResponse(string message, List<string>? errors = null)
    {
        return new ApiResponse<T>
        {
            Success = false,
            Code = 400,
            Message = message,
            Errors = errors
        };
    }
}
