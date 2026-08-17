namespace FinanceApp.Application.Modules.Reconciliation.Parsers;

public class ParsedBankRow
{
    public DateTime TransactionDate { get; set; }
    public TimeSpan? TransactionTime { get; set; }
    public decimal Amount { get; set; }
    /// <summary>in / out</summary>
    public string Direction { get; set; } = string.Empty;
    public decimal? Balance { get; set; }
    public string Counterparty { get; set; } = string.Empty;
    public string? CounterpartyAccount { get; set; }
    public string? CounterpartyBank { get; set; }
    public string? TransactionNumber { get; set; }
    public string? Description { get; set; }
    /// <summary>支付宝等特有字段，序列化为 JSON</summary>
    public string? ExtendedInfo { get; set; }
}
