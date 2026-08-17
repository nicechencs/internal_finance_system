using FinanceApp.Application.Modules.FinanceSettlement.DTOs;

namespace FinanceApp.Application.Modules.FinanceSettlement.Interfaces;

/// <summary>
/// 数据迁移服务接口
/// </summary>
public interface IDataMigrationService
{
    /// <summary>
    /// 获取数据一致性问题报告
    /// </summary>
    Task<DataMigrationIssuesDto> GetDataIssuesAsync();

    /// <summary>
    /// 修复应收款金额不一致
    /// </summary>
    Task FixReceivableAmountAsync(long receivableId);

    /// <summary>
    /// 修复应付款金额不一致
    /// </summary>
    Task FixPayableAmountAsync(long payableId);

    /// <summary>
    /// 批量修复所有金额不一致
    /// </summary>
    Task FixAllAmountIssuesAsync();
}
