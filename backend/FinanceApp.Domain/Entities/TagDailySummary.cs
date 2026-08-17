namespace FinanceApp.Domain.Entities;

/// <summary>
/// 标签每日统计快照，用于高频统计场景的预计算缓存
/// </summary>
public class TagDailySummary : BaseEntity
{
    /// <summary>统计日期（UTC，仅日期部分有意义）</summary>
    public DateTime SummaryDate { get; set; }

    /// <summary>标签 ID</summary>
    public long TagId { get; set; }

    /// <summary>指标作用域，与 TagScope 对应（小写字符串）</summary>
    public string MetricScope { get; set; } = string.Empty;

    /// <summary>当日收入金额</summary>
    public decimal IncomeAmount { get; set; }

    /// <summary>当日支出金额</summary>
    public decimal ExpenseAmount { get; set; }

    /// <summary>当日净额（收入 - 支出）</summary>
    public decimal NetAmount { get; set; }

    /// <summary>当日交易笔数</summary>
    public int TransactionCount { get; set; }

    /// <summary>重算版本号，用于纠错和回滚</summary>
    public int Version { get; set; } = 1;

    // Navigation
    public Tag Tag { get; set; } = null!;
}
