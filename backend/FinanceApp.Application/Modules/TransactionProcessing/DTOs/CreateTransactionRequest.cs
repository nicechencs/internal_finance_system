namespace FinanceApp.Application.Modules.TransactionProcessing.DTOs;

public class CreateTransactionRequest
{
    public DateTime TransactionDate { get; set; }
    public string TransactionType { get; set; } = string.Empty; // Income/Expense
    public decimal Amount { get; set; }
    public long AccountId { get; set; }
    public long? CategoryId { get; set; }
    public long? ProjectId { get; set; }
    public long? CustomerId { get; set; }
    public long? SupplierId { get; set; }
    public long? PersonId { get; set; }
    public string? Description { get; set; }
    public List<CreateAllocationRequest>? Allocations { get; set; }
}
