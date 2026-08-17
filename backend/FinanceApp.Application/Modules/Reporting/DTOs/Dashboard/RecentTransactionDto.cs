namespace FinanceApp.Application.Modules.Reporting.DTOs.Dashboard;

public class RecentTransactionDto
{
    public long Id { get; set; }
    public DateTime TransactionDate { get; set; }
    public string Type { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string AccountName { get; set; } = string.Empty;
    public string? CategoryName { get; set; }
    public string? CounterpartyName { get; set; }
    public string? Description { get; set; }
}
