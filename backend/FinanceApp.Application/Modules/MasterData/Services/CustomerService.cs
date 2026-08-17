using MapsterMapper;
using FinanceApp.Application.Common;
using FinanceApp.Application.Extensions;
using FinanceApp.Application.Modules.Identity.Interfaces;
using FinanceApp.Application.Modules.MasterData.DTOs.Customer;
using FinanceApp.Application.Modules.MasterData.Interfaces;
using FinanceApp.Application.Services;
using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Enums;
using FinanceApp.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FinanceApp.Application.Modules.MasterData.Services;

public class CustomerService : ServiceBase, ICustomerService
{
    private readonly IRepository<Customer> _customerRepository;
    private readonly IRepository<Receivable> _receivableRepository;
    private readonly IRepository<Payable> _payableRepository;
    private readonly IRepository<TagBinding> _tagBindingRepository;
    private readonly IMasterDataReferenceGuard _referenceGuard;
    private readonly IMapper _mapper;
    private readonly ILogger<CustomerService> _logger;
    private readonly IAuditLogService _auditLogService;
    private readonly IUnitOfWork _unitOfWork;

    public CustomerService(
        IRepository<Customer> customerRepository,
        IRepository<Receivable> receivableRepository,
        IRepository<Payable> payableRepository,
        IRepository<TagBinding> tagBindingRepository,
        IMasterDataReferenceGuard referenceGuard,
        IMapper mapper,
        ILogger<CustomerService> logger,
        IAuditLogService auditLogService,
        ICurrentUserService currentUserService,
        IDataPermissionService permissionService,
        IUnitOfWork unitOfWork)
        : base(currentUserService, permissionService)
    {
        _customerRepository = customerRepository;
        _receivableRepository = receivableRepository;
        _payableRepository = payableRepository;
        _tagBindingRepository = tagBindingRepository;
        _referenceGuard = referenceGuard;
        _mapper = mapper;
        _logger = logger;
        _auditLogService = auditLogService;
        _unitOfWork = unitOfWork;
    }

    public async Task<PageResponse<CustomerDto>> GetPagedAsync(PageRequest request)
    {
        _logger.LogDebug("CustomerService.GetPagedAsync - Page={Page}, PageSize={PageSize}",
            request.Page, request.PageSize);

        try
        {
            var baseQuery = _customerRepository.GetQueryable();

            // 应用权限过滤
            var query = ApplyPermissionFilter(baseQuery);

            // 按名称筛选
            if (!string.IsNullOrWhiteSpace(request.Name))
            {
                query = query.Where(c => c.Name.Contains(request.Name));
            }

            // 按联系人筛选
            if (!string.IsNullOrWhiteSpace(request.ContactPerson))
            {
                query = query.Where(c => c.ContactPerson != null && c.ContactPerson.Contains(request.ContactPerson));
            }

            // 按联系电话筛选
            if (!string.IsNullOrWhiteSpace(request.ContactPhone))
            {
                query = query.Where(c => c.ContactPhone != null && c.ContactPhone.Contains(request.ContactPhone));
            }

            // 标签过滤
            if (request.TagFilters != null && request.TagFilters.Count > 0)
            {
                var tagBindings = _tagBindingRepository.GetQueryable();
                query = query.ApplyOwnerTagFilters(tagBindings, request.TagFilters, TagScope.Customer);
            }

            IQueryable<Customer> orderedQuery = query.OrderByDescending(c => c.CreatedAt);

            // 应用自定义排序
            var sortableFields = new Dictionary<string, System.Linq.Expressions.Expression<Func<Customer, object>>>(StringComparer.OrdinalIgnoreCase)
            {
                ["name"] = c => c.Name,
                ["contactPerson"] = c => c.ContactPerson!,
                ["createdAt"] = c => c.CreatedAt,
                ["isActive"] = c => c.IsActive
            };
            orderedQuery = SortingHelper.ApplySorting(orderedQuery, request.SortBy, request.SortOrder, sortableFields);

            var total = await orderedQuery.CountAsync();

            var items = await orderedQuery
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync();

            var customerDtos = _mapper.Map<List<CustomerDto>>(items);
            await _tagBindingRepository.GetQueryable().ApplyTagsAsync(
                TagScope.Customer,
                customerDtos,
                dto => dto.Id,
                (dto, tags) => dto.Tags = tags);

            _logger.LogInformation("查询客户分页列表成功: Total={Total}, Count={Count}",
                total, customerDtos.Count);

            return new PageResponse<CustomerDto>
            {
                Items = customerDtos,
                Page = request.Page,
                PageSize = request.PageSize,
                Total = total
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "查询客户分页列表失败");
            throw;
        }
    }

    public async Task<CustomerDto> GetByIdAsync(long id)
    {
        _logger.LogDebug("CustomerService.GetByIdAsync - Id={Id}", id);

        try
        {
            var customer = await _customerRepository.GetByIdAsync(id);

            if (customer == null)
            {
                _logger.LogWarning("客户不存在: Id={Id}", id);
                throw new NotFoundException("客户不存在");
            }

            // 检查访问权限
            EnsureCanAccess(customer);

            _logger.LogInformation("查询客户详情成功: Id={Id}, 名称={Name}", id, customer.Name);

            var customerDto = _mapper.Map<CustomerDto>(customer);
            await _tagBindingRepository.GetQueryable().ApplyTagAsync(
                TagScope.Customer,
                customerDto,
                dto => dto.Id,
                (dto, tags) => dto.Tags = tags);

            return customerDto;
        }
        catch (NotFoundException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "查询客户详情异常: Id={Id}", id);
            throw;
        }
    }

    public async Task<CustomerDto> CreateAsync(CreateCustomerRequest request)
    {
        _logger.LogDebug("CustomerService.CreateAsync - Name={Name}", request.Name);

        try
        {
            var customer = new Customer
            {
                Name = request.Name,
                ShortName = request.ShortName,
                ContactPerson = request.ContactPerson,
                ContactPhone = request.ContactPhone,
                ContactEmail = request.ContactEmail,
                Address = request.Address,
                TaxNumber = request.TaxNumber,
                Description = request.Description,
                IsActive = true
            };

            await _customerRepository.AddAsync(customer);
            await _unitOfWork.SaveChangesAsync();

            await _auditLogService.LogAsync("Create", "Customer", customer.Id, null, SerializeForAudit(_mapper.Map<CustomerDto>(customer)));
            _logger.LogInformation("创建客户成功: Id={Id}, 名称={Name}",
                customer.Id, customer.Name);

            return _mapper.Map<CustomerDto>(customer);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建客户失败: 名称={Name}", request.Name);
            throw;
        }
    }

    public async Task<CustomerDto> UpdateAsync(long id, UpdateCustomerRequest request)
    {
        _logger.LogDebug("CustomerService.UpdateAsync - Id={Id}, Name={Name}", id, request.Name);

        try
        {
            var customer = await _customerRepository.GetByIdAsync(id);

            if (customer == null)
            {
                _logger.LogWarning("客户不存在: Id={Id}", id);
                throw new NotFoundException("客户不存在");
            }

            // 检查编辑权限
            EnsureCanEdit(customer);

            var oldDto = _mapper.Map<CustomerDto>(customer);

            customer.Name = request.Name;
            customer.ShortName = request.ShortName;
            customer.ContactPerson = request.ContactPerson;
            customer.ContactPhone = request.ContactPhone;
            customer.ContactEmail = request.ContactEmail;
            customer.Address = request.Address;
            customer.TaxNumber = request.TaxNumber;
            customer.Description = request.Description;
            customer.IsActive = request.IsActive;

            _customerRepository.Update(customer);
            await _unitOfWork.SaveChangesAsync();

            var newDto = _mapper.Map<CustomerDto>(customer);
            await _auditLogService.LogAsync("Update", "Customer", customer.Id, SerializeForAudit(oldDto), SerializeForAudit(newDto));
            _logger.LogInformation("更新客户成功: Id={Id}, 名称={Name}, 状态={IsActive}",
                customer.Id, customer.Name, customer.IsActive);

            return newDto;
        }
        catch (NotFoundException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新客户异常: Id={Id}, 名称={Name}", id, request.Name);
            throw;
        }
    }

    public async Task DeleteAsync(long id)
    {
        _logger.LogDebug("CustomerService.DeleteAsync - Id={Id}", id);

        try
        {
            var customer = await _customerRepository.GetByIdAsync(id);

            if (customer == null)
            {
                _logger.LogWarning("客户不存在: Id={Id}", id);
                throw new NotFoundException("客户不存在");
            }

            // 检查删除权限
            EnsureCanDelete(customer);

            var oldDto = _mapper.Map<CustomerDto>(customer);

            if (await _referenceGuard.HasCustomerReferencesAsync(id))
            {
                if (!customer.IsActive)
                {
                    _logger.LogInformation("客户已停用且存在历史引用，保留历史数据: Id={Id}, Name={Name}", id, customer.Name);
                    return;
                }

                customer.IsActive = false;
                _customerRepository.Update(customer);
                await _unitOfWork.SaveChangesAsync();

                await _auditLogService.LogAsync("Archive", "Customer", customer.Id, SerializeForAudit(oldDto), SerializeForAudit(_mapper.Map<CustomerDto>(customer)));
                _logger.LogInformation("客户存在历史引用，已改为停用: Id={Id}, Name={Name}", id, customer.Name);
                return;
            }

            _customerRepository.Delete(customer);
            await _unitOfWork.SaveChangesAsync();

            await _auditLogService.LogAsync("Delete", "Customer", customer.Id, SerializeForAudit(oldDto), null);
            _logger.LogInformation("删除客户成功: Id={Id}, 名称={Name}", id, customer.Name);
        }
        catch (NotFoundException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除客户异常: Id={Id}", id);
            throw;
        }
    }

    public async Task<List<CustomerDto>> GetActiveCustomersAsync()
    {
        _logger.LogDebug("CustomerService.GetActiveCustomersAsync");

        try
        {
            var customers = await _customerRepository.GetQueryable()
                .Where(c => c.IsActive)
                .OrderBy(c => c.Name)
                .ToListAsync();

            _logger.LogInformation("查询活跃客户列表成功: 数量={Count}", customers.Count);

            return _mapper.Map<List<CustomerDto>>(customers);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "查询活跃客户列表失败");
            throw;
        }
    }

    public async Task<CustomerStatisticsDto> GetStatisticsAsync()
    {
        _logger.LogDebug("CustomerService.GetStatisticsAsync");

        var query = ApplyPermissionFilter(_customerRepository.GetQueryable());
        var customers = await query.ToListAsync();

        var now = DateTime.UtcNow;
        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        return new CustomerStatisticsDto
        {
            TotalCount = customers.Count,
            ActiveCount = customers.Count(c => c.IsActive),
            InactiveCount = customers.Count(c => !c.IsActive),
            ThisMonthNewCount = customers.Count(c => c.CreatedAt >= monthStart)
        };
    }

    public async Task<CustomerStatisticsDto> GetStatisticsAsync(PageRequest request)
    {
        _logger.LogDebug("CustomerService.GetStatisticsAsync with filters");

        var query = ApplyPermissionFilter(_customerRepository.GetQueryable());

        // 标签过滤
        if (request.TagFilters != null && request.TagFilters.Count > 0)
        {
            var tagBindings = _tagBindingRepository.GetQueryable();
            query = query.ApplyOwnerTagFilters(tagBindings, request.TagFilters, TagScope.Customer);
        }

        if (!string.IsNullOrWhiteSpace(request.Name))
            query = query.Where(c => c.Name.Contains(request.Name));

        if (!string.IsNullOrWhiteSpace(request.ContactPerson))
            query = query.Where(c => c.ContactPerson != null && c.ContactPerson.Contains(request.ContactPerson));

        if (!string.IsNullOrWhiteSpace(request.ContactPhone))
            query = query.Where(c => c.ContactPhone != null && c.ContactPhone.Contains(request.ContactPhone));

        var customers = await query.ToListAsync();

        var now = DateTime.UtcNow;
        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        return new CustomerStatisticsDto
        {
            TotalCount = customers.Count,
            ActiveCount = customers.Count(c => c.IsActive),
            InactiveCount = customers.Count(c => !c.IsActive),
            ThisMonthNewCount = customers.Count(c => c.CreatedAt >= monthStart)
        };
    }

    public async Task<BatchCreateResponse<CustomerDto>> BatchCreateAsync(List<CreateCustomerRequest> items)
    {
        _logger.LogDebug("CustomerService.BatchCreateAsync - 总数={TotalCount}", items.Count);

        var response = new BatchCreateResponse<CustomerDto>
        {
            TotalCount = items.Count
        };

        for (int i = 0; i < items.Count; i++)
        {
            try
            {
                var result = await CreateSingleCustomerAsync(items[i]);
                response.SuccessItems.Add(result);
                response.SuccessCount++;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "批量创建第 {Index} 个客户失败: 名称={Name}",
                    i + 1, items[i].Name);
                response.Errors.Add(new BatchError { Index = i, Message = ex.Message });
                response.FailedCount++;
            }
        }

        _logger.LogInformation("批量创建客户完成: 总数={TotalCount}, 成功={SuccessCount}, 失败={FailedCount}",
            response.TotalCount, response.SuccessCount, response.FailedCount);

        return response;
    }

    public async Task<CustomerFinanceSummaryDto> GetFinanceSummaryAsync(long customerId)
    {
        _logger.LogDebug("CustomerService.GetFinanceSummaryAsync - CustomerId={CustomerId}", customerId);

        var customer = await _customerRepository.GetByIdAsync(customerId);
        if (customer == null)
        {
            _logger.LogWarning("客户不存在: Id={Id}", customerId);
            throw new NotFoundException("客户不存在");
        }

        var today = DateTime.UtcNow.Date;

        // Receivable aggregation
        var receivableQuery = _receivableRepository.GetQueryable()
            .Where(r => r.CustomerId == customerId);

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
            .Where(p => p.CustomerId == customerId);

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

        var result = new CustomerFinanceSummaryDto
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

        _logger.LogInformation("查询客户财务汇总成功: CustomerId={CustomerId}, TotalReceivable={TotalReceivable}, ReceivableRemaining={ReceivableRemaining}, TotalPayable={TotalPayable}, PayableRemaining={PayableRemaining}",
            customerId, result.TotalReceivable, result.ReceivableRemaining, result.TotalPayable, result.PayableRemaining);

        return result;
    }

    private async Task<CustomerDto> CreateSingleCustomerAsync(CreateCustomerRequest request)
    {
        var customer = new Customer
        {
            Name = request.Name,
            ShortName = request.ShortName,
            ContactPerson = request.ContactPerson,
            ContactPhone = request.ContactPhone,
            ContactEmail = request.ContactEmail,
            Address = request.Address,
            TaxNumber = request.TaxNumber,
            Description = request.Description,
            IsActive = true
        };

        await _customerRepository.AddAsync(customer);
        await _unitOfWork.SaveChangesAsync();

        var newDto = _mapper.Map<CustomerDto>(customer);
        await _auditLogService.LogAsync("Create", "Customer", customer.Id, null, SerializeForAudit(newDto));

        return newDto;
    }
}
