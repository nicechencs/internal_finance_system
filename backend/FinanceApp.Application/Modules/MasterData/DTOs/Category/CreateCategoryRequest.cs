namespace FinanceApp.Application.Modules.MasterData.DTOs.Category;

public class CreateCategoryRequest
{
    public string Name { get; set; } = string.Empty;
    public string CategoryType { get; set; } = string.Empty;
    public long? ParentId { get; set; }
    public string? Description { get; set; }
}
