using FinanceApp.Domain.Enums;

namespace FinanceApp.Domain.Entities;

public class ClassificationRule : BaseEntity
{
    public string RuleName { get; set; } = string.Empty;
    public int Priority { get; set; }
    public RuleMatchField MatchField { get; set; }
    public RuleMatchOperator MatchOperator { get; set; }
    public string MatchValue { get; set; } = string.Empty;
    // Range operator 的上限值（MatchValue 作为下限），仅 Amount + Range 使用；为 null 表示"仅下限"开放区间
    public string? MatchValueMax { get; set; }
    public long? CategoryId { get; set; }
    public long? ProjectId { get; set; }
    public long? CustomerId { get; set; }
    public long? SupplierId { get; set; }
    public long? PersonId { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public Category? Category { get; set; }
    public Project? Project { get; set; }
    public Customer? Customer { get; set; }
    public Supplier? Supplier { get; set; }
    public Person? Person { get; set; }
}
