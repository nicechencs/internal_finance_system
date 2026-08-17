using FinanceApp.Application.Common;
using FinanceApp.Application.Modules.Identity.DTOs;

namespace FinanceApp.Application.Modules.Identity.Interfaces;

public interface IAuditLogService
{
    Task<PageResponse<AuditLogDto>> GetPagedAsync(AuditLogPageRequest request);
    Task<AuditLogDto> GetByIdAsync(long id);
    Task LogAsync(string action, string entityType, long entityId, string? oldValue, string? newValue);
}
