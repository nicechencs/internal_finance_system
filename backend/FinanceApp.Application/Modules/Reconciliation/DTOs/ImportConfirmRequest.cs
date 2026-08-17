namespace FinanceApp.Application.Modules.Reconciliation.DTOs;

public class ImportConfirmRequest
{
    public long BatchId { get; set; }
    public List<int> SelectedRowNumbers { get; set; } = new();
}
