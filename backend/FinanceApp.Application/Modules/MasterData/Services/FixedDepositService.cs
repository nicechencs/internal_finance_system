using FinanceApp.Application.Common;
using FinanceApp.Application.Extensions;
using FinanceApp.Application.Modules.Identity.Interfaces;
using FinanceApp.Application.Modules.MasterData.DTOs.FixedDeposit;
using FinanceApp.Application.Modules.MasterData.Interfaces;
using FinanceApp.Application.Modules.TransactionProcessing.DTOs;
using FinanceApp.Application.Services;
using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Enums;
using FinanceApp.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MapsterMapper;

namespace FinanceApp.Application.Modules.MasterData.Services;

public class FixedDepositService : ServiceBase, IFixedDepositService
{
    // 候选交易查询配置常量
    private const int CANDIDATE_SEARCH_DAYS_BEFORE_MATURITY = 30;
    private const int CANDIDATE_SEARCH_DAYS_AFTER_TODAY = 7;
    private const int CANDIDATE_MAX_RESULTS = 10;

    private readonly IRepository<FixedDepositRecord> _repository;
    private readonly IRepository<Account> _accountRepository;
    private readonly IRepository<Transaction> _transactionRepository;
    private readonly IRepository<TagBinding> _tagBindingRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<FixedDepositService> _logger;
    private readonly IMapper _mapper;

    public FixedDepositService(
        IRepository<FixedDepositRecord> repository,
        IRepository<Account> accountRepository,
        IRepository<Transaction> transactionRepository,
        IRepository<TagBinding> tagBindingRepository,
        IUnitOfWork unitOfWork,
        IAuditLogService auditLogService,
        ILogger<FixedDepositService> logger,
        ICurrentUserService currentUserService,
        IDataPermissionService permissionService,
        IMapper mapper)
        : base(currentUserService, permissionService)
    {
        _repository = repository;
        _accountRepository = accountRepository;
        _transactionRepository = transactionRepository;
        _tagBindingRepository = tagBindingRepository;
        _unitOfWork = unitOfWork;
        _auditLogService = auditLogService;
        _logger = logger;
        _mapper = mapper;
    }

    public async Task<FixedDepositDto> CreateAsync(CreateFixedDepositRequest request)
    {
        var account = await _accountRepository.GetByIdAsync(request.AccountId)
            ?? throw new NotFoundException("账户不存在");

        if (account.AccountType != AccountType.FixedDeposit)
            throw new ValidationException("只有定期账户才能创建定期存款记录");

        EnsureCanEdit(account);

        if (request.Principal <= 0)
            throw new ValidationException("本金必须大于0");
        if (request.TermMonths <= 0)
            throw new ValidationException("期限必须大于0");
        if (request.InterestRate < 0 || request.InterestRate > 100)
            throw new ValidationException("利率必须在 0-100% 之间");

        var depositDate = request.DepositDate ?? DateTime.UtcNow.Date;
        var maturityDate = depositDate.AddMonths(request.TermMonths);

        var record = new FixedDepositRecord
        {
            AccountId = request.AccountId,
            Principal = request.Principal,
            DepositDate = depositDate,
            MaturityDate = maturityDate,
            TermMonths = request.TermMonths,
            InterestRate = request.InterestRate,
            Status = FixedDepositStatus.Active,
            DepositTransactionId = request.DepositTransactionId ?? 0,
            Notes = request.Notes
        };

        await _repository.AddAsync(record);
        await _unitOfWork.SaveChangesAsync();

        var dto = MapToDto(record, account.Name);
        await _auditLogService.LogAsync("Create", "FixedDeposit", record.Id, null, SerializeForAudit(dto));

        _logger.LogInformation("创建定期存款成功: Id={Id}, 账户={AccountId}, 本金={Principal}, 期限={TermMonths}月",
            record.Id, record.AccountId, record.Principal, record.TermMonths);

        return dto;
    }

    public async Task<List<FixedDepositDto>> GetAllAsync(GetFixedDepositsRequest request)
    {
        var query = ApplyPermissionFilter(_repository.GetQueryable())
            .Include(r => r.Account)
            .AsNoTracking();

        // 按账户 ID 筛选
        if (request.AccountIds != null && request.AccountIds.Length > 0)
        {
            query = query.Where(r => request.AccountIds.Contains(r.AccountId));
        }

        // 按状态筛选
        query = ApplyStatusFilter(query, request.Status);

        // 是否包含已支取
        if (!request.IncludeWithdrawn)
        {
            query = query.Where(r => r.Status != FixedDepositStatus.Withdrawn);
        }

        var records = await query
            .OrderByDescending(r => r.DepositDate)
            .ToListAsync();

        return records.Select(r => MapToDto(r, r.Account.Name)).ToList();
    }

    public async Task<List<FixedDepositDto>> GetByAccountAsync(long accountId)
    {
        var account = await _accountRepository.GetByIdAsync(accountId)
            ?? throw new NotFoundException("账户不存在");

        EnsureCanAccess(account);

        var records = await ApplyPermissionFilter(_repository.GetQueryable())
            .Where(r => r.AccountId == accountId)
            .Include(r => r.Account)
            .OrderByDescending(r => r.DepositDate)
            .ToListAsync();

        return records.Select(r => MapToDto(r, r.Account.Name)).ToList();
    }

    public async Task<FixedDepositDto> GetByIdAsync(long id)
    {
        var record = await _repository.GetQueryable()
            .Include(r => r.Account)
            .FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted)
            ?? throw new NotFoundException("定期存款记录不存在");

        EnsureCanAccess(record);

        return MapToDto(record, record.Account.Name);
    }

    public async Task<FixedDepositDto> WithdrawAsync(long id, WithdrawFixedDepositRequest request)
    {
        var record = await _repository.GetQueryable()
            .Include(r => r.Account)
            .FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted)
            ?? throw new NotFoundException("定期存款记录不存在");

        EnsureCanEdit(record);

        if (record.Status != FixedDepositStatus.Active)
            throw new ValidationException("只有存续中的定期存款才能支取");

        // 验证必须关联交易记录
        if (request.TransactionId <= 0)
            throw new ValidationException("必须选择关联的交易记录");

        var transaction = await _transactionRepository.GetByIdAsync(request.TransactionId)
            ?? throw new NotFoundException("关联的交易记录不存在");

        EnsureCanAccess(transaction);

        // 验证交易属于定期账户
        if (transaction.AccountId != record.AccountId)
            throw new ValidationException("交易记录必须属于当前定期账户");

        // 验证交易类型（必须是支出或转出）
        if (transaction.TransactionType != TransactionType.Expense &&
            transaction.TransactionType != TransactionType.Transfer)
            throw new ValidationException("交易记录必须是支出或转账类型");

        // 验证交易未被其他定期关联
        var existingLink = await _repository.GetQueryable()
            .AnyAsync(r => r.WithdrawalTransactionId == request.TransactionId && r.Id != id);
        if (existingLink)
            throw new ValidationException("该交易记录已被其他定期存款关联");

        var withdrawalDate = request.WithdrawalDate ?? DateTime.UtcNow.Date;
        var isEarly = withdrawalDate < record.MaturityDate;

        // 计算利息
        decimal actualInterest;
        if (request.ActualInterest.HasValue)
        {
            actualInterest = request.ActualInterest.Value;
        }
        else
        {
            // 自动计算: 正常到期用约定利率, 提前支取用活期利率(0.35%)
            if (isEarly)
            {
                var days = (withdrawalDate - record.DepositDate).Days;
                actualInterest = Math.Round(record.Principal * 0.35m / 100m * days / 365m, 2);
            }
            else
            {
                actualInterest = Math.Round(record.Principal * record.InterestRate / 100m * record.TermMonths / 12m, 2);
            }
        }

        // 验证金额匹配（使用固定金额上限，避免大额定期容差过大）
        var expectedAmount = record.Principal + actualInterest;
        var amountDiff = Math.Abs(transaction.Amount - expectedAmount);
        // 容差：取1%和100元中的较小值，避免大额定期容差过大
        var tolerance = Math.Min(expectedAmount * 0.01m, 100m);
        if (amountDiff > tolerance)
        {
            _logger.LogWarning(
                "定期支取金额不匹配: 交易金额={TransactionAmount}, 预期金额={ExpectedAmount}, 差额={Diff}, 容差={Tolerance}",
                transaction.Amount, expectedAmount, amountDiff, tolerance);
            throw new ValidationException(
                $"交易金额({transaction.Amount:F2})与预期金额({expectedAmount:F2})差异过大（差额{amountDiff:F2}元，允许{tolerance:F2}元），请检查");
        }

        var oldDto = MapToDto(record, record.Account.Name);

        record.Status = FixedDepositStatus.Withdrawn;
        record.WithdrawalDate = withdrawalDate;
        record.ActualInterest = actualInterest;
        record.IsEarlyWithdrawal = isEarly;
        record.WithdrawalTransactionId = request.TransactionId;
        record.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync();

        var newDto = MapToDto(record, record.Account.Name);
        await _auditLogService.LogAsync("Withdraw", "FixedDeposit", record.Id, SerializeForAudit(oldDto), SerializeForAudit(newDto));

        _logger.LogInformation("支取定期存款: Id={Id}, 提前支取={IsEarly}, 利息={Interest}, 关联交易={TransactionId}",
            record.Id, isEarly, actualInterest, request.TransactionId);

        return newDto;
    }

    public async Task<List<FixedDepositDto>> GetMaturingAsync(int days = 30)
    {
        var today = DateTime.UtcNow.Date;
        var targetDate = today.AddDays(days);

        var records = await ApplyPermissionFilter(_repository.GetQueryable())
            .Where(r => r.Status == FixedDepositStatus.Active
                && r.MaturityDate >= today
                && r.MaturityDate <= targetDate)
            .Include(r => r.Account)
            .OrderBy(r => r.MaturityDate)
            .ToListAsync();

        return records.Select(r => MapToDto(r, r.Account.Name)).ToList();
    }

    public async Task<FixedDepositStatisticsDto> GetStatisticsAsync(GetFixedDepositsRequest request)
    {
        _logger.LogDebug("获取定期存款统计: AccountIds={AccountIds}, Status={Status}",
            request.AccountIds != null ? string.Join(",", request.AccountIds) : "全部",
            request.Status ?? "全部");

        var query = ApplyPermissionFilter(_repository.GetQueryable())
            .AsNoTracking();

        // 按账户 ID 筛选
        if (request.AccountIds != null && request.AccountIds.Length > 0)
        {
            query = query.Where(r => request.AccountIds.Contains(r.AccountId));
        }

        // 按状态筛选
        query = ApplyStatusFilter(query, request.Status);

        // 是否包含已支取
        if (!request.IncludeWithdrawn)
        {
            query = query.Where(r => r.Status != FixedDepositStatus.Withdrawn);
        }

        var today = DateTime.UtcNow.Date;
        var upcomingDate = today.AddDays(30);

        // 使用数据库端聚合计算统计数据
        var stats = await query
            .GroupBy(r => 1)
            .Select(g => new FixedDepositStatisticsDto
            {
                TotalCount = g.Count(),
                ActiveCount = g.Count(r => r.Status == FixedDepositStatus.Active
                    && r.MaturityDate >= today),
                WithdrawnCount = g.Count(r => r.Status == FixedDepositStatus.Withdrawn),
                UpcomingCount = g.Count(r => r.Status == FixedDepositStatus.Active
                    && r.MaturityDate >= today
                    && r.MaturityDate <= upcomingDate),
                TotalPrincipal = g.Sum(r => r.Principal),
                ActivePrincipal = g.Where(r => r.Status == FixedDepositStatus.Active
                        && r.MaturityDate >= today)
                    .Sum(r => r.Principal),
                ExpectedInterest = g.Sum(r => r.Principal * r.InterestRate / 100m * r.TermMonths / 12m)
            })
            .FirstOrDefaultAsync();

        // 如果无数据，返回全零的 DTO
        if (stats == null)
        {
            stats = new FixedDepositStatisticsDto
            {
                TotalCount = 0,
                ActiveCount = 0,
                WithdrawnCount = 0,
                UpcomingCount = 0,
                TotalPrincipal = 0,
                ActivePrincipal = 0,
                ExpectedInterest = 0
            };
        }

        _logger.LogInformation("定期存款统计获取成功: 总数={TotalCount}, 活跃={ActiveCount}, 已支取={WithdrawnCount}, 即将到期={UpcomingCount}, 总本金={TotalPrincipal}, 活跃本金={ActivePrincipal}, 预期利息={ExpectedInterest}",
            stats.TotalCount, stats.ActiveCount, stats.WithdrawnCount, stats.UpcomingCount,
            stats.TotalPrincipal, stats.ActivePrincipal, stats.ExpectedInterest);

        return stats;
    }

    public async Task<FixedDepositDto> UpdateAsync(long id, UpdateFixedDepositRequest request)
    {
        var record = await _repository.GetQueryable()
            .Include(r => r.Account)
            .FirstOrDefaultAsync(r => r.Id == id)
            ?? throw new NotFoundException("定期存款记录不存在");

        EnsureCanEdit(record);

        // 已支取的不能修改
        if (record.Status == FixedDepositStatus.Withdrawn)
            throw new ValidationException("已支取的定期存款不能修改");

        // 有关联交易的不能修改
        if (record.DepositTransactionId > 0)
            throw new ValidationException("由交易记录创建的定期存款不能修改");

        // 验证新账户
        var account = await _accountRepository.GetByIdAsync(request.AccountId)
            ?? throw new NotFoundException("账户不存在");

        if (account.AccountType != AccountType.FixedDeposit)
            throw new ValidationException("只有定期账户才能创建定期存款记录");

        EnsureCanEdit(account);

        // 验证数据
        if (request.Principal <= 0)
            throw new ValidationException("本金必须大于0");
        if (request.TermMonths <= 0)
            throw new ValidationException("期限必须大于0");
        if (request.InterestRate < 0 || request.InterestRate > 100)
            throw new ValidationException("利率必须在 0-100% 之间");

        var oldDto = MapToDto(record, record.Account.Name);

        // 更新字段
        record.AccountId = request.AccountId;
        record.Principal = request.Principal;
        record.DepositDate = request.DepositDate;
        record.TermMonths = request.TermMonths;
        record.MaturityDate = request.DepositDate.AddMonths(request.TermMonths);
        record.InterestRate = request.InterestRate;
        record.Notes = request.Notes;

        await _unitOfWork.SaveChangesAsync();

        var newDto = MapToDto(record, account.Name);
        await _auditLogService.LogAsync("Update", "FixedDeposit", record.Id, SerializeForAudit(oldDto), SerializeForAudit(newDto));

        _logger.LogInformation("更新定期存款成功: Id={Id}, 账户={AccountId}, 本金={Principal}, 期限={TermMonths}月",
            record.Id, record.AccountId, record.Principal, record.TermMonths);

        return newDto;
    }

    public async Task DeleteAsync(long id)
    {
        var record = await _repository.GetByIdAsync(id)
            ?? throw new NotFoundException("定期存款记录不存在");

        EnsureCanDelete(record);

        // 已支取的不能删除
        if (record.Status == FixedDepositStatus.Withdrawn)
            throw new ValidationException("已支取的定期存款不能删除");

        // 有关联交易的不能删除
        if (record.DepositTransactionId > 0)
            throw new ValidationException("由交易记录创建的定期存款不能删除");

        record.IsDeleted = true;
        record.DeletedAt = DateTime.UtcNow;
        await _unitOfWork.SaveChangesAsync();

        await _auditLogService.LogAsync("Delete", "FixedDeposit", record.Id, null, null);

        _logger.LogInformation("删除定期存款记录: Id={Id}", id);
    }

    private static IQueryable<FixedDepositRecord> ApplyStatusFilter(
        IQueryable<FixedDepositRecord> query,
        string? statusValue)
    {
        if (string.IsNullOrWhiteSpace(statusValue)
            || !Enum.TryParse<FixedDepositStatus>(statusValue, true, out var status))
        {
            return query;
        }

        var today = DateTime.UtcNow.Date;

        return status switch
        {
            FixedDepositStatus.Active => query.Where(r =>
                r.Status == FixedDepositStatus.Active
                && r.MaturityDate >= today),
            FixedDepositStatus.Matured => query.Where(r =>
                r.Status == FixedDepositStatus.Matured
                || (r.Status == FixedDepositStatus.Active && r.MaturityDate < today)),
            FixedDepositStatus.Withdrawn => query.Where(r => r.Status == FixedDepositStatus.Withdrawn),
            _ => query
        };
    }

    private static FixedDepositDto MapToDto(FixedDepositRecord record, string accountName)
    {
        var today = DateTime.UtcNow.Date;
        var daysToMaturity = record.Status == FixedDepositStatus.Active
            ? Math.Max(0, (record.MaturityDate - today).Days)
            : 0;
        var expectedInterest = Math.Round(record.Principal * record.InterestRate / 100m * record.TermMonths / 12m, 2);

        return new FixedDepositDto
        {
            Id = record.Id,
            AccountId = record.AccountId,
            AccountName = accountName,
            Principal = record.Principal,
            DepositDate = record.DepositDate,
            MaturityDate = record.MaturityDate,
            TermMonths = record.TermMonths,
            InterestRate = record.InterestRate,
            Status = record.Status.ToString(),
            WithdrawalDate = record.WithdrawalDate,
            ActualInterest = record.ActualInterest,
            IsEarlyWithdrawal = record.IsEarlyWithdrawal,
            DaysToMaturity = daysToMaturity,
            ExpectedInterest = expectedInterest,
            Notes = record.Notes,
            DepositTransactionId = record.DepositTransactionId,
            CreatedAt = record.CreatedAt
        };
    }

    public async Task<List<TransactionDto>> GetWithdrawalCandidatesAsync(long id)
    {
        var record = await _repository.GetQueryable()
            .Include(r => r.Account)
            .FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted)
            ?? throw new NotFoundException("定期存款记录不存在");

        EnsureCanAccess(record);

        if (record.Status != FixedDepositStatus.Active)
            throw new ValidationException("只有存续中的定期存款才能查询候选交易");

        // 计算预期金额（本金+利息）
        var today = DateTime.UtcNow.Date;
        var isEarly = today < record.MaturityDate;
        decimal expectedInterest;
        if (isEarly)
        {
            var days = (today - record.DepositDate).Days;
            expectedInterest = Math.Round(record.Principal * 0.35m / 100m * days / 365m, 2);
        }
        else
        {
            expectedInterest = Math.Round(record.Principal * record.InterestRate / 100m * record.TermMonths / 12m, 2);
        }
        var expectedAmount = record.Principal + expectedInterest;

        // 查询候选交易（使用配置常量）
        var minDate = record.MaturityDate.AddDays(-CANDIDATE_SEARCH_DAYS_BEFORE_MATURITY);
        var maxDate = today.AddDays(CANDIDATE_SEARCH_DAYS_AFTER_TODAY + 1);

        // 先查询已关联的交易ID（避免在数据库端使用 NOT IN）
        var linkedTransactionIds = await _repository.GetQueryable()
            .Where(r => r.WithdrawalTransactionId != null)
            .Select(r => r.WithdrawalTransactionId!.Value)
            .ToListAsync();

        // 第一步：在数据库端筛选基本条件（可使用索引）
        var candidateTransactions = await ApplyPermissionFilter(_transactionRepository.GetQueryable())
            .Where(t =>
                t.AccountId == record.AccountId &&
                (t.TransactionType == TransactionType.Expense || t.TransactionType == TransactionType.Transfer) &&
                t.TransactionDate >= minDate &&
                t.TransactionDate < maxDate)
            .Select(t => new
            {
                t.Id,
                t.TransactionDate,
                t.Amount,
                t.TransactionType,
                t.TransferDirection,
                t.Description,
                t.Status,
                AccountName = t.Account.Name,
                CategoryName = t.Category != null ? t.Category.Name : null,
                ProjectName = t.Project != null ? t.Project.Name : null
            })
            .ToListAsync();

        // 第二步：在内存中过滤已关联的交易并按金额接近度排序
        var filteredCandidates = candidateTransactions
            .Where(t => !linkedTransactionIds.Contains(t.Id))
            .OrderBy(t => Math.Abs(t.Amount - expectedAmount))
            .ThenByDescending(t => t.TransactionDate)
            .Take(CANDIDATE_MAX_RESULTS)
            .ToList();

        _logger.LogInformation(
            "查询定期支取候选交易: FixedDepositId={Id}, 预期金额={ExpectedAmount}, 数据库筛选={DbCount}, 最终候选={FinalCount}",
            id, expectedAmount, candidateTransactions.Count, filteredCandidates.Count);

        // 映射为 DTO（使用匿名对象的数据）
        var candidateDtos = filteredCandidates.Select(t => new TransactionDto
        {
            Id = t.Id,
            TransactionDate = t.TransactionDate,
            Amount = t.Amount,
            TransactionType = t.TransactionType.ToString(),
            TransferDirection = t.TransferDirection.ToString(),
            Description = t.Description,
            Status = t.Status.ToString(),
            AccountName = t.AccountName,
            CategoryName = t.CategoryName,
            ProjectName = t.ProjectName
        }).ToList();

        await _tagBindingRepository.GetQueryable().ApplyTagsAsync(
            TagScope.Transaction,
            candidateDtos,
            dto => dto.Id,
            (dto, tags) => dto.Tags = tags);

        return candidateDtos;
    }
}
