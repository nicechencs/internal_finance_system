namespace FinanceApp.Application.Modules.MasterData.DTOs.TagRule;

public class RerunConfirmRequest
{
    public string TargetScope { get; set; } = string.Empty;
    public List<long> TransactionIds { get; set; } = new();
}
