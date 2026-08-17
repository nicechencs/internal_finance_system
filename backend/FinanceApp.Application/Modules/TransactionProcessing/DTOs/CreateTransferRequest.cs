namespace FinanceApp.Application.Modules.TransactionProcessing.DTOs;

public class CreateTransferRequest
{
    public long FromAccountId { get; set; }
    public long ToAccountId { get; set; }
    public decimal Amount { get; set; }
    public DateTime TransactionDate { get; set; }
    public string? Description { get; set; }

    // 定期存款参数（转入定期账户时使用）
    public int? TermMonths { get; set; }
    public decimal? InterestRate { get; set; }
}
