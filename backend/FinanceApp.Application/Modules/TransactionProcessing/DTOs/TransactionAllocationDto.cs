namespace FinanceApp.Application.Modules.TransactionProcessing.DTOs;

public class TransactionAllocationDto
{
    public long Id { get; set; }
    public long? ProjectId { get; set; }
    public string? ProjectName { get; set; }
    public long? PersonId { get; set; }
    public string? PersonName { get; set; }
    public decimal Amount { get; set; }
    public decimal? AllocationRate { get; set; }
    public string? Description { get; set; }
}
