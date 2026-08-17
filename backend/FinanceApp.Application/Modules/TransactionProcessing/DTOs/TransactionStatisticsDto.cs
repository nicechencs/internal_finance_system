namespace FinanceApp.Application.Modules.TransactionProcessing.DTOs;

public class TransactionStatisticsDto
{
    public decimal TotalIncome { get; set; }
    public decimal TotalExpense { get; set; }
    public decimal NetProfit { get; set; }
    public decimal TotalTransfer { get; set; }
    public int IncomeCount { get; set; }
    public int ExpenseCount { get; set; }
    public int TransferCount { get; set; }
    public int TotalCount { get; set; }
}
