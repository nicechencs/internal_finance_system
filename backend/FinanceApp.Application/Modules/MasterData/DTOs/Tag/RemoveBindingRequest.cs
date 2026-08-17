namespace FinanceApp.Application.Modules.MasterData.DTOs.Tag;

public class RemoveBindingRequest
{
    public string OwnerType { get; set; } = string.Empty;
    public long OwnerId { get; set; }
    public long TagId { get; set; }
}
