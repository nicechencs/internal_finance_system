using FinanceApp.Domain.Enums;

namespace FinanceApp.Domain.Entities;

public class Category : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public long? ParentId { get; set; }
    public CategoryType CategoryType { get; set; }
    public int Level { get; set; } = 1;
    public int SortOrder { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public Category? Parent { get; set; }
    public ICollection<Category> Children { get; set; } = new List<Category>();
}
