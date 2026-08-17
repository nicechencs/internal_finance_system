using FinanceApp.Application.Common;
using FinanceApp.Application.Extensions;
using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace FinanceApp.Application.Modules.TransactionProcessing.Services;

internal static class TransactionRequestFilterExtensions
{
    public static IQueryable<Transaction> ApplyRequestFilters(
        this IQueryable<Transaction> query,
        PageRequest request,
        Func<IQueryable<TagBinding>> tagBindingsFactory,
        ILogger? logger = null,
        bool logAppliedFilters = false)
    {
        if (request.StartDate.HasValue)
        {
            query = query.Where(t => t.TransactionDate >= request.StartDate.Value);
            LogFilter(logger, logAppliedFilters, "Applied start date filter: {StartDate}", request.StartDate.Value);
        }

        if (request.EndDate.HasValue)
        {
            var endOfDay = request.EndDate.Value.Date.AddDays(1);
            query = query.Where(t => t.TransactionDate < endOfDay);
            LogFilter(logger, logAppliedFilters, "Applied end date filter: {EndDate}", request.EndDate.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.TransactionType) && request.TransactionType != "all")
        {
            if (Enum.TryParse<TransactionType>(request.TransactionType, true, out var transactionType))
            {
                query = query.Where(t => t.TransactionType == transactionType);
                LogFilter(logger, logAppliedFilters, "Applied transaction type filter: {Type}", transactionType);
            }
        }

        if (request.AccountId.HasValue)
        {
            query = query.Where(t => t.AccountId == request.AccountId.Value);
            LogFilter(logger, logAppliedFilters, "Applied account filter: {AccountId}", request.AccountId.Value);
        }

        if (request.CategoryId.HasValue)
        {
            query = query.Where(t => t.CategoryId == request.CategoryId.Value);
            LogFilter(logger, logAppliedFilters, "Applied category filter: {CategoryId}", request.CategoryId.Value);
        }

        if (request.ProjectId.HasValue)
        {
            query = query.Where(t =>
                (t.ProjectId == request.ProjectId.Value && !t.IsAllocated) ||
                (t.IsAllocated && t.Allocations.Any(a => a.ProjectId == request.ProjectId.Value)));
            LogFilter(logger, logAppliedFilters, "Applied project filter: {ProjectId}", request.ProjectId.Value);
        }

        if (request.TagFilters != null && request.TagFilters.Count > 0)
        {
            query = query.ApplyTransactionTagFilters(tagBindingsFactory(), request.TagFilters);
        }

        var allocationStatuses = ParseAllocationStatuses(request.AllocationStatus);
        if (allocationStatuses.Count > 0)
        {
            query = query.Where(t => allocationStatuses.Contains(t.AllocationStatus));
            LogFilter(logger, logAppliedFilters, "Applied allocation status filter: {AllocationStatus}", request.AllocationStatus);
        }

        if (request.MinAmount.HasValue)
        {
            query = query.Where(t => t.Amount >= request.MinAmount.Value);
            LogFilter(logger, logAppliedFilters, "Applied min amount filter: {MinAmount}", request.MinAmount.Value);
        }

        if (request.MaxAmount.HasValue)
        {
            query = query.Where(t => t.Amount <= request.MaxAmount.Value);
            LogFilter(logger, logAppliedFilters, "Applied max amount filter: {MaxAmount}", request.MaxAmount.Value);
        }

        if (request.ExcludeTransfer == true)
        {
            query = query.Where(t => t.TransactionType != TransactionType.Transfer);
            LogFilter(logger, logAppliedFilters, "Applied exclude transfer filter: {ExcludeTransfer}", true);
        }

        return query;
    }

    internal static List<AllocationStatus> ParseAllocationStatuses(string? raw)
    {
        var statuses = new List<AllocationStatus>();
        if (string.IsNullOrWhiteSpace(raw))
        {
            return statuses;
        }

        foreach (var part in raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (Enum.TryParse<AllocationStatus>(part, true, out var status) && !statuses.Contains(status))
            {
                statuses.Add(status);
            }
        }

        return statuses;
    }

    private static void LogFilter<T>(ILogger? logger, bool logAppliedFilters, string message, T value)
    {
        if (!logAppliedFilters || logger == null)
        {
            return;
        }

        logger.LogDebug(message, value);
    }
}
