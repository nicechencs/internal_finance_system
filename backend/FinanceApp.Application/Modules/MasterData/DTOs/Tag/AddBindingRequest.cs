namespace FinanceApp.Application.Modules.MasterData.DTOs.Tag;

public class AddBindingRequest
{
    public string OwnerType { get; set; } = string.Empty;
    public long OwnerId { get; set; }
    public long TagId { get; set; }
}
