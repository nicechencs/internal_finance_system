namespace FinanceApp.Application.Modules.MasterData.DTOs.TagRule;

public class RerunPreviewResponse
{
    public int TotalScanned { get; set; }
    public int TotalAffected { get; set; }
    public int TotalTagsToAdd { get; set; }
    public List<RerunCandidateDto> Candidates { get; set; } = new();
}

public class RerunCandidateDto
{
    public long TransactionId { get; set; }
    public DateTime TransactionDate { get; set; }
    public decimal Amount { get; set; }
    public string? Counterparty { get; set; }
    public string? Description { get; set; }
    public List<MatchedRuleDto> MatchedRules { get; set; } = new();
    public List<TagToAddDto> TagsToAdd { get; set; } = new();
}

public class MatchedRuleDto
{
    public long RuleId { get; set; }
    public string RuleName { get; set; } = string.Empty;
    public int Priority { get; set; }
}

public class TagToAddDto
{
    public long TagId { get; set; }
    public string TagName { get; set; } = string.Empty;
    public string? TagColor { get; set; }
}
