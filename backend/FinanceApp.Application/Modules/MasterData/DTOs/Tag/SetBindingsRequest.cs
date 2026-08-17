namespace FinanceApp.Application.Modules.MasterData.DTOs.Tag;

public class SetBindingsRequest
{
    public string OwnerType { get; set; } = string.Empty;
    public long OwnerId { get; set; }
    public List<long> TagIds { get; set; } = new();
}
