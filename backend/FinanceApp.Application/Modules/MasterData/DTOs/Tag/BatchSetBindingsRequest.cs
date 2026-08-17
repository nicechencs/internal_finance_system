namespace FinanceApp.Application.Modules.MasterData.DTOs.Tag;

public class BatchSetBindingsRequest
{
    public string OwnerType { get; set; } = string.Empty;
    public List<long> OwnerIds { get; set; } = new();
    public List<long> TagIds { get; set; } = new();
}
