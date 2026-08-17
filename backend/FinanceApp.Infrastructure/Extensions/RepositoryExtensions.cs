using Microsoft.EntityFrameworkCore;
using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Interfaces;

namespace FinanceApp.Infrastructure.Extensions;

/// <summary>
/// Repository 扩展方法
/// </summary>
public static class RepositoryExtensions
{
    /// <summary>根据 ID 获取实体，不存在则抛出 <see cref="KeyNotFoundException"/></summary>
    public static async Task<T> GetOrThrowAsync<T>(
        this IRepository<T> repository,
        long id,
        string? entityName = null) where T : BaseEntity
    {
        var entity = await repository.GetByIdAsync(id);
        if (entity == null)
        {
            var name = entityName ?? typeof(T).Name;
            throw new KeyNotFoundException($"{name} with ID {id} not found.");
        }
        return entity;
    }

    /// <summary>根据 ID 获取实体（包含导航属性），不存在则抛出异常</summary>
    public static async Task<T> GetOrThrowAsync<T>(
        this IRepository<T> repository,
        long id,
        Func<IQueryable<T>, IQueryable<T>> includeFunc,
        string? entityName = null) where T : BaseEntity
    {
        var query = repository.GetQueryable().WhereNotDeleted();
        query = includeFunc(query);
        var entity = await query.FirstOrDefaultAsync(e => e.Id == id);

        if (entity == null)
        {
            var name = entityName ?? typeof(T).Name;
            throw new KeyNotFoundException($"{name} with ID {id} not found.");
        }
        return entity;
    }

    /// <summary>批量添加实体</summary>
    public static async Task AddRangeAsync<T>(
        this IRepository<T> repository,
        IEnumerable<T> entities) where T : BaseEntity
    {
        foreach (var entity in entities)
        {
            await repository.AddAsync(entity);
        }
    }

    /// <summary>批量更新实体</summary>
    public static void UpdateRange<T>(
        this IRepository<T> repository,
        IEnumerable<T> entities) where T : BaseEntity
    {
        foreach (var entity in entities)
        {
            repository.Update(entity);
        }
    }

    /// <summary>批量软删除实体</summary>
    public static void DeleteRange<T>(
        this IRepository<T> repository,
        IEnumerable<long> ids) where T : BaseEntity
    {
        foreach (var id in ids)
        {
            repository.Delete(id);
        }
    }

    /// <summary>检查实体是否存在且未删除</summary>
    public static async Task<bool> ExistsAndNotDeletedAsync<T>(
        this IRepository<T> repository,
        long id) where T : BaseEntity
    {
        var entity = await repository.GetByIdAsync(id);
        return entity != null && !entity.IsDeleted;
    }

    /// <summary>获取所有未删除的实体</summary>
    public static async Task<List<T>> GetAllNotDeletedAsync<T>(
        this IRepository<T> repository) where T : BaseEntity
    {
        return await repository.GetQueryable()
            .WhereNotDeleted()
            .ToListAsync();
    }

    /// <summary>获取分页数据（仅未删除的记录）</summary>
    public static async Task<(List<T> Items, int Total)> GetPagedNotDeletedAsync<T>(
        this IRepository<T> repository,
        int page,
        int pageSize) where T : BaseEntity
    {
        var query = repository.GetQueryable().WhereNotDeleted();
        var total = await query.CountAsync();
        var items = await query
            .Paginate(page, pageSize)
            .ToListAsync();

        return (items, total);
    }
}