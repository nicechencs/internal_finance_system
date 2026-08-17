using FinanceApp.Domain.Enums;

namespace FinanceApp.Domain.Entities;

public class Transaction : BaseEntity
{
    public long? BankTransactionId { get; set; }
    public DateTime TransactionDate { get; set; }
    public decimal Amount { get; set; }
    public TransactionType TransactionType { get; set; }
    public TransferDirection TransferDirection { get; set; } = TransferDirection.None;
    public long? CategoryId { get; set; }
    public long AccountId { get; set; }
    public long? ProjectId { get; set; }
    public long? CustomerId { get; set; }
    public long? SupplierId { get; set; }
    public long? PersonId { get; set; }
    public string? Description { get; set; }
    public TransactionStatus Status { get; set; } = TransactionStatus.Confirmed;
    public bool IsAllocated { get; set; }
    public AllocationStatus AllocationStatus { get; set; } = AllocationStatus.Unallocated;
    public override long? CreatedBy { get; set; }
    public long? RelatedTransactionId { get; set; }

    // Navigation properties
    public Transaction? RelatedTransaction { get; set; }
    public BankTransaction? BankTransaction { get; set; }
    public Category? Category { get; set; }
    public Account Account { get; set; } = null!;
    public Project? Project { get; set; }
    public Customer? Customer { get; set; }
    public Supplier? Supplier { get; set; }
    public Person? Person { get; set; }
    public override User? CreatedByUser { get; set; }
    public ICollection<TransactionAllocation> Allocations { get; set; } = new List<TransactionAllocation>();
    public ICollection<TagBinding> TagBindings { get; set; } = new List<TagBinding>();
    public ICollection<ReceivableDetail> ReceivableDetails { get; set; } = new List<ReceivableDetail>();
    public ICollection<PayableDetail> PayableDetails { get; set; } = new List<PayableDetail>();

    /// <summary>
    /// 计算交易可用余额（未核销金额）
    /// </summary>
    public decimal GetAvailableAmount()
    {
        var allocatedToReceivables = ReceivableDetails?.Sum(d => d.Amount) ?? 0;
        var allocatedToPayables = PayableDetails?.Sum(d => d.Amount) ?? 0;
        return Amount - allocatedToReceivables - allocatedToPayables;
    }

    /// <summary>
    /// 计算并更新分配状态
    /// </summary>
    public void UpdateAllocationStatus()
    {
        var availableAmount = GetAvailableAmount();

        if (availableAmount <= 0)
        {
            AllocationStatus = AllocationStatus.FullyAllocated;
        }
        else if (availableAmount < Amount)
        {
            AllocationStatus = AllocationStatus.PartiallyAllocated;
        }
        else
        {
            AllocationStatus = AllocationStatus.Unallocated;
        }
    }
}
