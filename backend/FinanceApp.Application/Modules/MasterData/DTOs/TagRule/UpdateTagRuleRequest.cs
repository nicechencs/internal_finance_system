namespace FinanceApp.Application.Modules.MasterData.DTOs.TagRule;

public class UpdateTagRuleRequest
{
    public string RuleName { get; set; } = string.Empty;
    public int Priority { get; set; }
    public string TargetScope { get; set; } = string.Empty;
    public string MatchField { get; set; } = string.Empty;
    public string MatchOperator { get; set; } = string.Empty;
    public string MatchValue { get; set; } = string.Empty;
    public string? MatchValueMax { get; set; }
    public bool IsActive { get; set; }
    public List<long> TagIds { get; set; } = new();
    public List<string> NewTagNames { get; set; } = new();
}
