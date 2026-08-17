using MapsterMapper;
using FinanceApp.Application.Common;
using FinanceApp.Application.Extensions;
using FinanceApp.Application.Modules.FinanceSettlement.DTOs.Payable;
using FinanceApp.Application.Modules.FinanceSettlement.Interfaces;
using FinanceApp.Application.Modules.Identity.Interfaces;
using FinanceApp.Application.Services;
using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FinanceApp.Application.Modules.FinanceSettlement.Services;

/// <summary>
/// 应付款业务类型服务实现
/// </summary>
public class PayableTypeService : ServiceBase, IPayableTypeService
{
    private readonly IRepository<PayableType> _payableTypeRepository;
    private readonly IRepository<Payable> _payableRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<PayableTypeService> _logger;
    private readonly IAuditLogService _auditLogService;
    private readonly IUnitOfWork _unitOfWork;

    public PayableTypeService(
        IRepository<PayableType> payableTypeRepository,
        IRepository<Payable> payableRepository,
        IMapper mapper,
        ILogger<PayableTypeService> logger,
        IAuditLogService auditLogService,
        ICurrentUserService currentUserService,
        IDataPermissionService permissionService,
        IUnitOfWork unitOfWork)
        : base(currentUserService, permissionService)
    {
        _payableTypeRepository = payableTypeRepository;
        _payableRepository = payableRepository;
        _mapper = mapper;
        _logger = logger;
        _auditLogService = auditLogService;
        _unitOfWork = unitOfWork;
    }

    public async Task<PageResponse<PayableTypeDto>> GetPagedAsync(PageRequest request)
    {
        _logger.LogDebug("查询应付款类型列表, Page={Page}, PageSize={PageSize}",
            request.Page, request.PageSize);

        var query = _payableTypeRepository.GetQueryable();

        // 应用自定义排序
        var sortableFields = new Dictionary<string, System.Linq.Expressions.Expression<Func<PayableType, object>>>(StringComparer.OrdinalIgnoreCase)
        {
            ["name"] = pt => pt.Name,
            ["code"] = pt => pt.Code!,
            ["sortOrder"] = pt => pt.SortOrder,
            ["isActive"] = pt => pt.IsActive,
            ["createdAt"] = pt => pt.CreatedAt
        };
        query = SortingHelper.ApplySorting(query, request.SortBy, request.SortOrder, sortableFields);

        // 默认排序：按 SortOrder 升序，然后按创建时间降序
        if (string.IsNullOrEmpty(request.SortBy))
        {
            query = query.OrderBy(pt => pt.SortOrder).ThenByDescending(pt => pt.CreatedAt);
        }

        var total = await query.CountAsync();
        var items = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync();

        var dtos = _mapper.Map<List<PayableTypeDto>>(items);

        _logger.LogInformation("查询应付款类型列表成功, 总数={Total}, 返回={Count}",
            total, dtos.Count);

        return new PageResponse<PayableTypeDto>
        {
            Items = dtos,
            Page = request.Page,
            PageSize = request.PageSize,
            Total = total
        };
    }

    public async Task<List<PayableTypeDto>> GetAllActiveAsync()
    {
        _logger.LogDebug("查询所有启用的应付款类型");

        var items = await _payableTypeRepository.GetQueryable()
            .Where(pt => pt.IsActive)
            .OrderBy(pt => pt.SortOrder)
            .ThenBy(pt => pt.Name)
            .ToListAsync();

        var dtos = _mapper.Map<List<PayableTypeDto>>(items);

        _logger.LogInformation("查询所有启用的应付款类型成功, 数量={Count}", dtos.Count);

        return dtos;
    }

    public async Task<PayableTypeDto> GetByIdAsync(long id)
    {
        _logger.LogDebug("查询应付款类型详情, Id={Id}", id);

        var payableType = await _payableTypeRepository.GetByIdAsync(id);

        if (payableType == null)
        {
            _logger.LogWarning("应付款类型不存在, Id={Id}", id);
            throw new NotFoundException("应付款类型不存在");
        }

        var dto = _mapper.Map<PayableTypeDto>(payableType);

        _logger.LogInformation("查询应付款类型详情成功, Id={Id}, Name={Name}", id, dto.Name);

        return dto;
    }

    public async Task<PayableTypeDto> CreateAsync(CreatePayableTypeRequest request)
    {
        _logger.LogDebug("创建应付款类型, Name={Name}, Code={Code}",
            request.Name, request.Code);

        // 验证名称不为空
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            _logger.LogWarning("创建应付款类型失败: 名称不能为空");
            throw new ValidationException("应付款类型名称不能为空");
        }

        // 验证 Code 唯一性（如果提供了 Code）
        if (!string.IsNullOrWhiteSpace(request.Code))
        {
            var existingByCode = await _payableTypeRepository.GetQueryable()
                .FirstOrDefaultAsync(pt => pt.Code == request.Code);

            if (existingByCode != null)
            {
                _logger.LogWarning("创建应付款类型失败: Code 已存在, Code={Code}", request.Code);
                throw new ValidationException($"应付款类型编码 '{request.Code}' 已存在");
            }
        }

        // 创建应付款类型
        var payableType = new PayableType
        {
            Name = request.Name.Trim(),
            Code = string.IsNullOrWhiteSpace(request.Code) ? null : request.Code.Trim(),
            Description = request.Description,
            IsActive = true,
            SortOrder = request.SortOrder
        };

        await _payableTypeRepository.AddAsync(payableType);
        await _unitOfWork.SaveChangesAsync();

        var dto = await GetByIdAsync(payableType.Id);
        await _auditLogService.LogAsync("Create", "PayableType", payableType.Id, null, SerializeForAudit(dto));

        _logger.LogInformation("创建应付款类型成功, Id={Id}, Name={Name}, Code={Code}",
            payableType.Id, request.Name, request.Code);

        return dto;
    }

    public async Task<PayableTypeDto> UpdateAsync(long id, UpdatePayableTypeRequest request)
    {
        _logger.LogDebug("更新应付款类型, Id={Id}, Name={Name}, Code={Code}",
            id, request.Name, request.Code);

        var payableType = await _payableTypeRepository.GetByIdAsync(id);

        if (payableType == null)
        {
            _logger.LogWarning("更新应付款类型失败: 记录不存在, Id={Id}", id);
            throw new NotFoundException("应付款类型不存在");
        }

        var oldDto = _mapper.Map<PayableTypeDto>(payableType);

        // 验证名称不为空
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            _logger.LogWarning("更新应付款类型失败: 名称不能为空, Id={Id}", id);
            throw new ValidationException("应付款类型名称不能为空");
        }

        // 验证 Code 唯一性（如果提供了 Code 且与原值不同）
        if (!string.IsNullOrWhiteSpace(request.Code) && request.Code != payableType.Code)
        {
            var existingByCode = await _payableTypeRepository.GetQueryable()
                .FirstOrDefaultAsync(pt => pt.Code == request.Code && pt.Id != id);

            if (existingByCode != null)
            {
                _logger.LogWarning("更新应付款类型失败: Code 已存在, Id={Id}, Code={Code}", id, request.Code);
                throw new ValidationException($"应付款类型编码 '{request.Code}' 已存在");
            }
        }

        // 更新应付款类型
        payableType.Name = request.Name.Trim();
        payableType.Code = string.IsNullOrWhiteSpace(request.Code) ? null : request.Code.Trim();
        payableType.Description = request.Description;
        payableType.IsActive = request.IsActive;
        payableType.SortOrder = request.SortOrder;

        _payableTypeRepository.Update(payableType);
        await _unitOfWork.SaveChangesAsync();

        var updatedDto = await GetByIdAsync(id);
        await _auditLogService.LogAsync("Update", "PayableType", id, SerializeForAudit(oldDto), SerializeForAudit(updatedDto));

        _logger.LogInformation("更新应付款类型成功, Id={Id}, Name={Name}, Code={Code}, IsActive={IsActive}",
            id, request.Name, request.Code, request.IsActive);

        return updatedDto;
    }

    public async Task DeleteAsync(long id)
    {
        _logger.LogDebug("删除应付款类型, Id={Id}", id);

        var payableType = await _payableTypeRepository.GetByIdAsync(id);

        if (payableType == null)
        {
            _logger.LogWarning("删除应付款类型失败: 记录不存在, Id={Id}", id);
            throw new NotFoundException("应付款类型不存在");
        }

        // 检查是否有关联的应付款
        var hasPayables = await _payableRepository.GetQueryable()
            .AnyAsync(p => p.PayableTypeId == id);

        if (hasPayables)
        {
            _logger.LogWarning("删除应付款类型失败: 存在关联的应付款记录, Id={Id}", id);
            throw new ValidationException("该应付款类型下存在应付款记录，无法删除");
        }

        var oldDto = _mapper.Map<PayableTypeDto>(payableType);

        _payableTypeRepository.Delete(id);
        await _unitOfWork.SaveChangesAsync();

        await _auditLogService.LogAsync("Delete", "PayableType", id, SerializeForAudit(oldDto), null);

        _logger.LogInformation("删除应付款类型成功, Id={Id}, Name={Name}",
            id, payableType.Name);
    }
}




