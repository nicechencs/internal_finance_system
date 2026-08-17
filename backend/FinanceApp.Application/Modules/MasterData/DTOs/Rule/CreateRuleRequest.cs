namespace FinanceApp.Application.Modules.MasterData.DTOs.Rule;

public class CreateRuleRequest
{
    public string Name { get; set; } = string.Empty;
    public long CategoryId { get; set; }
    public string MatchField { get; set; } = string.Empty;
    public string MatchOperator { get; set; } = string.Empty;
    public string MatchValue { get; set; } = string.Empty;
    public string? MatchValueMax { get; set; }
    public int Priority { get; set; } = 0;
}
