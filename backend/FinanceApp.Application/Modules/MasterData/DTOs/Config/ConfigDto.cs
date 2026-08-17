namespace FinanceApp.Application.Modules.MasterData.DTOs.Config;

public class ConfigDto
{
    public long Id { get; set; }
    public string ConfigKey { get; set; } = string.Empty;
    public string ConfigValue { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime UpdatedAt { get; set; }
}
