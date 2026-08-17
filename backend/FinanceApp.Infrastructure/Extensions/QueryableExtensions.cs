using Microsoft.EntityFrameworkCore;
using FinanceApp.Domain.Entities;

namespace FinanceApp.Infrastructure.Extensions;

/// <summary>
/// EF Core 查询扩展方法
/// </summary>
public static class QueryableExtensions
{
    /// <summary>为 Transaction 查询包含所有导航属性</summary>
    public static IQueryable<Transaction> IncludeAll(this IQueryable<Transaction> query)
    {
        return query
            .Include(t => t.BankTransaction)
            .Include(t => t.Category)
            .Include(t => t.Account)
            .Include(t => t.Project)
            .Include(t => t.Customer)
            .Include(t => t.Supplier)
            .Include(t => t.Person)
            .Include(t => t.CreatedByUser)
            .Include(t => t.Allocations);
    }

    /// <summary>为 Transaction 查询包含基本导航属性（不含 Allocations）</summary>
    public static IQueryable<Transaction> IncludeBasic(this IQueryable<Transaction> query)
    {
        return query
            .Include(t => t.Category)
            .Include(t => t.Account)
            .Include(t => t.Project)
            .Include(t => t.Customer)
            .Include(t => t.Supplier)
            .Include(t => t.Person);
    }

    /// <summary>为 Payable 查询包含所有导航属性</summary>
    public static IQueryable<Payable> IncludeAll(this IQueryable<Payable> query)
    {
        return query
            .Include(p => p.Supplier)
            .Include(p => p.Project)
            .Include(p => p.Details);
    }

    /// <summary>为 Payable 查询包含基本导航属性（不含 Details）</summary>
    public static IQueryable<Payable> IncludeBasic(this IQueryable<Payable> query)
    {
        return query
            .Include(p => p.Supplier)
            .Include(p => p.Project);
    }

    /// <summary>为 Receivable 查询包含所有导航属性</summary>
    public static IQueryable<Receivable> IncludeAll(this IQueryable<Receivable> query)
    {
        return query
            .Include(r => r.Project)
            .Include(r => r.Customer)
            .Include(r => r.Details);
    }

    /// <summary>为 Receivable 查询包含基本导航属性（不含 Details）</summary>
    public static IQueryable<Receivable> IncludeBasic(this IQueryable<Receivable> query)
    {
        return query
            .Include(r => r.Project)
            .Include(r => r.Customer);
    }

    /// <summary>为 Project 查询包含导航属性</summary>
    public static IQueryable<Project> IncludeAll(this IQueryable<Project> query)
    {
        return query.Include(p => p.Customer);
    }

    /// <summary>
    /// 过滤未删除的记录
    /// </summary>
    /// <typeparam name="T">实体类型</typeparam>
    /// <param name="query">查询</param>
    /// <returns>过滤后的查询</returns>
    public static IQueryable<T> WhereNotDeleted<T>(this IQueryable<T> query) where T : BaseEntity
    {
        return query.Where(e => !e.IsDeleted);
    }

    /// <summary>
    /// 分页查询
    /// </summary>
    /// <typeparam name="T">实体类型</typeparam>
    /// <param name="query">查询</param>
    /// <param name="page">页码（从 1 开始）</param>
    /// <param name="pageSize">每页大小</param>
    /// <returns>分页后的查询</returns>
    public static IQueryable<T> Paginate<T>(this IQueryable<T> query, int page, int pageSize)
    {
        return query
            .Skip((page - 1) * pageSize)
            .Take(pageSize);
    }

    /// <summary>
    /// 按创建时间降序排序
    /// </summary>
    /// <typeparam name="T">实体类型</typeparam>
    /// <param name="query">查询</param>
    /// <returns>排序后的查询</returns>
    public static IQueryable<T> OrderByCreatedDesc<T>(this IQueryable<T> query) where T : BaseEntity
    {
        return query.OrderByDescending(e => e.CreatedAt);
    }

    /// <summary>
    /// 按更新时间降序排序
    /// </summary>
    /// <typeparam name="T">实体类型</typeparam>
    /// <param name="query">查询</param>
    /// <returns>排序后的查询</returns>
    public static IQueryable<T> OrderByUpdatedDesc<T>(this IQueryable<T> query) where T : BaseEntity
    {
        return query.OrderByDescending(e => e.UpdatedAt);
    }
}