using FinanceApp.Domain.Enums;

namespace FinanceApp.Domain.Entities;

public class ImportBatch : BaseEntity
{
    public long AccountId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public long? FileSize { get; set; }
    public DateTime ImportDate { get; set; } = DateTime.UtcNow;
    public int RecordCount { get; set; }
    public int SuccessCount { get; set; }
    public int DuplicateCount { get; set; }
    public int ErrorCount { get; set; }
    public ImportBatchStatus Status { get; set; } = ImportBatchStatus.Pending;
    public string? ErrorMessage { get; set; }
    public long? ImportedBy { get; set; }

    // Navigation properties
    public Account Account { get; set; } = null!;
    public User? ImportedByUser { get; set; }
}
