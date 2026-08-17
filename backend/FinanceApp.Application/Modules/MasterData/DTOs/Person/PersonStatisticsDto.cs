namespace FinanceApp.Application.Modules.MasterData.DTOs.Person;

public class PersonStatisticsDto
{
    public int TotalCount { get; set; }
    public int ActiveCount { get; set; }
    public int InactiveCount { get; set; }
    public int ThisMonthNewCount { get; set; }
}
