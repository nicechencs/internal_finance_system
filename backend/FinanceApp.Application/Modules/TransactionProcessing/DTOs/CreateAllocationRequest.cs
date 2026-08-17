namespace FinanceApp.Application.Modules.TransactionProcessing.DTOs;

public class CreateAllocationRequest
{
    public long? ProjectId { get; set; }
    public long? PersonId { get; set; }
    public decimal? Amount { get; set; }
    public decimal? AllocationRate { get; set; }
    public string? Description { get; set; }
}
