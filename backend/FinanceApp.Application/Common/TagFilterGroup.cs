using FinanceApp.Domain.Enums;

namespace FinanceApp.Application.Common;

public enum TagMatchMode
{
    Or,
    And
}

public class TagFilterGroup
{
    public TagScope Scope { get; set; }
    public List<long> TagIds { get; set; } = new();
    public TagMatchMode MatchMode { get; set; } = TagMatchMode.Or;
}
