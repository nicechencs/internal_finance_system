namespace FinanceApp.Application.Modules.MasterData.DTOs.Person;

public class PersonDeliveryDto
{
    public long Id { get; set; }
    public long PersonId { get; set; }
    public string PersonName { get; set; } = string.Empty;
    public long? ProjectId { get; set; }
    public string? ProjectName { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? AcceptedAt { get; set; }
    public string? AcceptedBy { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
