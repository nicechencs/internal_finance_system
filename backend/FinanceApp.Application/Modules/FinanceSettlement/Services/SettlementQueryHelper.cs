using System.Linq.Expressions;
using FinanceApp.Application.Common;
using FinanceApp.Application.Extensions;
using FinanceApp.Application.Modules.MasterData.DTOs.Tag;
using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Enums;

namespace FinanceApp.Application.Modules.FinanceSettlement.Services;

/// <summary>
/// 应收/应付共用的查询筛选、状态计算和标签装配。
/// 只提取两边规则完全一致的片段；项目是否必填、对方类型校验、类型校验等差异仍留在各自服务中。
/// </summary>
internal static class SettlementQueryHelper
{
    public static readonly string[] AgingCategories = ["未到期", "1-30天", "31-60天", "61-90天", "90天以上"];

    /// <summary>
    /// 按到期日闭区间筛选。[start, end] 的 end 取当天结束（次日零点之前）。
    /// </summary>
    public static IQueryable<T> ApplyDueDateRange<T>(
        IQueryable<T> query,
        DateTime? startDate,
        DateTime? endDate,
        Expression<Func<T, DateTime?>> dueDate)
    {
        if (startDate.HasValue)
        {
            query = query.Where(CompareDueDate(dueDate, startDate.Value, greaterOrEqual: true));
        }

        if (endDate.HasValue)
        {
            var endOfDay = endDate.Value.Date.AddDays(1);
            query = query.Where(CompareDueDate(dueDate, endOfDay, greaterOrEqual: false));
        }

        return query;
    }

    public static IQueryable<T> ApplyOptionalIdEquals<T>(
        IQueryable<T> query,
        long? id,
        Expression<Func<T, long?>> idSelector)
    {
        if (!id.HasValue)
        {
            return query;
        }

        var value = id.Value;
        var parameter = idSelector.Parameters[0];
        var body = Expression.Equal(idSelector.Body, Expression.Convert(Expression.Constant(value, typeof(long)), typeof(long?)));
        return query.Where(Expression.Lambda<Func<T, bool>>(body, parameter));
    }

    public static IQueryable<T> ApplyStatusFilter<T, TStatus>(
        IQueryable<T> query,
        string? status,
        Expression<Func<T, TStatus>> statusSelector)
        where TStatus : struct, Enum
    {
        if (string.IsNullOrEmpty(status) || !Enum.TryParse<TStatus>(status, true, out var parsed))
        {
            return query;
        }

        var parameter = statusSelector.Parameters[0];
        var body = Expression.Equal(statusSelector.Body, Expression.Constant(parsed));
        return query.Where(Expression.Lambda<Func<T, bool>>(body, parameter));
    }

    public static IQueryable<T> ApplyTagFilter<T>(
        IQueryable<T> query,
        IQueryable<TagBinding> tagBindings,
        IEnumerable<TagFilterGroup>? filters,
        TagScope scope) where T : BaseEntity
    {
        if (filters == null)
        {
            return query;
        }

        var filterList = filters as ICollection<TagFilterGroup> ?? filters.ToList();
        if (filterList.Count == 0)
        {
            return query;
        }

        return query.ApplyOwnerTagFilters(tagBindings, filterList, scope);
    }

    /// <summary>
    /// 收款/付款后的状态：剩余为 0 则结清并写入结清时间；已收/已付大于 0 则为部分；否则保持原状态。
    /// </summary>
    public static (TStatus Status, DateTime? SettledAt) ResolvePaymentStatus<TStatus>(
        decimal remainingAmount,
        decimal settledAmount,
        TStatus currentStatus,
        DateTime? currentSettledAt,
        TStatus settledStatus,
        TStatus partialStatus,
        DateTime utcNow)
        where TStatus : struct, Enum
    {
        if (remainingAmount == 0)
        {
            return (settledStatus, utcNow);
        }

        if (settledAmount > 0)
        {
            return (partialStatus, currentSettledAt);
        }

        return (currentStatus, currentSettledAt);
    }

    public static decimal[] BucketAgingAmounts(
        IEnumerable<(DateTime DueDate, decimal RemainingAmount)> unsettled,
        DateTime today)
    {
        var items = unsettled as IList<(DateTime DueDate, decimal RemainingAmount)>
            ?? unsettled.ToList();

        decimal Sum(Func<(DateTime DueDate, decimal RemainingAmount), bool> predicate)
            => items.Where(predicate).Sum(item => item.RemainingAmount);

        var notDue = Sum(item => item.DueDate.Date >= today);
        var days1To30 = Sum(item => item.DueDate.Date < today && item.DueDate.Date >= today.AddDays(-30));
        var days31To60 = Sum(item => item.DueDate.Date < today.AddDays(-30) && item.DueDate.Date >= today.AddDays(-60));
        var days61To90 = Sum(item => item.DueDate.Date < today.AddDays(-60) && item.DueDate.Date >= today.AddDays(-90));
        var days90Plus = Sum(item => item.DueDate.Date < today.AddDays(-90));

        return [notDue, days1To30, days31To60, days61To90, days90Plus];
    }

    public static (List<string> Months, List<decimal> Amounts) BuildMonthlyTrend(
        IEnumerable<(DateTime CreatedAt, decimal Amount)> rows,
        DateTime start,
        DateTime end)
    {
        var grouped = rows
            .GroupBy(row => new { row.CreatedAt.Year, row.CreatedAt.Month })
            .ToDictionary(group => $"{group.Key.Year}-{group.Key.Month:D2}", group => group.Sum(row => row.Amount));

        var months = new List<string>();
        var amounts = new List<decimal>();

        var current = new DateTime(start.Year, start.Month, 1);
        var endMonth = new DateTime(end.Year, end.Month, 1);
        while (current <= endMonth)
        {
            var key = $"{current.Year}-{current.Month:D2}";
            months.Add(key);
            amounts.Add(grouped.TryGetValue(key, out var amount) ? amount : 0m);
            current = current.AddMonths(1);
        }

        return (months, amounts);
    }

    public static (DateTime Start, DateTime End) ResolveTrendRange(DateTime? startDate, DateTime? endDate, DateTime utcNow)
    {
        var end = (endDate ?? utcNow).Date;
        var start = (startDate ?? end.AddMonths(-6).AddDays(1 - end.AddMonths(-6).Day)).Date;
        return (start, end);
    }

    public static Task ApplyTagsAsync<TDto>(
        IQueryable<TagBinding>? tagBindings,
        TagScope scope,
        IEnumerable<TDto> items,
        Func<TDto, long> ownerIdSelector,
        Action<TDto, List<TagItemDto>> tagSetter)
        => tagBindings.ApplyTagsAsync(scope, items, ownerIdSelector, tagSetter);

    public static Task ApplyTagAsync<TDto>(
        IQueryable<TagBinding>? tagBindings,
        TagScope scope,
        TDto item,
        Func<TDto, long> ownerIdSelector,
        Action<TDto, List<TagItemDto>> tagSetter)
        => tagBindings.ApplyTagAsync(scope, item, ownerIdSelector, tagSetter);

    private static Expression<Func<T, bool>> CompareDueDate<T>(
        Expression<Func<T, DateTime?>> dueDate,
        DateTime boundary,
        bool greaterOrEqual)
    {
        var parameter = dueDate.Parameters[0];
        var comparison = greaterOrEqual
            ? Expression.GreaterThanOrEqual(dueDate.Body, Expression.Constant(boundary, typeof(DateTime?)))
            : Expression.LessThan(dueDate.Body, Expression.Constant(boundary, typeof(DateTime?)));
        return Expression.Lambda<Func<T, bool>>(comparison, parameter);
    }
}
