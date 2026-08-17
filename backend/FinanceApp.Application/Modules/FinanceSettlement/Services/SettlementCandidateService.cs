using MapsterMapper;
using FinanceApp.Application.Common;
using FinanceApp.Application.Modules.FinanceSettlement.DTOs.Payable;
using FinanceApp.Application.Modules.FinanceSettlement.DTOs.Receivable;
using FinanceApp.Application.Modules.FinanceSettlement.Interfaces;
using FinanceApp.Application.Services;
using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Enums;
using FinanceApp.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FinanceApp.Application.Modules.FinanceSettlement.Services;

public class SettlementCandidateService : ServiceBase, ISettlementCandidateService
{
    private const int CandidateLimit = 200;

    private readonly IRepository<Transaction> _transactionRepository;
    private readonly IRepository<Receivable> _receivableRepository;
    private readonly IRepository<Payable> _payableRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<SettlementCandidateService> _logger;

    public SettlementCandidateService(
        IRepository<Transaction> transactionRepository,
        IRepository<Receivable> receivableRepository,
        IRepository<Payable> payableRepository,
        IMapper mapper,
        ILogger<SettlementCandidateService> logger,
        ICurrentUserService currentUserService,
        IDataPermissionService permissionService)
        : base(currentUserService, permissionService)
    {
        _transactionRepository = transactionRepository;
        _receivableRepository = receivableRepository;
        _payableRepository = payableRepository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<List<ReceivableDto>> GetAvailableReceivablesForTransactionAsync(long transactionId, string? keyword = null)
    {
        var transaction = await LoadTransactionAsync(transactionId);
        if (transaction.TransactionType != TransactionType.Income)
        {
            throw new ValidationException("只有收入交易才能关联应收款");
        }

        var query = ApplyPermissionFilter(_receivableRepository.GetQueryable())
            .Include(r => r.Project)
            .Include(r => r.Customer)
            .Include(r => r.Supplier)
            .Include(r => r.Person)
            .Include(r => r.ReceivableType)
            .Include(r => r.Details)
            .Where(r => r.Status != ReceivableStatus.Settled && r.RemainingAmount > 0)
            .Where(r => r.Details.All(d => d.TransactionId != transactionId));

        query = ApplyReceivableCounterpartyFilter(query, transaction);
        query = ApplyReceivableKeyword(query, keyword);

        var items = await query.ToListAsync();
        var ranked = items
            .Where(r => SettlementCandidateCompatibility.IsReceivableCompatible(transaction, r))
            .OrderByDescending(r => SettlementCandidateCompatibility.ScoreReceivable(transaction, r))
            .ThenBy(r => r.DueDate ?? DateTime.MaxValue)
            .ThenBy(r => r.Id)
            .Take(CandidateLimit)
            .ToList();

        _logger.LogInformation(
            "查询交易可关联应收: TransactionId={TransactionId}, 返回={Count}",
            transactionId, ranked.Count);

        return _mapper.Map<List<ReceivableDto>>(ranked);
    }

    public async Task<List<PayableDto>> GetAvailablePayablesForTransactionAsync(long transactionId, string? keyword = null)
    {
        var transaction = await LoadTransactionAsync(transactionId);
        if (transaction.TransactionType != TransactionType.Expense)
        {
            throw new ValidationException("只有支出交易才能关联应付款");
        }

        var query = ApplyPermissionFilter(_payableRepository.GetQueryable())
            .Include(p => p.Project)
            .Include(p => p.Customer)
            .Include(p => p.Supplier)
            .Include(p => p.Person)
            .Include(p => p.PayableType)
            .Include(p => p.Details)
            .Where(p => p.Status != PayableStatus.Settled && p.RemainingAmount > 0)
            .Where(p => p.Details.All(d => d.TransactionId != transactionId));

        query = ApplyPayableCounterpartyFilter(query, transaction);
        query = ApplyPayableKeyword(query, keyword);

        var items = await query.ToListAsync();
        var ranked = items
            .Where(p => SettlementCandidateCompatibility.IsPayableCompatible(transaction, p))
            .OrderByDescending(p => SettlementCandidateCompatibility.ScorePayable(transaction, p))
            .ThenBy(p => p.DueDate ?? DateTime.MaxValue)
            .ThenBy(p => p.Id)
            .Take(CandidateLimit)
            .ToList();

        _logger.LogInformation(
            "查询交易可关联应付: TransactionId={TransactionId}, 返回={Count}",
            transactionId, ranked.Count);

        return _mapper.Map<List<PayableDto>>(ranked);
    }

    private async Task<Transaction> LoadTransactionAsync(long transactionId)
    {
        var transaction = await _transactionRepository.GetQueryable()
            .FirstOrDefaultAsync(t => t.Id == transactionId);

        if (transaction == null)
        {
            throw new NotFoundException("交易记录不存在");
        }

        EnsureCanAccess(transaction);
        return transaction;
    }

    private static IQueryable<Receivable> ApplyReceivableCounterpartyFilter(IQueryable<Receivable> query, Transaction transaction)
    {
        if (transaction.CustomerId.HasValue)
        {
            var customerId = transaction.CustomerId.Value;
            return query.Where(r =>
                !r.SupplierId.HasValue &&
                (!r.CustomerId.HasValue || r.CustomerId == customerId));
        }

        if (transaction.SupplierId.HasValue)
        {
            var supplierId = transaction.SupplierId.Value;
            return query.Where(r =>
                !r.CustomerId.HasValue &&
                (!r.SupplierId.HasValue || r.SupplierId == supplierId));
        }

        return query;
    }

    private static IQueryable<Payable> ApplyPayableCounterpartyFilter(IQueryable<Payable> query, Transaction transaction)
    {
        if (transaction.SupplierId.HasValue)
        {
            var supplierId = transaction.SupplierId.Value;
            return query.Where(p =>
                !p.CustomerId.HasValue &&
                (!p.SupplierId.HasValue || p.SupplierId == supplierId));
        }

        if (transaction.CustomerId.HasValue)
        {
            var customerId = transaction.CustomerId.Value;
            return query.Where(p =>
                !p.SupplierId.HasValue &&
                (!p.CustomerId.HasValue || p.CustomerId == customerId));
        }

        return query;
    }

    private static IQueryable<Receivable> ApplyReceivableKeyword(IQueryable<Receivable> query, string? keyword)
    {
        if (string.IsNullOrWhiteSpace(keyword))
        {
            return query;
        }

        var kw = keyword.Trim();
        return query.Where(r =>
            (r.Description != null && r.Description.Contains(kw)) ||
            (r.Project != null && r.Project.Name.Contains(kw)) ||
            (r.Customer != null && r.Customer.Name.Contains(kw)) ||
            (r.Supplier != null && r.Supplier.Name.Contains(kw)) ||
            (r.Person != null && r.Person.Name.Contains(kw)));
    }

    private static IQueryable<Payable> ApplyPayableKeyword(IQueryable<Payable> query, string? keyword)
    {
        if (string.IsNullOrWhiteSpace(keyword))
        {
            return query;
        }

        var kw = keyword.Trim();
        return query.Where(p =>
            (p.Description != null && p.Description.Contains(kw)) ||
            (p.Project != null && p.Project.Name.Contains(kw)) ||
            (p.Customer != null && p.Customer.Name.Contains(kw)) ||
            (p.Supplier != null && p.Supplier.Name.Contains(kw)) ||
            (p.Person != null && p.Person.Name.Contains(kw)));
    }
}
