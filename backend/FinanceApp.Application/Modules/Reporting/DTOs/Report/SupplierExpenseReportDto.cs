namespace FinanceApp.Application.Modules.Reporting.DTOs.Report;

public class SupplierExpenseReportDto
{
    public List<SupplierExpenseItemDto> Suppliers { get; set; } = new();
    public SupplierExpenseSummaryDto Summary { get; set; } = new();
}

public class SupplierExpenseItemDto
{
    public long SupplierId { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public decimal TotalExpense { get; set; }
    public int TransactionCount { get; set; }
    public int Rank { get; set; }
}

public class SupplierExpenseSummaryDto
{
    public decimal TotalExpense { get; set; }
    public int SupplierCount { get; set; }
}
