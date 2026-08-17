namespace FinanceApp.Application.Common;

public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message)
    {
    }

    public NotFoundException(string name, object key)
        : base($"Entity \"{name}\" ({key}) was not found.")
    {
    }
}

public class ValidationException : Exception
{
    public List<string> Errors { get; }

    /// <summary>
    /// 错误码，用于前端精确匹配
    /// </summary>
    public string? ErrorCode { get; }

    public ValidationException(List<string> errors) : base("One or more validation errors occurred.")
    {
        Errors = errors;
    }

    public ValidationException(string error) : base(error)
    {
        Errors = new List<string> { error };
    }

    public ValidationException(string error, string errorCode) : base(error)
    {
        Errors = new List<string> { error };
        ErrorCode = errorCode;
    }
}

public class BusinessException : Exception
{
    public BusinessException(string message) : base(message)
    {
    }
}

public class ForbiddenException : Exception
{
    public ForbiddenException(string message) : base(message)
    {
    }
}
