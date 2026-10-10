namespace FinanceApp.Application.Modules.MasterData.DTOs.Person;

public class UpdatePersonDeliveryRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public long? ProjectId { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? AcceptedAt { get; set; }
    public string? AcceptedBy { get; set; }
    public string? Notes { get; set; }
}
