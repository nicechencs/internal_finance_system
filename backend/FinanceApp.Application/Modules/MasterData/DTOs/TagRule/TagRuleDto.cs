namespace FinanceApp.Application.Modules.MasterData.DTOs.TagRule;

public class TagRuleDto
{
    public long Id { get; set; }
    public string RuleName { get; set; } = string.Empty;
    public int Priority { get; set; }
    public string TargetScope { get; set; } = string.Empty;
    public string MatchField { get; set; } = string.Empty;
    public string MatchOperator { get; set; } = string.Empty;
    public string MatchValue { get; set; } = string.Empty;
    public string? MatchValueMax { get; set; }
    public bool IsActive { get; set; }
    public List<TagRuleTagItemDto> Tags { get; set; } = new();
    public DateTime CreatedAt { get; set; }
}

public class TagRuleTagItemDto
{
    public long TagId { get; set; }
    public string TagName { get; set; } = string.Empty;
    public string? TagColor { get; set; }
}
