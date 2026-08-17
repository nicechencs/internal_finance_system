using FinanceApp.Application.Common;
using FinanceApp.Application.Extensions;
using FinanceApp.Application.Modules.TransactionProcessing.DTOs;
using FinanceApp.Application.Modules.TransactionProcessing.Interfaces;
using FinanceApp.Application.Services;
using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Enums;
using FinanceApp.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FinanceApp.Application.Modules.TransactionProcessing.Services;

public class TransactionStatisticsService : ServiceBase, ITransactionStatisticsService
{
    private readonly IRepository<Transaction> _transactionRepository;
    private readonly IRepository<ReceivableDetail> _receivableDetailRepository;
    private readonly IRepository<PayableDetail> _payableDetailRepository;
    private readonly IRepository<TagBinding> _tagBindingRepository;
    private readonly ILogger<TransactionStatisticsService> _logger;

    public TransactionStatisticsService(
        IRepository<Transaction> transactionRepository,
        IRepository<ReceivableDetail> receivableDetailRepository,
        IRepository<PayableDetail> payableDetailRepository,
        IRepository<TagBinding> tagBindingRepository,
        ILogger<TransactionStatisticsService> logger,
        ICurrentUserService currentUserService,
        IDataPermissionService permissionService)
        : base(currentUserService, permissionService)
    {
        _transactionRepository = transactionRepository;
        _receivableDetailRepository = receivableDetailRepository;
        _payableDetailRepository = payableDetailRepository;
        _tagBindingRepository = tagBindingRepository;
        _logger = logger;
    }

    public async Task<TransactionStatisticsDto> GetStatisticsAsync()
    {
        _logger.LogDebug("Starting full transaction statistics query");
        var sw = System.Diagnostics.Stopwatch.StartNew();

        var query = ApplyPermissionFilter(_transactionRepository.GetQueryable());
        var stats = await BuildStatisticsAsync(query, includeAllTransferRows: false);

        sw.Stop();
        _logger.LogInformation(
            "Full transaction statistics query completed: Count={Count}, Elapsed={Elapsed}ms",
            stats.TotalCount,
            sw.ElapsedMilliseconds);
        return stats;
    }

    public async Task<TransactionStatisticsDto> GetStatisticsAsync(PageRequest request)
    {
        _logger.LogDebug(
            "Querying filtered transaction statistics: Start={Start}, End={End}, Type={Type}, Account={Account}, Category={Category}, Project={Project}",
            request.StartDate,
            request.EndDate,
            request.TransactionType,
            request.AccountId,
            request.CategoryId,
            request.ProjectId);

        var sw = System.Diagnostics.Stopwatch.StartNew();
        var query = ApplyPermissionFilter(_transactionRepository.GetQueryable())
            .ApplyRequestFilters(request, _tagBindingRepository.GetQueryable);

        var stats = await BuildStatisticsAsync(query, includeAllTransferRows: false);

        sw.Stop();
        _logger.LogInformation(
            "Filtered transaction statistics query completed: Count={Count}, Income={Income}, Expense={Expense}, Elapsed={Elapsed}ms",
            stats.TotalCount,
            stats.TotalIncome,
            stats.TotalExpense,
            sw.ElapsedMilliseconds);

        return stats;
    }

    public async Task<TransactionStatisticsDto> GetAccountStatisticsAsync(long accountId)
    {
        _logger.LogDebug("获取账户交易统计: AccountId={AccountId}", accountId);
        var sw = System.Diagnostics.Stopwatch.StartNew();

        var query = ApplyPermissionFilter(_transactionRepository.GetQueryable())
            .Where(t => t.AccountId == accountId);

        var stats = await BuildStatisticsAsync(query, includeAllTransferRows: true);

        sw.Stop();
        _logger.LogInformation("账户交易统计获取成功: AccountId={AccountId}, 件数={Count}, 余额={Balance}, 耗时={Elapsed}ms",
            accountId, stats.TotalCount, stats.TotalIncome - stats.TotalExpense, sw.ElapsedMilliseconds);

        return stats;
    }

    public async Task<TransactionStatisticsDto> GetCustomerStatisticsAsync(long customerId)
    {
        var query = ApplyPermissionFilter(_transactionRepository.GetQueryable())
            .Where(t => t.CustomerId == customerId);

        return await BuildStatisticsAsync(query, includeAllTransferRows: false);
    }

    public async Task<TransactionStatisticsDto> GetSupplierStatisticsAsync(long supplierId)
    {
        var query = ApplyPermissionFilter(_transactionRepository.GetQueryable())
            .Where(t => t.SupplierId == supplierId);

        return await BuildStatisticsAsync(query, includeAllTransferRows: false);
    }

    public async Task<TransactionStatisticsDto> GetPersonStatisticsAsync(long personId)
    {
        var query = ApplyPermissionFilter(_transactionRepository.GetQueryable())
            .Where(t =>
                (t.PersonId == personId && !t.IsAllocated) ||
                (t.IsAllocated && t.Allocations.Any(a => a.PersonId == personId)));

        return await BuildStatisticsAsync(query, includeAllTransferRows: false);
    }

    public async Task<RelatedFinanceRecordDto> GetRelatedFinanceRecordsAsync(long transactionId)
    {
        _logger.LogDebug("TransactionStatisticsService.GetRelatedFinanceRecordsAsync - transactionId={TransactionId}", transactionId);

        var transaction = await _transactionRepository.GetByIdAsync(transactionId)
            ?? throw new NotFoundException("交易记录不存在");
        EnsureCanAccess(transaction);

        var result = new RelatedFinanceRecordDto();

        var receivableDetails = await ApplyPermissionFilter(_receivableDetailRepository.GetQueryable())
            .Include(rd => rd.Receivable)
                .ThenInclude(r => r.Project)
            .Include(rd => rd.Receivable)
                .ThenInclude(r => r.Customer)
            .Include(rd => rd.Receivable)
                .ThenInclude(r => r.Supplier)
            .Include(rd => rd.Receivable)
                .ThenInclude(r => r.Person)
            .Where(rd => rd.TransactionId == transactionId)
            .ToListAsync();

        foreach (var detail in receivableDetails)
        {
            result.Receivables.Add(new RelatedReceivableDto
            {
                Id = detail.Receivable.Id,
                ProjectId = detail.Receivable.ProjectId,
                ProjectName = detail.Receivable.Project.Name,
                CustomerId = detail.Receivable.CustomerId,
                CustomerName = detail.Receivable.Customer?.Name,
                SupplierId = detail.Receivable.SupplierId,
                SupplierName = detail.Receivable.Supplier?.Name,
                PersonId = detail.Receivable.PersonId,
                PersonName = detail.Receivable.Person?.Name,
                TotalAmount = detail.Receivable.TotalAmount,
                ReceivedAmount = detail.Receivable.ReceivedAmount,
                RemainingAmount = detail.Receivable.RemainingAmount,
                DueDate = detail.Receivable.DueDate,
                Status = detail.Receivable.Status.ToString().ToLower(),
                Description = detail.Receivable.Description,
                PaymentAmount = detail.Amount,
                PaymentDate = detail.PaymentDate
            });
        }

        var payableDetails = await ApplyPermissionFilter(_payableDetailRepository.GetQueryable())
            .Include(pd => pd.Payable)
                .ThenInclude(p => p.Supplier)
            .Include(pd => pd.Payable)
                .ThenInclude(p => p.Customer)
            .Include(pd => pd.Payable)
                .ThenInclude(p => p.Person)
            .Include(pd => pd.Payable)
                .ThenInclude(p => p.Project)
            .Where(pd => pd.TransactionId == transactionId)
            .ToListAsync();

        foreach (var detail in payableDetails)
        {
            result.Payables.Add(new RelatedPayableDto
            {
                Id = detail.Payable.Id,
                SupplierId = detail.Payable.SupplierId,
                SupplierName = detail.Payable.Supplier?.Name,
                CustomerId = detail.Payable.CustomerId,
                CustomerName = detail.Payable.Customer?.Name,
                PersonId = detail.Payable.PersonId,
                PersonName = detail.Payable.Person?.Name,
                ProjectId = detail.Payable.ProjectId,
                ProjectName = detail.Payable.Project?.Name,
                TotalAmount = detail.Payable.TotalAmount,
                PaidAmount = detail.Payable.PaidAmount,
                RemainingAmount = detail.Payable.RemainingAmount,
                DueDate = detail.Payable.DueDate,
                Status = detail.Payable.Status.ToString().ToLower(),
                Description = detail.Payable.Description,
                PaymentAmount = detail.Amount,
                PaymentDate = detail.PaymentDate
            });
        }

        return result;
    }

    private async Task<TransactionStatisticsDto> BuildStatisticsAsync(
        IQueryable<Transaction> query,
        bool includeAllTransferRows)
    {
        var typeAggregates = await query
            .Where(t => t.TransactionType == TransactionType.Income || t.TransactionType == TransactionType.Expense)
            .GroupBy(t => t.TransactionType)
            .Select(g => new TypeAggregate(g.Key, g.Count(), g.Sum(t => (decimal?)t.Amount) ?? 0m))
            .ToListAsync();

        var income = typeAggregates.FirstOrDefault(a => a.Type == TransactionType.Income);
        var expense = typeAggregates.FirstOrDefault(a => a.Type == TransactionType.Expense);
        var incomeCount = income?.Count ?? 0;
        var expenseCount = expense?.Count ?? 0;

        var transferRows = await query
            .Where(t => t.TransactionType == TransactionType.Transfer)
            .Select(t => new TransferProjection(
                t.Id,
                t.Amount,
                t.Description,
                t.TransferDirection,
                t.RelatedTransactionId,
                t.TransactionType))
            .ToListAsync();

        LogTransferDirectionWarnings(transferRows);

        var countedTransfers = includeAllTransferRows
            ? transferRows
            : transferRows
                .Where(t => ResolveProjectionDirection(t) == TransferDirection.Out)
                .ToList();

        var statistics = new TransactionStatisticsDto
        {
            TotalIncome = income?.Sum ?? 0m,
            TotalExpense = expense?.Sum ?? 0m,
            TotalTransfer = countedTransfers.Sum(t => t.Amount),
            IncomeCount = incomeCount,
            ExpenseCount = expenseCount,
            TransferCount = countedTransfers.Count,
            TotalCount = incomeCount + expenseCount + transferRows.Count
        };

        statistics.NetProfit = statistics.TotalIncome - statistics.TotalExpense;
        return statistics;
    }

    private void LogTransferDirectionWarnings(List<TransferProjection> transferRows)
    {
        var transfersWithoutDirection = transferRows
            .Where(t => ResolveProjectionDirection(t) == TransferDirection.None)
            .ToList();

        if (transfersWithoutDirection.Any())
        {
            _logger.LogWarning(
                "发现 {Count} 条方向未明确的转账交易: {TransactionIds}",
                transfersWithoutDirection.Count,
                string.Join(", ", transfersWithoutDirection.Select(t => t.Id)));
        }
    }

    private static TransferDirection ResolveProjectionDirection(TransferProjection transfer)
        => TransactionBalanceHelper.ResolveTransferDirection(
            transfer.TransactionType,
            transfer.TransferDirection,
            transfer.Description,
            transfer.Id,
            transfer.RelatedTransactionId);

    private sealed record TypeAggregate(TransactionType Type, int Count, decimal Sum);

    private sealed record TransferProjection(
        long Id,
        decimal Amount,
        string? Description,
        TransferDirection TransferDirection,
        long? RelatedTransactionId,
        TransactionType TransactionType);
}
