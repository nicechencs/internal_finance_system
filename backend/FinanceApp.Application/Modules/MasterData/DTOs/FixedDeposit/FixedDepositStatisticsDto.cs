namespace FinanceApp.Application.Modules.MasterData.DTOs.FixedDeposit;

public class FixedDepositStatisticsDto
{
    public int TotalCount { get; set; }
    public int ActiveCount { get; set; }
    public int WithdrawnCount { get; set; }
    public int UpcomingCount { get; set; }  // 30天内到期
    public decimal TotalPrincipal { get; set; }
    public decimal ActivePrincipal { get; set; }
    public decimal ExpectedInterest { get; set; }
}
