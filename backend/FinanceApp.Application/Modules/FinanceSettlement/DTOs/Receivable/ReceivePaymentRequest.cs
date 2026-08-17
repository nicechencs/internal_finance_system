using System.ComponentModel.DataAnnotations;

namespace FinanceApp.Application.Modules.FinanceSettlement.DTOs.Receivable;

public class ReceivePaymentRequest
{
    public DateTime PaymentDate { get; set; }
    public decimal Amount { get; set; }
    public string? PaymentMethod { get; set; }
    public string? Description { get; set; }
    [Range(1, long.MaxValue, ErrorMessage = "必须关联交易记录")]
    public long TransactionId { get; set; }
}
