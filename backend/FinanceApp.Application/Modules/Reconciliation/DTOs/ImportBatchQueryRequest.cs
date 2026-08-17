using FinanceApp.Application.Common;

namespace FinanceApp.Application.Modules.Reconciliation.DTOs;

public class ImportBatchQueryRequest : PageRequest
{
    public new string? Status { get; set; }
    public string? FileName { get; set; }
}
