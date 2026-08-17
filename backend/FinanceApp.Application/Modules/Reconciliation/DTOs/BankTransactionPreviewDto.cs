namespace FinanceApp.Application.Modules.Reconciliation.DTOs;

public class BankTransactionPreviewDto
{
    public int RowNumber { get; set; }
    public DateTime TransactionDate { get; set; }
    public TimeSpan? TransactionTime { get; set; }
    public decimal Amount { get; set; }
    public decimal? Balance { get; set; }
    public string Direction { get; set; } = string.Empty; // in/out
    public string CounterpartyName { get; set; } = string.Empty;
    public string? CounterpartyAccount { get; set; }
    public string? CounterpartyBank { get; set; }
    public string? TransactionNumber { get; set; }
    public string? Description { get; set; }
    /// <summary>支付宝等特有字段 JSON 或备注</summary>
    public string? Memo { get; set; }
    public string UniqueHash { get; set; } = string.Empty;
    public bool IsDuplicate { get; set; }
    public bool IsFileConflict { get; set; }
    public bool IsRecoverable { get; set; }
    public string? ConflictReason { get; set; }
    public long? MatchedCategoryId { get; set; }
    public string? MatchedCategoryName { get; set; }
}
