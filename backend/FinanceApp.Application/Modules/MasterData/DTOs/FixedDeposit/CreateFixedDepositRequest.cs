namespace FinanceApp.Application.Modules.MasterData.DTOs.FixedDeposit;

public class CreateFixedDepositRequest
{
    public long AccountId { get; set; }
    public decimal Principal { get; set; }
    public int TermMonths { get; set; }
    public decimal InterestRate { get; set; }
    public DateTime? DepositDate { get; set; }
    public string? Notes { get; set; }
    public long? DepositTransactionId { get; set; }
}
