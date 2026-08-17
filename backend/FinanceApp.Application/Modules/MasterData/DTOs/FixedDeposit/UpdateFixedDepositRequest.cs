namespace FinanceApp.Application.Modules.MasterData.DTOs.FixedDeposit;

public class UpdateFixedDepositRequest
{
    public long AccountId { get; set; }
    public decimal Principal { get; set; }
    public DateTime DepositDate { get; set; }
    public int TermMonths { get; set; }
    public decimal InterestRate { get; set; }
    public string? Notes { get; set; }
}
