namespace FinanceApp.Application.Modules.FinanceSettlement.DTOs;

public class DataMigrationIssuesDto
{
    public List<ProjectAmountIssue> ProjectAmountIssues { get; set; } = new();
    public List<ReceivableAmountIssue> ReceivableAmountIssues { get; set; } = new();
    public List<PayableAmountIssue> PayableAmountIssues { get; set; } = new();
    public List<UnlinkedReceivableDetail> UnlinkedReceivableDetails { get; set; } = new();
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
