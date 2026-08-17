namespace FinanceApp.Application.Modules.FinanceSettlement.DTOs.Payable;

public class PayableAgingDto
{
    public List<string> Categories { get; set; } = ["未到期", "1-30天", "31-60天", "61-90天", "90天以上"];
    public List<decimal> Amounts { get; set; } = [];
}
