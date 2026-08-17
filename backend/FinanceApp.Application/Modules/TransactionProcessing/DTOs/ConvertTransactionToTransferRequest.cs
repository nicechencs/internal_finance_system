namespace FinanceApp.Application.Modules.TransactionProcessing.DTOs;

public class ConvertTransactionToTransferRequest
{
    public long TargetAccountId { get; set; }
    public long? MatchedTransactionId { get; set; }
    public string? Description { get; set; }
}
