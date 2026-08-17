using FinanceApp.Application.Common;
using FinanceApp.Application.Modules.Identity.DTOs;
using FinanceApp.Application.Modules.Identity.Interfaces;
using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Interfaces;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FinanceApp.Application.Modules.Identity.Services;

public class AuditLogService : IAuditLogService
{
    private readonly IRepository<AuditLog> _auditLogRepository;
    private readonly IRepository<User> _userRepository;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AuditLogService> _logger;

    public AuditLogService(
        IRepository<AuditLog> auditLogRepository,
        IRepository<User> userRepository,
        IHttpContextAccessor httpContextAccessor,
        IUnitOfWork unitOfWork,
        ILogger<AuditLogService> logger)
    {
        _auditLogRepository = auditLogRepository;
        _userRepository = userRepository;
        _httpContextAccessor = httpContextAccessor;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<PageResponse<AuditLogDto>> GetPagedAsync(AuditLogPageRequest request)
    {
        _logger.LogDebug("AuditLogService.GetPagedAsync - Page={Page}, PageSize={PageSize}, Action={Action}, EntityType={EntityType}, Username={Username}",
            request.Page, request.PageSize, request.Action, request.EntityType, request.Username);

        var query = _auditLogRepository.GetQueryable()
            .Include(a => a.User)
            .AsQueryable();

        // 筛选条件
        if (!string.IsNullOrWhiteSpace(request.Action))
            query = query.Where(a => a.Action == request.Action);

        if (!string.IsNullOrWhiteSpace(request.EntityType))
            query = query.Where(a => a.EntityType == request.EntityType);

        if (request.UserId.HasValue)
            query = query.Where(a => a.UserId == request.UserId.Value);

        if (!string.IsNullOrWhiteSpace(request.Username))
        {
            var username = request.Username.Trim();
            if (username.Length > 50)
            {
                throw new ValidationException("用户名筛选条件过长，最多 50 个字符");
            }
            query = query.Where(a => a.User != null && a.User.Username.Contains(username));
        }

        if (request.StartDate.HasValue)
            query = query.Where(a => a.CreatedAt >= request.StartDate.Value.ToUniversalTime());

        if (request.EndDate.HasValue)
            query = query.Where(a => a.CreatedAt < request.EndDate.Value.AddDays(1).ToUniversalTime());

        var orderedQuery = query.OrderByDescending(a => a.CreatedAt);

        // 应用自定义排序
        var sortableFields = new Dictionary<string, System.Linq.Expressions.Expression<Func<AuditLog, object>>>(StringComparer.OrdinalIgnoreCase)
        {
            ["createdAt"] = a => a.CreatedAt,
            ["action"] = a => a.Action,
            ["entityType"] = a => a.EntityType
        };
        var sortedQuery = SortingHelper.ApplySorting(orderedQuery, request.SortBy, request.SortOrder, sortableFields);

        var total = await sortedQuery.CountAsync();
        var items = await sortedQuery
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync();

        var dtos = items.Select(a => new AuditLogDto
        {
            Id = a.Id,
            EntityType = a.EntityType,
            EntityId = a.EntityId,
            Action = a.Action,
            OldValue = a.OldValue,
            NewValue = a.NewValue,
            UserId = a.UserId,
            OperatorName = a.User?.Username ?? "System",
            IpAddress = a.IpAddress ?? "",
            CreatedAt = a.CreatedAt
        }).ToList();

        _logger.LogInformation("查询审计日志列表成功: 返回{Count}条记录, 总数={Total}",
            dtos.Count, total);

        return new PageResponse<AuditLogDto>
        {
            Items = dtos,
            Page = request.Page,
            PageSize = request.PageSize,
            Total = total
        };
    }

    public async Task<AuditLogDto> GetByIdAsync(long id)
    {
        _logger.LogDebug("AuditLogService.GetByIdAsync - Id={Id}", id);

        var auditLog = await _auditLogRepository.GetQueryable()
            .Include(a => a.User)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (auditLog == null)
        {
            throw new NotFoundException("审计日志不存在");
        }

        _logger.LogInformation("获取审计日志详情成功: Id={Id}, Action={Action}, EntityType={EntityType}, EntityId={EntityId}",
            id, auditLog.Action, auditLog.EntityType, auditLog.EntityId);

        return new AuditLogDto
        {
            Id = auditLog.Id,
            EntityType = auditLog.EntityType,
            EntityId = auditLog.EntityId,
            Action = auditLog.Action,
            OldValue = auditLog.OldValue,
            NewValue = auditLog.NewValue,
            UserId = auditLog.UserId,
            OperatorName = auditLog.User?.Username ?? "System",
            IpAddress = auditLog.IpAddress ?? "",
            CreatedAt = auditLog.CreatedAt
        };
    }

    public async Task LogAsync(string action, string entityType, long entityId, string? oldValue, string? newValue)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        var userId = GetCurrentUserId();
        var ipAddress = httpContext?.Connection.RemoteIpAddress?.ToString();
        var userAgent = httpContext?.Request.Headers["User-Agent"].ToString();

        _logger.LogDebug("AuditLogService.LogAsync - Action={Action}, EntityType={EntityType}, EntityId={EntityId}, UserId={UserId}, IP={IpAddress}",
            action, entityType, entityId, userId, ipAddress);

        var auditLog = new AuditLog
        {
            UserId = userId,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            OldValue = EnsureJsonValue(oldValue),
            NewValue = EnsureJsonValue(newValue),
            IpAddress = ipAddress,
            UserAgent = userAgent,
            CreatedAt = DateTime.UtcNow
        };

        await _auditLogRepository.AddAsync(auditLog);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("审计日志写入成功: Action={Action}, EntityType={EntityType}, EntityId={EntityId}, UserId={UserId}",
            action, entityType, entityId, userId);
    }

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    /// <summary>
    /// 确保值为合法 JSON（数据库列为 jsonb 类型）。
    /// 如果传入的不是合法 JSON，将其包装为 JSON 字符串。
    /// </summary>
    private static string? EnsureJsonValue(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return null;

        // 尝试解析为 JSON，如果已经是合法 JSON 则直接返回
        try
        {
            JsonDocument.Parse(value);
            return value;
        }
        catch (JsonException)
        {
            // 不是合法 JSON，包装为 JSON 字符串
            return JsonSerializer.Serialize(value, _jsonOptions);
        }
    }

    private long? GetCurrentUserId()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext?.User?.Identity?.IsAuthenticated == true)
        {
            var userIdClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (long.TryParse(userIdClaim, out var userId))
            {
                return userId;
            }
        }
        return null;
    }
}
