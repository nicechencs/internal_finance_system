using FinanceApp.Domain.Enums;

namespace FinanceApp.Domain.Entities;

public class TagRule : BaseEntity
{
    public string RuleName { get; set; } = string.Empty;
    public int Priority { get; set; }
    public TagScope TargetScope { get; set; }
    public RuleMatchField MatchField { get; set; }
    public RuleMatchOperator MatchOperator { get; set; }
    public string MatchValue { get; set; } = string.Empty;
    // Range operator 的上限值（MatchValue 作为下限），仅 Amount + Range 使用；为 null 表示"仅下限"开放区间
    public string? MatchValueMax { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public ICollection<TagRuleTag> TagRuleTags { get; set; } = new List<TagRuleTag>();
}
