using FinanceApp.Domain.Enums;

namespace FinanceApp.Domain.Entities;

/// <summary>
/// 人员交付 / 验收记录。人员必填，项目可选。
/// AcceptedBy 保存验收时的用户名快照，不关联用户表。
/// </summary>
public class PersonDelivery : BaseEntity
{
    public long PersonId { get; set; }
    public Person Person { get; set; } = null!;

    public long? ProjectId { get; set; }
    public Project? Project { get; set; }

    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public PersonDeliveryStatus Status { get; set; } = PersonDeliveryStatus.Pending;

    public DateTime? AcceptedAt { get; set; }
    public string? AcceptedBy { get; set; }
    public string? Notes { get; set; }
}
