namespace FinanceApp.Domain.Entities;

/// <summary>
/// 应付款业务类型（主数据）
/// </summary>
public class PayableType : BaseEntity
{
    /// <summary>
    /// 类型名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 类型编码
    /// </summary>
    public string? Code { get; set; }

    /// <summary>
    /// 说明
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 是否启用
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// 排序
    /// </summary>
    public int SortOrder { get; set; }

    // Navigation properties
    public ICollection<Payable> Payables { get; set; } = new List<Payable>();
}
