namespace FinanceApp.Application.Modules.MasterData.DTOs.TagRule;

public class RerunConfirmResponse
{
    public int ScannedCount { get; set; }
    public int AddedCount { get; set; }
    public int SkippedCount { get; set; }
    public string Message { get; set; } = string.Empty;
}
