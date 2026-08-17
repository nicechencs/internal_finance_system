using System.Diagnostics;
using FinanceApp.Application.Modules.Reporting.DTOs.Report;
using FinanceApp.Application.Modules.Reporting.Interfaces;
using FinanceApp.Application.Modules.Reporting.Models;
using FinanceApp.Application.Services;
using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Enums;
using FinanceApp.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FinanceApp.Application.Modules.Reporting.Services;

public class ReportService : ServiceBase, IReportService
{
    private readonly IRepository<Transaction> _transactionRepository;
    private readonly IRepository<Project> _projectRepository;
    private readonly IRepository<Person> _personRepository;
    private readonly IRepository<Supplier> _supplierRepository;
    private readonly IRepository<Customer> _customerRepository;
    private readonly IRepository<Account> _accountRepository;
    private readonly IRepository<Receivable> _receivableRepository;
    private readonly IRepository<Payable> _payableRepository;
    private readonly IProjectFinancialSummaryService _projectFinancialSummaryService;
    private readonly ILogger<ReportService> _logger;

    public ReportService(
        IRepository<Transaction> transactionRepository,
        IRepository<Project> projectRepository,
        IRepository<Person> personRepository,
        IRepository<Supplier> supplierRepository,
        IRepository<Customer> customerRepository,
        IRepository<Account> accountRepository,
        IRepository<Receivable> receivableRepository,
        IRepository<Payable> payableRepository,
        IProjectFinancialSummaryService projectFinancialSummaryService,
        ILogger<ReportService> logger,
        ICurrentUserService currentUserService,
        IDataPermissionService permissionService)
        : base(currentUserService, permissionService)
    {
        _transactionRepository = transactionRepository;
        _projectRepository = projectRepository;
        _personRepository = personRepository;
        _supplierRepository = supplierRepository;
        _customerRepository = customerRepository;
        _accountRepository = accountRepository;
        _receivableRepository = receivableRepository;
        _payableRepository = payableRepository;
        _projectFinancialSummaryService = projectFinancialSummaryService;
        _logger = logger;
    }

    public async Task<MonthlyProfitReportDto> GetMonthlyProfitReportAsync(int year, int month)
    {
        var sw = Stopwatch.StartNew();
        var startDate = new DateTime(year, month, 1);
        var endDate = startDate.AddMonths(1);

        _logger.LogDebug("ReportService.GetMonthlyProfitReportAsync: 开始生成报表, 年份={Year}, 月份={Month}, 起始日期={StartDate}, 结束日期={EndDate}",
            year, month, startDate.ToString("yyyy-MM-dd"), endDate.ToString("yyyy-MM-dd"));

        try
        {
            _logger.LogDebug("开始查询交易记录, 起始日期={StartDate}, 结束日期={EndDate}",
                startDate.ToString("yyyy-MM-dd"), endDate.ToString("yyyy-MM-dd"));

            var query = ApplyPermissionFilter(_transactionRepository.GetQueryable())
                .Where(t => t.TransactionDate >= startDate && t.TransactionDate < endDate);

            var transactionCount = await query.CountAsync();
            _logger.LogInformation("查询到交易记录, 数量={TransactionCount}", transactionCount);

            _logger.LogDebug("开始计算收入和支出");

            var income = await query
                .Where(t => t.TransactionType == TransactionType.Income)
                .SumAsync(t => (decimal?)t.Amount) ?? 0;

            var expense = await query
                .Where(t => t.TransactionType == TransactionType.Expense)
                .SumAsync(t => (decimal?)t.Amount) ?? 0;

            var netProfit = income - expense;
            var profitRate = income > 0 ? (netProfit / income) * 100 : 0;

            _logger.LogInformation("计算收支汇总完成, 收入={Income}, 支出={Expense}, 净利润={NetProfit}, 利润率={ProfitRate:F2}%",
                income, expense, netProfit, profitRate);

            _logger.LogDebug("开始按分类汇总收入和支出");

            var categoryGroups = await query
                .Where(t => t.Category != null
                            && (t.TransactionType == TransactionType.Income
                                || t.TransactionType == TransactionType.Expense))
                .GroupBy(t => new { t.TransactionType, CategoryName = t.Category!.Name })
                .Select(g => new
                {
                    g.Key.TransactionType,
                    g.Key.CategoryName,
                    Amount = g.Sum(t => t.Amount)
                })
                .ToListAsync();

            var incomeByCategory = categoryGroups
                .Where(g => g.TransactionType == TransactionType.Income)
                .Select(g => new CategoryAmountDto
                {
                    CategoryName = g.CategoryName,
                    Amount = g.Amount
                })
                .ToList();

            var expenseByCategory = categoryGroups
                .Where(g => g.TransactionType == TransactionType.Expense)
                .Select(g => new CategoryAmountDto
                {
                    CategoryName = g.CategoryName,
                    Amount = g.Amount
                })
                .ToList();

            _logger.LogInformation("分类汇总完成, 收入分类数={IncomeCategories}, 支出分类数={ExpenseCategories}",
                incomeByCategory.Count, expenseByCategory.Count);

            sw.Stop();
            _logger.LogInformation("报表生成完成, 耗时={ElapsedMs}ms", sw.ElapsedMilliseconds);

            if (sw.ElapsedMilliseconds > 3000)
                _logger.LogWarning("报表生成耗时过长, 耗时={ElapsedMs}ms", sw.ElapsedMilliseconds);

            return new MonthlyProfitReportDto
            {
                Year = year,
                Month = month,
                TotalIncome = income,
                TotalExpense = expense,
                NetProfit = netProfit,
                ProfitRate = profitRate,
                IncomeByCategory = incomeByCategory,
                ExpenseByCategory = expenseByCategory
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "生成月度利润报表失败, 年份={Year}, 月份={Month}",
                year, month);
            throw;
        }
    }

    public async Task<CashflowReportDto> GetCashflowReportAsync(DateTime startDate, DateTime endDate)
    {
        var sw = Stopwatch.StartNew();

        _logger.LogDebug("ReportService.GetCashflowReportAsync: 开始生成报表, 起始日期={StartDate}, 结束日期={EndDate}",
            startDate.ToString("yyyy-MM-dd"), endDate.ToString("yyyy-MM-dd"));

        try
        {
            _logger.LogDebug("开始查询交易记录");

            var query = ApplyPermissionFilter(_transactionRepository.GetQueryable())
                .Where(t => t.TransactionDate >= startDate && t.TransactionDate < endDate);

            var transactionCount = await query.CountAsync();
            _logger.LogInformation("查询到交易记录, 数量={TransactionCount}", transactionCount);

            _logger.LogDebug("开始查询账户余额");

            var accountQuery = ApplyPermissionFilter(_accountRepository.GetQueryable());
            var openingBalance = await accountQuery.SumAsync(a => (decimal?)a.CurrentBalance) ?? 0;
            var accountCount = await accountQuery.CountAsync();

            _logger.LogInformation("账户期初余额, 金额={OpeningBalance}, 账户数={AccountCount}",
                openingBalance, accountCount);

            _logger.LogDebug("开始计算总收入和总支出");

            var totalIncome = await query
                .Where(t => t.TransactionType == TransactionType.Income)
                .SumAsync(t => (decimal?)t.Amount) ?? 0;

            var totalExpense = await query
                .Where(t => t.TransactionType == TransactionType.Expense)
                .SumAsync(t => (decimal?)t.Amount) ?? 0;

            var closingBalance = openingBalance + totalIncome - totalExpense;

            _logger.LogDebug("开始计算月度明细, 起始月份={StartMonth}, 结束月份={EndMonth}",
                startDate.ToString("yyyy-MM"), endDate.ToString("yyyy-MM"));

            var monthlyGroups = await query
                .GroupBy(t => new { t.TransactionDate.Year, t.TransactionDate.Month, t.TransactionType })
                .Select(g => new
                {
                    g.Key.Year,
                    g.Key.Month,
                    g.Key.TransactionType,
                    Amount = g.Sum(t => t.Amount)
                })
                .ToListAsync();

            var monthlyLookup = monthlyGroups
                .GroupBy(g => (g.Year, g.Month))
                .ToDictionary(
                    g => g.Key,
                    g => (
                        Income: g.Where(x => x.TransactionType == TransactionType.Income).Sum(x => x.Amount),
                        Expense: g.Where(x => x.TransactionType == TransactionType.Expense).Sum(x => x.Amount)
                    ));

            var monthlyDetails = new List<MonthlyDetailDto>();
            var currentDate = new DateTime(startDate.Year, startDate.Month, 1);
            var endMonth = new DateTime(endDate.Year, endDate.Month, 1);
            var runningBalance = openingBalance;

            while (currentDate <= endMonth)
            {
                monthlyLookup.TryGetValue((currentDate.Year, currentDate.Month), out var monthAmounts);
                var monthIncome = monthAmounts.Income;
                var monthExpense = monthAmounts.Expense;
                var monthClosing = runningBalance + monthIncome - monthExpense;

                _logger.LogDebug("月度明细计算, 月份={Month}, 期初={Opening}, 收入={Income}, 支出={Expense}, 期末={Closing}",
                    currentDate.ToString("yyyy-MM"), runningBalance, monthIncome, monthExpense, monthClosing);

                monthlyDetails.Add(new MonthlyDetailDto
                {
                    Month = currentDate.ToString("yyyy-MM"),
                    OpeningBalance = runningBalance,
                    Income = monthIncome,
                    Expense = monthExpense,
                    ClosingBalance = monthClosing
                });

                runningBalance = monthClosing;
                currentDate = currentDate.AddMonths(1);
            }

            _logger.LogInformation("月度明细计算完成, 月份数={MonthCount}, 总收入={TotalIncome}, 总支出={TotalExpense}, 期末余额={ClosingBalance}",
                monthlyDetails.Count, totalIncome, totalExpense, closingBalance);

            sw.Stop();
            _logger.LogInformation("报表生成完成, 耗时={ElapsedMs}ms", sw.ElapsedMilliseconds);

            if (sw.ElapsedMilliseconds > 3000)
                _logger.LogWarning("报表生成耗时过长, 耗时={ElapsedMs}ms", sw.ElapsedMilliseconds);

            return new CashflowReportDto
            {
                StartDate = startDate.ToString("yyyy-MM-dd"),
                EndDate = endDate.ToString("yyyy-MM-dd"),
                OpeningBalance = openingBalance,
                TotalIncome = totalIncome,
                TotalExpense = totalExpense,
                ClosingBalance = closingBalance,
                MonthlyDetail = monthlyDetails
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "生成现金流量报表失败, 起始日期={StartDate}, 结束日期={EndDate}",
                startDate.ToString("yyyy-MM-dd"), endDate.ToString("yyyy-MM-dd"));
            throw;
        }
    }

    public async Task<ProjectProfitReportDto> GetProjectProfitReportAsync()
    {
        var sw = Stopwatch.StartNew();

        _logger.LogDebug("ReportService.GetProjectProfitReportAsync: 开始生成报表");

        try
        {
            _logger.LogDebug("开始查询项目数据");

            var projects = await ApplyPermissionFilter(_projectRepository.GetQueryable())
                .Select(p => new
                {
                    p.Id,
                    p.Name,
                    CustomerName = p.Customer != null ? p.Customer.Name : ""
                })
                .ToListAsync();

            _logger.LogInformation("查询到项目, 数量={ProjectCount}", projects.Count);

            _logger.LogDebug("开始构建项目利润明细");

            var projectIds = projects.Select(p => p.Id).ToList();
            var projectSummaries = await _projectFinancialSummaryService.GetProjectSummariesAsync(projectIds)
                ?? new Dictionary<long, ProjectFinancialSummary>();

            var projectItems = new List<ProjectProfitItemDto>(projects.Count);
            foreach (var project in projects)
            {
                if (!projectSummaries.TryGetValue(project.Id, out var projectSummary) || projectSummary == null)
                    projectSummary = new ProjectFinancialSummary { ProjectId = project.Id };

                projectItems.Add(new ProjectProfitItemDto
                {
                    ProjectId = project.Id,
                    ProjectName = project.Name,
                    CustomerName = project.CustomerName ?? "",
                    ContractAmount = projectSummary.ContractAmount,
                    ReceivedAmount = projectSummary.ReceivedAmount,
                    TotalCost = projectSummary.TotalCost,
                    ProfitAmount = projectSummary.ProfitAmount,
                    ProfitRate = projectSummary.ProfitRate
                });
            }

            var summary = new ProjectProfitSummaryDto
            {
                TotalContract = projectItems.Sum(p => p.ContractAmount),
                TotalReceived = projectItems.Sum(p => p.ReceivedAmount),
                TotalCost = projectItems.Sum(p => p.TotalCost),
                TotalProfit = projectItems.Sum(p => p.ProfitAmount),
                AvgProfitRate = projectItems.Any() ? projectItems.Average(p => p.ProfitRate) : 0
            };

            _logger.LogInformation("项目利润汇总, 总合同额={TotalContract}, 总收款={TotalReceived}, 总成本={TotalCost}, 总利润={TotalProfit}, 平均利润率={AvgProfitRate:F2}%",
                summary.TotalContract, summary.TotalReceived, summary.TotalCost, summary.TotalProfit, summary.AvgProfitRate);

            sw.Stop();
            _logger.LogInformation("报表生成完成, 耗时={ElapsedMs}ms, 数据行数={RowCount}",
                sw.ElapsedMilliseconds, projectItems.Count);

            if (sw.ElapsedMilliseconds > 3000)
                _logger.LogWarning("报表生成耗时过长, 耗时={ElapsedMs}ms", sw.ElapsedMilliseconds);

            return new ProjectProfitReportDto
            {
                Projects = projectItems,
                Summary = summary
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "生成项目利润报表失败");
            throw;
        }
    }

    public async Task<PersonCostReportDto> GetPersonCostReportAsync()
    {
        var sw = Stopwatch.StartNew();

        _logger.LogDebug("ReportService.GetPersonCostReportAsync: 开始生成报表");

        try
        {
            _logger.LogDebug("开始查询人员数据");

            var persons = await ApplyPermissionFilter(_personRepository.GetQueryable())
                .ToListAsync();

            _logger.LogInformation("查询到人员, 数量={PersonCount}", persons.Count);

            _logger.LogDebug("开始查询人员相关交易");

            var transactions = await ApplyPermissionFilter(_transactionRepository.GetQueryable())
                .Where(t => t.PersonId != null)
                .Select(t => new
                {
                    t.PersonId,
                    t.Amount,
                    CategoryName = t.Category != null ? t.Category.Name : null
                })
                .ToListAsync();

            _logger.LogInformation("查询到人员相关交易, 数量={TransactionCount}", transactions.Count);

            _logger.LogDebug("开始按人员和分类汇总成本");

            var personCostItems = persons.Select(person =>
            {
                var personTransactions = transactions.Where(t => t.PersonId == person.Id).ToList();

                var salary = personTransactions
                    .Where(t => t.CategoryName?.Contains("工资") == true || t.CategoryName?.Contains("薪资") == true)
                    .Sum(t => t.Amount);

                var commission = personTransactions
                    .Where(t => t.CategoryName?.Contains("提成") == true || t.CategoryName?.Contains("佣金") == true)
                    .Sum(t => t.Amount);

                var reimbursement = personTransactions
                    .Where(t => t.CategoryName?.Contains("报销") == true)
                    .Sum(t => t.Amount);

                var dividend = personTransactions
                    .Where(t => t.CategoryName?.Contains("分红") == true)
                    .Sum(t => t.Amount);

                return new PersonCostItemDto
                {
                    PersonId = person.Id,
                    PersonName = person.Name,
                    PersonType = person.PersonType.ToString(),
                    Salary = salary,
                    Commission = commission,
                    Reimbursement = reimbursement,
                    Dividend = dividend,
                    TotalCost = salary + commission + reimbursement + dividend
                };
            }).ToList();

            _logger.LogInformation("人员成本分类汇总完成");

            var summary = new PersonCostSummaryDto
            {
                TotalSalary = personCostItems.Sum(p => p.Salary),
                TotalCommission = personCostItems.Sum(p => p.Commission),
                TotalReimbursement = personCostItems.Sum(p => p.Reimbursement),
                TotalCost = personCostItems.Sum(p => p.TotalCost)
            };

            _logger.LogInformation("人员成本汇总, 总工资={TotalSalary}, 总提成={TotalCommission}, 总报销={TotalReimbursement}, 总成本={TotalCost}",
                summary.TotalSalary, summary.TotalCommission, summary.TotalReimbursement, summary.TotalCost);

            sw.Stop();
            _logger.LogInformation("报表生成完成, 耗时={ElapsedMs}ms, 数据行数={RowCount}",
                sw.ElapsedMilliseconds, personCostItems.Count);

            if (sw.ElapsedMilliseconds > 3000)
                _logger.LogWarning("报表生成耗时过长, 耗时={ElapsedMs}ms", sw.ElapsedMilliseconds);

            return new PersonCostReportDto
            {
                Persons = personCostItems,
                Summary = summary
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "生成人员成本报表失败");
            throw;
        }
    }

    public async Task<SupplierExpenseReportDto> GetSupplierExpenseReportAsync()
    {
        var sw = Stopwatch.StartNew();

        _logger.LogDebug("ReportService.GetSupplierExpenseReportAsync: 开始生成报表");

        try
        {
            _logger.LogDebug("开始查询供应商支出交易");

            var expenseBySupplier = await ApplyPermissionFilter(_transactionRepository.GetQueryable())
                .Where(t => t.SupplierId != null && t.TransactionType == TransactionType.Expense)
                .GroupBy(t => t.SupplierId!.Value)
                .Select(g => new
                {
                    SupplierId = g.Key,
                    TotalExpense = g.Sum(t => t.Amount),
                    TransactionCount = g.Count()
                })
                .Where(x => x.TotalExpense > 0)
                .ToListAsync();

            _logger.LogInformation("查询到有支出的供应商分组, 数量={SupplierGroupCount}", expenseBySupplier.Count);

            var supplierIds = expenseBySupplier.Select(x => x.SupplierId).ToList();
            var supplierNames = supplierIds.Count == 0
                ? new Dictionary<long, string>()
                : (await ApplyPermissionFilter(_supplierRepository.GetQueryable())
                    .Where(s => supplierIds.Contains(s.Id))
                    .Select(s => new { s.Id, s.Name })
                    .ToListAsync())
                    .ToDictionary(s => s.Id, s => s.Name);

            _logger.LogDebug("开始按供应商汇总支出");

            var supplierExpenses = expenseBySupplier
                .Where(x => supplierNames.ContainsKey(x.SupplierId))
                .OrderByDescending(x => x.TotalExpense)
                .Select(x => new SupplierExpenseItemDto
                {
                    SupplierId = x.SupplierId,
                    SupplierName = supplierNames[x.SupplierId],
                    TotalExpense = x.TotalExpense,
                    TransactionCount = x.TransactionCount,
                    Rank = 0
                })
                .ToList();

            _logger.LogDebug("开始为供应商分配排名");

            for (int i = 0; i < supplierExpenses.Count; i++)
            {
                supplierExpenses[i].Rank = i + 1;
            }

            _logger.LogInformation("供应商排序完成, 有支出供应商数={ActiveSupplierCount}", supplierExpenses.Count);

            var summary = new SupplierExpenseSummaryDto
            {
                TotalExpense = supplierExpenses.Sum(s => s.TotalExpense),
                SupplierCount = supplierExpenses.Count
            };

            _logger.LogInformation("供应商支出汇总, 总支出={TotalExpense}, 供应商数={SupplierCount}",
                summary.TotalExpense, summary.SupplierCount);

            sw.Stop();
            _logger.LogInformation("报表生成完成, 耗时={ElapsedMs}ms, 数据行数={RowCount}",
                sw.ElapsedMilliseconds, supplierExpenses.Count);

            if (sw.ElapsedMilliseconds > 3000)
                _logger.LogWarning("报表生成耗时过长, 耗时={ElapsedMs}ms", sw.ElapsedMilliseconds);

            return new SupplierExpenseReportDto
            {
                Suppliers = supplierExpenses,
                Summary = summary
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "生成供应商支出报表失败");
            throw;
        }
    }

    public async Task<AnnualOverviewReportDto> GetAnnualOverviewReportAsync(int year)
    {
        var sw = Stopwatch.StartNew();
        var startDate = new DateTime(year, 1, 1);
        var endDate = new DateTime(year + 1, 1, 1);

        _logger.LogDebug("ReportService.GetAnnualOverviewReportAsync: 开始生成报表, 年份={Year}, 起始日期={StartDate}, 结束日期={EndDate}",
            year, startDate.ToString("yyyy-MM-dd"), endDate.ToString("yyyy-MM-dd"));

        try
        {
            _logger.LogDebug("开始查询年度交易记录");

            var query = ApplyPermissionFilter(_transactionRepository.GetQueryable())
                .Where(t => t.TransactionDate >= startDate && t.TransactionDate < endDate);

            var transactionCount = await query.CountAsync();
            _logger.LogInformation("查询到年度交易记录, 数量={TransactionCount}", transactionCount);

            _logger.LogDebug("开始计算年度收支汇总");

            var totalIncome = await query
                .Where(t => t.TransactionType == TransactionType.Income)
                .SumAsync(t => (decimal?)t.Amount) ?? 0;

            var totalExpense = await query
                .Where(t => t.TransactionType == TransactionType.Expense)
                .SumAsync(t => (decimal?)t.Amount) ?? 0;

            var netProfit = totalIncome - totalExpense;
            var profitRate = totalIncome > 0 ? (netProfit / totalIncome) * 100 : 0;

            _logger.LogInformation("年度收支汇总完成, 总收入={TotalIncome}, 总支出={TotalExpense}, 净利润={NetProfit}, 利润率={ProfitRate:F2}%",
                totalIncome, totalExpense, netProfit, profitRate);

            _logger.LogDebug("开始查询应收应付数据");

            var receivableQuery = ApplyPermissionFilter(_receivableRepository.GetQueryable())
                .Where(r => r.Status != ReceivableStatus.Settled);
            var totalReceivable = await receivableQuery.SumAsync(r => (decimal?)r.RemainingAmount) ?? 0;
            var receivableCount = await receivableQuery.CountAsync();

            var payableQuery = ApplyPermissionFilter(_payableRepository.GetQueryable())
                .Where(p => p.Status != PayableStatus.Settled);
            var totalPayable = await payableQuery.SumAsync(p => (decimal?)p.RemainingAmount) ?? 0;
            var payableCount = await payableQuery.CountAsync();

            _logger.LogInformation("应收应付汇总完成, 应收余额={TotalReceivable} (笔数={ReceivableCount}), 应付余额={TotalPayable} (笔数={PayableCount})",
                totalReceivable, receivableCount, totalPayable, payableCount);

            _logger.LogDebug("开始计算月度趋势");

            var monthlyGroups = await query
                .GroupBy(t => new { t.TransactionDate.Month, t.TransactionType })
                .Select(g => new
                {
                    g.Key.Month,
                    g.Key.TransactionType,
                    Amount = g.Sum(t => t.Amount)
                })
                .ToListAsync();

            var monthlyLookup = monthlyGroups
                .GroupBy(g => g.Month)
                .ToDictionary(
                    g => g.Key,
                    g => (
                        Income: g.Where(x => x.TransactionType == TransactionType.Income).Sum(x => x.Amount),
                        Expense: g.Where(x => x.TransactionType == TransactionType.Expense).Sum(x => x.Amount)
                    ));

            var monthlyTrend = new List<MonthlyTrendDto>(12);
            for (int month = 1; month <= 12; month++)
            {
                monthlyLookup.TryGetValue(month, out var monthAmounts);
                var monthIncome = monthAmounts.Income;
                var monthExpense = monthAmounts.Expense;

                monthlyTrend.Add(new MonthlyTrendDto
                {
                    Month = month,
                    Income = monthIncome,
                    Expense = monthExpense,
                    Profit = monthIncome - monthExpense
                });
            }

            _logger.LogInformation("月度趋势计算完成, 月份数={MonthCount}", monthlyTrend.Count);

            _logger.LogDebug("开始计算 Top N 排名");

            var topProjects = await query
                .Where(t => t.ProjectId != null && t.TransactionType == TransactionType.Income)
                .GroupBy(t => new { t.ProjectId, Name = t.Project!.Name })
                .Select(g => new TopItemDto
                {
                    Id = g.Key.ProjectId!.Value,
                    Name = g.Key.Name,
                    Amount = g.Sum(t => t.Amount)
                })
                .OrderByDescending(x => x.Amount)
                .Take(10)
                .ToListAsync();

            var topCustomers = await query
                .Where(t => t.CustomerId != null && t.TransactionType == TransactionType.Income)
                .GroupBy(t => new { t.CustomerId, Name = t.Customer!.Name })
                .Select(g => new TopItemDto
                {
                    Id = g.Key.CustomerId!.Value,
                    Name = g.Key.Name,
                    Amount = g.Sum(t => t.Amount)
                })
                .OrderByDescending(x => x.Amount)
                .Take(10)
                .ToListAsync();

            var topSuppliers = await query
                .Where(t => t.SupplierId != null && t.TransactionType == TransactionType.Expense)
                .GroupBy(t => new { t.SupplierId, Name = t.Supplier!.Name })
                .Select(g => new TopItemDto
                {
                    Id = g.Key.SupplierId!.Value,
                    Name = g.Key.Name,
                    Amount = g.Sum(t => t.Amount)
                })
                .OrderByDescending(x => x.Amount)
                .Take(10)
                .ToListAsync();

            _logger.LogInformation("Top N 排序完成, Top项目数={TopProjectCount}, Top客户数={TopCustomerCount}, Top供应商数={TopSupplierCount}",
                topProjects.Count, topCustomers.Count, topSuppliers.Count);

            sw.Stop();
            _logger.LogInformation("报表生成完成, 耗时={ElapsedMs}ms", sw.ElapsedMilliseconds);

            if (sw.ElapsedMilliseconds > 3000)
                _logger.LogWarning("报表生成耗时过长, 耗时={ElapsedMs}ms", sw.ElapsedMilliseconds);

            return new AnnualOverviewReportDto
            {
                Year = year,
                TotalIncome = totalIncome,
                TotalExpense = totalExpense,
                NetProfit = netProfit,
                ProfitRate = profitRate,
                TotalReceivable = totalReceivable,
                TotalPayable = totalPayable,
                MonthlyTrend = monthlyTrend,
                TopProjects = topProjects,
                TopCustomers = topCustomers,
                TopSuppliers = topSuppliers
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "生成年度综合报表失败, 年份={Year}",
                year);
            throw;
        }
    }
}
