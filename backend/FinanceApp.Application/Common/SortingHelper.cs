using System.Linq.Expressions;
using FinanceApp.Domain.Entities;

namespace FinanceApp.Application.Common;

/// <summary>
/// 通用排序辅助类，基于白名单的安全排序实现
/// </summary>
public static class SortingHelper
{
    /// <summary>
    /// 对查询应用排序。如果 sortBy 为空或不在白名单中，返回原查询（不改变排序）。
    /// 调用方应在调用前设置好默认排序。
    /// </summary>
    public static IQueryable<T> ApplySorting<T>(
        IQueryable<T> query,
        string? sortBy,
        string? sortOrder,
        Dictionary<string, Expression<Func<T, object>>> sortableFields) where T : class
    {
        if (string.IsNullOrWhiteSpace(sortBy))
            return query;

        if (!sortableFields.TryGetValue(sortBy, out var keySelector))
            return query;

        var isDescending = string.Equals(sortOrder, "desc", StringComparison.OrdinalIgnoreCase);

        return isDescending
            ? query.OrderByDescending(keySelector)
            : query.OrderBy(keySelector);
    }

    /// <summary>
    /// BaseEntity 通用可排序字段
    /// </summary>
    public static Dictionary<string, Expression<Func<T, object>>> GetBaseFields<T>() where T : BaseEntity
        => new(StringComparer.OrdinalIgnoreCase)
        {
            ["createdAt"] = e => e.CreatedAt,
            ["id"] = e => e.Id
        };

    /// <summary>
    /// 合并多个字段字典
    /// </summary>
    public static Dictionary<string, Expression<Func<T, object>>> Merge<T>(
        params Dictionary<string, Expression<Func<T, object>>>[] dicts) where T : class
    {
        var result = new Dictionary<string, Expression<Func<T, object>>>(StringComparer.OrdinalIgnoreCase);
        foreach (var dict in dicts)
        {
            foreach (var kv in dict)
            {
                result[kv.Key] = kv.Value;
            }
        }
        return result;
    }
}
