namespace FinanceApp.Application.Modules.MasterData.DTOs.Account;

public class AccountStatisticsDto
{
    public int TotalCount { get; set; }
    public int ActiveCount { get; set; }
    public decimal TotalBalance { get; set; }
    public int FixedDepositCount { get; set; }
}
