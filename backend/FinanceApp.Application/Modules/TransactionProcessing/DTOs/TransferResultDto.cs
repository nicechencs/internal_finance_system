namespace FinanceApp.Application.Modules.TransactionProcessing.DTOs;

public class TransferResultDto
{
    public TransactionDto OutTransaction { get; set; } = null!;
    public TransactionDto InTransaction { get; set; } = null!;

    // 定期存款联动结果
    public FixedDepositLinkageInfo? FixedDepositLinkage { get; set; }
}

public class FixedDepositLinkageInfo
{
    public string Action { get; set; } = string.Empty; // "Created" or "Withdrawn"
    public long FixedDepositId { get; set; }
    public string? Message { get; set; }
}
