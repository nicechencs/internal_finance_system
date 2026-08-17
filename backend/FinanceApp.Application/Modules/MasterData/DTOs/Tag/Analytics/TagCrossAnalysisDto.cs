namespace FinanceApp.Application.Modules.MasterData.DTOs.Tag.Analytics;

public class TagCrossAnalysisDto
{
    public string RowScope { get; set; } = string.Empty;
    public string ColScope { get; set; } = string.Empty;
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
    /// <summary>行标签列表（含汇总统计）</summary>
    public List<TagSummaryItemDto> RowTags { get; set; } = new();
    /// <summary>列标签列表（含汇总统计）</summary>
    public List<TagSummaryItemDto> ColTags { get; set; } = new();
    /// <summary>交叉单元格（稀疏矩阵，仅包含 count > 0 的单元格）</summary>
    public List<TagCrossAnalysisCellDto> Cells { get; set; } = new();
}
