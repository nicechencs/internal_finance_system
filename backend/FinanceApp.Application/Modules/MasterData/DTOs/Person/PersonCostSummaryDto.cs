namespace FinanceApp.Application.Modules.MasterData.DTOs.Person;

public class PersonCostSummaryDto
{
    public long PersonId { get; set; }
    public string PersonName { get; set; } = string.Empty;
    public decimal TotalCost { get; set; }
    public decimal DirectCost { get; set; }
    public decimal AllocatedCost { get; set; }
    public int TransactionCount { get; set; }
    public DateTime? FirstTransactionDate { get; set; }
    public DateTime? LastTransactionDate { get; set; }
}
