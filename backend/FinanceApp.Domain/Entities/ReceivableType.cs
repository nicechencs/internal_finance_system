namespace FinanceApp.Domain.Entities;

/// <summary>
/// 应收款业务类型（主数据）
/// </summary>
public class ReceivableType : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }

    // Navigation properties
    public ICollection<Receivable> Receivables { get; set; } = new List<Receivable>();
}
