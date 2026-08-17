namespace FinanceApp.Application.Modules.MasterData.DTOs.Tag;

public class TagBindingDto
{
    public long Id { get; set; }
    public long TagId { get; set; }
    public string TagName { get; set; } = string.Empty;
    public string? TagColor { get; set; }
    public bool TagIsDeleted { get; set; }
    public string OwnerType { get; set; } = string.Empty;
    public long OwnerId { get; set; }
}
