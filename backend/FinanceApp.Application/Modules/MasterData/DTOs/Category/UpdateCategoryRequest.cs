namespace FinanceApp.Application.Modules.MasterData.DTOs.Category;

public class UpdateCategoryRequest
{
    public string Name { get; set; } = string.Empty;
    public long? ParentId { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; }
}
