namespace FinanceApp.Application.Modules.MasterData.Interfaces;

public interface IProjectFinancialRecalculationService
{
    /// <summary>
    /// 从原始数据重新计算项目的所有财务汇总字段（已收款、应收款、总成本、利润、利润率）。
    /// 不调用 SaveChanges，由调用方统一提交。
    /// </summary>
    Task RecalculateAsync(long projectId);
}
