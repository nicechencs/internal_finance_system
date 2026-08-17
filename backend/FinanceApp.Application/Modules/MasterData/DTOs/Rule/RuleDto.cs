namespace FinanceApp.Application.Modules.MasterData.DTOs.Rule;

public class RuleDto
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public long CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string MatchField { get; set; } = string.Empty; // CounterpartyName/Description/Amount
    public string MatchOperator { get; set; } = string.Empty; // Contains/Equals/Regex
    public string MatchValue { get; set; } = string.Empty;
    public string? MatchValueMax { get; set; }
    public int Priority { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}
