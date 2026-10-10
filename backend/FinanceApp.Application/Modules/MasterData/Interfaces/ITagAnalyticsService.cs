using FinanceApp.Application.Modules.MasterData.DTOs.Tag.Analytics;

namespace FinanceApp.Application.Modules.MasterData.Interfaces;

public interface ITagAnalyticsService
{
    /// <summary>
    /// 获取指定 scope 下所有标签的交易汇总统计
    /// </summary>
    Task<TagSummaryDto> GetTagSummaryAsync(string scope, DateTime? dateFrom = null, DateTime? dateTo = null);

    /// <summary>
    /// 获取两个 scope 的标签交叉分析矩阵
    /// </summary>
    Task<TagCrossAnalysisDto> GetTagCrossAnalysisAsync(string rowScope, string colScope, DateTime? dateFrom = null, DateTime? dateTo = null);

    /// <summary>
    /// 按 UTC 日期重建标签每日汇总快照。同一日期重复调用会覆盖旧快照。
    /// </summary>
    Task<TagDailySummaryRebuildResult> RebuildDailySummariesAsync(DateTime date);

    /// <summary>
    /// 查询已落库的标签每日汇总快照。from/to 为 UTC 日期（含首尾）。
    /// </summary>
    Task<List<TagDailySummaryDto>> GetDailySummariesAsync(DateTime from, DateTime to, long? tagId = null);
}
