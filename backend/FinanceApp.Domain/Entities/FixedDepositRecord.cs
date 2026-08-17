using FinanceApp.Domain.Enums;

namespace FinanceApp.Domain.Entities;

public class FixedDepositRecord : BaseEntity
{
    public long AccountId { get; set; }
    public virtual Account Account { get; set; } = null!;

    public decimal Principal { get; set; }
    public DateTime DepositDate { get; set; }
    public DateTime MaturityDate { get; set; }
    public int TermMonths { get; set; }
    public decimal InterestRate { get; set; }
    public FixedDepositStatus Status { get; set; } = FixedDepositStatus.Active;

    public DateTime? WithdrawalDate { get; set; }
    public decimal? ActualInterest { get; set; }
    public bool IsEarlyWithdrawal { get; set; }

    public long DepositTransactionId { get; set; }
    public long? WithdrawalTransactionId { get; set; }

    public string? Notes { get; set; }
}
