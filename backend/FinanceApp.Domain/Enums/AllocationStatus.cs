namespace FinanceApp.Domain.Enums;

/// <summary>
/// 交易分配状态
/// </summary>
public enum AllocationStatus
{
    /// <summary>
    /// 未分配 - 交易未绑定任何应收/应付
    /// </summary>
    Unallocated = 0,

    /// <summary>
    /// 部分分配 - 交易部分金额已绑定应收/应付
    /// </summary>
    PartiallyAllocated = 1,

    /// <summary>
    /// 完全分配 - 交易金额已全部绑定应收/应付
    /// </summary>
    FullyAllocated = 2
}
