namespace FinanceApp.Application.Modules.Reconciliation.DTOs;

public class ImportPreviewResponse
{
    public long BatchId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public int TotalRows { get; set; }
    public int DuplicateRows { get; set; }
    public int FileConflictRows { get; set; }
    public int RecoverableRows { get; set; }
    public int NewRows { get; set; }
    public string DetectedFormat { get; set; } = string.Empty;
    public List<BankTransactionPreviewDto> Previews { get; set; } = new();
}
