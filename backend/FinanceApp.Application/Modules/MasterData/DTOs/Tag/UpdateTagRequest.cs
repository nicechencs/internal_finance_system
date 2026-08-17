namespace FinanceApp.Application.Modules.MasterData.DTOs.Tag;

public class UpdateTagRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
    public string? Color { get; set; }
    public int SortOrder { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; }
}
