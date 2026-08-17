using MapsterMapper;
using FinanceApp.Application.Common;
using FinanceApp.Application.Extensions;
using FinanceApp.Application.Modules.Identity.Interfaces;
using FinanceApp.Application.Modules.MasterData.DTOs.Person;
using FinanceApp.Application.Modules.MasterData.Interfaces;
using FinanceApp.Application.Services;
using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Enums;
using FinanceApp.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FinanceApp.Application.Modules.MasterData.Services;

public class PersonService : ServiceBase, IPersonService
{
    private readonly IRepository<Person> _personRepository;
    private readonly IRepository<Transaction> _transactionRepository;
    private readonly IRepository<TransactionAllocation> _allocationRepository;
    private readonly IRepository<Payable> _payableRepository;
    private readonly IRepository<Receivable> _receivableRepository;
    private readonly IRepository<TagBinding> _tagBindingRepository;
    private readonly IMasterDataReferenceGuard _referenceGuard;
    private readonly IMapper _mapper;
    private readonly ILogger<PersonService> _logger;
    private readonly IAuditLogService _auditLogService;
    private readonly IUnitOfWork _unitOfWork;

    public PersonService(
        IRepository<Person> personRepository,
        IRepository<Transaction> transactionRepository,
        IRepository<TransactionAllocation> allocationRepository,
        IRepository<Payable> payableRepository,
        IRepository<Receivable> receivableRepository,
        IRepository<TagBinding> tagBindingRepository,
        IMasterDataReferenceGuard referenceGuard,
        IMapper mapper,
        ILogger<PersonService> logger,
        IAuditLogService auditLogService,
        ICurrentUserService currentUserService,
        IDataPermissionService permissionService,
        IUnitOfWork unitOfWork)
        : base(currentUserService, permissionService)
    {
        _personRepository = personRepository;
        _transactionRepository = transactionRepository;
        _allocationRepository = allocationRepository;
        _payableRepository = payableRepository;
        _receivableRepository = receivableRepository;
        _tagBindingRepository = tagBindingRepository;
        _referenceGuard = referenceGuard;
        _mapper = mapper;
        _logger = logger;
        _auditLogService = auditLogService;
        _unitOfWork = unitOfWork;
    }

    public async Task<PageResponse<PersonDto>> GetPagedAsync(PageRequest request)
    {
        _logger.LogDebug("PersonService.GetPagedAsync - Page={Page}, PageSize={PageSize}",
            request.Page, request.PageSize);

        var baseQuery = _personRepository.GetQueryable();

        // 应用权限过滤
        var query = ApplyPermissionFilter(baseQuery);

        // 按姓名筛选
        if (!string.IsNullOrWhiteSpace(request.Name))
        {
            query = query.Where(p => p.Name.Contains(request.Name));
        }

        // 按人员类型筛选
        if (!string.IsNullOrWhiteSpace(request.PersonType))
        {
            if (Enum.TryParse<Domain.Enums.PersonType>(request.PersonType, true, out var personType))
            {
                query = query.Where(p => p.PersonType == personType);
            }
        }

        // 按电话筛选
        if (!string.IsNullOrWhiteSpace(request.Phone))
        {
            query = query.Where(p => p.Phone != null && p.Phone.Contains(request.Phone));
        }

        // 标签过滤
        if (request.TagFilters != null && request.TagFilters.Count > 0)
        {
            var tagBindings = _tagBindingRepository.GetQueryable();
            query = query.ApplyOwnerTagFilters(tagBindings, request.TagFilters, TagScope.Person);
        }

        IQueryable<Person> orderedQuery = query.OrderByDescending(p => p.CreatedAt);

        // 应用自定义排序
        var sortableFields = new Dictionary<string, System.Linq.Expressions.Expression<Func<Person, object>>>(StringComparer.OrdinalIgnoreCase)
        {
            ["name"] = p => p.Name,
            ["personType"] = p => p.PersonType,
            ["createdAt"] = p => p.CreatedAt,
            ["isActive"] = p => p.IsActive
        };
        orderedQuery = SortingHelper.ApplySorting(orderedQuery, request.SortBy, request.SortOrder, sortableFields);

        var total = await orderedQuery.CountAsync();
        _logger.LogDebug("查询到总记录数: Total={Total}", total);

        var items = await orderedQuery
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync();

        var personDtos = _mapper.Map<List<PersonDto>>(items);
        await _tagBindingRepository.GetQueryable().ApplyTagsAsync(
            TagScope.Person,
            personDtos,
            dto => dto.Id,
            (dto, tags) => dto.Tags = tags);

        _logger.LogInformation("查询人员分页列表完成: Page={Page}, PageSize={PageSize}, Total={Total}, ReturnedCount={ReturnedCount}",
            request.Page, request.PageSize, total, personDtos.Count);

        return new PageResponse<PersonDto>
        {
            Items = personDtos,
            Page = request.Page,
            PageSize = request.PageSize,
            Total = total
        };
    }

    public async Task<PersonDto> GetByIdAsync(long id)
    {
        _logger.LogDebug("PersonService.GetByIdAsync - 开始查询人员详情: Id={Id}", id);

        var person = await _personRepository.GetQueryable()
            .FirstOrDefaultAsync(p => p.Id == id);

        if (person == null)
        {
            _logger.LogWarning("查询人员详情失败，人员不存在: Id={Id}", id);
            throw new NotFoundException("人员不存在");
        }

        // 检查访问权限
        EnsureCanAccess(person);

        _logger.LogDebug("查询人员详情成功: Id={Id}, 姓名={Name}, 类型={PersonType}",
            id, person.Name, person.PersonType);

        var personDto = _mapper.Map<PersonDto>(person);
        await _tagBindingRepository.GetQueryable().ApplyTagAsync(
            TagScope.Person,
            personDto,
            dto => dto.Id,
            (dto, tags) => dto.Tags = tags);

        return personDto;
    }

    public async Task<PersonDto> CreateAsync(CreatePersonRequest request)
    {
        _logger.LogDebug("PersonService.CreateAsync - 开始创建人员: 姓名={Name}, 类型={PersonType}, 电话={Phone}",
            request.Name, request.PersonType, request.Phone);

        // Validate person type
        if (!Enum.TryParse<PersonType>(request.PersonType, true, out var personType))
        {
            _logger.LogWarning("人员类型验证失败: PersonType={PersonType}", request.PersonType);
            throw new ValidationException("无效的人员类型");
        }

        _logger.LogDebug("人员类型验证通过: PersonType={PersonType}", personType);

        var person = new Person
        {
            Name = request.Name,
            PersonType = personType,
            IdNumber = request.IdNumber,
            Phone = request.Phone,
            Email = request.Email,
            BankAccount = request.BankAccount,
            BankName = request.BankName,
            JoinDate = request.JoinDate,
            IsActive = true
        };

        await _personRepository.AddAsync(person);
        await _unitOfWork.SaveChangesAsync();

        var dto = await GetByIdAsync(person.Id);
        await _auditLogService.LogAsync("Create", "Person", person.Id, null, SerializeForAudit(dto));
        _logger.LogInformation("创建人员成功: Id={Id}, 姓名={Name}, 类型={PersonType}, 电话={Phone}",
            person.Id, person.Name, person.PersonType, person.Phone);

        return dto;
    }

    public async Task<PersonDto> UpdateAsync(long id, UpdatePersonRequest request)
    {
        _logger.LogDebug("PersonService.UpdateAsync - 开始更新人员: Id={Id}, 姓名={Name}", id, request.Name);

        var person = await _personRepository.GetQueryable()
            .FirstOrDefaultAsync(p => p.Id == id);

        if (person == null)
        {
            _logger.LogWarning("更新人员失败，人员不存在: Id={Id}", id);
            throw new NotFoundException("人员不存在");
        }

        // 检查编辑权限
        EnsureCanEdit(person);

        _logger.LogDebug("找到待更新人员: Id={Id}, 原姓名={OldName}, 新姓名={NewName}",
            id, person.Name, request.Name);

        var oldDto = _mapper.Map<PersonDto>(person);

        // Validate person type
        if (!Enum.TryParse<PersonType>(request.PersonType, true, out var personType))
        {
            _logger.LogWarning("人员类型验证失败: PersonType={PersonType}", request.PersonType);
            throw new ValidationException("无效的人员类型");
        }

        person.Name = request.Name;
        person.PersonType = personType;
        person.IdNumber = request.IdNumber;
        person.Phone = request.Phone;
        person.Email = request.Email;
        person.BankAccount = request.BankAccount;
        person.BankName = request.BankName;
        person.JoinDate = request.JoinDate;
        person.LeaveDate = request.LeaveDate;
        person.IsActive = request.IsActive;

        _personRepository.Update(person);
        await _unitOfWork.SaveChangesAsync();

        await _auditLogService.LogAsync("Update", "Person", person.Id, SerializeForAudit(oldDto), SerializeForAudit(_mapper.Map<PersonDto>(person)));
        _logger.LogInformation("更新人员成功: Id={Id}, 姓名={Name}, 类型={PersonType}, 状态={IsActive}",
            person.Id, person.Name, person.PersonType, person.IsActive);

        return await GetByIdAsync(person.Id);
    }

    public async Task DeleteAsync(long id)
    {
        _logger.LogDebug("PersonService.DeleteAsync - 开始删除人员: Id={Id}", id);

        var person = await _personRepository.GetQueryable()
            .FirstOrDefaultAsync(p => p.Id == id);

        if (person == null)
        {
            _logger.LogWarning("删除人员失败，人员不存在: Id={Id}", id);
            throw new NotFoundException("人员不存在");
        }

        EnsureCanDelete(person);

        _logger.LogDebug("准备删除人员: Id={Id}, 姓名={Name}, 类型={PersonType}",
            id, person.Name, person.PersonType);

        var oldDto = _mapper.Map<PersonDto>(person);

        if (await _referenceGuard.HasPersonReferencesAsync(id))
        {
            if (!person.IsActive)
            {
                _logger.LogInformation("人员已停用且仅保留历史引用，跳过删除: Id={Id}, Name={Name}", id, person.Name);
                return;
            }

            person.IsActive = false;
            _personRepository.Update(person);
            await _unitOfWork.SaveChangesAsync();

            await _auditLogService.LogAsync("Archive", "Person", person.Id, SerializeForAudit(oldDto), SerializeForAudit(_mapper.Map<PersonDto>(person)));
            _logger.LogInformation("人员存在历史引用，删除改为停用: Id={Id}, Name={Name}", id, person.Name);
            return;
        }

        _personRepository.Delete(person);
        await _unitOfWork.SaveChangesAsync();

        await _auditLogService.LogAsync("Delete", "Person", person.Id, SerializeForAudit(oldDto), null);
        _logger.LogInformation("删除人员成功: Id={Id}, 姓名={Name}", id, person.Name);
    }

    public async Task<PersonCostSummaryDto> GetPersonCostSummaryAsync(long personId)
    {
        _logger.LogDebug("PersonService.GetPersonCostSummaryAsync - 开始查询人员成本汇总: PersonId={PersonId}", personId);

        var person = await ApplyPermissionFilter(_personRepository.GetQueryable())
            .FirstOrDefaultAsync(p => p.Id == personId);

        if (person == null)
        {
            _logger.LogWarning("查询人员成本汇总失败，人员不存在: PersonId={PersonId}", personId);
            throw new NotFoundException("人员不存在");
        }

        _logger.LogDebug("找到人员: PersonId={PersonId}, 姓名={Name}, 类型={PersonType}",
            personId, person.Name, person.PersonType);

        // Get direct transactions (where PersonId is set directly)
        _logger.LogDebug("开始查询直接交易: PersonId={PersonId}", personId);
        var directTransactions = await ApplyPermissionFilter(_transactionRepository.GetQueryable())
            .Where(t => t.PersonId == personId &&
                       !t.IsAllocated &&
                       t.TransactionType == TransactionType.Expense)
            .ToListAsync();

        var directCost = directTransactions.Sum(t => t.Amount);
        var directCount = directTransactions.Count;
        _logger.LogDebug("直接交易查询完成: PersonId={PersonId}, 直接成本={DirectCost}, 交易数={DirectCount}",
            personId, directCost, directCount);

        // Get allocated transactions (from transaction_allocations)
        _logger.LogDebug("开始查询分摊交易: PersonId={PersonId}", personId);
        var allocations = await ApplyPermissionFilter(_allocationRepository.GetQueryable())
            .Include(a => a.Transaction)
            .Where(a => a.PersonId == personId &&
                       a.Transaction.TransactionType == TransactionType.Expense)
            .ToListAsync();

        var allocatedCost = allocations.Sum(a => a.Amount);
        var allocatedCount = allocations.Count;
        _logger.LogDebug("分摊交易查询完成: PersonId={PersonId}, 分摊成本={AllocatedCost}, 分摊数={AllocatedCount}",
            personId, allocatedCost, allocatedCount);

        // Get date range
        var allTransactionDates = directTransactions
            .Select(t => t.TransactionDate)
            .Concat(allocations.Select(a => a.Transaction.TransactionDate))
            .OrderBy(d => d)
            .ToList();

        var firstDate = allTransactionDates.FirstOrDefault();
        var lastDate = allTransactionDates.LastOrDefault();
        var totalCost = directCost + allocatedCost;
        var totalCount = directCount + allocatedCount;

        _logger.LogInformation("查询人员成本汇总完成: PersonId={PersonId}, 姓名={Name}, 直接成本={DirectCost}, 分摊成本={AllocatedCost}, 总成本={TotalCost}, 交易数={TransactionCount}, 日期范围={FirstDate}~{LastDate}",
            personId, person.Name, directCost, allocatedCost, totalCost, totalCount, firstDate, lastDate);

        return new PersonCostSummaryDto
        {
            PersonId = personId,
            PersonName = person.Name,
            DirectCost = directCost,
            AllocatedCost = allocatedCost,
            TotalCost = totalCost,
            TransactionCount = totalCount,
            FirstTransactionDate = firstDate,
            LastTransactionDate = lastDate
        };
    }

    public async Task<BatchCreateResponse<PersonDto>> BatchCreateAsync(List<CreatePersonRequest> items)
    {
        _logger.LogDebug("PersonService.BatchCreateAsync - 开始批量创建人员: 总数={TotalCount}", items.Count);

        var response = new BatchCreateResponse<PersonDto>
        {
            TotalCount = items.Count
        };

        for (int i = 0; i < items.Count; i++)
        {
            try
            {
                _logger.LogDebug("处理第 {Index} 条记录: 姓名={Name}, 类型={PersonType}",
                    i + 1, items[i].Name, items[i].PersonType);

                var result = await CreateSinglePersonAsync(items[i]);
                response.SuccessItems.Add(result);
                response.SuccessCount++;

                _logger.LogDebug("第 {Index} 条记录创建成功: Id={Id}, 姓名={Name}",
                    i + 1, result.Id, result.Name);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "批量创建人员失败: 索引={Index}, 姓名={Name}, 错误={Error}",
                    i, items[i].Name, ex.Message);
                response.Errors.Add(new BatchError { Index = i, Message = ex.Message });
                response.FailedCount++;
            }
        }

        _logger.LogInformation("批量创建人员完成: 总数={TotalCount}, 成功={SuccessCount}, 失败={FailedCount}",
            response.TotalCount, response.SuccessCount, response.FailedCount);

        return response;
    }

    public async Task<List<PersonDto>> GetActivePersonsAsync()
    {
        _logger.LogDebug("PersonService.GetActivePersonsAsync");

        var persons = await _personRepository.GetQueryable()
            .Where(p => p.IsActive)
            .OrderBy(p => p.Name)
            .ToListAsync();

        _logger.LogInformation("查询活跃人员列表成功: 数量={Count}", persons.Count);

        return _mapper.Map<List<PersonDto>>(persons);
    }

    public async Task<PersonStatisticsDto> GetStatisticsAsync()
    {
        _logger.LogDebug("PersonService.GetStatisticsAsync");

        var query = ApplyPermissionFilter(_personRepository.GetQueryable());
        var persons = await query.ToListAsync();

        var now = DateTime.UtcNow;
        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        return new PersonStatisticsDto
        {
            TotalCount = persons.Count,
            ActiveCount = persons.Count(p => p.IsActive),
            InactiveCount = persons.Count(p => !p.IsActive),
            ThisMonthNewCount = persons.Count(p => p.CreatedAt >= monthStart)
        };
    }

    public async Task<PersonStatisticsDto> GetStatisticsAsync(PageRequest request)
    {
        _logger.LogDebug("PersonService.GetStatisticsAsync with filters");

        var query = ApplyPermissionFilter(_personRepository.GetQueryable());

        // 标签过滤
        if (request.TagFilters != null && request.TagFilters.Count > 0)
        {
            var tagBindings = _tagBindingRepository.GetQueryable();
            query = query.ApplyOwnerTagFilters(tagBindings, request.TagFilters, TagScope.Person);
        }

        if (!string.IsNullOrWhiteSpace(request.Name))
            query = query.Where(p => p.Name.Contains(request.Name));

        if (!string.IsNullOrWhiteSpace(request.PersonType))
        {
            if (Enum.TryParse<PersonType>(request.PersonType, true, out var personType))
                query = query.Where(p => p.PersonType == personType);
        }

        if (!string.IsNullOrWhiteSpace(request.Phone))
            query = query.Where(p => p.Phone != null && p.Phone.Contains(request.Phone));

        var persons = await query.ToListAsync();

        var now = DateTime.UtcNow;
        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        return new PersonStatisticsDto
        {
            TotalCount = persons.Count,
            ActiveCount = persons.Count(p => p.IsActive),
            InactiveCount = persons.Count(p => !p.IsActive),
            ThisMonthNewCount = persons.Count(p => p.CreatedAt >= monthStart)
        };
    }

    public async Task<PersonFinanceSummaryDto> GetFinanceSummaryAsync(long personId)
    {
        _logger.LogDebug("PersonService.GetFinanceSummaryAsync - PersonId={PersonId}", personId);

        var person = await _personRepository.GetQueryable()
            .FirstOrDefaultAsync(p => p.Id == personId);

        if (person == null)
        {
            _logger.LogWarning("人员不存在: PersonId={PersonId}", personId);
            throw new NotFoundException("人员不存在");
        }

        var today = DateTime.UtcNow.Date;

        // 直接交易成本
        var directTransactions = await _transactionRepository.GetQueryable()
            .Where(t => t.PersonId == personId &&
                       !t.IsAllocated &&
                       t.TransactionType == TransactionType.Expense)
            .ToListAsync();

        var directCost = directTransactions.Sum(t => t.Amount);

        // 分摊交易成本
        var allocations = await _allocationRepository.GetQueryable()
            .Include(a => a.Transaction)
            .Where(a => a.PersonId == personId &&
                       a.Transaction.TransactionType == TransactionType.Expense)
            .ToListAsync();

        var allocatedCost = allocations.Sum(a => a.Amount);
        var totalCount = directTransactions.Count + allocations.Count;

        // Receivable aggregation
        var receivableQuery = _receivableRepository.GetQueryable()
            .Where(r => r.PersonId == personId);

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
            .Where(p => p.PersonId == personId);

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

        // Merge project counts from transactions, receivables, and payables
        var transactionProjectIds = directTransactions
            .Where(t => t.ProjectId.HasValue)
            .Select(t => t.ProjectId!.Value)
            .Concat(allocations
                .Where(a => a.Transaction.ProjectId.HasValue)
                .Select(a => a.Transaction.ProjectId!.Value));

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

        var projectCount = transactionProjectIds
            .Union(receivableProjectIds)
            .Union(payableProjectIds)
            .Distinct()
            .Count();

        var result = new PersonFinanceSummaryDto
        {
            TotalCost = directCost + allocatedCost,
            DirectCost = directCost,
            AllocatedCost = allocatedCost,
            TransactionCount = totalCount,
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

        _logger.LogInformation("查询人员财务汇总成功: PersonId={PersonId}, TotalCost={TotalCost}, TotalReceivable={TotalReceivable}, TotalPayable={TotalPayable}",
            personId, result.TotalCost, result.TotalReceivable, result.TotalPayable);

        return result;
    }

    private async Task<PersonDto> CreateSinglePersonAsync(CreatePersonRequest request)
    {
        _logger.LogDebug("PersonService.CreateSinglePersonAsync - 开始创建单个人员: 姓名={Name}, 类型={PersonType}",
            request.Name, request.PersonType);

        // Validate person type
        if (!Enum.TryParse<PersonType>(request.PersonType, true, out var personType))
        {
            _logger.LogWarning("人员类型验证失败: PersonType={PersonType}", request.PersonType);
            throw new ValidationException("无效的人员类型");
        }

        var person = new Person
        {
            Name = request.Name,
            PersonType = personType,
            IdNumber = request.IdNumber,
            Phone = request.Phone,
            Email = request.Email,
            BankAccount = request.BankAccount,
            BankName = request.BankName,
            JoinDate = request.JoinDate,
            IsActive = true
        };

        await _personRepository.AddAsync(person);
        await _unitOfWork.SaveChangesAsync();

        await _auditLogService.LogAsync("Create", "Person", person.Id, null, SerializeForAudit(_mapper.Map<PersonDto>(person)));

        _logger.LogDebug("创建单个人员成功: Id={Id}, 姓名={Name}",
            person.Id, person.Name);

        return _mapper.Map<PersonDto>(person);
    }
}
