using FinanceApp.Application.Modules.Reporting.DTOs.Dashboard;
using FinanceApp.Application.Modules.Reporting.Interfaces;
using FinanceApp.Application.Services;
using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Enums;
using FinanceApp.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FinanceApp.Application.Modules.Reporting.Services;

public class DashboardService : ServiceBase, IDashboardService
{
    private readonly IRepository<Transaction> _transactionRepository;
    private readonly IRepository<Account> _accountRepository;
    private readonly IRepository<Project> _projectRepository;
    private readonly ILogger<DashboardService> _logger;

    public DashboardService(
        IRepository<Transaction> transactionRepository,
        IRepository<Account> accountRepository,
        IRepository<Project> projectRepository,
        ILogger<DashboardService> logger,
        ICurrentUserService currentUserService,
        IDataPermissionService permissionService)
        : base(currentUserService, permissionService)
    {
        _transactionRepository = transactionRepository;
        _accountRepository = accountRepository;
        _projectRepository = projectRepository;
        _logger = logger;
    }

    public async Task<DashboardSummaryDto> GetSummaryAsync()
    {
        _logger.LogDebug("DashboardService.GetSummaryAsync: 开始获取仪表盘汇总数据");

        try
        {
            var transactions = _transactionRepository.GetQueryable();

            // 应用权限过滤
            transactions = ApplyPermissionFilter(transactions);

            var totalIncome = await transactions
                .Where(t => t.TransactionType == TransactionType.Income)
                .SumAsync(t => (decimal?)t.Amount) ?? 0;

            var totalExpense = await transactions
                .Where(t => t.TransactionType == TransactionType.Expense)
                .SumAsync(t => (decimal?)t.Amount) ?? 0;

            var netProfit = totalIncome - totalExpense;

            var accounts = _accountRepository.GetQueryable();
            accounts = ApplyPermissionFilter(accounts);

            var totalBalance = await accounts
                .Where(a => a.IsActive)
                .SumAsync(a => (decimal?)a.CurrentBalance) ?? 0;

            var accountCount = await accounts
                .Where(a => a.IsActive)
                .CountAsync();

            var transactionCount = await transactions.CountAsync();

            var projects = _projectRepository.GetQueryable();
            projects = ApplyPermissionFilter(projects);

            var projectCount = await projects.CountAsync();

            _logger.LogInformation(
                "仪表盘汇总数据获取成功，收入={Income}, 支出={Expense}, 净利={Net}, 余额={Balance}, 账户数={AccountCount}, 交易数={TransactionCount}, 项目数={ProjectCount}",
                totalIncome, totalExpense, netProfit, totalBalance, accountCount, transactionCount, projectCount);

            return new DashboardSummaryDto
            {
                TotalIncome = totalIncome,
                TotalExpense = totalExpense,
                NetProfit = netProfit,
                TotalBalance = totalBalance,
                AccountCount = accountCount,
                TransactionCount = transactionCount,
                ProjectCount = projectCount
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取仪表盘汇总数据时发生异常");
            throw;
        }
    }

    public async Task<List<MonthlyStatsDto>> GetMonthlyStatsAsync(int months = 12)
    {
        _logger.LogDebug("DashboardService.GetMonthlyStatsAsync: 月数={Months}", months);

        try
        {
            var startDate = DateTime.UtcNow.AddMonths(-months + 1);
            startDate = new DateTime(startDate.Year, startDate.Month, 1, 0, 0, 0, DateTimeKind.Utc);

            // 应用权限过滤，确保 Viewer 只能看到自己创建的数据
            var monthlyGroups = await ApplyPermissionFilter(_transactionRepository.GetQueryable())
                .Where(t => t.TransactionDate >= startDate)
                .GroupBy(t => new { t.TransactionDate.Year, t.TransactionDate.Month, t.TransactionType })
                .Select(g => new
                {
                    g.Key.Year,
                    g.Key.Month,
                    g.Key.TransactionType,
                    Amount = g.Sum(t => t.Amount)
                })
                .ToListAsync();

            _logger.LogDebug("月度统计查询完成，起始日期={StartDate}, 分组数={Count}",
                startDate, monthlyGroups.Count);

            var monthlyLookup = monthlyGroups
                .GroupBy(g => (g.Year, g.Month))
                .ToDictionary(
                    g => g.Key,
                    g => (
                        Income: g.Where(x => x.TransactionType == TransactionType.Income).Sum(x => x.Amount),
                        Expense: g.Where(x => x.TransactionType == TransactionType.Expense).Sum(x => x.Amount)
                    ));

            var result = new List<MonthlyStatsDto>();

            for (int i = 0; i < months; i++)
            {
                var monthDate = startDate.AddMonths(i);
                monthlyLookup.TryGetValue((monthDate.Year, monthDate.Month), out var monthAmounts);

                result.Add(new MonthlyStatsDto
                {
                    Month = monthDate.ToString("yyyy-MM"),
                    Income = monthAmounts.Income,
                    Expense = monthAmounts.Expense,
                    Net = monthAmounts.Income - monthAmounts.Expense
                });
            }

            _logger.LogInformation("月度统计数据生成完成，共 {Count} 个月", result.Count);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取月度统计数据时发生异常，月数={Months}", months);
            throw;
        }
    }

    public async Task<List<CategoryStatsDto>> GetExpenseByCategoryAsync(DateTime? startDate, DateTime? endDate)
    {
        _logger.LogDebug("DashboardService.GetExpenseByCategoryAsync: 起始日期={StartDate}, 结束日期={EndDate}",
            startDate?.ToString("yyyy-MM-dd") ?? "无限制",
            endDate?.ToString("yyyy-MM-dd") ?? "无限制");

        try
        {
            var result = await GetByCategoryAsync(TransactionType.Expense, startDate, endDate);
            _logger.LogInformation("支出分类统计完成，共 {Count} 个分类", result.Count);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取支出分类统计时发生异常，起始日期={StartDate}, 结束日期={EndDate}",
                startDate, endDate);
            throw;
        }
    }

    public async Task<List<CategoryStatsDto>> GetIncomeByCategoryAsync(DateTime? startDate, DateTime? endDate)
    {
        _logger.LogDebug("DashboardService.GetIncomeByCategoryAsync: 起始日期={StartDate}, 结束日期={EndDate}",
            startDate?.ToString("yyyy-MM-dd") ?? "无限制",
            endDate?.ToString("yyyy-MM-dd") ?? "无限制");

        try
        {
            var result = await GetByCategoryAsync(TransactionType.Income, startDate, endDate);
            _logger.LogInformation("收入分类统计完成，共 {Count} 个分类", result.Count);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取收入分类统计时发生异常，起始日期={StartDate}, 结束日期={EndDate}",
                startDate, endDate);
            throw;
        }
    }

    public async Task<List<RecentTransactionDto>> GetRecentTransactionsAsync(int count = 10)
    {
        _logger.LogDebug("DashboardService.GetRecentTransactionsAsync: 数量={Count}", count);

        try
        {
            // 应用权限过滤，确保 Viewer 只能看到自己创建的数据
            var transactions = await ApplyPermissionFilter(_transactionRepository.GetQueryable())
                .Include(t => t.Account)
                .Include(t => t.Category)
                .Include(t => t.Customer)
                .Include(t => t.Supplier)
                .OrderByDescending(t => t.TransactionDate)
                .ThenByDescending(t => t.CreatedAt)
                .Take(count)
                .ToListAsync();

            var result = transactions.Select(t => new RecentTransactionDto
            {
                Id = t.Id,
                TransactionDate = t.TransactionDate,
                Type = t.TransactionType.ToString(),
                Amount = t.Amount,
                AccountName = t.Account?.Name ?? string.Empty,
                CategoryName = t.Category?.Name,
                CounterpartyName = t.Customer?.Name ?? t.Supplier?.Name,
                Description = t.Description
            }).ToList();

            _logger.LogInformation("最近交易记录获取成功，返回 {Count} 条记录", result.Count);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取最近交易记录时发生异常，数量={Count}", count);
            throw;
        }
    }

    private async Task<List<CategoryStatsDto>> GetByCategoryAsync(
        TransactionType type, DateTime? startDate, DateTime? endDate)
    {
        _logger.LogDebug("GetByCategoryAsync: 类型={Type}, 起始={StartDate}, 结束={EndDate}",
            type, startDate, endDate);

        try
        {
            // 应用权限过滤
            var query = ApplyPermissionFilter(_transactionRepository.GetQueryable())
                .Where(t => t.TransactionType == type);

            if (startDate.HasValue)
                query = query.Where(t => t.TransactionDate >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(t => t.TransactionDate <= endDate.Value);

            var grouped = await query
                .GroupBy(t => new { t.CategoryId, CategoryName = t.Category != null ? t.Category.Name : "未分类" })
                .Select(g => new
                {
                    g.Key.CategoryName,
                    Amount = g.Sum(t => t.Amount)
                })
                .OrderByDescending(x => x.Amount)
                .ToListAsync();

            var total = grouped.Sum(g => g.Amount);

            var result = grouped.Select(g => new CategoryStatsDto
            {
                CategoryName = g.CategoryName,
                Amount = g.Amount,
                Percentage = total > 0 ? Math.Round(g.Amount / total * 100, 2) : 0
            }).ToList();

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "按分类统计时发生异常，类型={Type}", type);
            throw;
        }
    }
}
