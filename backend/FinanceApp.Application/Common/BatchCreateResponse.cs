namespace FinanceApp.Application.Common;

public class BatchCreateResponse<T>
{
    public int TotalCount { get; set; }
    public int SuccessCount { get; set; }
    public int FailedCount { get; set; }
    public List<T> SuccessItems { get; set; } = new();
    public List<BatchError> Errors { get; set; } = new();
}

public class BatchError
{
    public int Index { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class BatchCreateRequest<T>
{
    public List<T> Items { get; set; } = new();
}
