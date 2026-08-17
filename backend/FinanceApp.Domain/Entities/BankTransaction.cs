using FinanceApp.Domain.Enums;

namespace FinanceApp.Domain.Entities;

public class BankTransaction : BaseEntity
{
    public long AccountId { get; set; }
    public long? ImportBatchId { get; set; }
    public DateTime TransactionDate { get; set; }
    public TimeSpan? TransactionTime { get; set; }
    public decimal Amount { get; set; }
    public decimal? Balance { get; set; }
    public BankTransactionDirection Direction { get; set; }
    public string? Counterparty { get; set; }
    public string? CounterpartyAccount { get; set; }
    public string? CounterpartyBank { get; set; }
    public string? Description { get; set; }
    public string? Memo { get; set; }
    public string? TransactionNumber { get; set; }
    public string UniqueHash { get; set; } = string.Empty;
    public bool IsProcessed { get; set; }

    // Navigation properties
    public Account Account { get; set; } = null!;
    public ImportBatch? ImportBatch { get; set; }
}
