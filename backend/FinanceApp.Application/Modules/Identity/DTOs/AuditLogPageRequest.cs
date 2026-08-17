using FinanceApp.Application.Common;

namespace FinanceApp.Application.Modules.Identity.DTOs;

public class AuditLogPageRequest : PageRequest
{
    public string? Action { get; set; }
    public string? EntityType { get; set; }
    public long? UserId { get; set; }
    public string? Username { get; set; }
}
