using MapsterMapper;
using FinanceApp.Application.Common;
using FinanceApp.Application.Extensions;
using FinanceApp.Application.Modules.Identity.Interfaces;
using FinanceApp.Application.Modules.MasterData.DTOs.Supplier;
using FinanceApp.Application.Modules.MasterData.Interfaces;
using FinanceApp.Application.Services;
using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Enums;
using FinanceApp.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FinanceApp.Application.Modules.MasterData.Services;

public class SupplierService : ServiceBase, ISupplierService
{
    private readonly IRepository<Supplier> _supplierRepository;
    private readonly IRepository<Payable> _payableRepository;
    private readonly IRepository<Receivable> _receivableRepository;
    private readonly IRepository<TagBinding> _tagBindingRepository;
    private readonly IMasterDataReferenceGuard _referenceGuard;
    private readonly IMapper _mapper;
    private readonly ILogger<SupplierService> _logger;
    private readonly IAuditLogService _auditLogService;
    private readonly IUnitOfWork _unitOfWork;

    public SupplierService(
        IRepository<Supplier> supplierRepository,
        IRepository<Payable> payableRepository,
        IRepository<Receivable> receivableRepository,
        IRepository<TagBinding> tagBindingRepository,
        IMasterDataReferenceGuard referenceGuard,
        IMapper mapper,
        ILogger<SupplierService> logger,
        IAuditLogService auditLogService,
        ICurrentUserService currentUserService,
        IDataPermissionService permissionService,
        IUnitOfWork unitOfWork)
        : base(currentUserService, permissionService)
    {
        _supplierRepository = supplierRepository;
        _payableRepository = payableRepository;
        _receivableRepository = receivableRepository;
        _tagBindingRepository = tagBindingRepository;
        _referenceGuard = referenceGuard;
        _mapper = mapper;
        _logger = logger;
        _auditLogService = auditLogService;
        _unitOfWork = unitOfWork;
    }

    public async Task<PageResponse<SupplierDto>> GetPagedAsync(PageRequest request)
    {
        _logger.LogDebug("SupplierService.GetPagedAsync - 开始查询供应商分页列表: Page={Page}, PageSize={PageSize}",
            request.Page, request.PageSize);

        try
        {
            var baseQuery = _supplierRepository.GetQueryable();

            // 应用权限过滤
            var query = ApplyPermissionFilter(baseQuery);

            // 按名称筛选
            if (!string.IsNullOrWhiteSpace(request.Name))
            {
                query = query.Where(s => s.Name.Contains(request.Name));
            }

            // 按联系人筛选
            if (!string.IsNullOrWhiteSpace(request.ContactPerson))
            {
                query = query.Where(s => s.ContactPerson != null && s.ContactPerson.Contains(request.ContactPerson));
            }

            // 按联系电话筛选
            if (!string.IsNullOrWhiteSpace(request.ContactPhone))
            {
                query = query.Where(s => s.ContactPhone != null && s.ContactPhone.Contains(request.ContactPhone));
            }

            // 标签过滤
            if (request.TagFilters != null && request.TagFilters.Count > 0)
            {
                var tagBindings = _tagBindingRepository.GetQueryable();
                query = query.ApplyOwnerTagFilters(tagBindings, request.TagFilters, TagScope.Supplier);
            }

            IQueryable<Supplier> orderedQuery = query.OrderByDescending(s => s.CreatedAt);

            // 应用自定义排序
            var sortableFields = new Dictionary<string, System.Linq.Expressions.Expression<Func<Supplier, object>>>(StringComparer.OrdinalIgnoreCase)
            {
                ["name"] = s => s.Name,
                ["contactPerson"] = s => s.ContactPerson!,
                ["createdAt"] = s => s.CreatedAt,
                ["isActive"] = s => s.IsActive
            };
            orderedQuery = SortingHelper.ApplySorting(orderedQuery, request.SortBy, request.SortOrder, sortableFields);

            var total = await orderedQuery.CountAsync();
            _logger.LogDebug("供应商总数: {Total}", total);

            var items = await orderedQuery
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync();

            var supplierDtos = _mapper.Map<List<SupplierDto>>(items);
            await _tagBindingRepository.GetQueryable().ApplyTagsAsync(
                TagScope.Supplier,
                supplierDtos,
                dto => dto.Id,
                (dto, tags) => dto.Tags = tags);

            _logger.LogInformation("查询供应商分页列表成功: Page={Page}, PageSize={PageSize}, Total={Total}, 返回数量={Count}",
                request.Page, request.PageSize, total, supplierDtos.Count);

            return new PageResponse<SupplierDto>
            {
                Items = supplierDtos,
                Page = request.Page,
                PageSize = request.PageSize,
                Total = total
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "查询供应商分页列表失败: Page={Page}, PageSize={PageSize}",
                request.Page, request.PageSize);
            throw;
        }
    }

    public async Task<SupplierDto> GetByIdAsync(long id)
    {
        _logger.LogDebug("SupplierService.GetByIdAsync - 开始查询供应商详情: Id={Id}", id);

        try
        {
            var supplier = await _supplierRepository.GetByIdAsync(id);

            if (supplier == null)
            {
                _logger.LogWarning("查询供应商详情失败，供应商不存在: Id={Id}", id);
                throw new NotFoundException("供应商不存在");
            }

            // 检查访问权限
            EnsureCanAccess(supplier);

            _logger.LogInformation("查询供应商详情成功: Id={Id}, 名称={Name}", id, supplier.Name);

            var supplierDto = _mapper.Map<SupplierDto>(supplier);
            await _tagBindingRepository.GetQueryable().ApplyTagAsync(
                TagScope.Supplier,
                supplierDto,
                dto => dto.Id,
                (dto, tags) => dto.Tags = tags);

            return supplierDto;
        }
        catch (NotFoundException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "查询供应商详情失败: Id={Id}", id);
            throw;
        }
    }

    public async Task<SupplierDto> CreateAsync(CreateSupplierRequest request)
    {
        _logger.LogDebug("SupplierService.CreateAsync - 开始创建供应商: 名称={Name}, 简称={ShortName}, 联系人={ContactPerson}, 电话={ContactPhone}",
            request.Name, request.ShortName, request.ContactPerson, request.ContactPhone);

        try
        {
            var supplier = new Supplier
            {
                Name = request.Name,
                ShortName = request.ShortName,
                ContactPerson = request.ContactPerson,
                ContactPhone = request.ContactPhone,
                ContactEmail = request.ContactEmail,
                Address = request.Address,
                TaxNumber = request.TaxNumber,
                BankAccount = request.BankAccount,
                BankName = request.BankName,
                Description = request.Description,
                IsActive = true
            };

            await _supplierRepository.AddAsync(supplier);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogDebug("供应商实体已保存到数据库: Id={Id}", supplier.Id);

            var newDto = _mapper.Map<SupplierDto>(supplier);
            await _auditLogService.LogAsync("Create", "Supplier", supplier.Id, null, SerializeForAudit(newDto));
            _logger.LogInformation("创建供应商成功: Id={Id}, 名称={Name}, 联系人={ContactPerson}, 电话={ContactPhone}",
                supplier.Id, supplier.Name, supplier.ContactPerson, supplier.ContactPhone);

            return newDto;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建供应商失败: 名称={Name}, 简称={ShortName}",
                request.Name, request.ShortName);
            throw;
        }
    }

    public async Task<SupplierDto> UpdateAsync(long id, UpdateSupplierRequest request)
    {
        _logger.LogDebug("SupplierService.UpdateAsync - 开始更新供应商: Id={Id}, 名称={Name}", id, request.Name);

        try
        {
            var supplier = await _supplierRepository.GetByIdAsync(id);

            if (supplier == null)
            {
                _logger.LogWarning("更新供应商失败，供应商不存在: Id={Id}", id);
                throw new NotFoundException("供应商不存在");
            }

            // 检查编辑权限
            EnsureCanEdit(supplier);

            var oldDto = _mapper.Map<SupplierDto>(supplier);

            _logger.LogDebug("更新供应商字段: Id={Id}, 原名称={OldName}, 新名称={NewName}, 原状态={OldIsActive}, 新状态={NewIsActive}",
                id, supplier.Name, request.Name, supplier.IsActive, request.IsActive);

            supplier.Name = request.Name;
            supplier.ShortName = request.ShortName;
            supplier.ContactPerson = request.ContactPerson;
            supplier.ContactPhone = request.ContactPhone;
            supplier.ContactEmail = request.ContactEmail;
            supplier.Address = request.Address;
            supplier.TaxNumber = request.TaxNumber;
            supplier.BankAccount = request.BankAccount;
            supplier.BankName = request.BankName;
            supplier.Description = request.Description;
            supplier.IsActive = request.IsActive;

            _supplierRepository.Update(supplier);
            await _unitOfWork.SaveChangesAsync();

            var newDto = _mapper.Map<SupplierDto>(supplier);
            await _auditLogService.LogAsync("Update", "Supplier", supplier.Id, SerializeForAudit(oldDto), SerializeForAudit(newDto));
            _logger.LogInformation("更新供应商成功: Id={Id}, 名称={Name}, 状态={IsActive}",
                supplier.Id, supplier.Name, supplier.IsActive);

            return newDto;
        }
        catch (NotFoundException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新供应商失败: Id={Id}, 名称={Name}", id, request.Name);
            throw;
        }
    }

    public async Task DeleteAsync(long id)
    {
        _logger.LogDebug("SupplierService.DeleteAsync - 开始删除供应商: Id={Id}", id);

        try
        {
            var supplier = await _supplierRepository.GetByIdAsync(id);

            if (supplier == null)
            {
                _logger.LogWarning("删除供应商失败，供应商不存在: Id={Id}", id);
                throw new NotFoundException("供应商不存在");
            }

            // 检查删除权限
            EnsureCanDelete(supplier);

            var oldDto = _mapper.Map<SupplierDto>(supplier);

            _logger.LogDebug("准备删除供应商: Id={Id}, 名称={Name}", id, supplier.Name);

            if (await _referenceGuard.HasSupplierReferencesAsync(id))
            {
                if (!supplier.IsActive)
                {
                    _logger.LogInformation("供应商已停用且仅保留历史引用，跳过删除: Id={Id}, Name={Name}", id, supplier.Name);
                    return;
                }

                supplier.IsActive = false;
                _supplierRepository.Update(supplier);
                await _unitOfWork.SaveChangesAsync();

                await _auditLogService.LogAsync("Archive", "Supplier", supplier.Id, SerializeForAudit(oldDto), SerializeForAudit(_mapper.Map<SupplierDto>(supplier)));
                _logger.LogInformation("供应商存在历史引用，删除改为停用: Id={Id}, Name={Name}", id, supplier.Name);
                return;
            }

            _supplierRepository.Delete(supplier);
            await _unitOfWork.SaveChangesAsync();

            await _auditLogService.LogAsync("Delete", "Supplier", supplier.Id, SerializeForAudit(oldDto), null);
            _logger.LogInformation("删除供应商成功: Id={Id}, 名称={Name}", id, supplier.Name);
        }
        catch (NotFoundException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除供应商失败: Id={Id}", id);
            throw;
        }
    }

    public async Task<List<SupplierDto>> GetActiveSuppliersAsync()
    {
        _logger.LogDebug("SupplierService.GetActiveSuppliersAsync - 开始查询活跃供应商列表");

        try
        {
            var suppliers = await _supplierRepository.GetQueryable()
                .Where(s => s.IsActive)
                .OrderBy(s => s.Name)
                .ToListAsync();

            _logger.LogInformation("查询活跃供应商列表成功: 数量={Count}", suppliers.Count);

            return _mapper.Map<List<SupplierDto>>(suppliers);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "查询活跃供应商列表失败");
            throw;
        }
    }

    public async Task<SupplierStatisticsDto> GetStatisticsAsync()
    {
        _logger.LogDebug("SupplierService.GetStatisticsAsync");

        var query = ApplyPermissionFilter(_supplierRepository.GetQueryable());
        var suppliers = await query.ToListAsync();

        var now = DateTime.UtcNow;
        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        return new SupplierStatisticsDto
        {
            TotalCount = suppliers.Count,
            ActiveCount = suppliers.Count(s => s.IsActive),
            InactiveCount = suppliers.Count(s => !s.IsActive),
            ThisMonthNewCount = suppliers.Count(s => s.CreatedAt >= monthStart)
        };
    }

    public async Task<SupplierStatisticsDto> GetStatisticsAsync(PageRequest request)
    {
        _logger.LogDebug("SupplierService.GetStatisticsAsync with filters");

        var query = ApplyPermissionFilter(_supplierRepository.GetQueryable());

        // 标签过滤
        if (request.TagFilters != null && request.TagFilters.Count > 0)
        {
            var tagBindings = _tagBindingRepository.GetQueryable();
            query = query.ApplyOwnerTagFilters(tagBindings, request.TagFilters, TagScope.Supplier);
        }

        if (!string.IsNullOrWhiteSpace(request.Name))
            query = query.Where(s => s.Name.Contains(request.Name));

        if (!string.IsNullOrWhiteSpace(request.ContactPerson))
            query = query.Where(s => s.ContactPerson != null && s.ContactPerson.Contains(request.ContactPerson));

        if (!string.IsNullOrWhiteSpace(request.ContactPhone))
            query = query.Where(s => s.ContactPhone != null && s.ContactPhone.Contains(request.ContactPhone));

        var suppliers = await query.ToListAsync();

        var now = DateTime.UtcNow;
        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        return new SupplierStatisticsDto
        {
            TotalCount = suppliers.Count,
            ActiveCount = suppliers.Count(s => s.IsActive),
            InactiveCount = suppliers.Count(s => !s.IsActive),
            ThisMonthNewCount = suppliers.Count(s => s.CreatedAt >= monthStart)
        };
    }

    public async Task<BatchCreateResponse<SupplierDto>> BatchCreateAsync(List<CreateSupplierRequest> items)
    {
        _logger.LogDebug("SupplierService.BatchCreateAsync - 开始批量创建供应商: 总数={TotalCount}", items.Count);

        try
        {
            var response = new BatchCreateResponse<SupplierDto>
            {
                TotalCount = items.Count
            };

            for (int i = 0; i < items.Count; i++)
            {
                try
                {
                    _logger.LogDebug("处理第 {Index}/{Total} 个供应商, 名称={Name}",
                        i + 1, items.Count, items[i].Name);

                    var result = await CreateAsync(items[i]);
                    response.SuccessItems.Add(result);
                    response.SuccessCount++;

                    _logger.LogDebug("第 {Index}/{Total} 个供应商创建成功: Id={Id}, 名称={Name}",
                        i + 1, items.Count, result.Id, result.Name);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "第 {Index}/{Total} 个供应商创建失败: 名称={Name}, 错误={Error}",
                        i + 1, items.Count, items[i].Name, ex.Message);
                    response.Errors.Add(new BatchError { Index = i, Message = ex.Message });
                    response.FailedCount++;
                }
            }

            _logger.LogInformation("批量创建供应商完成: 总数={TotalCount}, 成功={SuccessCount}, 失败={FailedCount}",
                response.TotalCount, response.SuccessCount, response.FailedCount);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "批量创建供应商过程发生异常: 总数={TotalCount}", items.Count);
            throw;
        }
    }

    public async Task<SupplierFinanceSummaryDto> GetFinanceSummaryAsync(long supplierId)
    {
        _logger.LogDebug("SupplierService.GetFinanceSummaryAsync - SupplierId={SupplierId}", supplierId);

        var supplier = await _supplierRepository.GetByIdAsync(supplierId);
        if (supplier == null)
        {
            _logger.LogWarning("供应商不存在: Id={Id}", supplierId);
            throw new NotFoundException("供应商不存在");
        }

        var today = DateTime.UtcNow.Date;

        // Receivable aggregation
        var receivableQuery = _receivableRepository.GetQueryable()
            .Where(r => r.SupplierId == supplierId);

        var receivableSummary = await receivableQuery
            .GroupBy(_ => 1)
            .Select(g => new
            {
                TotalReceivable = g.Sum(r => r.TotalAmount),
                TotalReceived = g.Sum(r => r.ReceivedAmount),
                ReceivableRemaining = g.Sum(r => r.RemainingAmount),
                OverdueCount = g.Count(r => r.Status != ReceivableStatus.Settled && r.DueDate.HasValue && r.DueDate.Value < today),
                OverdueAmount = g.Where(r => r.Status != ReceivableStatus.Settled && r.DueDate.HasValue && r.DueDate.Value < today).Sum(r => r.RemainingAmount),
            })
            .FirstOrDefaultAsync();

        // Payable aggregation
        var payableQuery = _payableRepository.GetQueryable()
            .Where(p => p.SupplierId == supplierId);

        var payableSummary = await payableQuery
            .GroupBy(_ => 1)
            .Select(g => new
            {
                TotalPayable = g.Sum(p => p.TotalAmount),
                TotalPaid = g.Sum(p => p.PaidAmount),
                PayableRemaining = g.Sum(p => p.RemainingAmount),
                OverdueCount = g.Count(p => p.Status != PayableStatus.Settled && p.DueDate.HasValue && p.DueDate.Value < today),
                OverdueAmount = g.Where(p => p.Status != PayableStatus.Settled && p.DueDate.HasValue && p.DueDate.Value < today).Sum(p => p.RemainingAmount),
            })
            .FirstOrDefaultAsync();

        // Merge project counts from both receivables and payables
        var receivableProjectIds = await receivableQuery
            .Where(r => r.ProjectId != 0)
            .Select(r => r.ProjectId)
            .Distinct()
            .ToListAsync();

        var payableProjectIds = await payableQuery
            .Where(p => p.ProjectId.HasValue)
            .Select(p => p.ProjectId!.Value)
            .Distinct()
            .ToListAsync();

        var projectCount = receivableProjectIds
            .Union(payableProjectIds)
            .Distinct()
            .Count();

        var result = new SupplierFinanceSummaryDto
        {
            TotalReceivable = receivableSummary?.TotalReceivable ?? 0,
            TotalReceived = receivableSummary?.TotalReceived ?? 0,
            ReceivableRemaining = receivableSummary?.ReceivableRemaining ?? 0,
            ReceivableOverdueCount = receivableSummary?.OverdueCount ?? 0,
            ReceivableOverdueAmount = receivableSummary?.OverdueAmount ?? 0,
            TotalPayable = payableSummary?.TotalPayable ?? 0,
            TotalPaid = payableSummary?.TotalPaid ?? 0,
            PayableRemaining = payableSummary?.PayableRemaining ?? 0,
            PayableOverdueCount = payableSummary?.OverdueCount ?? 0,
            PayableOverdueAmount = payableSummary?.OverdueAmount ?? 0,
            ProjectCount = projectCount
        };

        _logger.LogInformation("查询供应商财务汇总成功: SupplierId={SupplierId}, TotalReceivable={TotalReceivable}, ReceivableRemaining={ReceivableRemaining}, TotalPayable={TotalPayable}, PayableRemaining={PayableRemaining}",
            supplierId, result.TotalReceivable, result.ReceivableRemaining, result.TotalPayable, result.PayableRemaining);

        return result;
    }
}
