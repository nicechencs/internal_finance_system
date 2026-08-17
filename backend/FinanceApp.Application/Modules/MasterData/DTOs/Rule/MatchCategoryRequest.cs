namespace FinanceApp.Application.Modules.MasterData.DTOs.Rule;

public class MatchCategoryRequest
{
    public string CounterpartyName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}
