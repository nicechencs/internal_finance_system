using MapsterMapper;
using FinanceApp.Application.Common;
using FinanceApp.Application.Modules.Identity.Interfaces;
using FinanceApp.Application.Modules.TransactionProcessing.DTOs;
using FinanceApp.Application.Extensions;
using FinanceApp.Application.Modules.TransactionProcessing.Interfaces;
using FinanceApp.Application.Services;
using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Enums;
using FinanceApp.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FinanceApp.Application.Modules.TransactionProcessing.Services;

public class TransactionService : ServiceBase, ITransactionService
{
    private readonly IRepository<Transaction> _transactionRepository;
    private readonly IRepository<TransactionAllocation> _allocationRepository;
    private readonly IRepository<Account> _accountRepository;
    private readonly IRepository<TagBinding> _tagBindingRepository;
    private readonly IRepository<ReceivableDetail> _receivableDetailRepository;
    private readonly IRepository<PayableDetail> _payableDetailRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<TransactionService> _logger;
    private readonly IAllocationService _allocationService;
    private readonly IAccountBalanceService _accountBalanceService;
    private readonly ITransactionQueryService _queryService;
    private readonly ITransferService _transferService;
    private readonly ITransactionStatisticsService _statisticsService;

    public TransactionService(
        IRepository<Transaction> transactionRepository,
        IRepository<TransactionAllocation> allocationRepository,
        IRepository<Account> accountRepository,
        IRepository<TagBinding> tagBindingRepository,
        IRepository<ReceivableDetail> receivableDetailRepository,
        IRepository<PayableDetail> payableDetailRepository,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IAuditLogService auditLogService,
        ILogger<TransactionService> logger,
        ICurrentUserService currentUserService,
        IDataPermissionService permissionService,
        IAllocationService allocationService,
        IAccountBalanceService accountBalanceService,
        ITransactionQueryService queryService,
        ITransferService transferService,
        ITransactionStatisticsService statisticsService)
        : base(currentUserService, permissionService)
    {
        _transactionRepository = transactionRepository;
        _allocationRepository = allocationRepository;
        _accountRepository = accountRepository;
        _tagBindingRepository = tagBindingRepository;
        _receivableDetailRepository = receivableDetailRepository;
        _payableDetailRepository = payableDetailRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _auditLogService = auditLogService;
        _logger = logger;
        _allocationService = allocationService;
        _accountBalanceService = accountBalanceService;
        _queryService = queryService;
        _transferService = transferService;
        _statisticsService = statisticsService;
    }

    public Task<PageResponse<TransactionDto>> GetPagedAsync(PageRequest request)
        => _queryService.GetPagedAsync(request);

    public Task<TransactionDto> GetByIdAsync(long id)
        => _queryService.GetByIdAsync(id);

    public Task<List<TransactionDto>> GetByAccountAsync(long accountId)
        => _queryService.GetByAccountAsync(accountId);

    public Task<List<TransactionDto>> GetByProjectAsync(long projectId)
        => _queryService.GetByProjectAsync(projectId);

    public Task<List<TransactionDto>> GetByCategoryAsync(long categoryId)
        => _queryService.GetByCategoryAsync(categoryId);

    public Task<List<TransactionDto>> GetByCustomerAsync(long customerId)
        => _queryService.GetByCustomerAsync(customerId);

    public Task<List<TransactionDto>> GetBySupplierAsync(long supplierId)
        => _queryService.GetBySupplierAsync(supplierId);

    public Task<List<TransactionDto>> GetByPersonAsync(long personId)
        => _queryService.GetByPersonAsync(personId);

    public Task<decimal> GetAccountBalanceAsync(long accountId)
        => _accountBalanceService.GetAccountBalanceAsync(accountId);

    public Task<TransferResultDto> CreateTransferAsync(CreateTransferRequest request)
        => _transferService.CreateTransferAsync(request);

    public Task<TransactionStatisticsDto> GetStatisticsAsync()
        => _statisticsService.GetStatisticsAsync();

    public Task<TransactionStatisticsDto> GetStatisticsAsync(PageRequest request)
        => _statisticsService.GetStatisticsAsync(request);

    public Task<TransactionStatisticsDto> GetAccountStatisticsAsync(long accountId)
        => _statisticsService.GetAccountStatisticsAsync(accountId);

    public Task<TransactionStatisticsDto> GetCustomerStatisticsAsync(long customerId)
        => _statisticsService.GetCustomerStatisticsAsync(customerId);

    public Task<TransactionStatisticsDto> GetSupplierStatisticsAsync(long supplierId)
        => _statisticsService.GetSupplierStatisticsAsync(supplierId);

    public Task<TransactionStatisticsDto> GetPersonStatisticsAsync(long personId)
        => _statisticsService.GetPersonStatisticsAsync(personId);

    public Task<RelatedFinanceRecordDto> GetRelatedFinanceRecordsAsync(long transactionId)
        => _statisticsService.GetRelatedFinanceRecordsAsync(transactionId);

    /// <summary>
    /// 获取可用于应收款绑定的收入交易
    /// </summary>
    public async Task<List<TransactionDto>> GetAvailableForReceivableAsync(long? projectId = null, long? customerId = null, long? supplierId = null, long? personId = null, bool showAll = false, string? keyword = null)
    {
        var query = _transactionRepository.GetQueryable()
            .Include(t => t.Account)
            .Include(t => t.Category)
            .Include(t => t.Project)
            .Include(t => t.Customer)
            .Include(t => t.Supplier)
            .Include(t => t.Person)
            .Include(t => t.BankTransaction)
            .Include(t => t.ReceivableDetails)
            .Include(t => t.PayableDetails)
            .Where(t => t.TransactionType == TransactionType.Income)
            .Where(t => t.AllocationStatus != AllocationStatus.FullyAllocated);

        if (!showAll)
        {
            if (projectId.HasValue)
            {
                query = query.Where(t => !t.ProjectId.HasValue || t.ProjectId == projectId.Value);
            }

            // 支持三种对方类型的筛选
            if (customerId.HasValue)
            {
                query = query.Where(t =>
                    (!t.CustomerId.HasValue || t.CustomerId == customerId.Value) &&
                    !t.SupplierId.HasValue &&
                    !t.PersonId.HasValue);
            }
            else if (supplierId.HasValue)
            {
                query = query.Where(t =>
                    (!t.SupplierId.HasValue || t.SupplierId == supplierId.Value) &&
                    !t.CustomerId.HasValue &&
                    !t.PersonId.HasValue);
            }
            else if (personId.HasValue)
            {
                query = query.Where(t =>
                    (!t.PersonId.HasValue || t.PersonId == personId.Value) &&
                    !t.CustomerId.HasValue &&
                    !t.SupplierId.HasValue);
            }
        }

        // 关键词搜索：匹配备注、银行对方、摘要、交易描述
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var kw = keyword.Trim();
            query = query.Where(t =>
                (t.Description != null && t.Description.Contains(kw)) ||
                (t.BankTransaction != null && t.BankTransaction.Counterparty != null && t.BankTransaction.Counterparty.Contains(kw)) ||
                (t.BankTransaction != null && t.BankTransaction.Description != null && t.BankTransaction.Description.Contains(kw)) ||
                (t.BankTransaction != null && t.BankTransaction.Memo != null && t.BankTransaction.Memo.Contains(kw)));
        }

        query = ApplyPermissionFilter(query);

        var transactions = await query
            .OrderByDescending(t => t.TransactionDate)
            .Take(200)
            .ToListAsync();

        return BuildSelectableTransactions(
            transactions,
            transaction => CalculateSettlementCandidateScore(transaction, projectId, customerId, supplierId, personId));
    }

    /// <summary>
    /// 获取可用于应付款绑定的支出交易
    /// </summary>
    public Task<List<TransactionDto>> GetAvailableForPayableAsync(long? projectId = null, long? supplierId = null, long? customerId = null, long? personId = null, bool showAll = false, string? keyword = null)
    {
        return GetAvailableTransactionsForPayableAsync(projectId, supplierId, customerId, personId, showAll, keyword);
    }

    private async Task<List<TransactionDto>> GetAvailableTransactionsForPayableAsync(
        long? projectId = null,
        long? supplierId = null,
        long? customerId = null,
        long? personId = null,
        bool showAll = false,
        string? keyword = null)
    {
        var query = _transactionRepository.GetQueryable()
            .Include(t => t.Account)
            .Include(t => t.Category)
            .Include(t => t.Project)
            .Include(t => t.Supplier)
            .Include(t => t.Customer)
            .Include(t => t.Person)
            .Include(t => t.BankTransaction)
            .Include(t => t.ReceivableDetails)
            .Include(t => t.PayableDetails)
            .Where(t => t.TransactionType == TransactionType.Expense)
            .Where(t => t.AllocationStatus != AllocationStatus.FullyAllocated);

        if (!showAll)
        {
            if (projectId.HasValue)
            {
                query = query.Where(t => !t.ProjectId.HasValue || t.ProjectId == projectId.Value);
            }

            // 支持三种对方类型的筛选
            if (supplierId.HasValue)
            {
                query = query.Where(t =>
                    (!t.SupplierId.HasValue || t.SupplierId == supplierId.Value) &&
                    !t.CustomerId.HasValue &&
                    !t.PersonId.HasValue);
            }
            else if (customerId.HasValue)
            {
                query = query.Where(t =>
                    (!t.CustomerId.HasValue || t.CustomerId == customerId.Value) &&
                    !t.SupplierId.HasValue &&
                    !t.PersonId.HasValue);
            }
            else if (personId.HasValue)
            {
                query = query.Where(t =>
                    (!t.PersonId.HasValue || t.PersonId == personId.Value) &&
                    !t.CustomerId.HasValue &&
                    !t.SupplierId.HasValue);
            }
        }

        // 关键词搜索：匹配备注、银行对方、摘要、交易描述
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var kw = keyword.Trim();
            query = query.Where(t =>
                (t.Description != null && t.Description.Contains(kw)) ||
                (t.BankTransaction != null && t.BankTransaction.Counterparty != null && t.BankTransaction.Counterparty.Contains(kw)) ||
                (t.BankTransaction != null && t.BankTransaction.Description != null && t.BankTransaction.Description.Contains(kw)) ||
                (t.BankTransaction != null && t.BankTransaction.Memo != null && t.BankTransaction.Memo.Contains(kw)));
        }

        query = ApplyPermissionFilter(query);

        var transactions = await query
            .OrderByDescending(t => t.TransactionDate)
            .Take(200)
            .ToListAsync();

        return BuildSelectableTransactions(
            transactions,
            transaction => CalculateSettlementCandidateScore(transaction, projectId, customerId, supplierId, personId));
    }

    private async Task<List<TransactionDto>> GetAvailableTransactionsAsync(
        TransactionType transactionType,
        long? projectId = null,
        long? counterpartyId = null,
        bool isCustomer = true)
    {
        var query = _transactionRepository.GetQueryable()
            .Include(t => t.Account)
            .Include(t => t.Category)
            .Include(t => t.Project)
            .Include(t => t.ReceivableDetails)
            .Include(t => t.PayableDetails)
            .Where(t => t.TransactionType == transactionType)
            .Where(t => t.AllocationStatus != AllocationStatus.FullyAllocated);

        // 根据交易类型 Include 对应的对方
        if (transactionType == TransactionType.Income)
        {
            query = query.Include(t => t.Customer);
        }
        else if (transactionType == TransactionType.Expense)
        {
            query = query.Include(t => t.Supplier);
        }

        if (projectId.HasValue)
        {
            query = query.Where(t => t.ProjectId == projectId.Value);
        }

        if (counterpartyId.HasValue)
        {
            query = isCustomer
                ? query.Where(t => t.CustomerId == counterpartyId.Value)
                : query.Where(t => t.SupplierId == counterpartyId.Value);
        }

        query = ApplyPermissionFilter(query);

        var transactions = await query
            .OrderByDescending(t => t.TransactionDate)
            .Take(50)
            .ToListAsync();

        var dtos = transactions.Select(t =>
        {
            var dto = _mapper.Map<TransactionDto>(t);
            dto.AvailableAmount = t.GetAvailableAmount();
            return dto;
        }).ToList();

        return dtos;
    }

    private List<TransactionDto> BuildSelectableTransactions(
        IEnumerable<Transaction> transactions,
        Func<Transaction, int> scoreSelector)
    {
        return transactions
            .Where(t => t.GetAvailableAmount() > 0)
            .OrderByDescending(scoreSelector)
            .ThenByDescending(t => t.TransactionDate)
            .Take(50)
            .Select(t =>
            {
                var dto = _mapper.Map<TransactionDto>(t);
                dto.AvailableAmount = t.GetAvailableAmount();
                return dto;
            })
            .ToList();
    }

    private static int CalculateSettlementCandidateScore(
        Transaction transaction,
        long? projectId,
        long? customerId,
        long? supplierId,
        long? personId)
    {
        var score = 0;

        if (projectId.HasValue)
        {
            score += transaction.ProjectId == projectId.Value ? 2 : 1;
        }

        if (customerId.HasValue)
        {
            score += transaction.CustomerId == customerId.Value ? 3 : 1;
        }
        else if (supplierId.HasValue)
        {
            score += transaction.SupplierId == supplierId.Value ? 3 : 1;
        }
        else if (personId.HasValue)
        {
            score += transaction.PersonId == personId.Value ? 3 : 1;
        }
        else if (!transaction.CustomerId.HasValue && !transaction.SupplierId.HasValue && !transaction.PersonId.HasValue)
        {
            score += 1;
        }

        return score;
    }

    public async Task<TransactionDto> CreateAsync(CreateTransactionRequest request)
    {
        if (!Enum.TryParse<TransactionType>(request.TransactionType, true, out var transactionType) ||
            transactionType == TransactionType.Transfer)
        {
            throw new ValidationException("无效的交易类型");
        }

        var account = await _accountRepository.GetByIdAsync(request.AccountId)
            ?? throw new NotFoundException("账户不存在");

        var hasAllocations = request.Allocations is { Count: > 0 };
        if (hasAllocations)
        {
            _allocationService.ValidateAllocations(request.Allocations!, request.Amount);
        }

        await using var dbTransaction = await _unitOfWork.BeginTransactionAsync();

        _logger.LogDebug("开始创建交易: 日期={Date}, 金额={Amount}, 类型={Type}, 账户={Account}",
            request.TransactionDate, request.Amount, request.TransactionType, request.AccountId);

        try
        {
            var transaction = new Transaction
            {
                TransactionDate = request.TransactionDate,
                Amount = request.Amount,
                TransactionType = transactionType,
                AccountId = request.AccountId,
                CategoryId = request.CategoryId,
                Description = request.Description,
                Status = TransactionStatus.Confirmed,
                IsAllocated = hasAllocations
            };

            if (!hasAllocations)
            {
                transaction.ProjectId = request.ProjectId;
                transaction.CustomerId = request.CustomerId;
                transaction.SupplierId = request.SupplierId;
                transaction.PersonId = request.PersonId;
            }

            await _transactionRepository.AddAsync(transaction);
            await _unitOfWork.SaveChangesAsync();

            if (hasAllocations)
            {
                await _allocationService.CreateAllocationsAsync(transaction.Id, request.Allocations!, request.Amount);
            }

            _accountBalanceService.AdjustBalanceWithoutSave(account, transaction.Amount, transactionType);
            _accountRepository.Update(account);
            await _unitOfWork.SaveChangesAsync();

            if (dbTransaction != null)
            {
                await dbTransaction.CommitAsync();
            }

            var dto = await GetByIdAsync(transaction.Id);
            await _auditLogService.LogAsync("Create", "Transaction", transaction.Id, null, SerializeForAudit(dto));
            
            _logger.LogInformation("交易创建成功: Id={Id}, 最终余额={Balance}", transaction.Id, account.CurrentBalance);
            return dto;
        }
        catch (DbUpdateConcurrencyException ex)
        {
            if (dbTransaction != null)
            {
                await dbTransaction.RollbackAsync();
            }
            _logger.LogWarning(ex, "交易创建失败：检测到账户并发更新冲突，事务已回滚: AccountId={AccountId}", request.AccountId);
            throw new ValidationException("账户正在被其他操作更新，请稍后重试");
        }
        catch (Exception ex)
        {
            if (dbTransaction != null)
            {
                await dbTransaction.RollbackAsync();
                _logger.LogWarning("交易创建失败，事务已回滚: Error={Message}", ex.Message);
            }

            throw;
        }
    }

    public async Task<TransactionDto> UpdateAsync(long id, UpdateTransactionRequest request)
    {
        var transaction = await _transactionRepository.GetQueryable()
            .IncludeForEdit()
            .FirstOrDefaultAsync(t => t.Id == id)
            ?? throw new NotFoundException("交易记录不存在");

        EnsureCanEdit(transaction);

        if (transaction.TransactionType == TransactionType.Transfer)
        {
            throw new ValidationException("转账记录请使用专用转账流程维护");
        }

        // 验证交易类型
        if (!Enum.TryParse<TransactionType>(request.TransactionType, true, out var newTransactionType))
        {
            throw new ValidationException($"无效的交易类型: {request.TransactionType}");
        }

        // 验证账户存在
        var newAccount = await _accountRepository.GetByIdAsync(request.AccountId)
            ?? throw new NotFoundException("账户不存在");

        if (newTransactionType == TransactionType.Transfer)
        {
            throw new ValidationException("转账记录请使用专用转账流程维护");
        }

        var linkedReceivableAmount = await _receivableDetailRepository.GetQueryable()
            .Where(x => !x.IsDeleted && x.TransactionId == transaction.Id)
            .SumAsync(x => (decimal?)x.Amount) ?? 0m;
        var linkedPayableAmount = await _payableDetailRepository.GetQueryable()
            .Where(x => !x.IsDeleted && x.TransactionId == transaction.Id)
            .SumAsync(x => (decimal?)x.Amount) ?? 0m;

        ValidateTransactionUpdateAgainstFinanceBindings(
            newTransactionType,
            request.Amount,
            linkedReceivableAmount,
            linkedPayableAmount);

        var hasAllocations = request.Allocations is { Count: > 0 };
        if (hasAllocations)
        {
            _allocationService.ValidateAllocations(request.Allocations!, request.Amount);
        }

        // 如果存在应收或应付绑定，禁止修改项目和对方字段
        var hasFinanceBindings = linkedReceivableAmount > 0 || linkedPayableAmount > 0;
        if (hasFinanceBindings)
        {
            long? newProjectId, newCustomerId, newSupplierId, newPersonId;

            if (hasAllocations && request.Allocations!.Count > 1)
            {
                // 多条分摊：字段将被清空为 null
                newProjectId = null;
                newCustomerId = null;
                newSupplierId = null;
                newPersonId = null;
            }
            else if (hasAllocations && request.Allocations!.Count == 1)
            {
                // 单条分摊：ProjectId/PersonId 来自分摊，CustomerId/SupplierId 来自 request
                var allocation = request.Allocations[0];
                newProjectId = allocation.ProjectId;
                newPersonId = allocation.PersonId;
                newCustomerId = request.CustomerId;
                newSupplierId = request.SupplierId;
            }
            else
            {
                // 无分摊：全部来自 request
                newProjectId = request.ProjectId;
                newPersonId = request.PersonId;
                newCustomerId = request.CustomerId;
                newSupplierId = request.SupplierId;
            }

            if (transaction.ProjectId != newProjectId ||
                transaction.CustomerId != newCustomerId ||
                transaction.SupplierId != newSupplierId ||
                transaction.PersonId != newPersonId)
            {
                throw new ValidationException("该交易已关联应收/应付明细，不允许修改项目或对方信息。如需修改，请先取消关联。");
            }
        }

        var oldDto = await GetByIdAsync(transaction.Id);

        await using var dbTransaction = await _unitOfWork.BeginTransactionAsync();

        _logger.LogDebug("开始更新交易: Id={Id}, 调整内容={Request}", id, request.Description);
        try
        {
            // 如果金额、账户或交易类型发生变化，需要重新计算账户余额
            var needRecalculateBalance =
                transaction.Amount != request.Amount ||
                transaction.AccountId != request.AccountId ||
                transaction.TransactionType != newTransactionType;

            if (needRecalculateBalance)
            {
                // 获取旧账户（如果账户变更，需要单独获取）
                var oldAccount = transaction.AccountId == request.AccountId
                    ? newAccount
                    : await _accountRepository.GetByIdAsync(transaction.AccountId)
                        ?? throw new NotFoundException("原账户不存在");

                // 先恢复旧账户余额（撤销原交易的影响）
                var oldDelta = transaction.TransactionType == TransactionType.Income ? -transaction.Amount : transaction.Amount;
                _accountBalanceService.AdjustBalanceDirectWithoutSave(oldAccount, oldDelta);

                // 应用新账户余额（应用新交易的影响）
                var newDelta = newTransactionType == TransactionType.Income ? request.Amount : -request.Amount;
                _accountBalanceService.AdjustBalanceDirectWithoutSave(newAccount, newDelta);

                // 更新账户
                if (transaction.AccountId != request.AccountId)
                {
                    _accountRepository.Update(oldAccount);
                }
                _accountRepository.Update(newAccount);
            }

            // 更新交易基本信息
            transaction.TransactionDate = request.TransactionDate;
            transaction.TransactionType = newTransactionType;
            transaction.Amount = request.Amount;
            transaction.AccountId = request.AccountId;
            transaction.CategoryId = request.CategoryId;
            transaction.Description = request.Description;

            // 删除旧的分摊记录
            foreach (var oldAllocation in transaction.Allocations.ToList())
            {
                _allocationRepository.Delete(oldAllocation);
            }

            // 处理新的分摊记录
            if (hasAllocations && request.Allocations!.Count > 1)
            {
                transaction.IsAllocated = true;
                transaction.ProjectId = null;
                transaction.PersonId = null;
                transaction.CustomerId = null;
                transaction.SupplierId = null;

                await _allocationService.CreateAllocationsAsync(transaction.Id, request.Allocations, transaction.Amount);
            }
            else if (hasAllocations && request.Allocations!.Count == 1)
            {
                var allocation = request.Allocations[0];
                transaction.IsAllocated = false;
                transaction.ProjectId = allocation.ProjectId;
                transaction.PersonId = allocation.PersonId;
                transaction.CustomerId = request.CustomerId;
                transaction.SupplierId = request.SupplierId;
            }
            else
            {
                transaction.IsAllocated = false;
                transaction.ProjectId = request.ProjectId;
                transaction.PersonId = request.PersonId;
                transaction.CustomerId = request.CustomerId;
                transaction.SupplierId = request.SupplierId;
            }

            _transactionRepository.Update(transaction);
            await _unitOfWork.SaveChangesAsync();

            if (dbTransaction != null)
            {
                await dbTransaction.CommitAsync();
            }

            var newDto = await GetByIdAsync(transaction.Id);
            await _auditLogService.LogAsync("Update", "Transaction", transaction.Id, SerializeForAudit(oldDto), SerializeForAudit(newDto));

            _logger.LogInformation("交易更新成功: Id={Id}, 描述={Desc}", transaction.Id, transaction.Description);
            return newDto;
        }
        catch (DbUpdateConcurrencyException ex)
        {
            if (dbTransaction != null)
            {
                await dbTransaction.RollbackAsync();
            }
            _logger.LogWarning(ex, "交易更新失败：检测到账户并发更新冲突，事务已回滚: Id={Id}", id);
            throw new ValidationException("账户正在被其他操作更新，请稍后重试");
        }
        catch (Exception ex)
        {
            if (dbTransaction != null)
            {
                await dbTransaction.RollbackAsync();
                _logger.LogWarning("交易更新失败，事务已回滚: Id={Id}, Error={Message}", id, ex.Message);
            }

            throw;
        }
    }

    public async Task DeleteAsync(long id)
    {
        var transaction = await _transactionRepository.GetQueryable()
            .Include(t => t.Account)
            .Include(t => t.BankTransaction)
            .FirstOrDefaultAsync(t => t.Id == id)
            ?? throw new NotFoundException("交易记录不存在");

        EnsureCanDelete(transaction);

        await using var dbTransaction = await _unitOfWork.BeginTransactionAsync();

        try
        {
            var transactionsToDelete = new List<Transaction> { transaction };

            if (transaction.TransactionType == TransactionType.Transfer && transaction.RelatedTransactionId.HasValue)
            {
                var relatedTransaction = await _transactionRepository.GetQueryable()
                    .Include(t => t.Account)
                    .Include(t => t.BankTransaction)
                    .FirstOrDefaultAsync(t => t.Id == transaction.RelatedTransactionId.Value);

                if (relatedTransaction != null && !relatedTransaction.IsDeleted)
                {
                    transactionsToDelete.Add(relatedTransaction);
                }
            }

            var distinctTransactions = transactionsToDelete.DistinctBy(t => t.Id).ToList();

            foreach (var transactionToDelete in distinctTransactions)
            {
                await EnsureTransactionCanBeDeletedAsync(transactionToDelete);
            }

            foreach (var transactionToDelete in distinctTransactions)
            {
                transactionToDelete.Account.CurrentBalance -= TransactionBalanceHelper.GetSignedAmount(transactionToDelete);
                _accountRepository.Update(transactionToDelete.Account);

                if (transactionToDelete.BankTransaction != null)
                {
                    transactionToDelete.BankTransaction.IsProcessed = false;
                }

                transactionToDelete.IsDeleted = true;
                transactionToDelete.DeletedAt = DateTime.UtcNow;
                _transactionRepository.Update(transactionToDelete);
            }

            await _unitOfWork.SaveChangesAsync();

            if (dbTransaction != null)
            {
                await dbTransaction.CommitAsync();
            }

            var oldSnapshot = SerializeForAudit(new { transaction.Id, transaction.TransactionDate, transaction.Amount, TransactionType = transaction.TransactionType.ToString(), transaction.AccountId, transaction.Description });
            await _auditLogService.LogAsync("Delete", "Transaction", transaction.Id, oldSnapshot, null);
            
            _logger.LogInformation("交易删除成功: Id={Id}, 涉及账户数={Count}", transaction.Id, transactionsToDelete.Count);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            if (dbTransaction != null)
            {
                await dbTransaction.RollbackAsync();
            }
            _logger.LogWarning(ex, "交易删除失败：检测到账户并发更新冲突，事务已回滚: Id={Id}", id);
            throw new ValidationException("账户正在被其他操作更新，请稍后重试");
        }
        catch (Exception ex)
        {
            if (dbTransaction != null)
            {
                await dbTransaction.RollbackAsync();
                _logger.LogWarning("交易删除失败，事务已回滚: Id={Id}, Error={Message}", id, ex.Message);
            }

            throw;
        }
    }

    public async Task<List<TransactionDto>> GetTransferCandidatesAsync(long transactionId, long targetAccountId)
    {
        var sourceTransaction = await LoadTransactionForTransferAsync(transactionId);
        EnsureCanEdit(sourceTransaction);
        await ValidateTransferSourceAsync(sourceTransaction);

        if (sourceTransaction.AccountId == targetAccountId)
        {
            throw new ValidationException("目标账户不能与原账户相同");
        }

        var targetAccount = await _accountRepository.GetByIdAsync(targetAccountId)
            ?? throw new NotFoundException("目标账户不存在");
        EnsureCanAccess(targetAccount);

        var expectedTransactionType = GetOppositeTransactionType(sourceTransaction.TransactionType);
        var minDate = sourceTransaction.TransactionDate.Date.AddDays(-7);
        var maxDate = sourceTransaction.TransactionDate.Date.AddDays(8);
        var linkedTransactionIds = await GetLinkedFinanceTransactionIdsAsync();

        var candidates = await ApplyPermissionFilter(_transactionRepository.GetQueryable())
            .IncludeFullDetails()
            .Where(t =>
                t.Id != sourceTransaction.Id &&
                t.AccountId == targetAccountId &&
                t.TransactionType == expectedTransactionType &&
                t.RelatedTransactionId == null &&
                !t.IsAllocated &&
                t.Amount == sourceTransaction.Amount &&
                t.TransactionDate >= minDate &&
                t.TransactionDate < maxDate)
            .ToListAsync();

        var orderedCandidates = candidates
            .Where(t => !linkedTransactionIds.Contains(t.Id))
            .OrderBy(t => Math.Abs((t.TransactionDate.Date - sourceTransaction.TransactionDate.Date).TotalDays))
            .ThenByDescending(t => t.CreatedAt)
            .ToList();

        var candidateDtos = _mapper.Map<List<TransactionDto>>(orderedCandidates);
        await _tagBindingRepository.GetQueryable().ApplyTagsAsync(
            TagScope.Transaction,
            candidateDtos,
            dto => dto.Id,
            (dto, tags) => dto.Tags = tags);

        return candidateDtos;
    }

    public async Task<TransferResultDto> ConvertToTransferAsync(long transactionId, ConvertTransactionToTransferRequest request)
    {
        var sourceTransaction = await LoadTransactionForTransferAsync(transactionId);
        EnsureCanEdit(sourceTransaction);
        await ValidateTransferSourceAsync(sourceTransaction);

        if (sourceTransaction.AccountId == request.TargetAccountId)
        {
            throw new ValidationException("目标账户不能与原账户相同");
        }

        var targetAccount = await _accountRepository.GetByIdAsync(request.TargetAccountId)
            ?? throw new NotFoundException("目标账户不存在");
        EnsureCanAccess(targetAccount);

        var originalSourceTransactionType = sourceTransaction.TransactionType;
        var sourceDirection = TransactionBalanceHelper.GetDirectionForTransactionType(originalSourceTransactionType);
        if (sourceDirection == TransferDirection.None)
        {
            throw new ValidationException("只有收入或支出交易可以标记为内部转账");
        }

        var targetDirection = GetOppositeDirection(sourceDirection);

        await using var dbTransaction = await _unitOfWork.BeginTransactionAsync();

        try
        {
            Transaction targetTransaction;

            if (request.MatchedTransactionId.HasValue)
            {
                targetTransaction = await LoadTransactionForTransferAsync(request.MatchedTransactionId.Value);
                EnsureCanEdit(targetTransaction);
                await ValidateMatchedTransferTargetAsync(
                    sourceTransaction,
                    targetTransaction,
                    request.TargetAccountId,
                    originalSourceTransactionType);

                NormalizeAsTransfer(sourceTransaction, targetAccount, sourceDirection, request.Description);
                NormalizeAsTransfer(targetTransaction, sourceTransaction.Account, targetDirection, request.Description);
                targetTransaction.RelatedTransactionId = sourceTransaction.Id;
                _transactionRepository.Update(targetTransaction);
            }
            else
            {
                NormalizeAsTransfer(sourceTransaction, targetAccount, sourceDirection, request.Description);
                targetTransaction = new Transaction
                {
                    TransactionDate = sourceTransaction.TransactionDate,
                    Amount = sourceTransaction.Amount,
                    TransactionType = TransactionType.Transfer,
                    TransferDirection = targetDirection,
                    AccountId = targetAccount.Id,
                    Description = BuildTransferDescription(request.Description, sourceTransaction.Account, targetDirection),
                    Status = TransactionStatus.Confirmed,
                    IsAllocated = false,
                    RelatedTransactionId = sourceTransaction.Id
                };

                await _transactionRepository.AddAsync(targetTransaction);
                await _unitOfWork.SaveChangesAsync();

                targetAccount.CurrentBalance += TransactionBalanceHelper.GetSignedAmount(targetTransaction);
                _accountRepository.Update(targetAccount);
            }

            sourceTransaction.RelatedTransactionId = targetTransaction.Id;
            _transactionRepository.Update(sourceTransaction);
            await _unitOfWork.SaveChangesAsync();

            if (dbTransaction != null)
            {
                await dbTransaction.CommitAsync();
            }

            await _auditLogService.LogAsync("ConvertToTransfer", "Transaction", sourceTransaction.Id, null, SerializeForAudit(new { sourceTransaction.Id, sourceTransaction.Amount, targetTransactionId = targetTransaction.Id }));
            await _auditLogService.LogAsync("ConvertToTransfer", "Transaction", targetTransaction.Id, null, SerializeForAudit(new { targetTransaction.Id, targetTransaction.Amount, sourceTransactionId = sourceTransaction.Id }));

            return await BuildTransferResultAsync(sourceTransaction.Id, targetTransaction.Id);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            if (dbTransaction != null)
            {
                await dbTransaction.RollbackAsync();
            }
            _logger.LogWarning(ex, "交易转换为转账失败：检测到账户并发更新冲突，事务已回滚");
            throw new ValidationException("账户正在被其他操作更新，请稍后重试");
        }
        catch (Exception ex)
        {
            if (dbTransaction != null)
            {
                await dbTransaction.RollbackAsync();
                _logger.LogWarning("交易操作失败，事务已回滚: Error={Message}", ex.Message);
            }

            throw;
        }
    }

    private async Task<Transaction> LoadTransactionForTransferAsync(long transactionId)
    {
        return await _transactionRepository.GetQueryable()
            .IncludeFullDetails()
            .FirstOrDefaultAsync(t => t.Id == transactionId)
            ?? throw new NotFoundException("交易记录不存在");
    }

    private async Task ValidateTransferSourceAsync(Transaction transaction)
    {
        if (transaction.TransactionType == TransactionType.Transfer)
        {
            throw new ValidationException("该交易已经是转账记录");
        }

        if (transaction.IsAllocated || transaction.Allocations.Count > 0)
        {
            throw new ValidationException("已分摊交易不能标记为内部转账");
        }

        await EnsureTransactionNotLinkedToFinanceRecordsAsync(transaction.Id);
    }

    private async Task ValidateMatchedTransferTargetAsync(
        Transaction sourceTransaction,
        Transaction targetTransaction,
        long targetAccountId,
        TransactionType originalSourceTransactionType)
    {
        if (targetTransaction.Id == sourceTransaction.Id)
        {
            throw new ValidationException("不能将同一条交易配对为转账");
        }

        if (targetTransaction.AccountId != targetAccountId)
        {
            throw new ValidationException("匹配交易不属于目标账户");
        }

        if (targetTransaction.TransactionType == TransactionType.Transfer)
        {
            throw new ValidationException("匹配交易已经是转账记录");
        }

        if (targetTransaction.RelatedTransactionId.HasValue)
        {
            throw new ValidationException("匹配交易已经和其他转账关联");
        }

        if (targetTransaction.IsAllocated || targetTransaction.Allocations.Count > 0)
        {
            throw new ValidationException("已分摊交易不能作为转账匹配目标");
        }

        if (targetTransaction.Amount != sourceTransaction.Amount)
        {
            throw new ValidationException("匹配交易金额必须与原交易一致");
        }

        if (Math.Abs((targetTransaction.TransactionDate.Date - sourceTransaction.TransactionDate.Date).TotalDays) > 7)
        {
            throw new ValidationException("匹配交易日期与原交易相差过大，请重新选择");
        }

        var expectedTransactionType = GetOppositeTransactionType(originalSourceTransactionType);
        if (targetTransaction.TransactionType != expectedTransactionType)
        {
            throw new ValidationException("匹配交易的收支方向与原交易不一致");
        }

        await EnsureTransactionNotLinkedToFinanceRecordsAsync(targetTransaction.Id);
    }

    private async Task EnsureTransactionNotLinkedToFinanceRecordsAsync(long transactionId)
    {
        if (await _receivableDetailRepository.GetQueryable().AnyAsync(x => !x.IsDeleted && x.TransactionId == transactionId))
        {
            throw new ValidationException("该交易已关联应收记录，不能标记为内部转账");
        }

        if (await _payableDetailRepository.GetQueryable().AnyAsync(x => !x.IsDeleted && x.TransactionId == transactionId))
        {
            throw new ValidationException("该交易已关联应付记录，不能标记为内部转账");
        }
    }

    private async Task EnsureTransactionCanBeDeletedAsync(Transaction transaction)
    {
        if (transaction.IsAllocated)
        {
            throw new ValidationException("已分摊的交易不允许删除，请先撤销分摊");
        }

        if (await _allocationRepository.GetQueryable().AnyAsync(x => x.TransactionId == transaction.Id))
        {
            throw new ValidationException("已分摊的交易不允许删除，请先撤销分摊");
        }

        if (await _receivableDetailRepository.GetQueryable().AnyAsync(x => !x.IsDeleted && x.TransactionId == transaction.Id))
        {
            throw new ValidationException("已关联应收明细的交易不允许删除，请先撤销收款关联");
        }

        if (await _payableDetailRepository.GetQueryable().AnyAsync(x => !x.IsDeleted && x.TransactionId == transaction.Id))
        {
            throw new ValidationException("已关联应付明细的交易不允许删除，请先撤销付款关联");
        }
    }

    private static void ValidateTransactionUpdateAgainstFinanceBindings(
        TransactionType newTransactionType,
        decimal newAmount,
        decimal linkedReceivableAmount,
        decimal linkedPayableAmount)
    {
        if (linkedReceivableAmount > 0 && newTransactionType != TransactionType.Income)
        {
            throw new ValidationException("已关联应收明细的交易必须保持收入类型");
        }

        if (linkedPayableAmount > 0 && newTransactionType != TransactionType.Expense)
        {
            throw new ValidationException("已关联应付明细的交易必须保持支出类型");
        }

        if (linkedReceivableAmount > 0 && newAmount < linkedReceivableAmount)
        {
            throw new ValidationException("交易金额不能小于已关联的应收核销金额");
        }

        if (linkedPayableAmount > 0 && newAmount < linkedPayableAmount)
        {
            throw new ValidationException("交易金额不能小于已关联的应付核销金额");
        }
    }

    private async Task<HashSet<long>> GetLinkedFinanceTransactionIdsAsync()
    {
        var receivableIds = await _receivableDetailRepository.GetQueryable()
            .Where(x => !x.IsDeleted)
            .Select(x => x.TransactionId)
            .ToListAsync();
        var payableIds = await _payableDetailRepository.GetQueryable()
            .Where(x => !x.IsDeleted)
            .Select(x => x.TransactionId)
            .ToListAsync();

        return receivableIds.Concat(payableIds).ToHashSet();
    }

    private static void ResetBusinessFields(Transaction transaction)
    {
        transaction.CategoryId = null;
        transaction.ProjectId = null;
        transaction.CustomerId = null;
        transaction.SupplierId = null;
        transaction.PersonId = null;
        transaction.IsAllocated = false;
    }

    private static TransactionType GetOppositeTransactionType(TransactionType transactionType)
    {
        return transactionType switch
        {
            TransactionType.Expense => TransactionType.Income,
            TransactionType.Income => TransactionType.Expense,
            _ => throw new ValidationException("只有收入或支出交易可以识别为转账")
        };
    }

    private static TransferDirection GetOppositeDirection(TransferDirection direction)
    {
        return direction switch
        {
            TransferDirection.Out => TransferDirection.In,
            TransferDirection.In => TransferDirection.Out,
            _ => TransferDirection.None
        };
    }

    private static string BuildTransferDescription(
        string? description,
        Account counterpartAccount,
        TransferDirection direction)
    {
        return string.IsNullOrWhiteSpace(description)
            ? TransactionBalanceHelper.BuildDefaultTransferDescription(counterpartAccount, direction)
            : description;
    }

    private static void NormalizeAsTransfer(
        Transaction transaction,
        Account counterpartAccount,
        TransferDirection direction,
        string? description)
    {
        ResetBusinessFields(transaction);
        transaction.TransactionType = TransactionType.Transfer;
        transaction.TransferDirection = direction;
        transaction.Description = BuildTransferDescription(description, counterpartAccount, direction);
        transaction.Status = TransactionStatus.Confirmed;
    }

    private async Task<TransferResultDto> BuildTransferResultAsync(long firstTransactionId, long secondTransactionId)
    {
        var first = await _queryService.GetByIdAsync(firstTransactionId);
        var second = await _queryService.GetByIdAsync(secondTransactionId);

        var outTransaction = string.Equals(first.TransferDirection, TransferDirection.Out.ToString(), StringComparison.OrdinalIgnoreCase)
            ? first
            : second;
        var inTransaction = string.Equals(first.TransferDirection, TransferDirection.In.ToString(), StringComparison.OrdinalIgnoreCase)
            ? first
            : second;

        return new TransferResultDto
        {
            OutTransaction = outTransaction,
            InTransaction = inTransaction
        };
    }
}
