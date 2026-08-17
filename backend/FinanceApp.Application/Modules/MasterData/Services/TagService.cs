using FinanceApp.Application.Common;
using FinanceApp.Application.Modules.Identity.Interfaces;
using FinanceApp.Application.Modules.MasterData.DTOs.Tag;
using FinanceApp.Application.Modules.MasterData.Interfaces;
using FinanceApp.Application.Services;
using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Enums;
using FinanceApp.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace FinanceApp.Application.Modules.MasterData.Services;

public class TagService : ServiceBase, ITagService
{
    private readonly IRepository<Tag> _tagRepository;
    private readonly IRepository<TagBinding> _tagBindingRepository;
    private readonly ILogger<TagService> _logger;
    private readonly IAuditLogService _auditLogService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMemoryCache _cache;
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);
    private readonly IRepository<Transaction> _transactionRepository;
    private readonly IRepository<Project> _projectRepository;
    private readonly IRepository<Person> _personRepository;
    private readonly IRepository<Customer> _customerRepository;
    private readonly IRepository<Supplier> _supplierRepository;
    private readonly IRepository<Receivable> _receivableRepository;
    private readonly IRepository<Payable> _payableRepository;

    public TagService(
        IRepository<Tag> tagRepository,
        IRepository<TagBinding> tagBindingRepository,
        ILogger<TagService> logger,
        IAuditLogService auditLogService,
        ICurrentUserService currentUserService,
        IDataPermissionService permissionService,
        IUnitOfWork unitOfWork,
        IMemoryCache cache,
        IRepository<Transaction> transactionRepository,
        IRepository<Project> projectRepository,
        IRepository<Person> personRepository,
        IRepository<Customer> customerRepository,
        IRepository<Supplier> supplierRepository,
        IRepository<Receivable> receivableRepository,
        IRepository<Payable> payableRepository)
        : base(currentUserService, permissionService)
    {
        _tagRepository = tagRepository;
        _tagBindingRepository = tagBindingRepository;
        _logger = logger;
        _auditLogService = auditLogService;
        _unitOfWork = unitOfWork;
        _cache = cache;
        _transactionRepository = transactionRepository;
        _projectRepository = projectRepository;
        _personRepository = personRepository;
        _customerRepository = customerRepository;
        _supplierRepository = supplierRepository;
        _receivableRepository = receivableRepository;
        _payableRepository = payableRepository;
    }

    public async Task<List<TagDto>> GetTagsAsync(string scope, bool? isActive = null)
    {
        _logger.LogDebug("TagService.GetTagsAsync - Scope={Scope}, IsActive={IsActive}", scope, isActive);

        try
        {
            if (!Enum.TryParse<TagScope>(scope, true, out var tagScope))
            {
                _logger.LogWarning("标签 Scope 参数无效: Scope={Scope}", scope);
                throw new ValidationException($"无效的标签范围: {scope}");
            }

            var cacheKey = $"tags:{scope.ToLower()}:active:{isActive?.ToString().ToLower() ?? "null"}";
            if (_cache.TryGetValue(cacheKey, out List<TagDto>? cachedResult) && cachedResult != null)
            {
                _logger.LogDebug("TagService.GetTagsAsync - 命中缓存: {CacheKey}", cacheKey);
                return cachedResult;
            }

            var query = _tagRepository.GetQueryable()
                .Where(t => t.Scope == tagScope);

            if (isActive.HasValue)
            {
                query = query.Where(t => t.IsActive == isActive.Value);
            }

            var tags = await query
                .OrderBy(t => t.SortOrder)
                .ThenBy(t => t.Name)
                .ToListAsync();

            var result = tags.Select(MapToDto).ToList();

            _cache.Set(cacheKey, result, CacheTtl);

            _logger.LogInformation("获取标签列表成功: Scope={Scope}, 共 {Count} 条记录", scope, result.Count);

            return result;
        }
        catch (ValidationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取标签列表失败: Scope={Scope}", scope);
            throw;
        }
    }

    public async Task<TagDto> GetByIdAsync(long id)
    {
        _logger.LogDebug("TagService.GetByIdAsync - Id={Id}", id);

        try
        {
            var tag = await _tagRepository.GetByIdAsync(id);

            if (tag == null)
            {
                _logger.LogWarning("标签不存在: Id={Id}", id);
                throw new NotFoundException("标签不存在");
            }

            return MapToDto(tag);
        }
        catch (NotFoundException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取标签详情失败: Id={Id}", id);
            throw;
        }
    }

    public async Task<TagDto> CreateAsync(CreateTagRequest request)
    {
        _logger.LogDebug("TagService.CreateAsync - Scope={Scope}, Name={Name}", request.Scope, request.Name);

        try
        {
            if (!Enum.TryParse<TagScope>(request.Scope, true, out var tagScope))
            {
                _logger.LogWarning("标签 Scope 参数无效: Scope={Scope}", request.Scope);
                throw new ValidationException($"无效的标签范围: {request.Scope}");
            }

            // 检查 (scope, name) 唯一性，排除已软删除的
            var exists = await _tagRepository.GetQueryable()
                .AnyAsync(t => t.Scope == tagScope && t.Name == request.Name);

            if (exists)
            {
                _logger.LogWarning("标签名称已存在: Scope={Scope}, Name={Name}", request.Scope, request.Name);
                throw new ValidationException($"该范围下已存在同名标签: {request.Name}");
            }

            var tag = new Tag
            {
                Scope = tagScope,
                Name = request.Name,
                Code = request.Code,
                Color = request.Color,
                SortOrder = request.SortOrder,
                Description = request.Description,
                IsActive = request.IsActive,
                IsSystem = false
            };

            await _tagRepository.AddAsync(tag);
            await _unitOfWork.SaveChangesAsync();

            var dto = MapToDto(tag);

            await _auditLogService.LogAsync("Create", "Tag", tag.Id, null, SerializeForAudit(dto));
            InvalidateScopeCache(request.Scope);

            _logger.LogInformation("创建标签成功: Id={Id}, Scope={Scope}, Name={Name}",
                tag.Id, tag.Scope, tag.Name);

            return dto;
        }
        catch (ValidationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建标签失败: Scope={Scope}, Name={Name}", request.Scope, request.Name);
            throw;
        }
    }

    public async Task<TagDto> UpdateAsync(long id, UpdateTagRequest request)
    {
        _logger.LogDebug("TagService.UpdateAsync - Id={Id}, Name={Name}", id, request.Name);

        try
        {
            var tag = await _tagRepository.GetByIdAsync(id);

            if (tag == null)
            {
                _logger.LogWarning("标签不存在: Id={Id}", id);
                throw new NotFoundException("标签不存在");
            }

            if (tag.IsSystem)
            {
                _logger.LogWarning("尝试修改系统标签: Id={Id}", id);
                throw new ValidationException("系统标签不允许修改");
            }

            var oldDto = MapToDto(tag);

            // 如果改了 Name，检查新 (scope, name) 组合唯一性
            if (tag.Name != request.Name)
            {
                var exists = await _tagRepository.GetQueryable()
                    .AnyAsync(t => t.Scope == tag.Scope && t.Name == request.Name && t.Id != id);

                if (exists)
                {
                    _logger.LogWarning("标签名称已存在: Scope={Scope}, Name={Name}", tag.Scope, request.Name);
                    throw new ValidationException($"该范围下已存在同名标签: {request.Name}");
                }
            }

            tag.Name = request.Name;
            tag.Code = request.Code;
            tag.Color = request.Color;
            tag.SortOrder = request.SortOrder;
            tag.Description = request.Description;
            tag.IsActive = request.IsActive;

            _tagRepository.Update(tag);
            await _unitOfWork.SaveChangesAsync();

            var newDto = MapToDto(tag);

            await _auditLogService.LogAsync("Update", "Tag", tag.Id, SerializeForAudit(oldDto), SerializeForAudit(newDto));
            InvalidateScopeCache(tag.Scope.ToString());

            _logger.LogInformation("更新标签成功: Id={Id}, Name={Name}", id, tag.Name);

            return newDto;
        }
        catch (ValidationException)
        {
            throw;
        }
        catch (NotFoundException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新标签失败: Id={Id}, Name={Name}", id, request.Name);
            throw;
        }
    }

    public async Task DeleteAsync(long id)
    {
        _logger.LogDebug("TagService.DeleteAsync - Id={Id}", id);

        try
        {
            var tag = await _tagRepository.GetByIdAsync(id);

            if (tag == null)
            {
                _logger.LogWarning("标签不存在: Id={Id}", id);
                throw new NotFoundException("标签不存在");
            }

            if (tag.IsSystem)
            {
                _logger.LogWarning("尝试删除系统标签: Id={Id}", id);
                throw new ValidationException("系统标签不允许删除");
            }

            var oldDto = MapToDto(tag);

            // 软删除标签（保留 TagBinding 记录以维护历史可追溯性）
            tag.IsDeleted = true;
            tag.DeletedAt = DateTime.UtcNow;
            _tagRepository.Update(tag);
            await _unitOfWork.SaveChangesAsync();

            await _auditLogService.LogAsync("Delete", "Tag", tag.Id, SerializeForAudit(oldDto), null);
            InvalidateScopeCache(tag.Scope.ToString());

            _logger.LogInformation("软删除标签成功: Id={Id}, Name={Name}（保留历史绑定记录）",
                id, tag.Name);
        }
        catch (ValidationException)
        {
            throw;
        }
        catch (NotFoundException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除标签失败: Id={Id}", id);
            throw;
        }
    }

    /// <summary>
    /// 验证 owner 实体存在且当前用户有权访问
    /// </summary>
    private async Task ValidateOwnerAccessAsync(TagScope scope, long ownerId)
    {
        var exists = scope switch
        {
            TagScope.Transaction => await ApplyPermissionFilter(_transactionRepository.GetQueryable())
                                        .AnyAsync(e => e.Id == ownerId),
            TagScope.Project => await ApplyPermissionFilter(_projectRepository.GetQueryable())
                                    .AnyAsync(e => e.Id == ownerId),
            TagScope.Person => await ApplyPermissionFilter(_personRepository.GetQueryable())
                                   .AnyAsync(e => e.Id == ownerId),
            TagScope.Customer => await ApplyPermissionFilter(_customerRepository.GetQueryable())
                                     .AnyAsync(e => e.Id == ownerId),
            TagScope.Supplier => await ApplyPermissionFilter(_supplierRepository.GetQueryable())
                                     .AnyAsync(e => e.Id == ownerId),
            TagScope.Receivable => await ApplyPermissionFilter(_receivableRepository.GetQueryable())
                                       .AnyAsync(e => e.Id == ownerId),
            TagScope.Payable => await ApplyPermissionFilter(_payableRepository.GetQueryable())
                                    .AnyAsync(e => e.Id == ownerId),
            _ => throw new ValidationException($"不支持的标签范围: {scope}")
        };

        if (!exists)
        {
            throw new NotFoundException($"{scope} Id={ownerId} 不存在或无权访问");
        }
    }

    public async Task<List<TagBindingDto>> GetBindingsAsync(string ownerType, long ownerId)
    {
        _logger.LogDebug("TagService.GetBindingsAsync - OwnerType={OwnerType}, OwnerId={OwnerId}", ownerType, ownerId);

        try
        {
            if (!Enum.TryParse<TagScope>(ownerType, true, out var scope))
            {
                _logger.LogWarning("OwnerType 参数无效: OwnerType={OwnerType}", ownerType);
                throw new ValidationException($"无效的 OwnerType: {ownerType}");
            }

            await ValidateOwnerAccessAsync(scope, ownerId);

            var bindings = await _tagBindingRepository.GetQueryable()
                .IgnoreQueryFilters()
                .Include(b => b.Tag)
                .Where(b => !b.IsDeleted && b.OwnerType == scope && b.OwnerId == ownerId)
                .ToListAsync();

            var result = bindings
                .Select(b =>
                {
                    if (b.Tag == null)
                    {
                        _logger.LogWarning("TagBinding {BindingId} 引用了不存在的 Tag {TagId}", b.Id, b.TagId);
                    }
                    return new TagBindingDto
                    {
                        Id = b.Id,
                        TagId = b.TagId,
                        TagName = b.Tag?.Name ?? $"标签#{b.TagId}",
                        TagColor = b.Tag?.Color,
                        TagIsDeleted = b.Tag?.IsDeleted ?? true,
                        OwnerType = b.OwnerType.ToString().ToLower(),
                        OwnerId = b.OwnerId
                    };
                }).ToList();

            _logger.LogInformation("获取标签绑定成功: OwnerType={OwnerType}, OwnerId={OwnerId}, 共 {Count} 条记录",
                ownerType, ownerId, result.Count);

            return result;
        }
        catch (ValidationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取标签绑定失败: OwnerType={OwnerType}, OwnerId={OwnerId}", ownerType, ownerId);
            throw;
        }
    }

    public async Task SetBindingsAsync(SetBindingsRequest request)
    {
        _logger.LogDebug("TagService.SetBindingsAsync - OwnerType={OwnerType}, OwnerId={OwnerId}, TagIds={TagIds}",
            request.OwnerType, request.OwnerId, string.Join(",", request.TagIds));

        try
        {
            if (!Enum.TryParse<TagScope>(request.OwnerType, true, out var scope))
            {
                _logger.LogWarning("OwnerType 参数无效: OwnerType={OwnerType}", request.OwnerType);
                throw new ValidationException($"无效的 OwnerType: {request.OwnerType}");
            }

            await ValidateOwnerAccessAsync(scope, request.OwnerId);

            // 获取现有绑定
            var existingBindings = await _tagBindingRepository.GetQueryable()
                .Where(b => b.OwnerType == scope && b.OwnerId == request.OwnerId)
                .ToListAsync();

            var existingTagIds = existingBindings.Select(b => b.TagId).ToHashSet();
            var newTagIds = request.TagIds.ToHashSet();

            // 删除不在新 tagIds 中的绑定（软删除）
            var toDelete = existingBindings.Where(b => !newTagIds.Contains(b.TagId)).ToList();
            foreach (var binding in toDelete)
            {
                _tagBindingRepository.Delete(binding);
            }

            // 添加新 tagIds 中不存在的绑定
            var toAddIds = newTagIds.Where(tagId => !existingTagIds.Contains(tagId)).ToList();

            // 批量查询所有需要添加的标签（优化 N+1 查询）
            if (toAddIds.Count > 0)
            {
                var tags = await _tagRepository.GetQueryable()
                    .Where(t => toAddIds.Contains(t.Id))
                    .ToListAsync();

                var tagDict = tags.ToDictionary(t => t.Id);

                foreach (var tagId in toAddIds)
                {
                    // 验证 tag 存在
                    if (!tagDict.TryGetValue(tagId, out var tag))
                    {
                        _logger.LogWarning("标签不存在: TagId={TagId}", tagId);
                        throw new NotFoundException($"标签不存在: Id={tagId}");
                    }

                    // 验证 tag.Scope == ownerType
                    if (tag.Scope != scope)
                    {
                        _logger.LogWarning("标签范围不匹配: TagId={TagId}, TagScope={TagScope}, OwnerType={OwnerType}",
                            tagId, tag.Scope, request.OwnerType);
                        throw new ValidationException($"标签 {tag.Name} 的范围与 OwnerType 不匹配");
                    }

                    var binding = new TagBinding
                    {
                        TagId = tagId,
                        OwnerType = scope,
                        OwnerId = request.OwnerId
                    };
                    await _tagBindingRepository.AddAsync(binding);
                }
            }

            await _unitOfWork.SaveChangesAsync();

            // 审计日志：记录绑定变更（添加/移除的 TagId 列表）
            if (toDelete.Count > 0 || toAddIds.Count > 0)
            {
                var auditBefore = new { TagIds = existingTagIds.OrderBy(x => x).ToList() };
                var auditAfter = new { TagIds = newTagIds.OrderBy(x => x).ToList() };
                await _auditLogService.LogAsync("SetBindings", "TagBinding",
                    request.OwnerId,
                    SerializeForAudit(auditBefore),
                    SerializeForAudit(auditAfter));
            }

            _logger.LogInformation("设置标签绑定成功: OwnerType={OwnerType}, OwnerId={OwnerId}, 删除 {DeleteCount} 个，新增 {AddCount} 个",
                request.OwnerType, request.OwnerId, toDelete.Count, toAddIds.Count);
        }
        catch (ValidationException)
        {
            throw;
        }
        catch (NotFoundException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "设置标签绑定失败: OwnerType={OwnerType}, OwnerId={OwnerId}",
                request.OwnerType, request.OwnerId);
            throw;
        }
    }

    public async Task AddBindingAsync(string ownerType, long ownerId, long tagId)
    {
        _logger.LogDebug("TagService.AddBindingAsync - OwnerType={OwnerType}, OwnerId={OwnerId}, TagId={TagId}",
            ownerType, ownerId, tagId);

        try
        {
            if (!Enum.TryParse<TagScope>(ownerType, true, out var scope))
            {
                _logger.LogWarning("OwnerType 参数无效: OwnerType={OwnerType}", ownerType);
                throw new ValidationException($"无效的 OwnerType: {ownerType}");
            }

            await ValidateOwnerAccessAsync(scope, ownerId);

            // 验证 tag 存在，tag.Scope == ownerType
            var tag = await _tagRepository.GetByIdAsync(tagId);
            if (tag == null)
            {
                _logger.LogWarning("标签不存在: TagId={TagId}", tagId);
                throw new NotFoundException($"标签不存在: Id={tagId}");
            }

            if (tag.Scope != scope)
            {
                _logger.LogWarning("标签范围不匹配: TagId={TagId}, TagScope={TagScope}, OwnerType={OwnerType}",
                    tagId, tag.Scope, ownerType);
                throw new ValidationException($"标签 {tag.Name} 的范围与 OwnerType 不匹配");
            }

            // 检查绑定是否已存在（避免重复）
            var alreadyExists = await _tagBindingRepository.GetQueryable()
                .AnyAsync(b => b.OwnerType == scope && b.OwnerId == ownerId && b.TagId == tagId);

            if (!alreadyExists)
            {
                var binding = new TagBinding
                {
                    TagId = tagId,
                    OwnerType = scope,
                    OwnerId = ownerId
                };
                await _tagBindingRepository.AddAsync(binding);
                await _unitOfWork.SaveChangesAsync();

                await _auditLogService.LogAsync("AddBinding", "TagBinding", ownerId,
                    null, SerializeForAudit(new { OwnerType = ownerType, OwnerId = ownerId, TagId = tagId }));

                _logger.LogInformation("添加标签绑定成功: OwnerType={OwnerType}, OwnerId={OwnerId}, TagId={TagId}",
                    ownerType, ownerId, tagId);
            }
            else
            {
                _logger.LogDebug("标签绑定已存在，跳过: OwnerType={OwnerType}, OwnerId={OwnerId}, TagId={TagId}",
                    ownerType, ownerId, tagId);
            }
        }
        catch (ValidationException)
        {
            throw;
        }
        catch (NotFoundException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "添加标签绑定失败: OwnerType={OwnerType}, OwnerId={OwnerId}, TagId={TagId}",
                ownerType, ownerId, tagId);
            throw;
        }
    }

    public async Task RemoveBindingAsync(string ownerType, long ownerId, long tagId)
    {
        _logger.LogDebug("TagService.RemoveBindingAsync - OwnerType={OwnerType}, OwnerId={OwnerId}, TagId={TagId}",
            ownerType, ownerId, tagId);

        try
        {
            if (!Enum.TryParse<TagScope>(ownerType, true, out var scope))
            {
                _logger.LogWarning("OwnerType 参数无效: OwnerType={OwnerType}", ownerType);
                throw new ValidationException($"无效的 OwnerType: {ownerType}");
            }

            await ValidateOwnerAccessAsync(scope, ownerId);

            var binding = await _tagBindingRepository.GetQueryable()
                .FirstOrDefaultAsync(b => b.OwnerType == scope && b.OwnerId == ownerId && b.TagId == tagId);

            if (binding == null)
            {
                // 幂等：不存在就直接返回
                _logger.LogDebug("标签绑定不存在，幂等返回: OwnerType={OwnerType}, OwnerId={OwnerId}, TagId={TagId}",
                    ownerType, ownerId, tagId);
                return;
            }

            _tagBindingRepository.Delete(binding);
            await _unitOfWork.SaveChangesAsync();

            await _auditLogService.LogAsync("RemoveBinding", "TagBinding", ownerId,
                SerializeForAudit(new { OwnerType = ownerType, OwnerId = ownerId, TagId = tagId }), null);

            _logger.LogInformation("移除标签绑定成功: OwnerType={OwnerType}, OwnerId={OwnerId}, TagId={TagId}",
                ownerType, ownerId, tagId);
        }
        catch (ValidationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "移除标签绑定失败: OwnerType={OwnerType}, OwnerId={OwnerId}, TagId={TagId}",
                ownerType, ownerId, tagId);
            throw;
        }
    }

    public async Task BatchSetBindingsAsync(BatchSetBindingsRequest request)
    {
        _logger.LogDebug("TagService.BatchSetBindingsAsync - OwnerType={OwnerType}, OwnerIds={OwnerIds}",
            request.OwnerType, string.Join(",", request.OwnerIds));

        try
        {
            foreach (var ownerId in request.OwnerIds)
            {
                await SetBindingsAsync(new SetBindingsRequest
                {
                    OwnerType = request.OwnerType,
                    OwnerId = ownerId,
                    TagIds = request.TagIds
                });
            }

            _logger.LogInformation("批量设置标签绑定成功: OwnerType={OwnerType}, 共处理 {Count} 个 Owner",
                request.OwnerType, request.OwnerIds.Count);
        }
        catch (ValidationException)
        {
            throw;
        }
        catch (NotFoundException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "批量设置标签绑定失败: OwnerType={OwnerType}", request.OwnerType);
            throw;
        }
    }

    // ────────────────────────── 私有辅助 ──────────────────────────

    private void InvalidateScopeCache(string scope)
    {
        var scopeLower = scope.ToLower();
        _cache.Remove($"tags:{scopeLower}:active:null");
        _cache.Remove($"tags:{scopeLower}:active:true");
        _cache.Remove($"tags:{scopeLower}:active:false");
    }

    private static TagDto MapToDto(Tag tag) => new TagDto
    {
        Id = tag.Id,
        Scope = tag.Scope.ToString().ToLower(),
        Name = tag.Name,
        Code = tag.Code,
        Color = tag.Color,
        SortOrder = tag.SortOrder,
        Description = tag.Description,
        IsActive = tag.IsActive,
        IsSystem = tag.IsSystem,
        CreatedAt = tag.CreatedAt,
        UpdatedAt = tag.UpdatedAt
    };
}
