namespace FinanceApp.Domain.Entities;

public class SystemConfig : BaseEntity
{
    public string ConfigKey { get; set; } = string.Empty;
    public string? ConfigValue { get; set; }
    public string ConfigType { get; set; } = "string";
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
}
