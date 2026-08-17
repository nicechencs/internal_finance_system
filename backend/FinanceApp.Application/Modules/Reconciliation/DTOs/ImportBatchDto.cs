namespace FinanceApp.Application.Modules.Reconciliation.DTOs;

public class ImportBatchDto
{
    public long Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public long AccountId { get; set; }
    public string AccountName { get; set; } = string.Empty;
    public int TotalCount { get; set; }
    public int SuccessCount { get; set; }
    public int DuplicateCount { get; set; }
    public int ErrorCount { get; set; }
    public string? ErrorMessage { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
