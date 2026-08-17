using FinanceApp.Domain.Enums;

namespace FinanceApp.Domain.Entities;

public class Tag : BaseEntity
{
    public TagScope Scope { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Code { get; set; }

    public string? Color { get; set; }

    public int SortOrder { get; set; } = 0;

    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    public bool IsSystem { get; set; } = false;

    // 导航属性
    public ICollection<TagBinding> Bindings { get; set; } = new List<TagBinding>();
}
