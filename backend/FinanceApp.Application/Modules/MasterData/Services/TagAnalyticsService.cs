using FinanceApp.Application.Common;
using FinanceApp.Application.Extensions;
using FinanceApp.Application.Modules.Identity.Interfaces;
using FinanceApp.Application.Modules.MasterData.DTOs.Tag.Analytics;
using FinanceApp.Application.Modules.MasterData.Interfaces;
using FinanceApp.Application.Services;
using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Enums;
using FinanceApp.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FinanceApp.Application.Modules.MasterData.Services;

public class TagAnalyticsService : ServiceBase, ITagAnalyticsService
{
    private readonly IRepository<Tag> _tagRepository;
    private readonly IRepository<TagBinding> _tagBindingRepository;
    private readonly IRepository<Transaction> _transactionRepository;
    private readonly IRepository<TagDailySummary> _tagDailySummaryRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<TagAnalyticsService> _logger;

    public TagAnalyticsService(
        IRepository<Tag> tagRepository,
        IRepository<TagBinding> tagBindingRepository,
        IRepository<Transaction> transactionRepository,
        IRepository<TagDailySummary> tagDailySummaryRepository,
        IUnitOfWork unitOfWork,
        ILogger<TagAnalyticsService> logger,
        ICurrentUserService currentUserService,
        IDataPermissionService permissionService)
        : base(currentUserService, permissionService)
    {
        _tagRepository = tagRepository;
        _tagBindingRepository = tagBindingRepository;
        _transactionRepository = transactionRepository;
        _tagDailySummaryRepository = tagDailySummaryRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<TagSummaryDto> GetTagSummaryAsync(string scope, DateTime? dateFrom = null, DateTime? dateTo = null)
    {
        _logger.LogDebug("TagAnalyticsService.GetTagSummaryAsync - Scope={Scope}, DateFrom={DateFrom}, DateTo={DateTo}",
            scope, dateFrom, dateTo);

        var sw = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            if (!Enum.TryParse<TagScope>(scope, true, out var tagScope))
                throw new ValidationException($"无效的标签范围: {scope}");

            if (tagScope == TagScope.Receivable || tagScope == TagScope.Payable)
                throw new ValidationException($"暂不支持对 {scope} 作用域进行交易维度分析");

            var tags = await _tagRepository.GetQueryable()
                .Where(t => t.Scope == tagScope && t.IsActive)
                .OrderBy(t => t.SortOrder).ThenBy(t => t.Name)
                .ToListAsync();

            if (tags.Count == 0)
            {
                _logger.LogWarning("TagAnalyticsService.GetTagSummaryAsync - 未找到对应范围的活动标签: Scope={Scope}", scope);
                return new TagSummaryDto { Scope = scope.ToLower(), DateFrom = dateFrom, DateTo = dateTo };
            }

            var tagBindings = _tagBindingRepository.GetQueryable();
            var tagIds = tags.Select(t => t.Id).ToList();
            var startInclusive = dateFrom?.Date;
            var endExclusive = dateTo?.Date.AddDays(1);

            // 构建基础交易查询（应用权限过滤 + 日期过滤），排除划转
            var baseQuery = ApplyPermissionFilter(_transactionRepository.GetQueryable())
                .Where(t => t.TransactionType != TransactionType.Transfer);

            if (startInclusive.HasValue)
                baseQuery = baseQuery.Where(t => t.TransactionDate >= startInclusive.Value);
            if (endExclusive.HasValue)
                baseQuery = baseQuery.Where(t => t.TransactionDate < endExclusive.Value);

            // 计算总体（不含标签过滤）
            var allData = await baseQuery
                .Select(t => new { t.TransactionType, t.Amount })
                .ToListAsync();

            var totalIncome = allData.Where(t => t.TransactionType == TransactionType.Income).Sum(t => t.Amount);
            var totalExpense = allData.Where(t => t.TransactionType == TransactionType.Expense).Sum(t => t.Amount);
            var totalCount = allData.Count;

            // ── 单次 JOIN + GROUP BY 取得各标签的聚合数据，替代原来的 N+1 循环查询 ──
            // 通用路径：对 Transaction scope 直接通过 tag_bindings.owner_id = transaction.id 关联
            // 对 Project/Person/Customer/Supplier scope：先通过 TagQueryExtensions 取交易 ID 集合，
            //   再在内存中按 TagId 分组汇总（避免复杂的多路径 JOIN 难以在 EF 中表达）
            Dictionary<long, (int Count, decimal Income, decimal Expense)> tagStats;

            if (tagScope == TagScope.Transaction)
            {
                // 最优路径：可完全在数据库端完成 JOIN + GROUP BY
                var rawStats = await tagBindings
                    .Where(b => b.OwnerType == tagScope && tagIds.Contains(b.TagId))
                    .Join(baseQuery, b => b.OwnerId, t => t.Id,
                        (b, t) => new { b.TagId, t.TransactionType, t.Amount })
                    .GroupBy(x => x.TagId)
                    .Select(g => new
                    {
                        TagId = g.Key,
                        Count = g.Count(),
                        Income = g.Where(x => x.TransactionType == TransactionType.Income).Sum(x => x.Amount),
                        Expense = g.Where(x => x.TransactionType == TransactionType.Expense).Sum(x => x.Amount)
                    })
                    .ToListAsync();

                tagStats = rawStats.ToDictionary(
                    x => x.TagId,
                    x => (x.Count, x.Income, x.Expense));
            }
            else
            {
                // Project / Person / Customer / Supplier：
                // 对每个 tagId 应用 ApplyTransactionTagFilters（已有 direct+allocation 双路径）
                // 但一次性取出所有 tag 对应的交易 ID 集合，然后在内存汇总，避免每个 tag 独立查询
                // 注：此处仍需逐 tag 构建查询，但通过 ID 集合方式减少每次查询返回数据量
                var txData = await baseQuery
                    .Select(t => new { t.Id, t.TransactionType, t.Amount })
                    .ToListAsync();
                var txLookup = txData.ToDictionary(t => t.Id);

                tagStats = new Dictionary<long, (int Count, decimal Income, decimal Expense)>();
                foreach (var tag in tags)
                {
                    var filter = new TagFilterGroup
                    {
                        Scope = tagScope,
                        TagIds = new List<long> { tag.Id },
                        MatchMode = TagMatchMode.Or
                    };
                    var matchedIds = await baseQuery
                        .ApplyTransactionTagFilters(tagBindings, new List<TagFilterGroup> { filter })
                        .Select(t => t.Id)
                        .ToListAsync();

                    var income = matchedIds
                        .Where(id => txLookup.TryGetValue(id, out var tx) && tx.TransactionType == TransactionType.Income)
                        .Sum(id => txLookup[id].Amount);
                    var expense = matchedIds
                        .Where(id => txLookup.TryGetValue(id, out var tx) && tx.TransactionType == TransactionType.Expense)
                        .Sum(id => txLookup[id].Amount);
                    tagStats[tag.Id] = (matchedIds.Count, income, expense);
                }
            }

            var items = tags.Select(tag =>
            {
                var (count, income, expense) = tagStats.GetValueOrDefault(tag.Id);
                return new TagSummaryItemDto
                {
                    TagId = tag.Id,
                    TagName = tag.Name,
                    TagColor = tag.Color,
                    TransactionCount = count,
                    IncomeAmount = income,
                    ExpenseAmount = expense,
                    NetAmount = income - expense,
                    IncomePercentage = totalIncome > 0 ? Math.Round(income / totalIncome * 100, 2) : 0,
                    ExpensePercentage = totalExpense > 0 ? Math.Round(expense / totalExpense * 100, 2) : 0
                };
            }).ToList();

            sw.Stop();
            _logger.LogInformation(
                "获取标签汇总数据成功: Scope={Scope}, Tags={TagCount}, Transactions={TotalTx}, 耗时={ElapsedMs}ms",
                scope, tags.Count, totalCount, sw.ElapsedMilliseconds);

            return new TagSummaryDto
            {
                Scope = scope.ToLower(),
                DateFrom = dateFrom,
                DateTo = dateTo,
                TotalTransactionCount = totalCount,
                TotalIncomeAmount = totalIncome,
                TotalExpenseAmount = totalExpense,
                TotalNetAmount = totalIncome - totalExpense,
                Items = items
            };
        }
        catch (ValidationException) { throw; }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取标签汇总数据失败: Scope={Scope}", scope);
            throw;
        }
    }

    public async Task<TagCrossAnalysisDto> GetTagCrossAnalysisAsync(
        string rowScope, string colScope, DateTime? dateFrom = null, DateTime? dateTo = null)
    {
        _logger.LogDebug("TagAnalyticsService.GetTagCrossAnalysisAsync - RowScope={RowScope}, ColScope={ColScope}",
            rowScope, colScope);

        var sw = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            if (!Enum.TryParse<TagScope>(rowScope, true, out var rowTagScope))
                throw new ValidationException($"无效的行标签范围: {rowScope}");
            if (!Enum.TryParse<TagScope>(colScope, true, out var colTagScope))
                throw new ValidationException($"无效的列标签范围: {colScope}");

            if (rowTagScope == TagScope.Receivable || rowTagScope == TagScope.Payable ||
                colTagScope == TagScope.Receivable || colTagScope == TagScope.Payable)
                throw new ValidationException("暂不支持对 Receivable/Payable 作用域进行交叉分析");

            if (rowTagScope == colTagScope)
                throw new ValidationException("行维度与列维度不能相同");

            var rowTags = await _tagRepository.GetQueryable()
                .Where(t => t.Scope == rowTagScope && t.IsActive)
                .OrderBy(t => t.SortOrder).ThenBy(t => t.Name)
                .ToListAsync();

            var colTags = await _tagRepository.GetQueryable()
                .Where(t => t.Scope == colTagScope && t.IsActive)
                .OrderBy(t => t.SortOrder).ThenBy(t => t.Name)
                .ToListAsync();

            if (rowTags.Count == 0 || colTags.Count == 0)
            {
                _logger.LogWarning("TagAnalyticsService.GetTagCrossAnalysisAsync - 行或列无可用标签: RowScope={RowScope}, ColScope={ColScope}", rowScope, colScope);
                return new TagCrossAnalysisDto
                {
                    RowScope = rowScope.ToLower(), ColScope = colScope.ToLower(),
                    DateFrom = dateFrom, DateTo = dateTo,
                    RowTags = rowTags.Select(tag => new TagSummaryItemDto
                    {
                        TagId = tag.Id,
                        TagName = tag.Name,
                        TagColor = tag.Color
                    }).ToList(),
                    ColTags = colTags.Select(tag => new TagSummaryItemDto
                    {
                        TagId = tag.Id,
                        TagName = tag.Name,
                        TagColor = tag.Color
                    }).ToList(),
                    Cells = new List<TagCrossAnalysisCellDto>()
                };
            }

            var tagBindings = _tagBindingRepository.GetQueryable();
            var startInclusive = dateFrom?.Date;
            var endExclusive = dateTo?.Date.AddDays(1);

            var baseQuery = ApplyPermissionFilter(_transactionRepository.GetQueryable())
                .Where(t => t.TransactionType != TransactionType.Transfer);

            if (startInclusive.HasValue)
                baseQuery = baseQuery.Where(t => t.TransactionDate >= startInclusive.Value);
            if (endExclusive.HasValue)
                baseQuery = baseQuery.Where(t => t.TransactionDate < endExclusive.Value);

            // 预取所有交易的 Id + TransactionType + Amount
            var allTransactions = await baseQuery
                .Select(t => new { t.Id, t.TransactionType, t.Amount })
                .ToListAsync();

            var txDict = allTransactions.ToDictionary(t => t.Id);

            // 为每个 rowTag 预取匹配的交易 ID 集合
            var rowTagTxIds = new Dictionary<long, HashSet<long>>();
            foreach (var tag in rowTags)
            {
                var filter = new TagFilterGroup { Scope = rowTagScope, TagIds = new List<long> { tag.Id }, MatchMode = TagMatchMode.Or };
                var ids = await baseQuery.ApplyTransactionTagFilters(tagBindings, new List<TagFilterGroup> { filter })
                    .Select(t => t.Id).ToListAsync();
                rowTagTxIds[tag.Id] = ids.ToHashSet();
            }

            // 为每个 colTag 预取匹配的交易 ID 集合
            var colTagTxIds = new Dictionary<long, HashSet<long>>();
            foreach (var tag in colTags)
            {
                var filter = new TagFilterGroup { Scope = colTagScope, TagIds = new List<long> { tag.Id }, MatchMode = TagMatchMode.Or };
                var ids = await baseQuery.ApplyTransactionTagFilters(tagBindings, new List<TagFilterGroup> { filter })
                    .Select(t => t.Id).ToListAsync();
                colTagTxIds[tag.Id] = ids.ToHashSet();
            }

            // 构建 rowTag 汇总
            var rowTagSummaries = rowTags.Select(tag =>
            {
                var ids = rowTagTxIds.GetValueOrDefault(tag.Id, new HashSet<long>());
                var income = ids.Where(id => txDict.ContainsKey(id) && txDict[id].TransactionType == TransactionType.Income).Sum(id => txDict[id].Amount);
                var expense = ids.Where(id => txDict.ContainsKey(id) && txDict[id].TransactionType == TransactionType.Expense).Sum(id => txDict[id].Amount);
                return new TagSummaryItemDto
                {
                    TagId = tag.Id, TagName = tag.Name, TagColor = tag.Color,
                    TransactionCount = ids.Count, IncomeAmount = income, ExpenseAmount = expense, NetAmount = income - expense
                };
            }).ToList();

            // 构建 colTag 汇总
            var colTagSummaries = colTags.Select(tag =>
            {
                var ids = colTagTxIds.GetValueOrDefault(tag.Id, new HashSet<long>());
                var income = ids.Where(id => txDict.ContainsKey(id) && txDict[id].TransactionType == TransactionType.Income).Sum(id => txDict[id].Amount);
                var expense = ids.Where(id => txDict.ContainsKey(id) && txDict[id].TransactionType == TransactionType.Expense).Sum(id => txDict[id].Amount);
                return new TagSummaryItemDto
                {
                    TagId = tag.Id, TagName = tag.Name, TagColor = tag.Color,
                    TransactionCount = ids.Count, IncomeAmount = income, ExpenseAmount = expense, NetAmount = income - expense
                };
            }).ToList();

            // 构建交叉矩阵（稀疏）
            var cells = new List<TagCrossAnalysisCellDto>();
            foreach (var rowTag in rowTags)
            {
                var rowIds = rowTagTxIds.GetValueOrDefault(rowTag.Id, new HashSet<long>());
                if (rowIds.Count == 0) continue;

                foreach (var colTag in colTags)
                {
                    var colIds = colTagTxIds.GetValueOrDefault(colTag.Id, new HashSet<long>());
                    var intersection = rowIds.Intersect(colIds).ToList();
                    if (intersection.Count == 0) continue;

                    var income = intersection.Where(id => txDict.ContainsKey(id) && txDict[id].TransactionType == TransactionType.Income).Sum(id => txDict[id].Amount);
                    var expense = intersection.Where(id => txDict.ContainsKey(id) && txDict[id].TransactionType == TransactionType.Expense).Sum(id => txDict[id].Amount);

                    cells.Add(new TagCrossAnalysisCellDto
                    {
                        RowTagId = rowTag.Id, ColTagId = colTag.Id,
                        TransactionCount = intersection.Count,
                        IncomeAmount = income, ExpenseAmount = expense, NetAmount = income - expense
                    });
                }
            }

            sw.Stop();
            _logger.LogInformation(
                "获取标签交叉分析成功: RowScope={RowScope}, ColScope={ColScope}, Cells={CellCount}, 耗时={ElapsedMs}ms",
                rowScope, colScope, cells.Count, sw.ElapsedMilliseconds);

            return new TagCrossAnalysisDto
            {
                RowScope = rowScope.ToLower(), ColScope = colScope.ToLower(),
                DateFrom = dateFrom, DateTo = dateTo,
                RowTags = rowTagSummaries, ColTags = colTagSummaries, Cells = cells
            };
        }
        catch (ValidationException) { throw; }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取标签交叉分析失败: RowScope={RowScope}, ColScope={ColScope}", rowScope, colScope);
            throw;
        }
    }

    public async Task<TagDailySummaryRebuildResult> RebuildDailySummariesAsync(DateTime date)
    {
        var summaryDate = DateTime.SpecifyKind(date.Date, DateTimeKind.Utc);
        var endExclusive = summaryDate.AddDays(1);

        _logger.LogInformation("重建标签每日汇总快照: SummaryDate={SummaryDate}", summaryDate);

        var existing = await _tagDailySummaryRepository.GetQueryable()
            .IgnoreQueryFilters()
            .Where(s => s.SummaryDate >= summaryDate && s.SummaryDate < endExclusive)
            .ToListAsync();

        var nextVersion = existing.Count == 0 ? 1 : existing.Max(s => s.Version) + 1;

        await using var dbTransaction = await _unitOfWork.BeginTransactionAsync();
        try
        {
            foreach (var row in existing.Where(s => !s.IsDeleted))
            {
                _tagDailySummaryRepository.Delete(row);
            }

            var aggregates = await AggregateDailySummariesAsync(summaryDate, endExclusive);
            foreach (var row in aggregates)
            {
                row.Version = nextVersion;
                await _tagDailySummaryRepository.AddAsync(row);
            }

            await _unitOfWork.SaveChangesAsync();
            if (dbTransaction != null)
                await dbTransaction.CommitAsync();

            _logger.LogInformation(
                "标签每日汇总快照重建完成: SummaryDate={SummaryDate}, Rows={RowCount}, Version={Version}",
                summaryDate, aggregates.Count, nextVersion);

            return new TagDailySummaryRebuildResult
            {
                SummaryDate = summaryDate,
                RowCount = aggregates.Count,
                Version = nextVersion
            };
        }
        catch (Exception ex)
        {
            if (dbTransaction != null)
                await dbTransaction.RollbackAsync();
            _logger.LogError(ex, "重建标签每日汇总快照失败: SummaryDate={SummaryDate}", summaryDate);
            throw;
        }
    }

    public async Task<List<TagDailySummaryDto>> GetDailySummariesAsync(DateTime from, DateTime to, long? tagId = null)
    {
        var start = DateTime.SpecifyKind(from.Date, DateTimeKind.Utc);
        var endInclusive = DateTime.SpecifyKind(to.Date, DateTimeKind.Utc);
        if (endInclusive < start)
            throw new ValidationException("结束日期不能早于开始日期");

        var endExclusive = endInclusive.AddDays(1);

        var query = _tagDailySummaryRepository.GetQueryable()
            .Include(s => s.Tag)
            .Where(s => s.SummaryDate >= start && s.SummaryDate < endExclusive);

        if (tagId.HasValue)
            query = query.Where(s => s.TagId == tagId.Value);

        var rows = await query
            .OrderBy(s => s.SummaryDate)
            .ThenBy(s => s.TagId)
            .ThenBy(s => s.MetricScope)
            .ToListAsync();

        return rows.Select(s => new TagDailySummaryDto
        {
            Id = s.Id,
            SummaryDate = s.SummaryDate,
            TagId = s.TagId,
            TagName = s.Tag?.Name ?? string.Empty,
            TagColor = s.Tag?.Color,
            MetricScope = s.MetricScope,
            IncomeAmount = s.IncomeAmount,
            ExpenseAmount = s.ExpenseAmount,
            NetAmount = s.NetAmount,
            TransactionCount = s.TransactionCount,
            Version = s.Version
        }).ToList();
    }

    /// <summary>
    /// 按标签 + 作用域聚合某一 UTC 日的交易。
    /// Transaction 作用域直接用标签绑定；其他作用域只计入已经能关联到交易的绑定。
    /// 划转不计入收支。同一交易挂多个标签时分别计入各标签。
    /// </summary>
    private async Task<List<TagDailySummary>> AggregateDailySummariesAsync(DateTime summaryDate, DateTime endExclusive)
    {
        var transactions = await _transactionRepository.GetQueryable()
            .Where(t => t.TransactionDate >= summaryDate && t.TransactionDate < endExclusive)
            .Where(t => t.TransactionType != TransactionType.Transfer)
            .Select(t => new DailyTx(
                t.Id,
                t.TransactionType,
                t.Amount,
                t.ProjectId,
                t.PersonId,
                t.CustomerId,
                t.SupplierId))
            .ToListAsync();

        if (transactions.Count == 0)
            return new List<TagDailySummary>();

        var transactionIds = transactions.Select(t => t.Id).ToList();
        var projectIds = transactions.Where(t => t.ProjectId.HasValue).Select(t => t.ProjectId!.Value).Distinct().ToList();
        var personIds = transactions.Where(t => t.PersonId.HasValue).Select(t => t.PersonId!.Value).Distinct().ToList();
        var customerIds = transactions.Where(t => t.CustomerId.HasValue).Select(t => t.CustomerId!.Value).Distinct().ToList();
        var supplierIds = transactions.Where(t => t.SupplierId.HasValue).Select(t => t.SupplierId!.Value).Distinct().ToList();

        var bindings = await _tagBindingRepository.GetQueryable()
            .Where(b =>
                (b.OwnerType == TagScope.Transaction && transactionIds.Contains(b.OwnerId))
                || (b.OwnerType == TagScope.Project && projectIds.Contains(b.OwnerId))
                || (b.OwnerType == TagScope.Person && personIds.Contains(b.OwnerId))
                || (b.OwnerType == TagScope.Customer && customerIds.Contains(b.OwnerId))
                || (b.OwnerType == TagScope.Supplier && supplierIds.Contains(b.OwnerId)))
            .Select(b => new DailyBinding(b.TagId, b.OwnerType, b.OwnerId))
            .ToListAsync();

        var grouped = new Dictionary<(long TagId, TagScope Scope), DailyBucket>();

        void Add(long tagId, TagScope scope, DailyTx tx)
        {
            var key = (tagId, scope);
            if (!grouped.TryGetValue(key, out var bucket))
            {
                bucket = new DailyBucket();
                grouped[key] = bucket;
            }

            if (!bucket.TransactionIds.Add(tx.Id))
                return;

            if (tx.TransactionType == TransactionType.Income)
                bucket.Income += tx.Amount;
            else if (tx.TransactionType == TransactionType.Expense)
                bucket.Expense += tx.Amount;
        }

        var txById = transactions.ToDictionary(t => t.Id);
        foreach (var binding in bindings.Where(b => b.OwnerType == TagScope.Transaction))
        {
            if (txById.TryGetValue(binding.OwnerId, out var tx))
                Add(binding.TagId, TagScope.Transaction, tx);
        }

        AddByOwner(bindings, TagScope.Project, transactions.Where(t => t.ProjectId.HasValue), t => t.ProjectId!.Value, Add);
        AddByOwner(bindings, TagScope.Person, transactions.Where(t => t.PersonId.HasValue), t => t.PersonId!.Value, Add);
        AddByOwner(bindings, TagScope.Customer, transactions.Where(t => t.CustomerId.HasValue), t => t.CustomerId!.Value, Add);
        AddByOwner(bindings, TagScope.Supplier, transactions.Where(t => t.SupplierId.HasValue), t => t.SupplierId!.Value, Add);

        return grouped
            .OrderBy(g => g.Key.TagId)
            .ThenBy(g => g.Key.Scope)
            .Select(g => new TagDailySummary
            {
                SummaryDate = summaryDate,
                TagId = g.Key.TagId,
                MetricScope = g.Key.Scope.ToString().ToLowerInvariant(),
                IncomeAmount = g.Value.Income,
                ExpenseAmount = g.Value.Expense,
                NetAmount = g.Value.Income - g.Value.Expense,
                TransactionCount = g.Value.TransactionIds.Count
            })
            .ToList();
    }

    private static void AddByOwner(
        IEnumerable<DailyBinding> bindings,
        TagScope scope,
        IEnumerable<DailyTx> transactions,
        Func<DailyTx, long> ownerId,
        Action<long, TagScope, DailyTx> add)
    {
        var tagIdsByOwner = bindings
            .Where(b => b.OwnerType == scope)
            .GroupBy(b => b.OwnerId)
            .ToDictionary(g => g.Key, g => g.Select(b => b.TagId).Distinct().ToList());

        foreach (var tx in transactions)
        {
            if (!tagIdsByOwner.TryGetValue(ownerId(tx), out var tagIds))
                continue;
            foreach (var tagId in tagIds)
                add(tagId, scope, tx);
        }
    }

    private sealed record DailyBinding(long TagId, TagScope OwnerType, long OwnerId);

    private sealed record DailyTx(
        long Id,
        TransactionType TransactionType,
        decimal Amount,
        long? ProjectId,
        long? PersonId,
        long? CustomerId,
        long? SupplierId);

    private sealed class DailyBucket
    {
        public decimal Income { get; set; }
        public decimal Expense { get; set; }
        public HashSet<long> TransactionIds { get; } = new();
    }
}
