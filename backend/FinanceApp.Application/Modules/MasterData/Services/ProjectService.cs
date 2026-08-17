using MapsterMapper;
using FinanceApp.Application.Common;
using FinanceApp.Application.Extensions;
using FinanceApp.Application.Modules.Identity.Interfaces;
using FinanceApp.Application.Modules.MasterData.DTOs.Project;
using FinanceApp.Application.Modules.MasterData.Interfaces;
using FinanceApp.Application.Modules.FinanceSettlement.Interfaces;
using FinanceApp.Application.Modules.FinanceSettlement.DTOs.Receivable;
using FinanceApp.Application.Modules.Reporting.Interfaces;
using FinanceApp.Application.Services;
using FinanceApp.Domain.Constants;
using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Enums;
using FinanceApp.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FinanceApp.Application.Modules.MasterData.Services;

public class ProjectService : ServiceBase, IProjectService
{
    private readonly IRepository<Project> _projectRepository;
    private readonly IRepository<Customer> _customerRepository;
    private readonly IRepository<Transaction> _transactionRepository;
    private readonly IRepository<TransactionAllocation> _allocationRepository;
    private readonly IRepository<TagBinding> _tagBindingRepository;
    private readonly IRepository<Receivable> _receivableRepository;
    private readonly IProjectFinancialSummaryService _financialSummaryService;
    private readonly IMasterDataReferenceGuard _referenceGuard;
    private readonly IReceivableService _receivableService;
    private readonly IProjectFinancialRecalculationService _recalculationService;
    private readonly IMapper _mapper;
    private readonly ILogger<ProjectService> _logger;
    private readonly IAuditLogService _auditLogService;
    private readonly IUnitOfWork _unitOfWork;

    public ProjectService(
        IRepository<Project> projectRepository,
        IRepository<Customer> customerRepository,
        IRepository<Transaction> transactionRepository,
        IRepository<TransactionAllocation> allocationRepository,
        IRepository<TagBinding> tagBindingRepository,
        IRepository<Receivable> receivableRepository,
        IProjectFinancialSummaryService financialSummaryService,
        IMasterDataReferenceGuard referenceGuard,
        IReceivableService receivableService,
        IProjectFinancialRecalculationService recalculationService,
        IMapper mapper,
        ILogger<ProjectService> logger,
        IAuditLogService auditLogService,
        ICurrentUserService currentUserService,
        IDataPermissionService permissionService,
        IUnitOfWork unitOfWork)
        : base(currentUserService, permissionService)
    {
        _projectRepository = projectRepository;
        _customerRepository = customerRepository;
        _transactionRepository = transactionRepository;
        _allocationRepository = allocationRepository;
        _tagBindingRepository = tagBindingRepository;
        _receivableRepository = receivableRepository;
        _financialSummaryService = financialSummaryService;
        _referenceGuard = referenceGuard;
        _receivableService = receivableService;
        _recalculationService = recalculationService;
        _mapper = mapper;
        _logger = logger;
        _auditLogService = auditLogService;
        _unitOfWork = unitOfWork;
    }

    public async Task<string> GenerateProjectCodeAsync()
    {
        var year = DateTime.Now.Year;
        var prefix = $"PRJ-{year}-";

        _logger.LogDebug("ProjectService.GenerateProjectCodeAsync - 开始生成项目编号: Prefix={Prefix}", prefix);

        var existingCodes = await _projectRepository.GetQueryable()
            .Where(p => p.ProjectCode != null && p.ProjectCode.ToUpper().StartsWith(prefix))
            .Select(p => p.ProjectCode!)
            .ToListAsync();

        var maxSequence = 0;
        foreach (var existingCode in existingCodes)
        {
            if (TryParseGeneratedProjectCodeSequence(existingCode, prefix, out var sequence) && sequence > maxSequence)
            {
                maxSequence = sequence;
            }
        }

        var nextSequence = maxSequence + 1;
        string generatedCode;
        do
        {
            generatedCode = $"{prefix}{nextSequence:D3}";
            nextSequence++;
        }
        while (existingCodes.Any(code => string.Equals(code.Trim(), generatedCode, StringComparison.OrdinalIgnoreCase)));

        _logger.LogInformation("生成项目编号成功: Code={Code}", generatedCode);
        return generatedCode;
    }

    public async Task<PageResponse<ProjectDto>> GetPagedAsync(PageRequest request)
    {
        _logger.LogDebug("ProjectService.GetPagedAsync - 开始获取项目分页列表: Page={Page}, PageSize={PageSize}",
            request.Page, request.PageSize);

        try
        {
            var baseQuery = _projectRepository.GetQueryable()
                .Include(p => p.Customer);

            // 应用权限过滤
            var filtered = ApplyPermissionFilter(baseQuery);

            // 应用筛选条件
            if (!string.IsNullOrWhiteSpace(request.Name))
                filtered = filtered.Where(p => p.Name.Contains(request.Name));
            if (request.CustomerId.HasValue)
                filtered = filtered.Where(p => p.CustomerId == request.CustomerId.Value);
            if (!string.IsNullOrWhiteSpace(request.Status))
            {
                if (Enum.TryParse<Domain.Enums.ProjectStatus>(request.Status, true, out var status))
                    filtered = filtered.Where(p => p.Status == status);
            }

            // 标签过滤
            if (request.TagFilters != null && request.TagFilters.Count > 0)
            {
                var tagBindings = _tagBindingRepository.GetQueryable();
                filtered = filtered.ApplyOwnerTagFilters(tagBindings, request.TagFilters, TagScope.Project);
            }

            IQueryable<Project> query = filtered.OrderByDescending(p => p.CreatedAt);

            // 应用自定义排序
            var sortableFields = new Dictionary<string, System.Linq.Expressions.Expression<Func<Project, object>>>(StringComparer.OrdinalIgnoreCase)
            {
                ["name"] = p => p.Name,
                ["contractAmount"] = p => p.ContractAmount,
                ["profitRate"] = p => p.ProfitRate,
                ["status"] = p => p.Status,
                ["startDate"] = p => p.StartDate!,
                ["endDate"] = p => p.EndDate!,
                ["createdAt"] = p => p.CreatedAt
            };
            query = SortingHelper.ApplySorting(query, request.SortBy, request.SortOrder, sortableFields);

            var total = await query.CountAsync();
            _logger.LogDebug("项目总数: {Total}", total);

            var items = await query
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync();

            var projectDtos = _mapper.Map<List<ProjectDto>>(items);
            await _tagBindingRepository.GetQueryable().ApplyTagsAsync(
                TagScope.Project,
                projectDtos,
                dto => dto.Id,
                (dto, tags) => dto.Tags = tags);

            _logger.LogInformation("获取项目分页列表成功: 返回{Count}条记录, 总计{Total}条",
                projectDtos.Count, total);

            return new PageResponse<ProjectDto>
            {
                Items = projectDtos,
                Page = request.Page,
                PageSize = request.PageSize,
                Total = total
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取项目分页列表失败: Page={Page}, PageSize={PageSize}",
                request.Page, request.PageSize);
            throw;
        }
    }

    public async Task<ProjectDto> GetByIdAsync(long id)
    {
        _logger.LogDebug("ProjectService.GetByIdAsync - 开始获取项目详情: Id={Id}", id);

        try
        {
            var project = await _projectRepository.GetQueryable()
                .Include(p => p.Customer)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (project == null)
            {
                _logger.LogWarning("项目不存在: Id={Id}", id);
                throw new NotFoundException("项目不存在");
            }

            // 检查访问权限
            EnsureCanAccess(project);

            _logger.LogInformation("获取项目详情成功: Id={Id}, 名称={Name}", id, project.Name);
            var projectDto = _mapper.Map<ProjectDto>(project);
            var summary = await _financialSummaryService.GetProjectSummaryAsync(project.Id);

            projectDto.ReceivedAmount = summary.ReceivedAmount;
            projectDto.ReceivableAmount = summary.ReceivableAmount;
            projectDto.TotalCost = summary.TotalCost;
            projectDto.ProfitAmount = summary.ProfitAmount;
            projectDto.ProfitRate = summary.ProfitRate;
            await _tagBindingRepository.GetQueryable().ApplyTagAsync(
                TagScope.Project,
                projectDto,
                dto => dto.Id,
                (dto, tags) => dto.Tags = tags);

            return projectDto;
        }
        catch (NotFoundException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取项目详情失败: Id={Id}", id);
            throw;
        }
    }

    public async Task<ProjectDto> CreateAsync(CreateProjectRequest request)
    {
        _logger.LogDebug("ProjectService.CreateAsync - 开始创建项目: 名称={Name}, 项目编号={Code}, 客户ID={CustomerId}, 合同金额={Amount}",
            request.Name, request.ProjectCode, request.CustomerId, request.ContractAmount);

        try
        {
            var normalizedProjectCode = NormalizeProjectCode(request.ProjectCode);

            // Validate customer exists if provided
            if (request.CustomerId.HasValue)
            {
                var customer = await _customerRepository.GetByIdAsync(request.CustomerId.Value);
                if (customer == null)
                {
                    _logger.LogWarning("客户不存在: CustomerId={CustomerId}", request.CustomerId);
                    throw new NotFoundException("客户不存在");
                }

                _logger.LogDebug("客户验证通过: CustomerId={CustomerId}, 客户名称={CustomerName}",
                    request.CustomerId, customer.Name);
            }

            await EnsureProjectCodeUniqueAsync(normalizedProjectCode);

            // Validate status
            if (!Enum.TryParse<ProjectStatus>(request.Status, true, out var projectStatus))
            {
                _logger.LogWarning("项目状态验证失败: Status={Status}", request.Status);
                throw new ValidationException("无效的项目状态");
            }

            var project = new Project
            {
                Name = request.Name,
                ProjectCode = normalizedProjectCode,
                CustomerId = request.CustomerId,
                ContractAmount = request.ContractAmount,
                ReceivedAmount = 0,
                ReceivableAmount = request.ContractAmount,
                TotalCost = 0,
                ProfitAmount = 0,
                ProfitRate = 0,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                Status = projectStatus,
                Description = request.Description
            };

            _logger.LogDebug("初始化项目实体: 应收金额={ReceivableAmount}, 状态={Status}",
                project.ReceivableAmount, project.Status);

            // 使用事务保证项目创建和默认应收记录的原子性
            await using var dbTransaction = await _unitOfWork.BeginTransactionAsync();

            try
            {
                await _projectRepository.AddAsync(project);
                await _unitOfWork.SaveChangesAsync();

                // 直接创建默认应收实体，避免调用 ReceivableService 导致嵌套 SaveChanges
                _logger.LogDebug("自动创建项目默认应收记录: ProjectId={ProjectId}, 金额={Amount}",
                    project.Id, project.ContractAmount);

                var receivable = new Receivable
                {
                    ProjectId = project.Id,
                    CustomerId = project.CustomerId,
                    TotalAmount = project.ContractAmount,
                    ReceivedAmount = 0,
                    RemainingAmount = project.ContractAmount,
                    DueDate = project.EndDate?.Date,
                    Status = Domain.Enums.ReceivableStatus.Pending,
                    Description = "项目合同应收款"
                };

                await _receivableRepository.AddAsync(receivable);
                await _unitOfWork.SaveChangesAsync();

                if (dbTransaction != null) await dbTransaction.CommitAsync();

                var dto = await GetByIdAsync(project.Id);
                await _auditLogService.LogAsync("Create", "Project", project.Id, null, SerializeForAudit(dto));
                _logger.LogInformation("创建项目成功: Id={Id}, 名称={Name}, 项目编号={Code}, 合同金额={Amount}",
                    project.Id, project.Name, project.ProjectCode, project.ContractAmount);

                return dto;
            }
            catch (Exception ex)
            {
                if (dbTransaction != null) await dbTransaction.RollbackAsync();

                if (ex is NotFoundException)
                {
                    throw;
                }

                if (ex is ValidationException)
                {
                    throw;
                }

                if (ex is DbUpdateException dbUpdateException && IsDuplicateProjectCodeException(dbUpdateException))
                {
                    throw CreateDuplicateProjectCodeValidationException(request.ProjectCode);
                }

                _logger.LogError(ex, "创建项目失败: 名称={Name}, 项目编号={Code}, 客户ID={CustomerId}",
                    request.Name, request.ProjectCode, request.CustomerId);
                throw;
            }
        }
        catch (NotFoundException)
        {
            throw;
        }
        catch (ValidationException)
        {
            throw;
        }
    }

    public async Task<ProjectDto> UpdateAsync(long id, UpdateProjectRequest request)
    {
        _logger.LogDebug("ProjectService.UpdateAsync - 开始更新项目: Id={Id}, 名称={Name}, 状态={Status}",
            id, request.Name, request.Status);

        try
        {
            var normalizedProjectCode = NormalizeProjectCode(request.ProjectCode);

            var project = await _projectRepository.GetQueryable()
                .FirstOrDefaultAsync(p => p.Id == id);

            if (project == null)
            {
                _logger.LogWarning("项目不存在: Id={Id}", id);
                throw new NotFoundException("项目不存在");
            }

            // 检查编辑权限
            EnsureCanEdit(project);
            EnsureProjectMutable(project, "已取消的项目不允许编辑");

            // Validate customer exists if changed
            if (request.CustomerId.HasValue && request.CustomerId != project.CustomerId)
            {
                _logger.LogDebug("客户ID发生变更: 原客户ID={OldCustomerId}, 新客户ID={NewCustomerId}",
                    project.CustomerId, request.CustomerId.Value);

                var customer = await _customerRepository.GetByIdAsync(request.CustomerId.Value);
                if (customer == null)
                {
                    _logger.LogWarning("客户不存在: CustomerId={CustomerId}", request.CustomerId.Value);
                    throw new NotFoundException("客户不存在");
                }

                _logger.LogDebug("新客户验证通过: CustomerId={CustomerId}, 客户名称={CustomerName}",
                    request.CustomerId.Value, customer.Name);
            }

            await EnsureProjectCodeUniqueAsync(normalizedProjectCode, id);

            // Validate status
            if (!Enum.TryParse<ProjectStatus>(request.Status, true, out var projectStatus))
            {
                _logger.LogWarning("项目状态验证失败: Status={Status}", request.Status);
                throw new ValidationException("无效的项目状态");
            }

            var oldDto = _mapper.Map<ProjectDto>(project);

            var oldStatus = project.Status;
            var oldContractAmount = project.ContractAmount;

            // 检查应收链路保护
            var customerChanged = request.CustomerId != project.CustomerId;
            var contractAmountChanged = request.ContractAmount != project.ContractAmount;

            if (customerChanged || contractAmountChanged)
            {
                var existingReceivables = await _receivableRepository.GetQueryable()
                    .Where(r => !r.IsDeleted && r.ProjectId == id)
                    .ToListAsync();

                if (existingReceivables.Any())
                {
                    var hasReceivedPayments = existingReceivables.Any(r => r.ReceivedAmount > 0);

                    if (customerChanged)
                    {
                        if (!request.CustomerId.HasValue)
                        {
                            throw new ValidationException("该项目已有应收款，不允许清空客户。如需调整，请先处理已有的应收款。");
                        }

                        if (hasReceivedPayments)
                        {
                            throw new ValidationException("该项目已有收款记录，不允许修改客户。如需变更客户，请先处理已有的应收款。");
                        }
                        // 同步更新未收款应收的客户
                        foreach (var receivable in existingReceivables)
                        {
                            receivable.CustomerId = request.CustomerId;
                            _receivableRepository.Update(receivable);
                        }
                        _logger.LogInformation("同步更新项目应收款客户: ProjectId={ProjectId}, 新客户Id={CustomerId}, 影响应收数量={Count}",
                            id, request.CustomerId, existingReceivables.Count);
                    }

                    if (contractAmountChanged)
                    {
                        var totalReceivedAmount = existingReceivables.Sum(r => r.ReceivedAmount);
                        if (request.ContractAmount < totalReceivedAmount)
                        {
                            throw new ValidationException($"新合同金额({request.ContractAmount})不能小于已收款总额({totalReceivedAmount})");
                        }

                        if (existingReceivables.Count > 1)
                        {
                            throw new ValidationException("该项目存在多条应收计划，不支持直接修改合同金额。请先调整收款计划后再修改合同金额。");
                        }

                        // 单条应收链路下，合同金额变化直接同步到该应收，避免项目头与应收明细脱节
                        if (existingReceivables.Count == 1)
                        {
                            var defaultReceivable = existingReceivables[0];
                            defaultReceivable.TotalAmount = request.ContractAmount;
                            defaultReceivable.RemainingAmount = request.ContractAmount - defaultReceivable.ReceivedAmount;

                            if (defaultReceivable.RemainingAmount == 0)
                            {
                                defaultReceivable.Status = ReceivableStatus.Settled;
                                defaultReceivable.SettledAt ??= DateTime.UtcNow;
                            }
                            else if (defaultReceivable.ReceivedAmount > 0)
                            {
                                defaultReceivable.Status = ReceivableStatus.Partial;
                                defaultReceivable.SettledAt = null;
                            }
                            else
                            {
                                defaultReceivable.Status = ReceivableStatus.Pending;
                                defaultReceivable.SettledAt = null;
                            }

                            _receivableRepository.Update(defaultReceivable);
                            _logger.LogInformation("同步更新默认应收金额: ReceivableId={ReceivableId}, 新总额={TotalAmount}, 新剩余={RemainingAmount}",
                                defaultReceivable.Id, defaultReceivable.TotalAmount, defaultReceivable.RemainingAmount);
                        }
                    }
                }
            }

            project.Name = request.Name;
            project.ProjectCode = normalizedProjectCode;
            project.CustomerId = request.CustomerId;
            project.ContractAmount = request.ContractAmount;
            project.StartDate = request.StartDate;
            project.EndDate = request.EndDate;
            project.Status = projectStatus;
            project.Description = request.Description;

            // Recalculate receivable amount
            var oldReceivableAmount = project.ReceivableAmount;
            project.ReceivableAmount = project.ContractAmount - project.ReceivedAmount;

            _logger.LogDebug("重新计算应收金额: 合同金额={ContractAmount}, 已收金额={ReceivedAmount}, 应收金额={ReceivableAmount}",
                project.ContractAmount, project.ReceivedAmount, project.ReceivableAmount);

            if (oldStatus != projectStatus)
            {
                _logger.LogInformation("项目状态变更: Id={Id}, 原状态={OldStatus}, 新状态={NewStatus}",
                    id, oldStatus, projectStatus);
            }

            if (oldContractAmount != request.ContractAmount)
            {
                _logger.LogInformation("合同金额变更: Id={Id}, 原金额={OldAmount}, 新金额={NewAmount}, 应收金额={ReceivableAmount}",
                    id, oldContractAmount, request.ContractAmount, project.ReceivableAmount);
            }

            _projectRepository.Update(project);
            await _unitOfWork.SaveChangesAsync();

            // 统一重算项目财务汇总（在基本字段保存后）
            await _recalculationService.RecalculateAsync(project.Id);
            await _unitOfWork.SaveChangesAsync();

            await _auditLogService.LogAsync("Update", "Project", project.Id, SerializeForAudit(oldDto), SerializeForAudit(_mapper.Map<ProjectDto>(project)));
            _logger.LogInformation("更新项目成功: Id={Id}, 名称={Name}, 状态={Status}, 应收金额={ReceivableAmount}",
                project.Id, project.Name, project.Status, project.ReceivableAmount);

            return await GetByIdAsync(project.Id);
        }
        catch (NotFoundException)
        {
            throw;
        }
        catch (ValidationException)
        {
            throw;
        }
        catch (DbUpdateException ex) when (IsDuplicateProjectCodeException(ex))
        {
            throw CreateDuplicateProjectCodeValidationException(request.ProjectCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新项目失败: Id={Id}, 名称={Name}",
                id, request.Name);
            throw;
        }
    }

    public async Task DeleteAsync(long id)
    {
        _logger.LogDebug("ProjectService.DeleteAsync - 开始删除项目: Id={Id}", id);

        try
        {
            var project = await _projectRepository.GetQueryable()
                .FirstOrDefaultAsync(p => p.Id == id);

            if (project == null)
            {
                _logger.LogWarning("项目不存在: Id={Id}", id);
                throw new NotFoundException("项目不存在");
            }

            // 检查删除权限
            EnsureCanDelete(project);
            EnsureProjectMutable(project, "已取消的项目不允许删除");

            _logger.LogDebug("准备删除项目: Id={Id}, 名称={Name}, 项目编号={Code}",
                id, project.Name, project.ProjectCode);

            var oldDto = _mapper.Map<ProjectDto>(project);

            if (await _referenceGuard.HasProjectReferencesAsync(id))
            {
                project.Status = ProjectStatus.Cancelled;
                _projectRepository.Update(project);
                await _unitOfWork.SaveChangesAsync();

                await _auditLogService.LogAsync("Archive", "Project", project.Id, SerializeForAudit(oldDto), SerializeForAudit(_mapper.Map<ProjectDto>(project)));
                _logger.LogInformation("项目存在历史引用，删除改为归档: Id={Id}, Name={Name}", id, project.Name);
                return;
            }

            _projectRepository.Delete(project);
            await _unitOfWork.SaveChangesAsync();

            await _auditLogService.LogAsync("Delete", "Project", project.Id, SerializeForAudit(oldDto), null);
            _logger.LogInformation("删除项目成功: Id={Id}, 名称={Name}", id, project.Name);
        }
        catch (NotFoundException)
        {
            throw;
        }
        catch (ValidationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除项目失败: Id={Id}", id);
            throw;
        }
    }

    public async Task<List<ProjectProfitReportDto>> GetProjectProfitReportAsync()
    {
        _logger.LogDebug("ProjectService.GetProjectProfitReportAsync - 开始生成项目利润报表");

        try
        {
            var projects = await ApplyPermissionFilter(_projectRepository.GetQueryable())
                .Include(p => p.Customer)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            _logger.LogInformation("查询到{Count}个项目，开始计算利润", projects.Count);

            var reportList = new List<ProjectProfitReportDto>();

            foreach (var project in projects)
            {
                reportList.Add(new ProjectProfitReportDto
                {
                    ProjectId = project.Id,
                    ProjectName = project.Name,
                    ProjectCode = project.ProjectCode,
                    CustomerName = project.Customer?.Name,
                    ContractAmount = project.ContractAmount,
                    ReceivedAmount = project.ReceivedAmount,
                    ReceivableAmount = project.ReceivableAmount,
                    DirectCost = 0m,
                    AllocatedCost = 0m,
                    TotalCost = project.TotalCost,
                    ProfitAmount = project.ProfitAmount,
                    ProfitRate = project.ProfitRate,
                    Status = project.Status.ToString(),
                    StartDate = project.StartDate,
                    EndDate = project.EndDate
                });
            }

            _logger.LogInformation("项目利润报表生成成功: 共{Count}个项目", reportList.Count);
            return reportList;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "生成项目利润报表失败");
            throw;
        }
    }

    public async Task<List<ProjectDto>> GetActiveProjectsAsync()
    {
        _logger.LogDebug("ProjectService.GetActiveProjectsAsync - 开始获取活跃项目列表");

        try
        {
            var projects = await ApplyPermissionFilter(_projectRepository.GetQueryable())
                .Include(p => p.Customer)
                .Where(p => p.Status == ProjectStatus.Active)
                .OrderBy(p => p.Name)
                .ToListAsync();

            var result = _mapper.Map<List<ProjectDto>>(projects);
            _logger.LogInformation("获取活跃项目列表成功: 共{Count}个活跃项目", result.Count);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取活跃项目列表失败");
            throw;
        }
    }

    public async Task<BatchCreateResponse<ProjectDto>> BatchCreateAsync(List<CreateProjectRequest> items)
    {
        _logger.LogDebug("ProjectService.BatchCreateAsync - 开始批量创建项目: 共{Count}个项目", items.Count);

        var response = new BatchCreateResponse<ProjectDto>
        {
            TotalCount = items.Count
        };

        try
        {
            for (int i = 0; i < items.Count; i++)
            {
                try
                {
                    _logger.LogDebug("处理第{Index}个: 名称={Name}",
                        i + 1, items[i].Name);

                    var result = await CreateAsync(items[i]);
                    response.SuccessItems.Add(result);
                    response.SuccessCount++;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "第{Index}个失败: 名称={Name}, 错误={Message}",
                        i + 1, items[i].Name, ex.Message);

                    response.Errors.Add(new BatchError { Index = i, Message = ex.Message });
                    response.FailedCount++;
                }
            }

            _logger.LogInformation("批量创建项目完成: 总数={Total}, 成功={Success}, 失败={Failed}",
                response.TotalCount, response.SuccessCount, response.FailedCount);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "批量创建项目失败: 总数={Count}", items.Count);
            throw;
        }
    }

    public async Task<ProjectStatisticsDto> GetStatisticsAsync()
    {
        _logger.LogDebug("ProjectService.GetStatisticsAsync - 开始获取项目统计数据");

        try
        {
            var query = ApplyPermissionFilter(_projectRepository.GetQueryable());
            var projects = await query.ToListAsync();

            var result = new ProjectStatisticsDto
            {
                TotalCount = projects.Count,
                TotalContractAmount = projects.Sum(p => p.ContractAmount),
                TotalProfit = projects.Sum(p => p.ProfitAmount),
                TotalReceivable = projects.Sum(p => p.ReceivableAmount)
            };

            _logger.LogInformation("获取项目统计数据成功: 总数={TotalCount}, 合同总额={TotalContractAmount}, 总利润={TotalProfit}, 总应收={TotalReceivable}",
                result.TotalCount, result.TotalContractAmount, result.TotalProfit, result.TotalReceivable);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取项目统计数据失败");
            throw;
        }
    }

    public async Task<ProjectStatisticsDto> GetStatisticsAsync(PageRequest request)
    {
        _logger.LogDebug("ProjectService.GetStatisticsAsync with filters");

        try
        {
            var query = ApplyPermissionFilter(_projectRepository.GetQueryable());

            // 标签过滤
            if (request.TagFilters != null && request.TagFilters.Count > 0)
            {
                var tagBindings = _tagBindingRepository.GetQueryable();
                query = query.ApplyOwnerTagFilters(tagBindings, request.TagFilters, TagScope.Project);
            }

            if (!string.IsNullOrWhiteSpace(request.Name))
                query = query.Where(p => p.Name.Contains(request.Name));

            if (request.CustomerId.HasValue)
                query = query.Where(p => p.CustomerId == request.CustomerId.Value);

            if (!string.IsNullOrWhiteSpace(request.Status))
            {
                if (Enum.TryParse<ProjectStatus>(request.Status, true, out var status))
                    query = query.Where(p => p.Status == status);
            }

            var projects = await query.ToListAsync();

            return new ProjectStatisticsDto
            {
                TotalCount = projects.Count,
                TotalContractAmount = projects.Sum(p => p.ContractAmount),
                TotalProfit = projects.Sum(p => p.ProfitAmount),
                TotalReceivable = projects.Sum(p => p.ReceivableAmount)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取项目统计数据失败");
            throw;
        }
    }

    public async Task<ProfitAnalysisResponse> GetProfitAnalysisAsync(long id, int months = 12)
    {
        _logger.LogDebug("ProjectService.GetProfitAnalysisAsync - Id={Id}, Months={Months}", id, months);

        try
        {
            var project = await ApplyPermissionFilter(_projectRepository.GetQueryable())
                .FirstOrDefaultAsync(p => p.Id == id);

            if (project == null)
            {
                _logger.LogWarning("项目不存在: Id={Id}", id);
                throw new NotFoundException("项目不存在");
            }

            var startDate = DateTime.UtcNow.AddMonths(-months).Date;
            startDate = new DateTime(startDate.Year, startDate.Month, 1, 0, 0, 0, DateTimeKind.Utc);

            // 获取直接关联的交易
            var directTransactions = await ApplyPermissionFilter(_transactionRepository.GetQueryable())
                .Include(t => t.Category)
                .Where(t => t.ProjectId == id && !t.IsAllocated && t.TransactionDate >= startDate)
                .ToListAsync();

            // 获取分摊关联的交易
            var allocations = await ApplyPermissionFilter(_allocationRepository.GetQueryable())
                .Include(a => a.Transaction)
                    .ThenInclude(t => t.Category)
                .Where(a => a.ProjectId == id && a.Transaction.TransactionDate >= startDate)
                .ToListAsync();

            // 按月汇总
            var monthlyData = new List<MonthlyProfitDto>();
            var now = DateTime.UtcNow;

            for (var date = startDate; date <= now; date = date.AddMonths(1))
            {
                var year = date.Year;
                var month = date.Month;

                var monthIncome = directTransactions
                    .Where(t => t.TransactionDate.Year == year && t.TransactionDate.Month == month
                                && t.TransactionType == TransactionType.Income)
                    .Sum(t => t.Amount);

                var monthDirectExpense = directTransactions
                    .Where(t => t.TransactionDate.Year == year && t.TransactionDate.Month == month
                                && t.TransactionType == TransactionType.Expense)
                    .Sum(t => t.Amount);

                var monthAllocatedExpense = allocations
                    .Where(a => a.Transaction.TransactionDate.Year == year
                                && a.Transaction.TransactionDate.Month == month
                                && a.Transaction.TransactionType == TransactionType.Expense)
                    .Sum(a => a.Amount);

                var totalExpense = monthDirectExpense + monthAllocatedExpense;

                monthlyData.Add(new MonthlyProfitDto
                {
                    Month = $"{year}-{month:D2}",
                    Income = monthIncome,
                    Expense = totalExpense,
                    Profit = monthIncome - totalExpense
                });
            }

            // 费用分类占比
            var categoryExpenses = new Dictionary<string, decimal>();

            foreach (var t in directTransactions.Where(t => t.TransactionType == TransactionType.Expense))
            {
                var catName = t.Category?.Name ?? "未分类";
                categoryExpenses.TryAdd(catName, 0);
                categoryExpenses[catName] += t.Amount;
            }

            foreach (var a in allocations.Where(a => a.Transaction.TransactionType == TransactionType.Expense))
            {
                var catName = a.Transaction.Category?.Name ?? "未分类";
                categoryExpenses.TryAdd(catName, 0);
                categoryExpenses[catName] += a.Amount;
            }

            var totalExpenseAll = categoryExpenses.Values.Sum();
            var expenseCategories = categoryExpenses
                .OrderByDescending(kv => kv.Value)
                .Select(kv => new ExpenseCategoryDto
                {
                    CategoryName = kv.Key,
                    Amount = kv.Value,
                    Percentage = totalExpenseAll > 0 ? Math.Round(kv.Value / totalExpenseAll * 100, 2) : 0
                })
                .ToList();

            var totalIncome = monthlyData.Sum(m => m.Income);
            var totalExpenseSum = monthlyData.Sum(m => m.Expense);

            _logger.LogInformation("获取项目利润分析成功: Id={Id}, 月份数={Count}, 总收入={Income}, 总支出={Expense}",
                id, monthlyData.Count, totalIncome, totalExpenseSum);

            return new ProfitAnalysisResponse
            {
                MonthlyData = monthlyData,
                ExpenseCategories = expenseCategories,
                TotalIncome = totalIncome,
                TotalExpense = totalExpenseSum,
                TotalProfit = totalIncome - totalExpenseSum
            };
        }
        catch (NotFoundException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取项目利润分析失败: Id={Id}", id);
            throw;
        }
    }

    private async Task EnsureProjectCodeUniqueAsync(string? projectCode, long? excludeProjectId = null)
    {
        if (string.IsNullOrWhiteSpace(projectCode))
        {
            return;
        }

        var normalizedProjectCode = projectCode.Trim();
        var normalizedUpperCode = normalizedProjectCode.ToUpper();

        var query = _projectRepository.GetQueryable()
            .Where(p => p.ProjectCode != null && p.ProjectCode.Trim().ToUpper() == normalizedUpperCode);

        if (excludeProjectId.HasValue)
        {
            query = query.Where(p => p.Id != excludeProjectId.Value);
        }

        if (await query.AnyAsync())
        {
            _logger.LogWarning("项目编号已存在: ProjectCode={ProjectCode}, ExcludeProjectId={ExcludeProjectId}",
                normalizedProjectCode, excludeProjectId);
            throw CreateDuplicateProjectCodeValidationException(normalizedProjectCode);
        }
    }

    private static string? NormalizeProjectCode(string? projectCode)
    {
        if (string.IsNullOrWhiteSpace(projectCode))
        {
            throw new ValidationException("项目编号不能为空，请手动输入或点击一键生成");
        }

        var normalizedProjectCode = projectCode.Trim();
        if (normalizedProjectCode.Length > ValidationConstants.Project.CodeMaxLength)
        {
            throw new ValidationException($"项目编号长度不能超过 {ValidationConstants.Project.CodeMaxLength} 个字符");
        }

        return normalizedProjectCode;
    }

    private static bool TryParseGeneratedProjectCodeSequence(string? projectCode, string prefix, out int sequence)
    {
        sequence = 0;
        if (string.IsNullOrWhiteSpace(projectCode))
        {
            return false;
        }

        var normalizedProjectCode = projectCode.Trim();
        if (!normalizedProjectCode.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var sequencePart = normalizedProjectCode[prefix.Length..];
        return int.TryParse(sequencePart, out sequence);
    }

    private static bool IsDuplicateProjectCodeException(DbUpdateException exception)
    {
        var message = exception.InnerException?.Message ?? exception.Message;
        return message.Contains("idx_projects_code", StringComparison.OrdinalIgnoreCase)
            || (message.Contains("project_code", StringComparison.OrdinalIgnoreCase)
                && message.Contains("unique", StringComparison.OrdinalIgnoreCase));
    }

    private static void EnsureProjectMutable(Project project, string message)
    {
        if (project.Status == ProjectStatus.Cancelled)
        {
            throw new ValidationException(message);
        }
    }

    private static ValidationException CreateDuplicateProjectCodeValidationException(string? projectCode)
    {
        var displayCode = string.IsNullOrWhiteSpace(projectCode) ? "当前项目编号" : projectCode.Trim();
        return new ValidationException($"项目编号已存在: {displayCode}");
    }

    public async Task InitializeReceivablesAsync(long projectId, InitializeReceivablesRequest request)
    {
        _logger.LogInformation("[ProjectService.InitializeReceivablesAsync] ProjectId={ProjectId}, Mode={Mode}",
            projectId, request.Mode);

        var project = await _projectRepository.GetByIdAsync(projectId);
        if (project == null)
        {
            throw new ValidationException("项目不存在");
        }

        EnsureProjectMutable(project, "已取消的项目不允许初始化应收");

        // 幂等性检查：检查项目是否已经初始化过收款计划
        var existingReceivables = await _receivableRepository.GetQueryable()
            .Where(r => r.ProjectId == projectId && !r.IsDeleted)
            .ToListAsync();

        if (existingReceivables.Any())
        {
            _logger.LogWarning("[ProjectService.InitializeReceivablesAsync] 项目已存在收款计划, ProjectId={ProjectId}", projectId);
            throw new ValidationException("该项目已存在收款计划，不能重复初始化");
        }

        if (request.Mode == "once")
        {
            // 一次性收款
            var receivableRequest = new CreateReceivableRequest
            {
                ProjectId = projectId,
                CustomerId = project.CustomerId,
                TotalAmount = project.ContractAmount,
                Description = "一次性收款"
            };

            await _receivableService.CreateAsync(receivableRequest);
        }
        else if (request.Mode == "installment")
        {
            // 分期收款
            if (request.Installments == null || request.Installments.Count == 0)
            {
                throw new ValidationException("分期收款必须提供收款明细");
            }

            var totalAmount = request.Installments.Sum(i => i.Amount);
            if (Math.Abs(totalAmount - project.ContractAmount) > 0.01m)
            {
                throw new ValidationException($"收款明细总额({totalAmount})与合同金额({project.ContractAmount})不一致");
            }

            // 批量创建应收款，避免 N+1 查询
            var receivables = request.Installments.Select(installment => new Receivable
            {
                ProjectId = projectId,
                CustomerId = project.CustomerId,
                TotalAmount = installment.Amount,
                ReceivedAmount = 0,
                RemainingAmount = installment.Amount,
                DueDate = installment.DueDate,
                Status = ReceivableStatus.Pending,
                Description = installment.Name
            }).ToList();

            foreach (var receivable in receivables)
            {
                await _receivableRepository.AddAsync(receivable);
            }
            await _unitOfWork.SaveChangesAsync();
        }

        _logger.LogInformation("[ProjectService.InitializeReceivablesAsync] 成功, ProjectId={ProjectId}", projectId);
    }

    public async Task RecalculateProjectFinancialsAsync(long projectId)
    {
        await _recalculationService.RecalculateAsync(projectId);
    }
}
