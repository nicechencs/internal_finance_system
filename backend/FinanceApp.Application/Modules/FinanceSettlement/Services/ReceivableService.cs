using System.Linq.Expressions;
using MapsterMapper;
using FinanceApp.Application.Common;
using FinanceApp.Application.Extensions;
using FinanceApp.Application.Modules.FinanceSettlement.DTOs.Receivable;
using FinanceApp.Application.Modules.FinanceSettlement.Interfaces;
using FinanceApp.Application.Modules.FinanceSettlement.Services;
using FinanceApp.Application.Modules.Identity.Interfaces;
using FinanceApp.Application.Modules.MasterData.Interfaces;
using FinanceApp.Application.Services;
using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Enums;
using FinanceApp.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FinanceApp.Application.Modules.FinanceSettlement.Services;

public class ReceivableService : ServiceBase, IReceivableService
{
    private readonly IRepository<Receivable> _receivableRepository;
    private readonly IRepository<ReceivableDetail> _detailRepository;
    private readonly IRepository<Project> _projectRepository;
    private readonly IRepository<Customer> _customerRepository;
    private readonly IRepository<Supplier> _supplierRepository;
    private readonly IRepository<Person> _personRepository;
    private readonly IRepository<TagBinding> _tagBindingRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<ReceivableService> _logger;
    private readonly IAuditLogService _auditLogService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ISettlementTransactionBindingService _settlementTransactionBindingService;
    private readonly TransactionAllocationHelper _transactionAllocationHelper;
    private readonly IProjectFinancialRecalculationService _projectRecalculationService;

    public ReceivableService(
        IRepository<Receivable> receivableRepository,
        IRepository<ReceivableDetail> detailRepository,
        IRepository<Project> projectRepository,
        IRepository<Customer> customerRepository,
        IRepository<Supplier> supplierRepository,
        IRepository<Person> personRepository,
        IRepository<TagBinding> tagBindingRepository,
        IMapper mapper,
        ILogger<ReceivableService> logger,
        IAuditLogService auditLogService,
        ICurrentUserService currentUserService,
        IDataPermissionService permissionService,
        IUnitOfWork unitOfWork,
        ISettlementTransactionBindingService settlementTransactionBindingService,
        TransactionAllocationHelper transactionAllocationHelper,
        IProjectFinancialRecalculationService projectRecalculationService)
        : base(currentUserService, permissionService)
    {
        _receivableRepository = receivableRepository;
        _detailRepository = detailRepository;
        _projectRepository = projectRepository;
        _customerRepository = customerRepository;
        _supplierRepository = supplierRepository;
        _personRepository = personRepository;
        _tagBindingRepository = tagBindingRepository;
        _mapper = mapper;
        _logger = logger;
        _auditLogService = auditLogService;
        _unitOfWork = unitOfWork;
        _settlementTransactionBindingService = settlementTransactionBindingService;
        _transactionAllocationHelper = transactionAllocationHelper;
        _projectRecalculationService = projectRecalculationService;
    }

    private IQueryable<Receivable> IncludeAll(IQueryable<Receivable> query)
    {
        return query
            .Include(r => r.Project)
            .Include(r => r.Customer)
            .Include(r => r.Supplier)
            .Include(r => r.Person)
            .Include(r => r.ReceivableType)
            .Include(r => r.Details);
    }

    /// <summary>
    /// 返回对方类型描述，用于日志输出（如 "客户(Id=1)" / "供应商(Id=2)" / "人员(Id=3)"）
    /// </summary>
    private static string DescribeCounterparty(long? customerId, long? supplierId, long? personId)
    {
        if (customerId.HasValue) return $"客户(Id={customerId.Value})";
        if (supplierId.HasValue) return $"供应商(Id={supplierId.Value})";
        if (personId.HasValue) return $"人员(Id={personId.Value})";
        return "无";
    }

    private async Task ValidateCounterparty(long? customerId, long? supplierId, long? personId)
    {
        var selectedCount = (customerId.HasValue ? 1 : 0)
            + (supplierId.HasValue ? 1 : 0)
            + (personId.HasValue ? 1 : 0);

        if (selectedCount == 0)
        {
            _logger.LogWarning("应收款对方验证失败: 未选择任何对方类型");
            throw new ValidationException("必须选择一个对方（客户、供应商或人员）");
        }

        if (selectedCount > 1)
        {
            _logger.LogWarning("应收款对方验证失败: 选择了多个对方类型, CustomerId={CustomerId}, SupplierId={SupplierId}, PersonId={PersonId}",
                customerId, supplierId, personId);
            throw new ValidationException("只能选择一个对方（客户、供应商或人员）");
        }

        if (customerId.HasValue)
        {
            var customer = await _customerRepository.GetByIdAsync(customerId.Value);
            if (customer == null)
            {
                _logger.LogWarning("应收款对方验证失败: 客户不存在, CustomerId={CustomerId}", customerId.Value);
                throw new NotFoundException("客户不存在");
            }
        }

        if (supplierId.HasValue)
        {
            var supplier = await _supplierRepository.GetByIdAsync(supplierId.Value);
            if (supplier == null)
            {
                _logger.LogWarning("应收款对方验证失败: 供应商不存在, SupplierId={SupplierId}", supplierId.Value);
                throw new NotFoundException("供应商不存在");
            }
        }

        if (personId.HasValue)
        {
            var person = await _personRepository.GetByIdAsync(personId.Value);
            if (person == null)
            {
                _logger.LogWarning("应收款对方验证失败: 人员不存在, PersonId={PersonId}", personId.Value);
                throw new NotFoundException("人员不存在");
            }
        }
    }

    public async Task<PageResponse<ReceivableDto>> GetPagedAsync(PageRequest request)
    {
        _logger.LogDebug("查询应收款列表, Page={Page}, PageSize={PageSize}, StartDate={StartDate}, EndDate={EndDate}",
            request.Page, request.PageSize, request.StartDate, request.EndDate);

        var baseQuery = IncludeAll(_receivableRepository.GetQueryable());

        // 应用权限过滤
        var query = ApplyPermissionFilter(baseQuery);

        // 日期范围筛选（到期日期）
        if (request.StartDate.HasValue)
        {
            query = query.Where(r => r.DueDate >= request.StartDate.Value);
        }

        if (request.EndDate.HasValue)
        {
            var endOfDay = request.EndDate.Value.Date.AddDays(1);
            query = query.Where(r => r.DueDate < endOfDay);
        }

        // 项目筛选
        if (request.ProjectId.HasValue)
        {
            query = query.Where(r => r.ProjectId == request.ProjectId.Value);
        }

        // 客户筛选
        if (request.CustomerId.HasValue)
        {
            query = query.Where(r => r.CustomerId == request.CustomerId.Value);
        }

        // 状态筛选
        if (!string.IsNullOrEmpty(request.Status) && Enum.TryParse<ReceivableStatus>(request.Status, true, out var status))
        {
            query = query.Where(r => r.Status == status);
        }

        // 标签筛选
        if (request.TagFilters != null && request.TagFilters.Count > 0)
        {
            var tagBindings = _tagBindingRepository.GetQueryable();
            query = query.ApplyOwnerTagFilters(tagBindings, request.TagFilters, TagScope.Receivable);
        }

        query = query.OrderByDescending(r => r.CreatedAt);

        // 应用自定义排序
        var sortableFields = new Dictionary<string, System.Linq.Expressions.Expression<Func<Receivable, object>>>(StringComparer.OrdinalIgnoreCase)
        {
            ["totalAmount"] = r => r.TotalAmount,
            ["remainingAmount"] = r => r.RemainingAmount,
            ["dueDate"] = r => r.DueDate!,
            ["status"] = r => r.Status,
            ["createdAt"] = r => r.CreatedAt
        };
        query = SortingHelper.ApplySorting(query, request.SortBy, request.SortOrder, sortableFields);

        var total = await query.CountAsync();
        var items = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync();

        var receivableDtos = _mapper.Map<List<ReceivableDto>>(items);
        await _tagBindingRepository.GetQueryable().ApplyTagsAsync(
            TagScope.Receivable,
            receivableDtos,
            dto => dto.Id,
            (dto, tags) => dto.Tags = tags);

        _logger.LogInformation("查询应收款列表成功, 总数={Total}, 返回={Count}",
            total, receivableDtos.Count);

        return new PageResponse<ReceivableDto>
        {
            Items = receivableDtos,
            Page = request.Page,
            PageSize = request.PageSize,
            Total = total
        };
    }

    public async Task<ReceivableDto> GetByIdAsync(long id)
    {
        _logger.LogDebug("查询应收款详情, Id={Id}", id);

        var receivable = await IncludeAll(_receivableRepository.GetQueryable())
            .FirstOrDefaultAsync(r => r.Id == id);

        if (receivable == null)
        {
            _logger.LogWarning("应收款不存在, Id={Id}", id);
            throw new NotFoundException("应收记录不存在");
        }

        // 检查访问权限
        EnsureCanAccess(receivable);

        var receivableDto = _mapper.Map<ReceivableDto>(receivable);
        await _tagBindingRepository.GetQueryable().ApplyTagAsync(
            TagScope.Receivable,
            receivableDto,
            dto => dto.Id,
            (dto, tags) => dto.Tags = tags);

        return receivableDto;
    }

    public async Task<ReceivableDto> CreateAsync(CreateReceivableRequest request)
    {
        var counterpartyDesc = DescribeCounterparty(request.CustomerId, request.SupplierId, request.PersonId);
        _logger.LogDebug("创建应收款, 项目={ProjectId}, 对方={Counterparty}, 金额={Amount}",
            request.ProjectId, counterpartyDesc, request.TotalAmount);

        // Validate project exists
        var project = await _projectRepository.GetByIdAsync(request.ProjectId);
        if (project == null)
        {
            _logger.LogWarning("创建应收款失败: 项目不存在, ProjectId={ProjectId}", request.ProjectId);
            throw new NotFoundException("项目不存在");
        }

        // Validate counterparty
        await ValidateCounterparty(request.CustomerId, request.SupplierId, request.PersonId);

        // Validate amount
        if (request.TotalAmount <= 0)
        {
            _logger.LogWarning("创建应收款失败: 金额无效, Amount={Amount}", request.TotalAmount);
            throw new ValidationException("应收金额必须大于0");
        }

        // Create receivable
        var receivable = new Receivable
        {
            ProjectId = request.ProjectId,
            CustomerId = request.CustomerId,
            SupplierId = request.SupplierId,
            PersonId = request.PersonId,
            TotalAmount = request.TotalAmount,
            ReceivedAmount = 0,
            RemainingAmount = request.TotalAmount,
            DueDate = request.DueDate,
            Status = ReceivableStatus.Pending,
            Description = request.Description
        };

        await using var dbTransaction = await _unitOfWork.BeginTransactionAsync();
        try
        {
            await _receivableRepository.AddAsync(receivable);
            await _unitOfWork.SaveChangesAsync();

            // 重算项目财务汇总
            await _projectRecalculationService.RecalculateAsync(receivable.ProjectId);
            await _unitOfWork.SaveChangesAsync();

            if (dbTransaction != null) await dbTransaction.CommitAsync();
        }
        catch
        {
            if (dbTransaction != null) await dbTransaction.RollbackAsync();
            throw;
        }

        var dto = await GetByIdAsync(receivable.Id);
        await _auditLogService.LogAsync("Create", "Receivable", receivable.Id, null, SerializeForAudit(dto));
        _logger.LogInformation("创建应收款成功, Id={Id}, 项目={ProjectId}, 对方={Counterparty}, 金额={Amount}",
            receivable.Id, request.ProjectId, counterpartyDesc, request.TotalAmount);

        return dto;
    }

    public async Task<ReceivableDto> UpdateAsync(long id, UpdateReceivableRequest request)
    {
        var newCounterpartyDesc = DescribeCounterparty(request.CustomerId, request.SupplierId, request.PersonId);
        _logger.LogDebug("更新应收款, Id={Id}, 新对方={Counterparty}", id, newCounterpartyDesc);

        var receivable = await IncludeAll(_receivableRepository.GetQueryable())
            .FirstOrDefaultAsync(r => r.Id == id);
        if (receivable == null)
        {
            _logger.LogWarning("更新应收款失败: 记录不存在, Id={Id}", id);
            throw new NotFoundException("应收记录不存在");
        }

        // 检查编辑权限
        EnsureCanEdit(receivable);

        var oldCounterpartyDesc = DescribeCounterparty(receivable.CustomerId, receivable.SupplierId, receivable.PersonId);
        var oldDto = _mapper.Map<ReceivableDto>(receivable);
        var oldProjectId = receivable.ProjectId;

        // 如果已有收款记录，不允许修改总金额
        if (receivable.ReceivedAmount > 0 && receivable.TotalAmount != request.TotalAmount)
        {
            _logger.LogWarning("更新应收款失败: 已有收款记录不允许修改总金额, Id={Id}, ReceivedAmount={ReceivedAmount}, 原金额={OldAmount}, 新金额={NewAmount}",
                id, receivable.ReceivedAmount, receivable.TotalAmount, request.TotalAmount);
            throw new ValidationException("已有收款记录的应收款不允许修改总金额");
        }

        // Validate project exists
        var project = await _projectRepository.GetByIdAsync(request.ProjectId);
        if (project == null)
        {
            _logger.LogWarning("更新应收款失败: 项目不存在, ProjectId={ProjectId}", request.ProjectId);
            throw new NotFoundException("项目不存在");
        }

        // Validate counterparty
        await ValidateCounterparty(request.CustomerId, request.SupplierId, request.PersonId);

        // Validate amount
        if (request.TotalAmount <= 0)
        {
            _logger.LogWarning("更新应收款失败: 金额无效, Amount={Amount}", request.TotalAmount);
            throw new ValidationException("应收金额必须大于0");
        }

        // 记录对方类型切换
        if (oldCounterpartyDesc != newCounterpartyDesc)
        {
            _logger.LogInformation("应收款对方类型变更, Id={Id}, 旧对方={OldCounterparty}, 新对方={NewCounterparty}",
                id, oldCounterpartyDesc, newCounterpartyDesc);
        }

        // Update receivable
        receivable.ProjectId = request.ProjectId;
        receivable.CustomerId = request.CustomerId;
        receivable.SupplierId = request.SupplierId;
        receivable.PersonId = request.PersonId;
        receivable.TotalAmount = request.TotalAmount;
        receivable.RemainingAmount = request.TotalAmount - receivable.ReceivedAmount;
        receivable.DueDate = request.DueDate;
        receivable.Description = request.Description;

        await using var dbTransaction = await _unitOfWork.BeginTransactionAsync();
        try
        {
            _receivableRepository.Update(receivable);
            await _unitOfWork.SaveChangesAsync();

            // 重算项目财务汇总（如果 ProjectId 变更，新旧项目都要重算）
            await _projectRecalculationService.RecalculateAsync(receivable.ProjectId);
            if (oldProjectId != receivable.ProjectId)
            {
                await _projectRecalculationService.RecalculateAsync(oldProjectId);
            }
            await _unitOfWork.SaveChangesAsync();

            if (dbTransaction != null) await dbTransaction.CommitAsync();
        }
        catch
        {
            if (dbTransaction != null) await dbTransaction.RollbackAsync();
            throw;
        }

        var updatedDto = await GetByIdAsync(id);
        await _auditLogService.LogAsync("Update", "Receivable", receivable.Id, SerializeForAudit(oldDto), SerializeForAudit(updatedDto));
        _logger.LogInformation("更新应收款成功, Id={Id}, 对方={Counterparty}, 金额={Amount}", id, newCounterpartyDesc, request.TotalAmount);

        return updatedDto;
    }

    public async Task DeleteAsync(long id)
    {
        _logger.LogDebug("删除应收款, Id={Id}", id);

        var receivable = await IncludeAll(_receivableRepository.GetQueryable())
            .FirstOrDefaultAsync(r => r.Id == id);
        if (receivable == null)
        {
            _logger.LogWarning("删除应收款失败: 记录不存在, Id={Id}", id);
            throw new NotFoundException("应收记录不存在");
        }

        // 检查删除权限
        EnsureCanDelete(receivable);

        // 检查是否已有收款记录
        if (receivable.ReceivedAmount > 0)
        {
            _logger.LogWarning("删除应收款失败: 已有收款记录, Id={Id}, ReceivedAmount={ReceivedAmount}",
                id, receivable.ReceivedAmount);
            throw new ValidationException("已有收款记录的应收款不允许删除");
        }

        var counterpartyDesc = DescribeCounterparty(receivable.CustomerId, receivable.SupplierId, receivable.PersonId);
        var oldDto = _mapper.Map<ReceivableDto>(receivable);

        await using var dbTransaction = await _unitOfWork.BeginTransactionAsync();
        try
        {
            _receivableRepository.Delete(id);
            await _unitOfWork.SaveChangesAsync();

            // 重算项目财务汇总
            await _projectRecalculationService.RecalculateAsync(receivable.ProjectId);
            await _unitOfWork.SaveChangesAsync();

            if (dbTransaction != null) await dbTransaction.CommitAsync();
        }
        catch
        {
            if (dbTransaction != null) await dbTransaction.RollbackAsync();
            throw;
        }

        await _auditLogService.LogAsync("Delete", "Receivable", id, SerializeForAudit(oldDto), null);
        _logger.LogInformation("删除应收款成功, Id={Id}, 对方={Counterparty}, 金额={Amount}",
            id, counterpartyDesc, receivable.TotalAmount);
    }

    public async Task<ReceivableDto> ReceivePaymentAsync(long receivableId, ReceivePaymentRequest request)
    {
        _logger.LogDebug("应收款收款, ReceivableId={ReceivableId}, 金额={Amount}, 收款日期={PaymentDate}",
            receivableId, request.Amount, request.PaymentDate);

        var receivable = await IncludeAll(_receivableRepository.GetQueryable())
            .FirstOrDefaultAsync(r => r.Id == receivableId);

        if (receivable == null)
        {
            _logger.LogWarning("应收款收款失败: 记录不存在, ReceivableId={ReceivableId}", receivableId);
            throw new NotFoundException("应收记录不存在");
        }

        var oldPaymentDto = _mapper.Map<ReceivableDto>(receivable);
        var oldStatus = receivable.Status;

        // Validate payment amount
        if (request.Amount <= 0)
        {
            _logger.LogWarning("应收款收款失败: 金额无效, ReceivableId={ReceivableId}, Amount={Amount}",
                receivableId, request.Amount);
            throw new ValidationException("收款金额必须大于0");
        }

        if (request.Amount > receivable.RemainingAmount)
        {
            _logger.LogWarning("应收款收款失败: 金额超出剩余, ReceivableId={ReceivableId}, Amount={Amount}, RemainingAmount={RemainingAmount}",
                receivableId, request.Amount, receivable.RemainingAmount);
            throw new ValidationException($"收款金额不能超过剩余应收金额({receivable.RemainingAmount})");
        }

        if (receivable.Details.Any(detail => !detail.IsDeleted && detail.TransactionId == request.TransactionId))
        {
            _logger.LogWarning("应收款收款失败: 交易重复关联到同一应收, ReceivableId={ReceivableId}, TransactionId={TransactionId}",
                receivableId, request.TransactionId);
            throw new ValidationException("该交易已关联到当前应收记录，请勿重复登记");
        }

        await _settlementTransactionBindingService.ValidateReceivableBindingAsync(
            request.TransactionId,
            request.Amount,
            receivable.ProjectId,
            receivable.CustomerId,
            receivable.SupplierId,
            receivable.PersonId);

        // Create receivable detail
        var detail = new ReceivableDetail
        {
            ReceivableId = receivableId,
            TransactionId = request.TransactionId,
            PaymentDate = request.PaymentDate,
            Amount = request.Amount,
            PaymentMethod = request.PaymentMethod,
            Description = request.Description
        };

        await _detailRepository.AddAsync(detail);

        // Update receivable amounts and status
        receivable.ReceivedAmount += request.Amount;
        receivable.RemainingAmount -= request.Amount;

        if (receivable.RemainingAmount == 0)
        {
            receivable.Status = ReceivableStatus.Settled;
            receivable.SettledAt = DateTime.UtcNow;
        }
        else if (receivable.ReceivedAmount > 0)
        {
            receivable.Status = ReceivableStatus.Partial;
        }

        _receivableRepository.Update(receivable);

        // 统一重算项目财务汇总
        await _projectRecalculationService.RecalculateAsync(receivable.ProjectId);

        // 更新交易分配状态
        await _transactionAllocationHelper.UpdateAllocationStatusAsync(request.TransactionId, saveChanges: false);

        try
        {
            await _unitOfWork.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _unitOfWork.ClearChangeTracker();
            _logger.LogWarning(ex, "应收款收款失败: 检测到并发更新冲突, ReceivableId={ReceivableId}, ProjectId={ProjectId}",
                receivableId, receivable.ProjectId);
            throw new ValidationException("记录已被其他操作更新，请刷新后重试");
        }
        catch (DbUpdateException ex) when (IsDuplicateSettlementBindingException(ex))
        {
            _unitOfWork.ClearChangeTracker();
            _logger.LogWarning(ex, "应收款收款失败: 检测到重复交易关联冲突, ReceivableId={ReceivableId}, TransactionId={TransactionId}",
                receivableId, request.TransactionId);
            throw new ValidationException("该交易已关联到当前应收记录，请勿重复登记");
        }

        var updatedDto = await GetByIdAsync(receivableId);
        await _auditLogService.LogAsync("Update", "Receivable", receivable.Id, SerializeForAudit(oldPaymentDto), SerializeForAudit(updatedDto));

        if (oldStatus != receivable.Status)
        {
            _logger.LogInformation("应收款状态变化, Id={Id}, 旧状态={OldStatus}, 新状态={NewStatus}",
                receivableId, oldStatus, receivable.Status);
        }

        _logger.LogInformation("应收款收款成功, Id={Id}, 本次收款={Amount}, 已收={ReceivedAmount}, 剩余={RemainingAmount}, 状态={Status}",
            receivableId, request.Amount, receivable.ReceivedAmount, receivable.RemainingAmount, receivable.Status);

        return updatedDto;
    }

    private static bool IsDuplicateSettlementBindingException(DbUpdateException exception)
    {
        var message = exception.InnerException?.Message ?? exception.Message;
        return message.Contains("ux_receivable_details_receivable_transaction", StringComparison.OrdinalIgnoreCase);
    }

    public async Task<ReceivableSummaryDto> GetReceivableSummaryAsync()
    {
        _logger.LogDebug("查询应收款汇总统计");

        var query = ApplyPermissionFilter(_receivableRepository.GetQueryable());

        var today = DateTime.UtcNow.Date;

        var summary = await query
            .GroupBy(_ => 1)
            .Select(g => new ReceivableSummaryDto
            {
                TotalReceivable = g.Sum(r => r.TotalAmount),
                TotalReceived = g.Sum(r => r.ReceivedAmount),
                TotalRemaining = g.Sum(r => r.RemainingAmount),
                PendingCount = g.Count(r => r.Status == ReceivableStatus.Pending),
                PartialCount = g.Count(r => r.Status == ReceivableStatus.Partial),
                SettledCount = g.Count(r => r.Status == ReceivableStatus.Settled),
                OverdueCount = g.Count(r =>
                    r.Status != ReceivableStatus.Settled &&
                    r.DueDate.HasValue &&
                    r.DueDate.Value < today)
            })
            .FirstOrDefaultAsync() ?? new ReceivableSummaryDto();

        _logger.LogInformation("查询应收款汇总成功, 总应收={TotalReceivable}, 已收={TotalReceived}, 剩余={TotalRemaining}, 逾期={OverdueCount}",
            summary.TotalReceivable, summary.TotalReceived, summary.TotalRemaining, summary.OverdueCount);

        return summary;
    }

    public async Task<ReceivableTrendDto> GetTrendAsync(DateTime? startDate, DateTime? endDate)
    {
        var end = (endDate ?? DateTime.UtcNow).Date;
        var start = (startDate ?? end.AddMonths(-6).AddDays(1 - end.AddMonths(-6).Day)).Date;

        _logger.LogDebug("查询应收款趋势, Start={Start}, End={End}", start, end);

        var query = ApplyPermissionFilter(_receivableRepository.GetQueryable());

        var data = await query
            .Where(r => r.CreatedAt >= start && r.CreatedAt <= end.AddDays(1))
            .Select(r => new { r.CreatedAt, r.TotalAmount })
            .ToListAsync();

        var grouped = data
            .GroupBy(r => new { r.CreatedAt.Year, r.CreatedAt.Month })
            .ToDictionary(g => $"{g.Key.Year}-{g.Key.Month:D2}", g => g.Sum(r => r.TotalAmount));

        var months = new List<string>();
        var amounts = new List<decimal>();

        var current = new DateTime(start.Year, start.Month, 1);
        var endMonth = new DateTime(end.Year, end.Month, 1);
        while (current <= endMonth)
        {
            var key = $"{current.Year}-{current.Month:D2}";
            months.Add(key);
            amounts.Add(grouped.TryGetValue(key, out var amt) ? amt : 0m);
            current = current.AddMonths(1);
        }

        _logger.LogInformation("查询应收款趋势成功, 月份数={Count}", months.Count);

        return new ReceivableTrendDto { Months = months, Amounts = amounts };
    }

    public async Task<ReceivableAgingDto> GetAgingAsync()
    {
        _logger.LogDebug("查询应收款账龄分析");

        var query = ApplyPermissionFilter(_receivableRepository.GetQueryable());

        var unsettled = await query
            .Where(r => r.Status != ReceivableStatus.Settled && r.DueDate.HasValue)
            .Select(r => new { r.DueDate, r.RemainingAmount })
            .ToListAsync();

        var today = DateTime.UtcNow.Date;

        var notDue     = unsettled.Where(r => r.DueDate!.Value.Date >= today).Sum(r => r.RemainingAmount);
        var days1_30   = unsettled.Where(r => r.DueDate!.Value.Date < today && r.DueDate.Value.Date >= today.AddDays(-30)).Sum(r => r.RemainingAmount);
        var days31_60  = unsettled.Where(r => r.DueDate!.Value.Date < today.AddDays(-30) && r.DueDate.Value.Date >= today.AddDays(-60)).Sum(r => r.RemainingAmount);
        var days61_90  = unsettled.Where(r => r.DueDate!.Value.Date < today.AddDays(-60) && r.DueDate.Value.Date >= today.AddDays(-90)).Sum(r => r.RemainingAmount);
        var days90plus = unsettled.Where(r => r.DueDate!.Value.Date < today.AddDays(-90)).Sum(r => r.RemainingAmount);

        _logger.LogInformation("查询应收款账龄成功, 未到期={NotDue}, 1-30天={D1_30}, 31-60天={D31_60}, 61-90天={D61_90}, 90天以上={D90Plus}",
            notDue, days1_30, days31_60, days61_90, days90plus);

        return new ReceivableAgingDto
        {
            Categories = ["未到期", "1-30天", "31-60天", "61-90天", "90天以上"],
            Amounts = [notDue, days1_30, days31_60, days61_90, days90plus]
        };
    }

    public Task<List<ReceivableDto>> GetByProjectIdAsync(long projectId)
        => GetByFilterAsync(r => r.ProjectId == projectId, nameof(GetByProjectIdAsync), "ProjectId", projectId);

    public Task<List<ReceivableDto>> GetByCustomerIdAsync(long customerId)
        => GetByFilterAsync(r => r.CustomerId == customerId, nameof(GetByCustomerIdAsync), "CustomerId", customerId);

    public Task<List<ReceivableDto>> GetBySupplierIdAsync(long supplierId)
        => GetByFilterAsync(r => r.SupplierId == supplierId, nameof(GetBySupplierIdAsync), "SupplierId", supplierId);

    public Task<List<ReceivableDto>> GetByPersonIdAsync(long personId)
        => GetByFilterAsync(r => r.PersonId == personId, nameof(GetByPersonIdAsync), "PersonId", personId);

    private async Task<List<ReceivableDto>> GetByFilterAsync(
        Expression<Func<Receivable, bool>> predicate,
        string callerName,
        string paramName,
        long paramValue,
        int limit = 500)
    {
        _logger.LogInformation("[ReceivableService.{Caller}] {Param}={Value}", callerName, paramName, paramValue);

        var query = ApplyPermissionFilter(
            IncludeAll(_receivableRepository.GetQueryable())
                .Where(predicate));

        var receivables = await query
            .OrderBy(r => r.DueDate)
            .Take(limit)
            .ToListAsync();

        var result = _mapper.Map<List<ReceivableDto>>(receivables);
        await _tagBindingRepository.GetQueryable().ApplyTagsAsync(
            TagScope.Receivable,
            result,
            dto => dto.Id,
            (dto, tags) => dto.Tags = tags);

        _logger.LogInformation("[ReceivableService.{Caller}] 成功, Count={Count}", callerName, result.Count);
        return result;
    }

    public async Task<ReceivableStatisticsDto> GetStatisticsAsync(PageRequest request)
    {
        _logger.LogDebug("ReceivableService.GetStatisticsAsync");

        var query = ApplyPermissionFilter(_receivableRepository.GetQueryable());

        // 日期范围筛选
        if (request.StartDate.HasValue)
            query = query.Where(r => r.DueDate >= request.StartDate.Value);
        if (request.EndDate.HasValue)
        {
            var endOfDay = request.EndDate.Value.Date.AddDays(1);
            query = query.Where(r => r.DueDate < endOfDay);
        }

        // 项目筛选
        if (request.ProjectId.HasValue)
        {
            query = query.Where(r => r.ProjectId == request.ProjectId.Value);
        }

        // 客户筛选
        if (request.CustomerId.HasValue)
        {
            query = query.Where(r => r.CustomerId == request.CustomerId.Value);
        }

        // 状态筛选
        if (!string.IsNullOrEmpty(request.Status) && Enum.TryParse<ReceivableStatus>(request.Status, true, out var status))
        {
            query = query.Where(r => r.Status == status);
        }

        // 标签筛选
        if (request.TagFilters != null && request.TagFilters.Count > 0)
        {
            var tagBindings = _tagBindingRepository.GetQueryable();
            query = query.ApplyOwnerTagFilters(tagBindings, request.TagFilters, TagScope.Receivable);
        }

        var today = DateTime.UtcNow.Date;
        var result = await query
            .GroupBy(_ => 1)
            .Select(g => new ReceivableStatisticsDto
            {
                TotalCount = g.Count(),
                PendingCount = g.Count(r => r.Status == ReceivableStatus.Pending),
                PartialCount = g.Count(r => r.Status == ReceivableStatus.Partial),
                SettledCount = g.Count(r => r.Status == ReceivableStatus.Settled),
                TotalAmount = g.Sum(r => r.TotalAmount),
                ReceivedAmount = g.Sum(r => r.ReceivedAmount),
                RemainingAmount = g.Sum(r => r.RemainingAmount),
                OverdueAmount = g.Sum(r =>
                    r.Status != ReceivableStatus.Settled && r.DueDate.HasValue && r.DueDate.Value < today && r.RemainingAmount > 0
                    ? r.RemainingAmount : 0)
            })
            .FirstOrDefaultAsync() ?? new ReceivableStatisticsDto();

        return result;
    }
}
