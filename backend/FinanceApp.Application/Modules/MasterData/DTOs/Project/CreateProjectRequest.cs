namespace FinanceApp.Application.Modules.MasterData.DTOs.Project;

public class CreateProjectRequest
{
    public string Name { get; set; } = string.Empty;
    public string ProjectCode { get; set; } = string.Empty;
    public long? CustomerId { get; set; }
    public decimal ContractAmount { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string Status { get; set; } = "Active";
    public string? Description { get; set; }
}
