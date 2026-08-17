namespace FinanceApp.Application.Modules.MasterData.DTOs.Account;

public class BalanceTrendDto
{
    public DateTime Date { get; set; }
    public decimal Balance { get; set; }
}

public class BalanceTrendResponse
{
    public List<BalanceTrendDto> Trends { get; set; } = new();
    public decimal CurrentBalance { get; set; }
    public decimal OpeningBalance { get; set; }
}
