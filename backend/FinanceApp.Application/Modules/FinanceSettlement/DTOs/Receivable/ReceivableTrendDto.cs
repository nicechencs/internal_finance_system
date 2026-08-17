namespace FinanceApp.Application.Modules.FinanceSettlement.DTOs.Receivable;

public class ReceivableTrendDto
{
    public List<string> Months { get; set; } = [];
    public List<decimal> Amounts { get; set; } = [];
}
