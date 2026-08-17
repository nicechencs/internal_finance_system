namespace FinanceApp.Domain.Entities;

public abstract class BaseEntity
{
    public long Id { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }

    // 数据权限字段
    public virtual long? CreatedBy { get; set; }

    // 导航属性
    public virtual User? CreatedByUser { get; set; }
}
