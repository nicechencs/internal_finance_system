using System.Linq.Expressions;
using MapsterMapper;
using FinanceApp.Application.Common;
using FinanceApp.Application.Interfaces;
using FinanceApp.Application.Modules.Identity.Interfaces;
using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FinanceApp.Application.Services.Base;

/// <summary>
/// CRUD 服务基类，提供标准的增删改查实现。
/// 子类需实现 <see cref="MapCreateRequestToEntity"/> 和 <see cref="MapUpdateRequestToEntity"/>，
/// 并可重写各验证/钩子方法来扩展行为。
/// </summary>
public abstract class CrudServiceBase<TEntity, TDto, TCreateRequest, TUpdateRequest>
    : ICrudService<TDto, TCreateRequest, TUpdateRequest>
    where TEntity : BaseEntity
    where TDto : class
    where TCreateRequest : class
    where TUpdateRequest : class
{
    protected readonly IRepository<TEntity> Repository;
    protected readonly IMapper Mapper;
    protected readonly IAuditLogService AuditLogService;
    protected readonly ILogger Logger;
    protected readonly IUnitOfWork UnitOfWork;

    /// <summary>实体类型名称，用于日志和审计</summary>
    protected abstract string EntityTypeName { get; }

    protected CrudServiceBase(
        IRepository<TEntity> repository, IMapper mapper,
        IAuditLogService auditLogService, ILogger logger,
        IUnitOfWork unitOfWork)
    {
        Repository = repository;
        Mapper = mapper;
        AuditLogService = auditLogService;
        Logger = logger;
        UnitOfWork = unitOfWork;
    }

    /// <inheritdoc />
    public virtual async Task<PageResponse<TDto>> GetPagedAsync(PageRequest request)
    {
        ValidationHelper.ValidatePageRequest(request.Page, request.PageSize);
        var query = BuildPagedQuery();
        query = ApplySorting(query, request.SortBy, request.SortOrder);
        var total = await query.CountAsync();
        var items = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync();

        return new PageResponse<TDto>
        {
            Items = Mapper.Map<List<TDto>>(items),
            Page = request.Page,
            PageSize = request.PageSize,
            Total = total
        };
    }

    /// <inheritdoc />
    public virtual async Task<TDto> GetByIdAsync(long id)
    {
        ValidationHelper.ValidateId(id);
        var entity = await Repository.GetByIdAsync(id)
            ?? throw new NotFoundException($"{EntityTypeName}不存在");
        return Mapper.Map<TDto>(entity);
    }

    /// <inheritdoc />
    public virtual async Task<TDto> CreateAsync(TCreateRequest request)
    {
        ValidationHelper.ValidateNotNull(request, nameof(request));
        await ValidateCreateRequestAsync(request);

        var entity = MapCreateRequestToEntity(request);
        await BeforeCreateAsync(entity, request);

        var created = await Repository.AddAsync(entity);
        await UnitOfWork.SaveChangesAsync();

        await AfterCreateAsync(created, request);
        await LogAuditAsync("Create", created.Id, null, Mapper.Map<TDto>(created));
        Logger.LogInformation("{EntityType} 创建成功，ID: {Id}", EntityTypeName, created.Id);
        return Mapper.Map<TDto>(created);
    }

    /// <inheritdoc />
    public virtual async Task<TDto> UpdateAsync(long id, TUpdateRequest request)
    {
        ValidationHelper.ValidateId(id);
        ValidationHelper.ValidateNotNull(request, nameof(request));

        var entity = await Repository.GetByIdAsync(id)
            ?? throw new NotFoundException($"{EntityTypeName}不存在");
        await ValidateUpdateRequestAsync(id, request);

        var oldDto = Mapper.Map<TDto>(entity);
        MapUpdateRequestToEntity(request, entity);
        await BeforeUpdateAsync(entity, request);

        Repository.Update(entity);
        await UnitOfWork.SaveChangesAsync();

        await AfterUpdateAsync(entity, request);
        await LogAuditAsync("Update", entity.Id, oldDto, Mapper.Map<TDto>(entity));
        Logger.LogInformation("{EntityType} 更新成功，ID: {Id}", EntityTypeName, entity.Id);
        return Mapper.Map<TDto>(entity);
    }

    /// <inheritdoc />
    public virtual async Task DeleteAsync(long id)
    {
        ValidationHelper.ValidateId(id);
        var entity = await Repository.GetByIdAsync(id)
            ?? throw new NotFoundException($"{EntityTypeName}不存在");

        await ValidateDeleteAsync(id);
        await BeforeDeleteAsync(entity);

        var oldDto = Mapper.Map<TDto>(entity);
        Repository.Delete(id);
        await UnitOfWork.SaveChangesAsync();

        await AfterDeleteAsync(entity);
        await LogAuditAsync("Delete", entity.Id, oldDto, null);
        Logger.LogInformation("{EntityType} 删除成功，ID: {Id}", EntityTypeName, entity.Id);
    }

    // ── 可重写的虚方法 ──────────────────────────────────────────

    /// <summary>构建分页查询，可重写以自定义排序和过滤</summary>
    protected virtual IQueryable<TEntity> BuildPagedQuery()
        => Repository.GetQueryable().OrderByDescending(e => e.CreatedAt);

    /// <summary>获取允许排序的字段映射（属性名 -> Expression），子类可重写以扩展</summary>
    protected virtual Dictionary<string, Expression<Func<TEntity, object>>> GetSortableFields()
        => new(StringComparer.OrdinalIgnoreCase)
        {
            ["createdAt"] = e => e.CreatedAt,
            ["updatedAt"] = e => e.UpdatedAt,
            ["id"] = e => e.Id
        };

    /// <summary>应用排序，如果 sortBy 不在白名单中则使用默认排序</summary>
    protected IQueryable<TEntity> ApplySorting(IQueryable<TEntity> query, string? sortBy, string? sortOrder)
    {
        if (string.IsNullOrWhiteSpace(sortBy))
            return query;

        var sortableFields = GetSortableFields();
        if (!sortableFields.TryGetValue(sortBy, out var keySelector))
            return query;

        var isDescending = string.Equals(sortOrder, "desc", StringComparison.OrdinalIgnoreCase);

        // 需要先去掉 BuildPagedQuery 中的默认排序再应用新排序
        // 通过 Expression 方式排序，安全无注入风险
        return isDescending
            ? query.OrderByDescending(keySelector)
            : query.OrderBy(keySelector);
    }

    /// <summary>创建前验证，可重写以添加业务校验</summary>
    protected virtual Task ValidateCreateRequestAsync(TCreateRequest request) => Task.CompletedTask;

    /// <summary>更新前验证，可重写以添加业务校验</summary>
    protected virtual Task ValidateUpdateRequestAsync(long id, TUpdateRequest request) => Task.CompletedTask;

    /// <summary>删除前验证，可重写以添加业务校验</summary>
    protected virtual Task ValidateDeleteAsync(long id) => Task.CompletedTask;

    /// <summary>创建前钩子</summary>
    protected virtual Task BeforeCreateAsync(TEntity entity, TCreateRequest request) => Task.CompletedTask;

    /// <summary>创建后钩子</summary>
    protected virtual Task AfterCreateAsync(TEntity entity, TCreateRequest request) => Task.CompletedTask;

    /// <summary>更新前钩子</summary>
    protected virtual Task BeforeUpdateAsync(TEntity entity, TUpdateRequest request) => Task.CompletedTask;

    /// <summary>更新后钩子</summary>
    protected virtual Task AfterUpdateAsync(TEntity entity, TUpdateRequest request) => Task.CompletedTask;

    /// <summary>删除前钩子</summary>
    protected virtual Task BeforeDeleteAsync(TEntity entity) => Task.CompletedTask;

    /// <summary>删除后钩子</summary>
    protected virtual Task AfterDeleteAsync(TEntity entity) => Task.CompletedTask;

    // ── 子类必须实现的抽象方法 ──────────────────────────────────

    /// <summary>将创建请求映射为实体</summary>
    protected abstract TEntity MapCreateRequestToEntity(TCreateRequest request);

    /// <summary>将更新请求映射到已有实体</summary>
    protected abstract void MapUpdateRequestToEntity(TUpdateRequest request, TEntity entity);

    // ── 私有辅助 ────────────────────────────────────────────────

    private async Task LogAuditAsync(string action, long entityId, object? oldValue, object? newValue)
    {
        try
        {
            var oldJson = oldValue != null ? System.Text.Json.JsonSerializer.Serialize(oldValue) : null;
            var newJson = newValue != null ? System.Text.Json.JsonSerializer.Serialize(newValue) : null;
            await AuditLogService.LogAsync(action, EntityTypeName, entityId, oldJson, newJson);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "记录审计日志失败");
        }
    }
}
