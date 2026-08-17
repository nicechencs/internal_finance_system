using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FinanceApp.Application.Common;
using FinanceApp.Application.Modules.Identity.DTOs;
using FinanceApp.Application.Modules.Identity.Interfaces;

using FinanceApp.Api.Controllers;

namespace FinanceApp.Api.Controllers.Identity;

[ApiController]
[Route("api/audit-logs")]
[Authorize(Roles = "Admin,Accountant")]
public class AuditLogController : BaseApiController
{
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<AuditLogController> _logger;

    public AuditLogController(IAuditLogService auditLogService, ILogger<AuditLogController> logger)
    {
        _auditLogService = auditLogService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PageResponse<AuditLogDto>>>> GetPaged([FromQuery] AuditLogPageRequest request)
    {
        _logger.LogInformation("[AuditLogController.GetPaged] Page={Page}, PageSize={PageSize}, Action={Action}, EntityType={EntityType}",
            request.Page, request.PageSize, request.Action, request.EntityType);

        var result = await _auditLogService.GetPagedAsync(request);
        return Ok(ApiResponse<PageResponse<AuditLogDto>>.SuccessResponse(result));
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<ApiResponse<AuditLogDto>>> GetById(long id)
    {
        _logger.LogInformation("[AuditLogController.GetById] Id={Id}", id);

        var result = await _auditLogService.GetByIdAsync(id);
        return Ok(ApiResponse<AuditLogDto>.SuccessResponse(result));
    }
}
