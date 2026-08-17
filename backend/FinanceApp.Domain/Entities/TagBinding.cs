using FinanceApp.Domain.Enums;

namespace FinanceApp.Domain.Entities;

public class TagBinding : BaseEntity
{
    public long TagId { get; set; }

    public TagScope OwnerType { get; set; }

    public long OwnerId { get; set; }

    // 导航属性
    public Tag Tag { get; set; } = null!;
}
