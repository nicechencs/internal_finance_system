using FinanceApp.Application.Common;
using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Enums;

namespace FinanceApp.Application.Extensions;

/// <summary>
/// 标签查询过滤扩展方法
/// </summary>
/// <remarks>
/// 所有方法接受 IQueryable&lt;TagBinding&gt; 参数而非 AppDbContext，
/// 避免 Application 层对 Infrastructure 层产生依赖（循环依赖）。
/// 调用方传入 dbContext.Set&lt;TagBinding&gt;() 即可。
/// </remarks>
public static class TagQueryExtensions
{
    /// <summary>
    /// 根据标签过滤组，提取符合条件的 OwnerId 集合（内部辅助方法）
    /// </summary>
    private static IQueryable<long> GetMatchingOwnerIds(
        IQueryable<TagBinding> tagBindings,
        TagFilterGroup filter)
    {
        var tagIds = filter.TagIds.Distinct().ToList();
        var scope = filter.Scope;

        var bindingsForScope = tagBindings
            .Where(b => b.OwnerType == scope && tagIds.Contains(b.TagId));

        if (filter.MatchMode == TagMatchMode.Or)
        {
            // OR：只要 owner 拥有 tagIds 中任意一个标签即满足
            return bindingsForScope.Select(b => b.OwnerId).Distinct();
        }
        else
        {
            // AND：owner 必须同时拥有所有指定标签
            var tagCount = tagIds.Count;
            return bindingsForScope
                .GroupBy(b => b.OwnerId)
                .Where(g => g.Select(b => b.TagId).Distinct().Count() == tagCount)
                .Select(g => g.Key);
        }
    }

    /// <summary>
    /// 对任意主数据实体（Project/Person/Customer/Supplier）应用单个标签组过滤。
    /// 过滤逻辑基于 entity.Id 与 TagBinding.OwnerId 匹配。
    /// </summary>
    /// <typeparam name="T">实体类型，必须继承 BaseEntity</typeparam>
    /// <param name="query">原始查询</param>
    /// <param name="tagBindings">TagBinding 查询源，通常为 dbContext.Set&lt;TagBinding&gt;()</param>
    /// <param name="filter">标签过滤组</param>
    public static IQueryable<T> ApplyTagFilter<T>(
        this IQueryable<T> query,
        IQueryable<TagBinding> tagBindings,
        TagFilterGroup filter) where T : BaseEntity
    {
        if (filter.TagIds.Count == 0)
            return query;

        var matchingOwnerIds = GetMatchingOwnerIds(tagBindings, filter);
        return query.Where(e => matchingOwnerIds.Contains(e.Id));
    }

    /// <summary>
    /// 对主数据列表（Project/Person/Customer/Supplier）应用单个标签组过滤。
    /// 语义同 ApplyTagFilter，提供更明确的命名供主数据场景使用。
    /// </summary>
    /// <typeparam name="T">实体类型，必须继承 BaseEntity</typeparam>
    /// <param name="query">原始查询</param>
    /// <param name="tagBindings">TagBinding 查询源，通常为 dbContext.Set&lt;TagBinding&gt;()</param>
    /// <param name="filter">标签过滤组</param>
    public static IQueryable<T> ApplyOwnerTagFilter<T>(
        this IQueryable<T> query,
        IQueryable<TagBinding> tagBindings,
        TagFilterGroup filter) where T : BaseEntity
    {
        return query.ApplyTagFilter(tagBindings, filter);
    }

    /// <summary>
    /// 对主数据列表应用同一 Scope 下的多组标签过滤，组间关系为 AND。
    /// </summary>
    public static IQueryable<T> ApplyOwnerTagFilters<T>(
        this IQueryable<T> query,
        IQueryable<TagBinding> tagBindings,
        IEnumerable<TagFilterGroup>? filters,
        TagScope scope) where T : BaseEntity
    {
        if (filters == null)
            return query;

        foreach (var filter in filters.Where(f => f.Scope == scope && f.TagIds.Count > 0))
        {
            query = query.ApplyOwnerTagFilter(tagBindings, filter);
        }

        return query;
    }

    /// <summary>
    /// 对交易列表应用多组标签过滤，组间关系为 AND（每组都必须满足）。
    /// 支持五种 Scope：
    /// <list type="bullet">
    ///   <item>Transaction — 匹配 Transaction.Id</item>
    ///   <item>Project — 匹配 Transaction.ProjectId 或 Allocations 中任意分摊的 ProjectId</item>
    ///   <item>Person — 匹配 Transaction.PersonId 或 Allocations 中任意分摊的 PersonId</item>
    ///   <item>Customer — 匹配 Transaction.CustomerId</item>
    ///   <item>Supplier — 匹配 Transaction.SupplierId</item>
    /// </list>
    /// </summary>
    /// <param name="query">交易查询</param>
    /// <param name="tagBindings">TagBinding 查询源，通常为 dbContext.Set&lt;TagBinding&gt;()</param>
    /// <param name="filters">标签过滤组列表，为空或 null 时直接返回原查询</param>
    public static IQueryable<Transaction> ApplyTransactionTagFilters(
        this IQueryable<Transaction> query,
        IQueryable<TagBinding> tagBindings,
        List<TagFilterGroup>? filters)
    {
        if (filters == null || filters.Count == 0)
            return query;

        foreach (var filter in filters)
        {
            if (filter.TagIds.Count == 0)
                continue;

            switch (filter.Scope)
            {
                case TagScope.Transaction:
                {
                    // 直接匹配交易本身的 Id
                    var transactionIds = GetMatchingOwnerIds(tagBindings, filter);
                    query = query.Where(t => transactionIds.Contains(t.Id));
                    break;
                }

                case TagScope.Project:
                {
                    // 两条路径：
                    // 1. Transaction.ProjectId（直接关联）
                    // 2. Transaction.Allocations 中任意分摊记录的 ProjectId（分摊关联）
                    var projectIds = GetMatchingOwnerIds(tagBindings, filter);
                    query = query.Where(t =>
                        (t.ProjectId.HasValue && !t.IsAllocated && projectIds.Contains(t.ProjectId.Value)) ||
                        (t.IsAllocated && t.Allocations.Any(a => a.ProjectId.HasValue && projectIds.Contains(a.ProjectId.Value))));
                    break;
                }

                case TagScope.Person:
                {
                    // 两条路径：
                    // 1. Transaction.PersonId（直接关联）
                    // 2. Transaction.Allocations 中任意分摊记录的 PersonId（分摊关联）
                    var personIds = GetMatchingOwnerIds(tagBindings, filter);
                    query = query.Where(t =>
                        (t.PersonId.HasValue && !t.IsAllocated && personIds.Contains(t.PersonId.Value)) ||
                        (t.IsAllocated && t.Allocations.Any(a => a.PersonId.HasValue && personIds.Contains(a.PersonId.Value))));
                    break;
                }

                case TagScope.Customer:
                {
                    // 仅直接关联路径
                    var customerIds = GetMatchingOwnerIds(tagBindings, filter);
                    query = query.Where(t =>
                        t.CustomerId.HasValue && customerIds.Contains(t.CustomerId.Value));
                    break;
                }

                case TagScope.Supplier:
                {
                    // 仅直接关联路径
                    var supplierIds = GetMatchingOwnerIds(tagBindings, filter);
                    query = query.Where(t =>
                        t.SupplierId.HasValue && supplierIds.Contains(t.SupplierId.Value));
                    break;
                }
            }
        }

        return query;
    }
}
