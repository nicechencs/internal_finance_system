using FinanceApp.Application.Modules.MasterData.DTOs.Tag;

namespace FinanceApp.Application.Modules.TransactionProcessing.DTOs;

public class TransactionDto
{
    public long Id { get; set; }
    public long? BankTransactionId { get; set; }
    public DateTime TransactionDate { get; set; }
    public string TransactionType { get; set; } = string.Empty; // Income/Expense/Transfer
    public string? TransferDirection { get; set; }
    public decimal Amount { get; set; }
    public long AccountId { get; set; }
    public string AccountName { get; set; } = string.Empty;
    public long? CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public long? ProjectId { get; set; }
    public string? ProjectName { get; set; }
    public long? CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public long? SupplierId { get; set; }
    public string? SupplierName { get; set; }
    public long? PersonId { get; set; }
    public string? PersonName { get; set; }
    public string? Counterparty { get; set; }
    public TimeSpan? TransactionTime { get; set; }
    public string? CounterpartyAccount { get; set; }
    public string? CounterpartyBank { get; set; }
    public string? TransactionNumber { get; set; }
    public string? Description { get; set; }
    public string? Memo { get; set; }
    public string Status { get; set; } = string.Empty;
    public bool IsAllocated { get; set; }
    public long? RelatedTransactionId { get; set; }
    public string? RelatedAccountName { get; set; }
    public List<TransactionAllocationDto> Allocations { get; set; } = new();
    public List<TagItemDto> Tags { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public decimal AvailableAmount { get; set; }
    public string AllocationStatus { get; set; } = string.Empty;
}
