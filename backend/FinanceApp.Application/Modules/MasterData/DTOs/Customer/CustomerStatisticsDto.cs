namespace FinanceApp.Application.Modules.MasterData.DTOs.Customer;

public class CustomerStatisticsDto
{
    public int TotalCount { get; set; }
    public int ActiveCount { get; set; }
    public int ThisMonthNewCount { get; set; }
    public int InactiveCount { get; set; }
}
