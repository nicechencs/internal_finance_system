namespace FinanceApp.Application.Modules.FinanceSettlement.DTOs.Payable;

public class PayableTrendDto
{
    public List<string> Months { get; set; } = [];
    public List<decimal> Amounts { get; set; } = [];
}
