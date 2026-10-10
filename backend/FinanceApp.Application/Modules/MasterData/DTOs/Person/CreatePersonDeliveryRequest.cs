namespace FinanceApp.Application.Modules.MasterData.DTOs.Person;

public class CreatePersonDeliveryRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public long? ProjectId { get; set; }

    /// <summary>
    /// Pending / Accepted / Rejected。空则按 Pending 处理。
    /// </summary>
    public string? Status { get; set; }

    public DateTime? AcceptedAt { get; set; }
    public string? AcceptedBy { get; set; }
    public string? Notes { get; set; }
}
