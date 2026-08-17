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
}
