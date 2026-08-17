namespace FinanceApp.Application.Modules.MasterData.DTOs.FixedDeposit;

public class WithdrawFixedDepositRequest
{
    public DateTime? WithdrawalDate { get; set; }
    public decimal? ActualInterest { get; set; }
    public long TransactionId { get; set; }  // 必须关联的交易记录ID
}
