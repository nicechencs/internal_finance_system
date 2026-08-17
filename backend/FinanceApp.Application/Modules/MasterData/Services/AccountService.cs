using MapsterMapper;
using FinanceApp.Application.Common;
using FinanceApp.Application.Modules.Identity.Interfaces;
using FinanceApp.Application.Modules.MasterData.DTOs.Account;
using FinanceApp.Application.Modules.MasterData.Interfaces;
using FinanceApp.Application.Services;
using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Enums;
using FinanceApp.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Transaction = FinanceApp.Domain.Entities.Transaction;

namespace FinanceApp.Application.Modules.MasterData.Services;

public class AccountService : ServiceBase, IAccountService
{
    private readonly IRepository<Account> _accountRepository;
    private readonly IRepository<Transaction> _transactionRepository;
    private readonly IMasterDataReferenceGuard _referenceGuard;
    private readonly IMapper _mapper;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<AccountService> _logger;
    private readonly IUnitOfWork _unitOfWork;

    public AccountService(
        IRepository<Account> accountRepository,
        IRepository<Transaction> transactionRepository,
        IMasterDataReferenceGuard referenceGuard,
        IMapper mapper,
        IAuditLogService auditLogService,
        ILogger<AccountService> logger,
        ICurrentUserService currentUserService,
        IDataPermissionService permissionService,
        IUnitOfWork unitOfWork)
        : base(currentUserService, permissionService)
    {
        _accountRepository = accountRepository;
        _transactionRepository = transactionRepository;
        _referenceGuard = referenceGuard;
        _mapper = mapper;
        _auditLogService = auditLogService;
        _logger = logger;
        _unitOfWork = unitOfWork;
    }

    public async Task<PageResponse<AccountDto>> GetPagedAsync(PageRequest request)
    {
        _logger.LogDebug("AccountService.GetPagedAsync - Page={Page}, PageSize={PageSize}",
            request.Page, request.PageSize);

        try
        {
            var baseQuery = _accountRepository.GetQueryable();

            // 应用权限过滤
            var query = ApplyPermissionFilter(baseQuery);

            // 按名称筛选
            if (!string.IsNullOrWhiteSpace(request.Name))
            {
                query = query.Where(a => a.Name.Contains(request.Name));
            }

            IQueryable<Account> orderedQuery = query.OrderByDescending(a => a.CreatedAt);

            // 应用自定义排序
            var sortableFields = new Dictionary<string, System.Linq.Expressions.Expression<Func<Account, object>>>(StringComparer.OrdinalIgnoreCase)
            {
                ["name"] = a => a.Name,
                ["accountType"] = a => a.AccountType,
                ["currentBalance"] = a => a.CurrentBalance,
                ["bankName"] = a => a.BankName!,
                ["createdAt"] = a => a.CreatedAt,
                ["isActive"] = a => a.IsActive
            };
            orderedQuery = SortingHelper.ApplySorting(orderedQuery, request.SortBy, request.SortOrder, sortableFields);

            var total = await orderedQuery.CountAsync();

            var items = await orderedQuery
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync();

            var accountDtos = _mapper.Map<List<AccountDto>>(items);

            _logger.LogInformation("获取账户分页列表成功: 返回{Count}条记录, 总数={Total}",
                accountDtos.Count, total);

            return new PageResponse<AccountDto>
            {
                Items = accountDtos,
                Page = request.Page,
                PageSize = request.PageSize,
                Total = total
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取账户分页列表失败: Page={Page}, PageSize={PageSize}",
                request.Page, request.PageSize);
            throw;
        }
    }

    public async Task<AccountDto> GetByIdAsync(long id)
    {
        _logger.LogDebug("AccountService.GetByIdAsync - Id={Id}", id);

        try
        {
            var account = await _accountRepository.GetByIdAsync(id);

            if (account == null)
            {
                _logger.LogWarning("账户不存在: Id={Id}", id);
                throw new NotFoundException("账户不存在");
            }

            // 检查访问权限
            EnsureCanAccess(account);

            _logger.LogInformation("获取账户详情成功: Id={Id}, 名称={Name}, 类型={Type}",
                id, account.Name, account.AccountType);
            return _mapper.Map<AccountDto>(account);
        }
        catch (NotFoundException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取账户详情失败: Id={Id}", id);
            throw;
        }
    }

    public async Task<AccountDto> CreateAsync(CreateAccountRequest request)
    {
        _logger.LogDebug("AccountService.CreateAsync - 名称={Name}, 类型={Type}, 初始余额={Balance}",
            request.Name, request.AccountType, request.InitialBalance);

        try
        {
            // Parse AccountType enum
            if (!Enum.TryParse<AccountType>(request.AccountType, true, out var accountType))
            {
                _logger.LogWarning("账户类型验证失败: Type={Type}", request.AccountType);
                throw new ValidationException("无效的账户类型");
            }

            var account = new Account
            {
                Name = request.Name,
                AccountType = accountType,
                AccountNumber = request.AccountNumber,
                BankName = request.BankName,
                OpeningBalance = request.InitialBalance,
                CurrentBalance = request.InitialBalance,
                Currency = request.Currency,
                IsActive = true,
                InterestStartDate = request.InterestStartDate,
                MaturityDate = request.MaturityDate,
                InterestRate = request.InterestRate,
                AutoRenewal = request.AutoRenewal
            };

            await _accountRepository.AddAsync(account);
            await _unitOfWork.SaveChangesAsync();

            var newDto = _mapper.Map<AccountDto>(account);
            await _auditLogService.LogAsync("Create", "Account", account.Id, null, SerializeForAudit(newDto));

            _logger.LogInformation("创建账户成功: Id={Id}, 名称={Name}, 类型={Type}, 初始余额={Balance}",
                account.Id, account.Name, account.AccountType, account.OpeningBalance);

            return newDto;
        }
        catch (ValidationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建账户失败: 名称={Name}, 类型={Type}",
                request.Name, request.AccountType);
            throw;
        }
    }

    public async Task<AccountDto> UpdateAsync(long id, UpdateAccountRequest request)
    {
        _logger.LogDebug("AccountService.UpdateAsync - Id={Id}, 名称={Name}", id, request.Name);

        try
        {
            var account = await _accountRepository.GetByIdAsync(id);

            if (account == null)
            {
                _logger.LogWarning("账户不存在: Id={Id}", id);
                throw new NotFoundException("账户不存在");
            }

            // 检查修改权限
            EnsureCanEdit(account);

            var oldDto = _mapper.Map<AccountDto>(account);

            _logger.LogDebug("更新前账户状态: Id={Id}, 名称={OldName}, 状态={OldIsActive}",
                id, account.Name, account.IsActive);

            account.Name = request.Name;
            account.IsActive = request.IsActive;
            account.BankName = request.BankName;
            account.AccountNumber = request.AccountNumber;
            account.InterestStartDate = request.InterestStartDate;
            account.MaturityDate = request.MaturityDate;
            account.InterestRate = request.InterestRate;
            account.AutoRenewal = request.AutoRenewal;

            _accountRepository.Update(account);
            await _unitOfWork.SaveChangesAsync();

            var newDto = _mapper.Map<AccountDto>(account);
            await _auditLogService.LogAsync("Update", "Account", account.Id, SerializeForAudit(oldDto), SerializeForAudit(newDto));

            _logger.LogInformation("更新账户成功: Id={Id}, 名称={Name}, 状态={IsActive}",
                account.Id, account.Name, account.IsActive);

            return newDto;
        }
        catch (NotFoundException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新账户失败: Id={Id}, 名称={Name}", id, request.Name);
            throw;
        }
    }

    public async Task DeleteAsync(long id)
    {
        _logger.LogDebug("AccountService.DeleteAsync - Id={Id}", id);

        try
        {
            var account = await _accountRepository.GetByIdAsync(id);

            if (account == null)
            {
                _logger.LogWarning("账户不存在: Id={Id}", id);
                throw new NotFoundException("账户不存在");
            }

            // 检查删除权限
            EnsureCanDelete(account);

            var oldDto = _mapper.Map<AccountDto>(account);

            if (await _referenceGuard.HasAccountReferencesAsync(id))
            {
                if (!account.IsActive)
                {
                    _logger.LogInformation("账户已停用且存在历史引用，保留历史数据: Id={Id}, Name={Name}", id, account.Name);
                    return;
                }

                account.IsActive = false;
                _accountRepository.Update(account);
                await _unitOfWork.SaveChangesAsync();

                await _auditLogService.LogAsync("Archive", "Account", account.Id, SerializeForAudit(oldDto), SerializeForAudit(_mapper.Map<AccountDto>(account)));
                _logger.LogInformation("账户存在历史引用，已改为停用: Id={Id}, Name={Name}", id, account.Name);
                return;
            }

            _accountRepository.Delete(account);
            await _unitOfWork.SaveChangesAsync();

            await _auditLogService.LogAsync("Delete", "Account", account.Id, SerializeForAudit(oldDto), null);

            _logger.LogInformation("删除账户成功: Id={Id}, 名称={Name}", id, account.Name);
        }
        catch (NotFoundException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除账户失败: Id={Id}", id);
            throw;
        }
    }

    public async Task<List<AccountDto>> GetActiveAccountsAsync()
    {
        _logger.LogDebug("AccountService.GetActiveAccountsAsync");

        try
        {
            // 应用权限过滤，确保 Viewer 只能看到自己创建的数据
            var accounts = await ApplyPermissionFilter(_accountRepository.GetQueryable())
                .Where(a => a.IsActive)
                .OrderBy(a => a.Name)
                .ToListAsync();

            var accountDtos = _mapper.Map<List<AccountDto>>(accounts);

            _logger.LogInformation("获取活跃账户列表成功: 返回{Count}条记录", accountDtos.Count);

            return accountDtos;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取活跃账户列表失败");
            throw;
        }
    }

    public async Task<List<AccountDto>> GetMaturingAccountsAsync(int days = 30)
    {
        _logger.LogDebug("AccountService.GetMaturingAccountsAsync - Days={Days}", days);

        try
        {
            var targetDate = DateTime.UtcNow.AddDays(days).Date;
            var today = DateTime.UtcNow.Date;

            // 应用权限过滤，确保 Viewer 只能看到自己创建的数据
            var accounts = await ApplyPermissionFilter(_accountRepository.GetQueryable())
                .Where(a => a.AccountType == AccountType.FixedDeposit
                    && a.IsActive
                    && a.MaturityDate.HasValue
                    && a.MaturityDate.Value.Date >= today
                    && a.MaturityDate.Value.Date <= targetDate)
                .OrderBy(a => a.MaturityDate)
                .ToListAsync();

            var accountDtos = _mapper.Map<List<AccountDto>>(accounts);

            _logger.LogInformation("获取即将到期定期存款成功: 返回{Count}条记录", accountDtos.Count);

            return accountDtos;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取即将到期定期存款失败");
            throw;
        }
    }

    public async Task<BalanceTrendResponse> GetBalanceTrendAsync(long id, int months = 6)
    {
        _logger.LogDebug("AccountService.GetBalanceTrendAsync - Id={Id}, Months={Months}", id, months);

        try
        {
            var account = await _accountRepository.GetByIdAsync(id);
            if (account == null)
            {
                _logger.LogWarning("账户不存在: Id={Id}", id);
                throw new NotFoundException("账户不存在");
            }

            // 检查访问权限
            EnsureCanAccess(account);

            var startDate = DateTime.UtcNow.AddMonths(-months).Date;
            startDate = new DateTime(startDate.Year, startDate.Month, 1, 0, 0, 0, DateTimeKind.Utc);

            // 获取该账户在时间范围内的所有交易，按日期排序（应用权限过滤）
            var transactionQuery = PermissionService.ApplyPermissionFilter(_transactionRepository.GetQueryable());
            var transactions = await transactionQuery
                .Where(t => t.AccountId == id && t.TransactionDate >= startDate)
                .OrderBy(t => t.TransactionDate)
                .ToListAsync();

            // 计算起始日期之前的余额（从初始余额 + 之前所有交易的净额，应用权限过滤）
            var priorTransactions = await transactionQuery
                .Where(t => t.AccountId == id && t.TransactionDate < startDate)
                .ToListAsync();

            var priorBalance = account.OpeningBalance;
            foreach (var t in priorTransactions)
            {
                priorBalance += TransactionBalanceHelper.GetSignedAmount(t);
            }

            // 按月汇总，生成每月末余额
            var trends = new List<BalanceTrendDto>();
            var runningBalance = priorBalance;
            var now = DateTime.UtcNow;

            for (var date = startDate; date <= now; date = date.AddMonths(1))
            {
                var monthEnd = new DateTime(date.Year, date.Month, DateTime.DaysInMonth(date.Year, date.Month), 23, 59, 59, DateTimeKind.Utc);
                if (monthEnd > now) monthEnd = now;

                var monthTransactions = transactions
                    .Where(t => t.TransactionDate.Year == date.Year && t.TransactionDate.Month == date.Month)
                    .ToList();

                foreach (var t in monthTransactions)
                {
                    runningBalance += TransactionBalanceHelper.GetSignedAmount(t);
                }

                trends.Add(new BalanceTrendDto
                {
                    Date = monthEnd,
                    Balance = runningBalance
                });
            }

            _logger.LogInformation("获取账户余额趋势成功: Id={Id}, 数据点={Count}", id, trends.Count);

            return new BalanceTrendResponse
            {
                Trends = trends,
                CurrentBalance = account.CurrentBalance,
                OpeningBalance = account.OpeningBalance
            };
        }
        catch (NotFoundException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取账户余额趋势失败: Id={Id}", id);
            throw;
        }
    }

    public async Task<AccountStatisticsDto> GetStatisticsAsync()
    {
        _logger.LogDebug("AccountService.GetStatisticsAsync");

        var query = ApplyPermissionFilter(_accountRepository.GetQueryable());
        var accounts = await query.ToListAsync();

        return new AccountStatisticsDto
        {
            TotalCount = accounts.Count,
            ActiveCount = accounts.Count(a => a.IsActive),
            TotalBalance = accounts.Where(a => a.IsActive).Sum(a => a.CurrentBalance),
            FixedDepositCount = accounts.Count(a => a.AccountType == AccountType.FixedDeposit && a.IsActive)
        };
    }
}
