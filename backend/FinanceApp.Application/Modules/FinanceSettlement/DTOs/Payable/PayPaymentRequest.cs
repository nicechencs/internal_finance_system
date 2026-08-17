using System.ComponentModel.DataAnnotations;

namespace FinanceApp.Application.Modules.FinanceSettlement.DTOs.Payable;

public class PayPaymentRequest
{
    public DateTime PaymentDate { get; set; }
    public decimal Amount { get; set; }
    public string? PaymentMethod { get; set; }
    public string? Description { get; set; }
    [Range(1, long.MaxValue, ErrorMessage = "必须关联交易记录")]
    public long TransactionId { get; set; }
}
