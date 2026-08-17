namespace FinanceApp.Application.Modules.FinanceSettlement.DTOs;

/// <summary>
/// 数据迁移问题报告
/// </summary>
public class DataMigrationIssuesDto
{
    /// <summary>
    /// 项目金额不一致列表
    /// </summary>
    public List<ProjectAmountIssue> ProjectAmountIssues { get; set; } = new();

    /// <summary>
    /// 应收款金额不一致列表
    /// </summary>
    public List<ReceivableAmountIssue> ReceivableAmountIssues { get; set; } = new();

    /// <summary>
    /// 应付款金额不一致列表
    /// </summary>
    public List<PayableAmountIssue> PayableAmountIssues { get; set; } = new();

    /// <summary>
    /// 未关联交易的收款记录
    /// </summary>
    public List<UnlinkedReceivableDetail> UnlinkedReceivableDetails { get; set; } = new();

    /// <summary>
    /// 未关联交易的付款记录
    /// </summary>
    public List<UnlinkedPayableDetail> UnlinkedPayableDetails { get; set; } = new();
}

public class ProjectAmountIssue
{
    public long ProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public decimal ContractAmount { get; set; }
    public decimal ReceivableTotalAmount { get; set; }
    public decimal Difference { get; set; }
}

public class ReceivableAmountIssue
{
    public long ReceivableId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public decimal ReceivedAmount { get; set; }
    public decimal DetailTotalAmount { get; set; }
    public decimal Difference { get; set; }
}

public class PayableAmountIssue
{
    public long PayableId { get; set; }
    public string? ProjectName { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal DetailTotalAmount { get; set; }
    public decimal Difference { get; set; }
}

public class UnlinkedReceivableDetail
{
    public long Id { get; set; }
    public long ReceivableId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public DateTime PaymentDate { get; set; }
    public decimal Amount { get; set; }
}

public class UnlinkedPayableDetail
{
    public long Id { get; set; }
    public long PayableId { get; set; }
    public string? ProjectName { get; set; }
    public DateTime PaymentDate { get; set; }
    public decimal Amount { get; set; }
}
