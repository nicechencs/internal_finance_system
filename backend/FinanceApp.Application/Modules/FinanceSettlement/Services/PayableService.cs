using System.Linq.Expressions;
using MapsterMapper;
using FinanceApp.Application.Common;
using FinanceApp.Application.Extensions;
using FinanceApp.Application.Modules.FinanceSettlement.DTOs.Payable;
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

public class PayableService : ServiceBase, IPayableService
{
    private readonly IRepository<Payable> _payableRepository;
    private readonly IRepository<PayableDetail> _detailRepository;
    private readonly IRepository<Supplier> _supplierRepository;
    private readonly IRepository<Customer> _customerRepository;
    private readonly IRepository<Person> _personRepository;
    private readonly IRepository<Project> _projectRepository;
    private readonly IRepository<TagBinding> _tagBindingRepository;
    private readonly IRepository<PayableType> _payableTypeRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<PayableService> _logger;
    private readonly IAuditLogService _auditLogService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ISettlementTransactionBindingService _settlementTransactionBindingService;
    private readonly TransactionAllocationHelper _transactionAllocationHelper;
    private readonly IProjectFinancialRecalculationService _projectRecalculationService;

    public PayableService(
        IRepository<Payable> payableRepository,
        IRepository<PayableDetail> detailRepository,
        IRepository<Supplier> supplierRepository,
        IRepository<Customer> customerRepository,
        IRepository<Person> personRepository,
        IRepository<Project> projectRepository,
        IRepository<TagBinding> tagBindingRepository,
        IRepository<PayableType> payableTypeRepository,
        IMapper mapper,
        ILogger<PayableService> logger,
        IAuditLogService auditLogService,
        ICurrentUserService currentUserService,
        IDataPermissionService permissionService,
        IUnitOfWork unitOfWork,
        ISettlementTransactionBindingService settlementTransactionBindingService,
        TransactionAllocationHelper transactionAllocationHelper,
        IProjectFinancialRecalculationService projectRecalculationService)
        : base(currentUserService, permissionService)
    {
        _payableRepository = payableRepository;
        _detailRepository = detailRepository;
        _supplierRepository = supplierRepository;
        _customerRepository = customerRepository;
        _personRepository = personRepository;
        _projectRepository = projectRepository;
        _tagBindingRepository = tagBindingRepository;
        _payableTypeRepository = payableTypeRepository;
        _mapper = mapper;
        _logger = logger;
        _auditLogService = auditLogService;
        _unitOfWork = unitOfWork;
        _settlementTransactionBindingService = settlementTransactionBindingService;
        _transactionAllocationHelper = transactionAllocationHelper;
        _projectRecalculationService = projectRecalculationService;
    }

    private IQueryable<Payable> IncludeAll(IQueryable<Payable> query)
    {
        return query
            .Include(p => p.Supplier)
            .Include(p => p.Customer)
            .Include(p => p.Person)
            .Include(p => p.Project)
            .Include(p => p.PayableType)
            .Include(p => p.Details);
    }

    /// <summary>
    /// 返回对方类型描述，用于日志输出
    /// </summary>
    private static string DescribeCounterparty(long? supplierId, long? customerId, long? personId)
    {
        if (supplierId.HasValue) return $"供应商(Id={supplierId.Value})";
        if (customerId.HasValue) return $"客户(Id={customerId.Value})";
        if (personId.HasValue) return $"人员(Id={personId.Value})";
        return "无";
    }

    private async Task ValidateCounterparty(long? supplierId, long? customerId, long? personId)
    {
        var selectedCount = (supplierId.HasValue ? 1 : 0)
            + (customerId.HasValue ? 1 : 0)
            + (personId.HasValue ? 1 : 0);

        if (selectedCount == 0)
        {
            _logger.LogWarning("应付款对方验证失败: 未选择任何对方类型");
            throw new ValidationException("必须选择一个对方（客户、供应商或人员）");
        }

        if (selectedCount > 1)
        {
            _logger.LogWarning("应付款对方验证失败: 选择了多个对方类型, SupplierId={SupplierId}, CustomerId={CustomerId}, PersonId={PersonId}",
                supplierId, customerId, personId);
            throw new ValidationException("只能选择一个对方（客户、供应商或人员）");
        }

        if (supplierId.HasValue)
        {
            var supplier = await _supplierRepository.GetByIdAsync(supplierId.Value);
            if (supplier == null)
            {
                _logger.LogWarning("应付款对方验证失败: 供应商不存在, SupplierId={SupplierId}", supplierId.Value);
                throw new NotFoundException("供应商不存在");
            }
        }

        if (customerId.HasValue)
        {
            var customer = await _customerRepository.GetByIdAsync(customerId.Value);
            if (customer == null)
            {
                _logger.LogWarning("应付款对方验证失败: 客户不存在, CustomerId={CustomerId}", customerId.Value);
                throw new NotFoundException("客户不存在");
            }
        }

        if (personId.HasValue)
        {
            var person = await _personRepository.GetByIdAsync(personId.Value);
            if (person == null)
            {
                _logger.LogWarning("应付款对方验证失败: 人员不存在, PersonId={PersonId}", personId.Value);
                throw new NotFoundException("人员不存在");
            }
        }
    }

    public async Task<PageResponse<PayableDto>> GetPagedAsync(PageRequest request)
    {
        _logger.LogDebug("查询应付款列表, Page={Page}, PageSize={PageSize}, StartDate={StartDate}, EndDate={EndDate}",
            request.Page, request.PageSize, request.StartDate, request.EndDate);

        var baseQuery = IncludeAll(_payableRepository.GetQueryable());

        // 应用权限过滤
        var query = ApplyPermissionFilter(baseQuery);

        // 日期范围筛选（到期日期）
        if (request.StartDate.HasValue)
        {
            query = query.Where(p => p.DueDate >= request.StartDate.Value);
        }

        if (request.EndDate.HasValue)
        {
            var endOfDay = request.EndDate.Value.Date.AddDays(1);
            query = query.Where(p => p.DueDate < endOfDay);
        }

        // 项目筛选
        if (request.ProjectId.HasValue)
        {
            query = query.Where(p => p.ProjectId == request.ProjectId.Value);
        }

        // 供应商筛选
        if (request.SupplierId.HasValue)
        {
            query = query.Where(p => p.SupplierId == request.SupplierId.Value);
        }

        // 状态筛选
        if (!string.IsNullOrEmpty(request.Status) && Enum.TryParse<PayableStatus>(request.Status, true, out var status))
        {
            query = query.Where(p => p.Status == status);
        }

        // 标签筛选
        if (request.TagFilters != null && request.TagFilters.Count > 0)
        {
            var tagBindings = _tagBindingRepository.GetQueryable();
            query = query.ApplyOwnerTagFilters(tagBindings, request.TagFilters, TagScope.Payable);
        }

        query = query.OrderByDescending(p => p.CreatedAt);

        // 应用自定义排序
        var sortableFields = new Dictionary<string, System.Linq.Expressions.Expression<Func<Payable, object>>>(StringComparer.OrdinalIgnoreCase)
        {
            ["totalAmount"] = p => p.TotalAmount,
            ["remainingAmount"] = p => p.RemainingAmount,
            ["dueDate"] = p => p.DueDate!,
            ["status"] = p => p.Status,
            ["createdAt"] = p => p.CreatedAt
        };
        query = SortingHelper.ApplySorting(query, request.SortBy, request.SortOrder, sortableFields);

        var total = await query.CountAsync();
        var items = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync();

        var payableDtos = _mapper.Map<List<PayableDto>>(items);
        await _tagBindingRepository.GetQueryable().ApplyTagsAsync(
            TagScope.Payable,
            payableDtos,
            dto => dto.Id,
            (dto, tags) => dto.Tags = tags);

        _logger.LogInformation("查询应付款列表成功, 总数={Total}, 返回={Count}", total, payableDtos.Count);

        return new PageResponse<PayableDto>
        {
            Items = payableDtos,
            Page = request.Page,
            PageSize = request.PageSize,
            Total = total
        };
    }

    public async Task<PayableDto> GetByIdAsync(long id)
    {
        _logger.LogDebug("查询应付款详情, Id={Id}", id);

        var payable = await IncludeAll(_payableRepository.GetQueryable())
            .FirstOrDefaultAsync(p => p.Id == id);

        if (payable == null)
        {
            _logger.LogWarning("应付款不存在, Id={Id}", id);
            throw new NotFoundException("应付记录不存在");
        }

        // 检查访问权限
        EnsureCanAccess(payable);

        var payableDto = _mapper.Map<PayableDto>(payable);
        await _tagBindingRepository.GetQueryable().ApplyTagAsync(
            TagScope.Payable,
            payableDto,
            dto => dto.Id,
            (dto, tags) => dto.Tags = tags);

        return payableDto;
    }

    public async Task<PayableDto> CreateAsync(CreatePayableRequest request)
    {
        var counterpartyDesc = DescribeCounterparty(request.SupplierId, request.CustomerId, request.PersonId);
        _logger.LogDebug("创建应付款, 对方={Counterparty}, 项目={ProjectId}, 金额={Amount}",
            counterpartyDesc, request.ProjectId, request.TotalAmount);

        // Validate counterparty
        await ValidateCounterparty(request.SupplierId, request.CustomerId, request.PersonId);

        // Validate project if provided
        if (request.ProjectId.HasValue)
        {
            var project = await _projectRepository.GetByIdAsync(request.ProjectId.Value);
            if (project == null)
            {
                _logger.LogWarning("创建应付款失败: 项目不存在, ProjectId={ProjectId}", request.ProjectId.Value);
                throw new NotFoundException("项目不存在");
            }
        }

        // Validate payable type if provided
        if (request.PayableTypeId.HasValue)
        {
            var payableType = await _payableTypeRepository.GetByIdAsync(request.PayableTypeId.Value);
            if (payableType == null || !payableType.IsActive)
            {
                _logger.LogWarning("创建应付款失败: 业务类型不存在或未启用, PayableTypeId={PayableTypeId}", request.PayableTypeId.Value);
                throw new NotFoundException("业务类型不存在或未启用");
            }
        }

        // Validate amount
        if (request.TotalAmount <= 0)
        {
            _logger.LogWarning("创建应付款失败: 金额无效, Amount={Amount}", request.TotalAmount);
            throw new ValidationException("应付金额必须大于0");
        }

        // Create payable
        var payable = new Payable
        {
            SupplierId = request.SupplierId,
            CustomerId = request.CustomerId,
            PersonId = request.PersonId,
            ProjectId = request.ProjectId,
            PayableTypeId = request.PayableTypeId,
            TotalAmount = request.TotalAmount,
            PaidAmount = 0,
            RemainingAmount = request.TotalAmount,
            DueDate = request.DueDate,
            Status = PayableStatus.Pending,
            Description = request.Description
        };

        await using var dbTransaction = await _unitOfWork.BeginTransactionAsync();
        try
        {
            await _payableRepository.AddAsync(payable);
            await _unitOfWork.SaveChangesAsync();

            // 重算项目财务汇总
            if (payable.ProjectId.HasValue)
            {
                await _projectRecalculationService.RecalculateAsync(payable.ProjectId.Value);
                await _unitOfWork.SaveChangesAsync();
            }

            if (dbTransaction != null) await dbTransaction.CommitAsync();
        }
        catch
        {
            if (dbTransaction != null) await dbTransaction.RollbackAsync();
            throw;
        }

        var dto = await GetByIdAsync(payable.Id);
        await _auditLogService.LogAsync("Create", "Payable", payable.Id, null, SerializeForAudit(dto));
        _logger.LogInformation("创建应付款成功, Id={Id}, 对方={Counterparty}, 项目={ProjectId}, 金额={Amount}",
            payable.Id, counterpartyDesc, request.ProjectId, request.TotalAmount);

        return dto;
    }

    public async Task<PayableDto> UpdateAsync(long id, UpdatePayableRequest request)
    {
        var newCounterpartyDesc = DescribeCounterparty(request.SupplierId, request.CustomerId, request.PersonId);
        _logger.LogDebug("更新应付款, Id={Id}, 新对方={Counterparty}", id, newCounterpartyDesc);

        var payable = await IncludeAll(_payableRepository.GetQueryable())
            .FirstOrDefaultAsync(p => p.Id == id);
        if (payable == null)
        {
            _logger.LogWarning("更新应付款失败: 记录不存在, Id={Id}", id);
            throw new NotFoundException("应付记录不存在");
        }

        // 检查编辑权限
        EnsureCanEdit(payable);

        var oldCounterpartyDesc = DescribeCounterparty(payable.SupplierId, payable.CustomerId, payable.PersonId);
        var oldDto = _mapper.Map<PayableDto>(payable);
        var oldProjectId = payable.ProjectId;

        // 如果已有付款记录，不允许修改总金额
        if (payable.PaidAmount > 0 && payable.TotalAmount != request.TotalAmount)
        {
            _logger.LogWarning("更新应付款失败: 已有付款记录不允许修改总金额, Id={Id}, PaidAmount={PaidAmount}, 原金额={OldAmount}, 新金额={NewAmount}",
                id, payable.PaidAmount, payable.TotalAmount, request.TotalAmount);
            throw new ValidationException("已有付款记录的应付款不允许修改总金额");
        }

        // Validate counterparty
        await ValidateCounterparty(request.SupplierId, request.CustomerId, request.PersonId);

        // Validate project if provided
        if (request.ProjectId.HasValue)
        {
            var project = await _projectRepository.GetByIdAsync(request.ProjectId.Value);
            if (project == null)
            {
                _logger.LogWarning("更新应付款失败: 项目不存在, ProjectId={ProjectId}", request.ProjectId.Value);
                throw new NotFoundException("项目不存在");
            }
        }

        // Validate payable type if provided
        if (request.PayableTypeId.HasValue)
        {
            var payableType = await _payableTypeRepository.GetByIdAsync(request.PayableTypeId.Value);
            if (payableType == null || !payableType.IsActive)
            {
                _logger.LogWarning("更新应付款失败: 业务类型不存在或未启用, PayableTypeId={PayableTypeId}", request.PayableTypeId.Value);
                throw new NotFoundException("业务类型不存在或未启用");
            }
        }

        // Validate amount
        if (request.TotalAmount <= 0)
        {
            _logger.LogWarning("更新应付款失败: 金额无效, Amount={Amount}", request.TotalAmount);
            throw new ValidationException("应付金额必须大于0");
        }

        // 记录对方类型切换
        if (oldCounterpartyDesc != newCounterpartyDesc)
        {
            _logger.LogInformation("应付款对方类型变更, Id={Id}, 旧对方={OldCounterparty}, 新对方={NewCounterparty}",
                id, oldCounterpartyDesc, newCounterpartyDesc);
        }

        // Update payable
        payable.SupplierId = request.SupplierId;
        payable.CustomerId = request.CustomerId;
        payable.PersonId = request.PersonId;
        payable.ProjectId = request.ProjectId;
        payable.PayableTypeId = request.PayableTypeId;
        payable.TotalAmount = request.TotalAmount;
        payable.RemainingAmount = request.TotalAmount - payable.PaidAmount;
        payable.DueDate = request.DueDate;
        payable.Description = request.Description;

        await using var dbTransaction = await _unitOfWork.BeginTransactionAsync();
        try
        {
            _payableRepository.Update(payable);
            await _unitOfWork.SaveChangesAsync();

            // 重算项目财务汇总（如果 ProjectId 变更，新旧项目都要重算）
            if (payable.ProjectId.HasValue)
            {
                await _projectRecalculationService.RecalculateAsync(payable.ProjectId.Value);
            }
            if (oldProjectId.HasValue && oldProjectId != payable.ProjectId)
            {
                await _projectRecalculationService.RecalculateAsync(oldProjectId.Value);
            }
            if (payable.ProjectId.HasValue || (oldProjectId.HasValue && oldProjectId != payable.ProjectId))
            {
                await _unitOfWork.SaveChangesAsync();
            }

            if (dbTransaction != null) await dbTransaction.CommitAsync();
        }
        catch
        {
            if (dbTransaction != null) await dbTransaction.RollbackAsync();
            throw;
        }

        var updatedDto = await GetByIdAsync(id);
        await _auditLogService.LogAsync("Update", "Payable", payable.Id, SerializeForAudit(oldDto), SerializeForAudit(updatedDto));
        _logger.LogInformation("更新应付款成功, Id={Id}, 对方={Counterparty}, 金额={Amount}", id, newCounterpartyDesc, request.TotalAmount);

        return updatedDto;
    }

    public async Task DeleteAsync(long id)
    {
        _logger.LogDebug("删除应付款, Id={Id}", id);

        var payable = await IncludeAll(_payableRepository.GetQueryable())
            .FirstOrDefaultAsync(p => p.Id == id);
        if (payable == null)
        {
            _logger.LogWarning("删除应付款失败: 记录不存在, Id={Id}", id);
            throw new NotFoundException("应付记录不存在");
        }

        // 检查删除权限
        EnsureCanDelete(payable);

        // 检查是否已有付款记录
        if (payable.PaidAmount > 0)
        {
            _logger.LogWarning("删除应付款失败: 已有付款记录, Id={Id}, PaidAmount={PaidAmount}",
                id, payable.PaidAmount);
            throw new ValidationException("已有付款记录的应付款不允许删除");
        }

        var counterpartyDesc = DescribeCounterparty(payable.SupplierId, payable.CustomerId, payable.PersonId);
        var oldDto = _mapper.Map<PayableDto>(payable);

        await using var dbTransaction = await _unitOfWork.BeginTransactionAsync();
        try
        {
            _payableRepository.Delete(id);
            await _unitOfWork.SaveChangesAsync();

            // 重算项目财务汇总
            if (payable.ProjectId.HasValue)
            {
                await _projectRecalculationService.RecalculateAsync(payable.ProjectId.Value);
                await _unitOfWork.SaveChangesAsync();
            }

            if (dbTransaction != null) await dbTransaction.CommitAsync();
        }
        catch
        {
            if (dbTransaction != null) await dbTransaction.RollbackAsync();
            throw;
        }

        await _auditLogService.LogAsync("Delete", "Payable", id, SerializeForAudit(oldDto), null);
        _logger.LogInformation("删除应付款成功, Id={Id}, 对方={Counterparty}, 金额={Amount}",
            id, counterpartyDesc, payable.TotalAmount);
    }

    public async Task<PayableDto> PayPaymentAsync(long payableId, PayPaymentRequest request)
    {
        _logger.LogDebug("应付款付款, PayableId={PayableId}, 金额={Amount}, 付款日期={PaymentDate}",
            payableId, request.Amount, request.PaymentDate);

        var payable = await IncludeAll(_payableRepository.GetQueryable())
            .FirstOrDefaultAsync(p => p.Id == payableId);

        if (payable == null)
        {
            _logger.LogWarning("应付款付款失败: 记录不存在, PayableId={PayableId}", payableId);
            throw new NotFoundException("应付记录不存在");
        }

        var oldStatus = payable.Status;
        var oldPaymentDto = _mapper.Map<PayableDto>(payable);

        // Validate payment amount
        if (request.Amount <= 0)
        {
            _logger.LogWarning("应付款付款失败: 金额无效, PayableId={PayableId}, Amount={Amount}",
                payableId, request.Amount);
            throw new ValidationException("付款金额必须大于0");
        }

        if (request.Amount > payable.RemainingAmount)
        {
            _logger.LogWarning("应付款付款失败: 金额超出剩余, PayableId={PayableId}, Amount={Amount}, RemainingAmount={RemainingAmount}",
                payableId, request.Amount, payable.RemainingAmount);
            throw new ValidationException($"付款金额不能超过剩余应付金额({payable.RemainingAmount})");
        }

        if (payable.Details.Any(detail => !detail.IsDeleted && detail.TransactionId == request.TransactionId))
        {
            _logger.LogWarning("应付款付款失败: 交易重复关联到同一应付, PayableId={PayableId}, TransactionId={TransactionId}",
                payableId, request.TransactionId);
            throw new ValidationException("该交易已关联到当前应付记录，请勿重复登记");
        }

        await _settlementTransactionBindingService.ValidatePayableBindingAsync(
            request.TransactionId,
            request.Amount,
            payable.ProjectId,
            payable.SupplierId,
            payable.CustomerId,
            payable.PersonId);

        // Create payable detail
        var detail = new PayableDetail
        {
            PayableId = payableId,
            TransactionId = request.TransactionId,
            PaymentDate = request.PaymentDate,
            Amount = request.Amount,
            PaymentMethod = request.PaymentMethod,
            Description = request.Description
        };

        await _detailRepository.AddAsync(detail);

        // Update payable amounts and status
        payable.PaidAmount += request.Amount;
        payable.RemainingAmount -= request.Amount;

        if (payable.RemainingAmount == 0)
        {
            payable.Status = PayableStatus.Settled;
            payable.SettledAt = DateTime.UtcNow;
        }
        else if (payable.PaidAmount > 0)
        {
            payable.Status = PayableStatus.Partial;
        }

        _payableRepository.Update(payable);

        // 统一重算项目财务汇总
        if (payable.ProjectId.HasValue)
        {
            await _projectRecalculationService.RecalculateAsync(payable.ProjectId.Value);
        }

        // 更新交易分配状态
        await _transactionAllocationHelper.UpdateAllocationStatusAsync(request.TransactionId, saveChanges: false);

        try
        {
            await _unitOfWork.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _unitOfWork.ClearChangeTracker();
            _logger.LogWarning(ex, "应付款付款失败: 检测到并发更新冲突, PayableId={PayableId}, ProjectId={ProjectId}",
                payableId, payable.ProjectId);
            throw new ValidationException("记录已被其他操作更新，请刷新后重试");
        }
        catch (DbUpdateException ex) when (IsDuplicateSettlementBindingException(ex))
        {
            _unitOfWork.ClearChangeTracker();
            _logger.LogWarning(ex, "应付款付款失败: 检测到重复交易关联冲突, PayableId={PayableId}, TransactionId={TransactionId}",
                payableId, request.TransactionId);
            throw new ValidationException("该交易已关联到当前应付记录，请勿重复登记");
        }

        var updatedDto = await GetByIdAsync(payableId);
        await _auditLogService.LogAsync("Update", "Payable", payable.Id, SerializeForAudit(oldPaymentDto), SerializeForAudit(updatedDto));

        if (oldStatus != payable.Status)
        {
            _logger.LogInformation("应付款状态变化, Id={Id}, 旧状态={OldStatus}, 新状态={NewStatus}",
                payableId, oldStatus, payable.Status);
        }

        _logger.LogInformation("应付款付款成功, Id={Id}, 金额={Amount}, 已付={PaidAmount}, 剩余={RemainingAmount}, 状态={Status}",
            payableId, request.Amount, payable.PaidAmount, payable.RemainingAmount, payable.Status);

        return updatedDto;
    }

    private static bool IsDuplicateSettlementBindingException(DbUpdateException exception)
    {
        var message = exception.InnerException?.Message ?? exception.Message;
        return message.Contains("ux_payable_details_payable_transaction", StringComparison.OrdinalIgnoreCase);
    }

    public async Task<PayableSummaryDto> GetPayableSummaryAsync()
    {
        _logger.LogDebug("查询应付款汇总统计");

        var query = ApplyPermissionFilter(_payableRepository.GetQueryable());

        var today = DateTime.UtcNow.Date;

        var summary = await query
            .GroupBy(_ => 1)
            .Select(g => new PayableSummaryDto
            {
                TotalPayable = g.Sum(p => p.TotalAmount),
                TotalPaid = g.Sum(p => p.PaidAmount),
                TotalRemaining = g.Sum(p => p.RemainingAmount),
                PendingCount = g.Count(p => p.Status == PayableStatus.Pending),
                PartialCount = g.Count(p => p.Status == PayableStatus.Partial),
                SettledCount = g.Count(p => p.Status == PayableStatus.Settled),
                OverdueCount = g.Count(p =>
                    p.Status != PayableStatus.Settled &&
                    p.DueDate.HasValue &&
                    p.DueDate.Value < today)
            })
            .FirstOrDefaultAsync() ?? new PayableSummaryDto();

        _logger.LogInformation("查询应付款汇总成功, 总应付={TotalPayable}, 已付={TotalPaid}, 剩余={TotalRemaining}, 逾期={OverdueCount}",
            summary.TotalPayable, summary.TotalPaid, summary.TotalRemaining, summary.OverdueCount);

        return summary;
    }

    public async Task<PayableTrendDto> GetTrendAsync(DateTime? startDate, DateTime? endDate)
    {
        var end = (endDate ?? DateTime.UtcNow).Date;
        var start = (startDate ?? end.AddMonths(-6).AddDays(1 - end.AddMonths(-6).Day)).Date;

        _logger.LogDebug("查询应付款趋势, Start={Start}, End={End}", start, end);

        var query = ApplyPermissionFilter(_payableRepository.GetQueryable());

        var data = await query
            .Where(p => p.CreatedAt >= start && p.CreatedAt <= end.AddDays(1))
            .Select(p => new { p.CreatedAt, p.TotalAmount })
            .ToListAsync();

        var grouped = data
            .GroupBy(p => new { p.CreatedAt.Year, p.CreatedAt.Month })
            .ToDictionary(g => $"{g.Key.Year}-{g.Key.Month:D2}", g => g.Sum(p => p.TotalAmount));

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

        _logger.LogInformation("查询应付款趋势成功, 月份数={Count}", months.Count);

        return new PayableTrendDto { Months = months, Amounts = amounts };
    }

    public async Task<PayableAgingDto> GetAgingAsync()
    {
        _logger.LogDebug("查询应付款账龄分析");

        var query = ApplyPermissionFilter(_payableRepository.GetQueryable());

        var unsettled = await query
            .Where(p => p.Status != PayableStatus.Settled && p.DueDate.HasValue)
            .Select(p => new { p.DueDate, p.RemainingAmount })
            .ToListAsync();

        var today = DateTime.UtcNow.Date;

        var notDue     = unsettled.Where(p => p.DueDate!.Value.Date >= today).Sum(p => p.RemainingAmount);
        var days1_30   = unsettled.Where(p => p.DueDate!.Value.Date < today && p.DueDate.Value.Date >= today.AddDays(-30)).Sum(p => p.RemainingAmount);
        var days31_60  = unsettled.Where(p => p.DueDate!.Value.Date < today.AddDays(-30) && p.DueDate.Value.Date >= today.AddDays(-60)).Sum(p => p.RemainingAmount);
        var days61_90  = unsettled.Where(p => p.DueDate!.Value.Date < today.AddDays(-60) && p.DueDate.Value.Date >= today.AddDays(-90)).Sum(p => p.RemainingAmount);
        var days90plus = unsettled.Where(p => p.DueDate!.Value.Date < today.AddDays(-90)).Sum(p => p.RemainingAmount);

        _logger.LogInformation("查询应付款账龄成功, 未到期={NotDue}, 1-30天={D1_30}, 31-60天={D31_60}, 61-90天={D61_90}, 90天以上={D90Plus}",
            notDue, days1_30, days31_60, days61_90, days90plus);

        return new PayableAgingDto
        {
            Categories = ["未到期", "1-30天", "31-60天", "61-90天", "90天以上"],
            Amounts = [notDue, days1_30, days31_60, days61_90, days90plus]
        };
    }

    public Task<List<PayableDto>> GetByCustomerIdAsync(long customerId)
        => GetByFilterAsync(p => p.CustomerId == customerId, nameof(GetByCustomerIdAsync), "CustomerId", customerId);

    public Task<List<PayableDto>> GetBySupplierIdAsync(long supplierId)
        => GetByFilterAsync(p => p.SupplierId == supplierId, nameof(GetBySupplierIdAsync), "SupplierId", supplierId);

    public Task<List<PayableDto>> GetByPersonIdAsync(long personId)
        => GetByFilterAsync(p => p.PersonId == personId, nameof(GetByPersonIdAsync), "PersonId", personId);

    public Task<List<PayableDto>> GetByProjectIdAsync(long projectId)
        => GetByFilterAsync(p => p.ProjectId == projectId, nameof(GetByProjectIdAsync), "ProjectId", projectId);

    private async Task<List<PayableDto>> GetByFilterAsync(
        Expression<Func<Payable, bool>> predicate,
        string callerName,
        string paramName,
        long paramValue,
        int limit = 500)
    {
        _logger.LogInformation("[PayableService.{Caller}] {Param}={Value}", callerName, paramName, paramValue);

        var query = ApplyPermissionFilter(
            IncludeAll(_payableRepository.GetQueryable())
                .Where(predicate));

        var payables = await query
            .OrderBy(p => p.DueDate)
            .Take(limit)
            .ToListAsync();

        var result = _mapper.Map<List<PayableDto>>(payables);
        await _tagBindingRepository.GetQueryable().ApplyTagsAsync(
            TagScope.Payable,
            result,
            dto => dto.Id,
            (dto, tags) => dto.Tags = tags);

        _logger.LogInformation("[PayableService.{Caller}] 成功, Count={Count}", callerName, result.Count);
        return result;
    }

    public async Task<PayableStatisticsDto> GetStatisticsAsync(PageRequest request)
    {
        _logger.LogDebug("PayableService.GetStatisticsAsync");

        var query = ApplyPermissionFilter(_payableRepository.GetQueryable());

        // 日期范围筛选
        if (request.StartDate.HasValue)
            query = query.Where(p => p.DueDate >= request.StartDate.Value);
        if (request.EndDate.HasValue)
        {
            var endOfDay = request.EndDate.Value.Date.AddDays(1);
            query = query.Where(p => p.DueDate < endOfDay);
        }

        // 项目筛选
        if (request.ProjectId.HasValue)
        {
            query = query.Where(p => p.ProjectId == request.ProjectId.Value);
        }

        // 供应商筛选
        if (request.SupplierId.HasValue)
        {
            query = query.Where(p => p.SupplierId == request.SupplierId.Value);
        }

        // 状态筛选
        if (!string.IsNullOrEmpty(request.Status) && Enum.TryParse<PayableStatus>(request.Status, true, out var status))
        {
            query = query.Where(p => p.Status == status);
        }

        // 标签筛选
        if (request.TagFilters != null && request.TagFilters.Count > 0)
        {
            var tagBindings = _tagBindingRepository.GetQueryable();
            query = query.ApplyOwnerTagFilters(tagBindings, request.TagFilters, TagScope.Payable);
        }

        var today = DateTime.UtcNow.Date;
        var result = await query
            .GroupBy(_ => 1)
            .Select(g => new PayableStatisticsDto
            {
                TotalCount = g.Count(),
                PendingCount = g.Count(p => p.Status == PayableStatus.Pending),
                PartialCount = g.Count(p => p.Status == PayableStatus.Partial),
                SettledCount = g.Count(p => p.Status == PayableStatus.Settled),
                TotalAmount = g.Sum(p => p.TotalAmount),
                PaidAmount = g.Sum(p => p.PaidAmount),
                RemainingAmount = g.Sum(p => p.RemainingAmount),
                OverdueAmount = g.Sum(p =>
                    p.Status != PayableStatus.Settled && p.DueDate.HasValue && p.DueDate.Value < today && p.RemainingAmount > 0
                    ? p.RemainingAmount : 0)
            })
            .FirstOrDefaultAsync() ?? new PayableStatisticsDto();

        return result;
    }
}
