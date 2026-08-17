namespace FinanceApp.Application.Modules.MasterData.DTOs.Category;

public class CategoryDto
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string CategoryType { get; set; } = string.Empty; // Income/Expense
    public long? ParentId { get; set; }
    public string? ParentName { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}
