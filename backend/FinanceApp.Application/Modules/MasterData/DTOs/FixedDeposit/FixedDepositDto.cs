namespace FinanceApp.Application.Modules.MasterData.DTOs.FixedDeposit;

public class FixedDepositDto
{
    public long Id { get; set; }
    public long AccountId { get; set; }
    public string AccountName { get; set; } = string.Empty;
    public decimal Principal { get; set; }
    public DateTime DepositDate { get; set; }
    public DateTime MaturityDate { get; set; }
    public int TermMonths { get; set; }
    public decimal InterestRate { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? WithdrawalDate { get; set; }
    public decimal? ActualInterest { get; set; }
    public bool IsEarlyWithdrawal { get; set; }
    public int DaysToMaturity { get; set; }
    public decimal ExpectedInterest { get; set; }
    public string? Notes { get; set; }
    public long DepositTransactionId { get; set; }
    public DateTime CreatedAt { get; set; }
}
