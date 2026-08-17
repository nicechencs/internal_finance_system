namespace FinanceApp.Application.Modules.TransactionProcessing.DTOs;

public class RelatedFinanceRecordDto
{
    public List<RelatedReceivableDto> Receivables { get; set; } = new();
    public List<RelatedPayableDto> Payables { get; set; } = new();
}

public class RelatedReceivableDto
{
    public long Id { get; set; }
    public long ProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public long? CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public long? SupplierId { get; set; }
    public string? SupplierName { get; set; }
    public long? PersonId { get; set; }
    public string? PersonName { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal ReceivedAmount { get; set; }
    public decimal RemainingAmount { get; set; }
    public DateTime? DueDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal PaymentAmount { get; set; } // 本次交易关联的收款金额
    public DateTime PaymentDate { get; set; } // 本次交易关联的收款日期
}

public class RelatedPayableDto
{
    public long Id { get; set; }
    public long? SupplierId { get; set; }
    public string? SupplierName { get; set; }
    public long? CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public long? PersonId { get; set; }
    public string? PersonName { get; set; }
    public long? ProjectId { get; set; }
    public string? ProjectName { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal RemainingAmount { get; set; }
    public DateTime? DueDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal PaymentAmount { get; set; } // 本次交易关联的付款金额
    public DateTime PaymentDate { get; set; } // 本次交易关联的付款日期
}
