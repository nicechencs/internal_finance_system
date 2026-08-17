using MapsterMapper;
using FinanceApp.Application.Common;
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

public class TransactionQueryService : ServiceBase, ITransactionQueryService
{
    private readonly IRepository<Transaction> _transactionRepository;
    private readonly IRepository<TagBinding> _tagBindingRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<TransactionQueryService> _logger;

    public TransactionQueryService(
        IRepository<Transaction> transactionRepository,
        IRepository<TagBinding> tagBindingRepository,
        IMapper mapper,
        ILogger<TransactionQueryService> logger,
        ICurrentUserService currentUserService,
        IDataPermissionService permissionService)
        : base(currentUserService, permissionService)
    {
        _transactionRepository = transactionRepository;
        _tagBindingRepository = tagBindingRepository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<PageResponse<TransactionDto>> GetPagedAsync(PageRequest request)
    {
        _logger.LogDebug("TransactionQueryService.GetPagedAsync - 获取交易记录分页列表: Page={Page}, PageSize={PageSize}, StartDate={StartDate}, EndDate={EndDate}, Type={Type}",
            request.Page, request.PageSize, request.StartDate, request.EndDate, request.TransactionType);

        try
        {
            var baseQuery = _transactionRepository.GetQueryable()
                .IncludeFullDetails()
                .IncludeSettlementDetails();

            // 应用权限过滤
            var query = ApplyPermissionFilter(baseQuery)
                .ApplyRequestFilters(request, _tagBindingRepository.GetQueryable, _logger, logAppliedFilters: true);
            query = query.OrderByDescending(t => t.TransactionDate)
                .ThenByDescending(t => t.CreatedAt);

            var sortableFields = new Dictionary<string, System.Linq.Expressions.Expression<Func<Transaction, object>>>(StringComparer.OrdinalIgnoreCase)
            {
                ["transactionDate"] = t => t.TransactionDate,
                ["amount"] = t => t.Amount,
                ["availableAmount"] = t => t.Amount
                    - t.ReceivableDetails.Sum(d => d.Amount)
                    - t.PayableDetails.Sum(d => d.Amount),
                ["transactionType"] = t => t.TransactionType,
                ["createdAt"] = t => t.CreatedAt,
                ["status"] = t => t.Status
            };
            query = SortingHelper.ApplySorting(query, request.SortBy, request.SortOrder, sortableFields);

            var total = await query.CountAsync();
            _logger.LogDebug("查询到交易记录总数: {Total}", total);

            var items = await query
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync();

            var transactionDtos = _mapper.Map<List<TransactionDto>>(items);
            await _tagBindingRepository.GetQueryable().ApplyTagsAsync(
                TagScope.Transaction,
                transactionDtos,
                dto => dto.Id,
                (dto, tags) => dto.Tags = tags);

            _logger.LogInformation("成功获取交易记录分页列表: 返回={Count}条, 总数={Total}", transactionDtos.Count, total);

            return new PageResponse<TransactionDto>
            {
                Items = transactionDtos,
                Page = request.Page,
                PageSize = request.PageSize,
                Total = total
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取交易记录分页列表失败: Page={Page}, PageSize={PageSize}", request.Page, request.PageSize);
            throw;
        }
    }

    public async Task<TransactionDto> GetByIdAsync(long id)
    {
        _logger.LogDebug("TransactionQueryService.GetByIdAsync - 获取交易记录详情: Id={Id}", id);

        try
        {
            var transaction = await _transactionRepository.GetQueryable()
                .IncludeFullDetails()
                .IncludeSettlementDetails()
                .FirstOrDefaultAsync(t => t.Id == id);

            if (transaction == null)
            {
                _logger.LogWarning("交易记录不存在: Id={Id}", id);
                throw new NotFoundException("交易记录不存在");
            }

            // 检查访问权限
            EnsureCanAccess(transaction);

            _logger.LogDebug("成功获取交易记录详情: Id={Id}, 金额={Amount}, 类型={Type}",
                id, transaction.Amount, transaction.TransactionType);

            var transactionDto = _mapper.Map<TransactionDto>(transaction);
            await _tagBindingRepository.GetQueryable().ApplyTagAsync(
                TagScope.Transaction,
                transactionDto,
                dto => dto.Id,
                (dto, tags) => dto.Tags = tags);

            return transactionDto;
        }
        catch (NotFoundException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取交易记录详情失败: Id={Id}", id);
            throw;
        }
    }

    public async Task<List<TransactionDto>> GetByAccountAsync(long accountId)
    {
        _logger.LogDebug("TransactionQueryService.GetByAccountAsync - 获取账户关联交易记录: AccountId={AccountId}", accountId);

        try
        {
            var query = _transactionRepository.GetQueryable()
                .IncludeFullDetails()
                .Where(t => t.AccountId == accountId);

            // 应用权限过滤
            query = ApplyPermissionFilter(query);

            var transactions = await query
                .OrderByDescending(t => t.TransactionDate)
                .ToListAsync();

            _logger.LogInformation("成功获取账户关联交易记录: AccountId={AccountId}, 数量={Count}",
                accountId, transactions.Count);

            var transactionDtos = _mapper.Map<List<TransactionDto>>(transactions);
            await _tagBindingRepository.GetQueryable().ApplyTagsAsync(
                TagScope.Transaction,
                transactionDtos,
                dto => dto.Id,
                (dto, tags) => dto.Tags = tags);

            return transactionDtos;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取账户关联交易记录失败: AccountId={AccountId}", accountId);
            throw;
        }
    }

    public async Task<List<TransactionDto>> GetByProjectAsync(long projectId)
    {
        _logger.LogDebug("TransactionQueryService.GetByProjectAsync - 获取项目关联交易记录: ProjectId={ProjectId}", projectId);

        try
        {
            var query = _transactionRepository.GetQueryable()
                .IncludeFullDetails()
                .Where(t =>
                    (t.ProjectId == projectId && !t.IsAllocated) ||
                    (t.IsAllocated && t.Allocations.Any(a => a.ProjectId == projectId))
                );

            // 应用权限过滤
            query = ApplyPermissionFilter(query);

            var transactions = await query
                .OrderByDescending(t => t.TransactionDate)
                .ToListAsync();

            _logger.LogInformation("成功获取项目关联交易记录: ProjectId={ProjectId}, 数量={Count}",
                projectId, transactions.Count);

            var transactionDtos = _mapper.Map<List<TransactionDto>>(transactions);
            await _tagBindingRepository.GetQueryable().ApplyTagsAsync(
                TagScope.Transaction,
                transactionDtos,
                dto => dto.Id,
                (dto, tags) => dto.Tags = tags);

            return transactionDtos;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取项目关联交易记录失败: ProjectId={ProjectId}", projectId);
            throw;
        }
    }

    public async Task<List<TransactionDto>> GetByCategoryAsync(long categoryId)
    {
        _logger.LogDebug("TransactionQueryService.GetByCategoryAsync - 获取分类关联交易记录: CategoryId={CategoryId}", categoryId);

        try
        {
            var query = _transactionRepository.GetQueryable()
                .IncludeFullDetails()
                .Where(t => t.CategoryId == categoryId);

            // 应用权限过滤
            query = ApplyPermissionFilter(query);

            var transactions = await query
                .OrderByDescending(t => t.TransactionDate)
                .ToListAsync();

            _logger.LogInformation("成功获取分类关联交易记录: CategoryId={CategoryId}, 数量={Count}",
                categoryId, transactions.Count);

            var transactionDtos = _mapper.Map<List<TransactionDto>>(transactions);
            await _tagBindingRepository.GetQueryable().ApplyTagsAsync(
                TagScope.Transaction,
                transactionDtos,
                dto => dto.Id,
                (dto, tags) => dto.Tags = tags);

            return transactionDtos;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取分类关联交易记录失败: CategoryId={CategoryId}", categoryId);
            throw;
        }
    }

    public async Task<List<TransactionDto>> GetByCustomerAsync(long customerId)
    {
        _logger.LogDebug("TransactionQueryService.GetByCustomerAsync - 获取客户关联交易记录: CustomerId={CustomerId}", customerId);

        try
        {
            var query = _transactionRepository.GetQueryable()
                .IncludeFullDetails()
                .Where(t => t.CustomerId == customerId);

            // 应用权限过滤
            query = ApplyPermissionFilter(query);

            var transactions = await query
                .OrderByDescending(t => t.TransactionDate)
                .ToListAsync();

            _logger.LogInformation("成功获取客户关联交易记录: CustomerId={CustomerId}, 数量={Count}",
                customerId, transactions.Count);

            var transactionDtos = _mapper.Map<List<TransactionDto>>(transactions);
            await _tagBindingRepository.GetQueryable().ApplyTagsAsync(
                TagScope.Transaction,
                transactionDtos,
                dto => dto.Id,
                (dto, tags) => dto.Tags = tags);

            return transactionDtos;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取客户关联交易记录失败: CustomerId={CustomerId}", customerId);
            throw;
        }
    }

    public async Task<List<TransactionDto>> GetBySupplierAsync(long supplierId)
    {
        _logger.LogDebug("TransactionQueryService.GetBySupplierAsync - 获取供应商关联交易记录: SupplierId={SupplierId}", supplierId);

        try
        {
            var query = _transactionRepository.GetQueryable()
                .IncludeFullDetails()
                .Where(t => t.SupplierId == supplierId);

            // 应用权限过滤
            query = ApplyPermissionFilter(query);

            var transactions = await query
                .OrderByDescending(t => t.TransactionDate)
                .ToListAsync();

            _logger.LogInformation("成功获取供应商关联交易记录: SupplierId={SupplierId}, 数量={Count}",
                supplierId, transactions.Count);

            var transactionDtos = _mapper.Map<List<TransactionDto>>(transactions);
            await _tagBindingRepository.GetQueryable().ApplyTagsAsync(
                TagScope.Transaction,
                transactionDtos,
                dto => dto.Id,
                (dto, tags) => dto.Tags = tags);

            return transactionDtos;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取供应商关联交易记录失败: SupplierId={SupplierId}", supplierId);
            throw;
        }
    }

    public async Task<List<TransactionDto>> GetByPersonAsync(long personId)
    {
        _logger.LogDebug("TransactionQueryService.GetByPersonAsync - 获取人员关联交易记录: PersonId={PersonId}", personId);

        try
        {
            var query = _transactionRepository.GetQueryable()
                .IncludeFullDetails()
                .Where(t =>
                    (t.PersonId == personId && !t.IsAllocated) ||
                    (t.IsAllocated && t.Allocations.Any(a => a.PersonId == personId))
                );

            // 应用权限过滤
            query = ApplyPermissionFilter(query);

            var transactions = await query
                .OrderByDescending(t => t.TransactionDate)
                .ToListAsync();

            _logger.LogInformation("成功获取人员关联交易记录: PersonId={PersonId}, 数量={Count}",
                personId, transactions.Count);

            var transactionDtos = _mapper.Map<List<TransactionDto>>(transactions);
            await _tagBindingRepository.GetQueryable().ApplyTagsAsync(
                TagScope.Transaction,
                transactionDtos,
                dto => dto.Id,
                (dto, tags) => dto.Tags = tags);

            return transactionDtos;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取人员关联交易记录失败: PersonId={PersonId}", personId);
            throw;
        }
    }
}
