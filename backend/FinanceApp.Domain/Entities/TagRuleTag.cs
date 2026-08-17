namespace FinanceApp.Domain.Entities;

public class TagRuleTag
{
    public long TagRuleId { get; set; }
    public long TagId { get; set; }

    // Navigation properties
    public TagRule TagRule { get; set; } = null!;
    public Tag Tag { get; set; } = null!;
}
