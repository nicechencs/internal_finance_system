namespace FinanceApp.Application.Modules.MasterData.DTOs.TagRule;

public class RerunPreviewRequest
{
    public string TargetScope { get; set; } = string.Empty;
    public List<long>? EntityIds { get; set; }
}
