using FinanceApp.Application.Common;
using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Interfaces;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace FinanceApp.Application.Services;

/// <summary>
/// Service 基类，提供统一的权限检查方法
/// </summary>
public abstract class ServiceBase
{
    protected readonly ICurrentUserService CurrentUserService;
    protected readonly IDataPermissionService PermissionService;

    private static readonly JsonSerializerOptions AuditJsonOptions = new()
    {
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    protected ServiceBase(
        ICurrentUserService currentUserService,
        IDataPermissionService permissionService)
    {
        CurrentUserService = currentUserService;
        PermissionService = permissionService;
    }

    /// <summary>
    /// 将对象序列化为审计日志 JSON 快照
    /// </summary>
    protected static string? SerializeForAudit(object? value)
    {
        if (value == null) return null;
        return JsonSerializer.Serialize(value, AuditJsonOptions);
    }

    /// <summary>
    /// 对查询应用权限过滤
    /// </summary>
    protected IQueryable<T> ApplyPermissionFilter<T>(IQueryable<T> query) where T : BaseEntity
    {
        return PermissionService.ApplyPermissionFilter(query);
    }

    /// <summary>
    /// 确保有权限编辑指定实体
    /// </summary>
    protected void EnsureCanEdit<T>(T entity) where T : BaseEntity
    {
        if (!PermissionService.CanModify(entity))
        {
            throw new ForbiddenException("无权修改此数据");
        }
    }

    /// <summary>
    /// 确保有权限删除指定实体
    /// </summary>
    protected void EnsureCanDelete<T>(T entity) where T : BaseEntity
    {
        if (!PermissionService.CanDelete(entity))
        {
            throw new ForbiddenException("无权删除此数据");
        }
    }

    /// <summary>
    /// 确保有权限访问指定实体
    /// </summary>
    protected void EnsureCanAccess<T>(T entity) where T : BaseEntity
    {
        if (!PermissionService.CanAccess(entity))
        {
            throw new ForbiddenException("无权访问此数据");
        }
    }
}
