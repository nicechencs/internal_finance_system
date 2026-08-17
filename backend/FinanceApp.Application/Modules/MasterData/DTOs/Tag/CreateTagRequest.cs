namespace FinanceApp.Application.Modules.MasterData.DTOs.Tag;

public class CreateTagRequest
{
    public string Scope { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
    public string? Color { get; set; }
    public int SortOrder { get; set; } = 0;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
}
