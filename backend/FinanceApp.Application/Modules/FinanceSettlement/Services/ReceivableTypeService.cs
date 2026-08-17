using MapsterMapper;
using FinanceApp.Application.Common;
using FinanceApp.Application.Extensions;
using FinanceApp.Application.Modules.FinanceSettlement.DTOs.Receivable;
using FinanceApp.Application.Modules.FinanceSettlement.Interfaces;
using FinanceApp.Application.Modules.Identity.Interfaces;
using FinanceApp.Application.Services;
using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FinanceApp.Application.Modules.FinanceSettlement.Services;

/// <summary>
/// 应收款业务类型服务实现
/// </summary>
public class ReceivableTypeService : ServiceBase, IReceivableTypeService
{
    private readonly IRepository<ReceivableType> _receivableTypeRepository;
    private readonly IRepository<Receivable> _receivableRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<ReceivableTypeService> _logger;
    private readonly IAuditLogService _auditLogService;
    private readonly IUnitOfWork _unitOfWork;

    public ReceivableTypeService(
        IRepository<ReceivableType> receivableTypeRepository,
        IRepository<Receivable> receivableRepository,
        IMapper mapper,
        ILogger<ReceivableTypeService> logger,
        IAuditLogService auditLogService,
        ICurrentUserService currentUserService,
        IDataPermissionService permissionService,
        IUnitOfWork unitOfWork)
        : base(currentUserService, permissionService)
    {
        _receivableTypeRepository = receivableTypeRepository;
        _receivableRepository = receivableRepository;
        _mapper = mapper;
        _logger = logger;
        _auditLogService = auditLogService;
        _unitOfWork = unitOfWork;
    }

    public async Task<PageResponse<ReceivableTypeDto>> GetPagedAsync(PageRequest request)
    {
        _logger.LogDebug("查询应收款类型列表, Page={Page}, PageSize={PageSize}",
            request.Page, request.PageSize);

        var query = _receivableTypeRepository.GetQueryable();

        // 应用自定义排序
        var sortableFields = new Dictionary<string, System.Linq.Expressions.Expression<Func<ReceivableType, object>>>(StringComparer.OrdinalIgnoreCase)
        {
            ["name"] = rt => rt.Name,
            ["code"] = rt => rt.Code!,
            ["sortOrder"] = rt => rt.SortOrder,
            ["isActive"] = rt => rt.IsActive,
            ["createdAt"] = rt => rt.CreatedAt
        };
        query = SortingHelper.ApplySorting(query, request.SortBy, request.SortOrder, sortableFields);

        // 默认排序：按 SortOrder 升序，然后按创建时间降序
        if (string.IsNullOrEmpty(request.SortBy))
        {
            query = query.OrderBy(rt => rt.SortOrder).ThenByDescending(rt => rt.CreatedAt);
        }

        var total = await query.CountAsync();
        var items = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync();

        var dtos = _mapper.Map<List<ReceivableTypeDto>>(items);

        _logger.LogInformation("查询应收款类型列表成功, 总数={Total}, 返回={Count}",
            total, dtos.Count);

        return new PageResponse<ReceivableTypeDto>
        {
            Items = dtos,
            Page = request.Page,
            PageSize = request.PageSize,
            Total = total
        };
    }

    public async Task<List<ReceivableTypeDto>> GetAllActiveAsync()
    {
        _logger.LogDebug("查询所有启用的应收款类型");

        var items = await _receivableTypeRepository.GetQueryable()
            .Where(rt => rt.IsActive)
            .OrderBy(rt => rt.SortOrder)
            .ThenBy(rt => rt.Name)
            .ToListAsync();

        var dtos = _mapper.Map<List<ReceivableTypeDto>>(items);

        _logger.LogInformation("查询所有启用的应收款类型成功, 数量={Count}", dtos.Count);

        return dtos;
    }

    public async Task<ReceivableTypeDto> GetByIdAsync(long id)
    {
        _logger.LogDebug("查询应收款类型详情, Id={Id}", id);

        var receivableType = await _receivableTypeRepository.GetByIdAsync(id);

        if (receivableType == null)
        {
            _logger.LogWarning("应收款类型不存在, Id={Id}", id);
            throw new NotFoundException("应收款类型不存在");
        }

        var dto = _mapper.Map<ReceivableTypeDto>(receivableType);

        _logger.LogInformation("查询应收款类型详情成功, Id={Id}, Name={Name}", id, dto.Name);

        return dto;
    }

    public async Task<ReceivableTypeDto> CreateAsync(CreateReceivableTypeRequest request)
    {
        _logger.LogDebug("创建应收款类型, Name={Name}, Code={Code}",
            request.Name, request.Code);

        // 验证名称不为空
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            _logger.LogWarning("创建应收款类型失败: 名称不能为空");
            throw new ValidationException("应收款类型名称不能为空");
        }

        // 验证 Code 唯一性（如果提供了 Code）
        if (!string.IsNullOrWhiteSpace(request.Code))
        {
            var existingByCode = await _receivableTypeRepository.GetQueryable()
                .FirstOrDefaultAsync(rt => rt.Code == request.Code);

            if (existingByCode != null)
            {
                _logger.LogWarning("创建应收款类型失败: Code 已存在, Code={Code}", request.Code);
                throw new ValidationException($"应收款类型编码 '{request.Code}' 已存在");
            }
        }

        // 创建应收款类型
        var receivableType = new ReceivableType
        {
            Name = request.Name.Trim(),
            Code = string.IsNullOrWhiteSpace(request.Code) ? null : request.Code.Trim(),
            Description = request.Description,
            IsActive = true,
            SortOrder = request.SortOrder
        };

        await _receivableTypeRepository.AddAsync(receivableType);
        await _unitOfWork.SaveChangesAsync();

        var dto = await GetByIdAsync(receivableType.Id);
        await _auditLogService.LogAsync("Create", "ReceivableType", receivableType.Id, null, SerializeForAudit(dto));

        _logger.LogInformation("创建应收款类型成功, Id={Id}, Name={Name}, Code={Code}",
            receivableType.Id, request.Name, request.Code);

        return dto;
    }

    public async Task<ReceivableTypeDto> UpdateAsync(long id, UpdateReceivableTypeRequest request)
    {
        _logger.LogDebug("更新应收款类型, Id={Id}, Name={Name}, Code={Code}",
            id, request.Name, request.Code);

        var receivableType = await _receivableTypeRepository.GetByIdAsync(id);

        if (receivableType == null)
        {
            _logger.LogWarning("更新应收款类型失败: 记录不存在, Id={Id}", id);
            throw new NotFoundException("应收款类型不存在");
        }

        var oldDto = _mapper.Map<ReceivableTypeDto>(receivableType);

        // 验证名称不为空
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            _logger.LogWarning("更新应收款类型失败: 名称不能为空, Id={Id}", id);
            throw new ValidationException("应收款类型名称不能为空");
        }

        // 验证 Code 唯一性（如果提供了 Code 且与原值不同）
        if (!string.IsNullOrWhiteSpace(request.Code) && request.Code != receivableType.Code)
        {
            var existingByCode = await _receivableTypeRepository.GetQueryable()
                .FirstOrDefaultAsync(rt => rt.Code == request.Code && rt.Id != id);

            if (existingByCode != null)
            {
                _logger.LogWarning("更新应收款类型失败: Code 已存在, Id={Id}, Code={Code}", id, request.Code);
                throw new ValidationException($"应收款类型编码 '{request.Code}' 已存在");
            }
        }

        // 更新应收款类型
        receivableType.Name = request.Name.Trim();
        receivableType.Code = string.IsNullOrWhiteSpace(request.Code) ? null : request.Code.Trim();
        receivableType.Description = request.Description;
        receivableType.IsActive = request.IsActive;
        receivableType.SortOrder = request.SortOrder;

        _receivableTypeRepository.Update(receivableType);
        await _unitOfWork.SaveChangesAsync();

        var updatedDto = await GetByIdAsync(id);
        await _auditLogService.LogAsync("Update", "ReceivableType", id, SerializeForAudit(oldDto), SerializeForAudit(updatedDto));

        _logger.LogInformation("更新应收款类型成功, Id={Id}, Name={Name}, Code={Code}, IsActive={IsActive}",
            id, request.Name, request.Code, request.IsActive);

        return updatedDto;
    }

    public async Task DeleteAsync(long id)
    {
        _logger.LogDebug("删除应收款类型, Id={Id}", id);

        var receivableType = await _receivableTypeRepository.GetByIdAsync(id);

        if (receivableType == null)
        {
            _logger.LogWarning("删除应收款类型失败: 记录不存在, Id={Id}", id);
            throw new NotFoundException("应收款类型不存在");
        }

        // 检查是否有关联的应收款
        var hasReceivables = await _receivableRepository.GetQueryable()
            .AnyAsync(r => r.ReceivableTypeId == id);

        if (hasReceivables)
        {
            _logger.LogWarning("删除应收款类型失败: 存在关联的应收款记录, Id={Id}", id);
            throw new ValidationException("该应收款类型下存在应收款记录，无法删除");
        }

        var oldDto = _mapper.Map<ReceivableTypeDto>(receivableType);

        _receivableTypeRepository.Delete(id);
        await _unitOfWork.SaveChangesAsync();

        await _auditLogService.LogAsync("Delete", "ReceivableType", id, SerializeForAudit(oldDto), null);

        _logger.LogInformation("删除应收款类型成功, Id={Id}, Name={Name}",
            id, receivableType.Name);
    }
}
