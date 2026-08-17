using MapsterMapper;
using FinanceApp.Application.Common;
using FinanceApp.Application.Modules.Identity.Interfaces;
using FinanceApp.Application.Modules.MasterData.DTOs.Category;
using FinanceApp.Application.Modules.MasterData.Interfaces;
using FinanceApp.Application.Services;
using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Enums;
using FinanceApp.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FinanceApp.Application.Modules.MasterData.Services;

public class CategoryService : ServiceBase, ICategoryService
{
    private readonly IRepository<Category> _categoryRepository;
    private readonly IRepository<ClassificationRule> _ruleRepository;
    private readonly IMasterDataReferenceGuard _referenceGuard;
    private readonly IMapper _mapper;
    private readonly ILogger<CategoryService> _logger;
    private readonly IAuditLogService _auditLogService;
    private readonly IUnitOfWork _unitOfWork;

    public CategoryService(
        IRepository<Category> categoryRepository,
        IRepository<ClassificationRule> ruleRepository,
        IMasterDataReferenceGuard referenceGuard,
        IMapper mapper,
        ILogger<CategoryService> logger,
        IAuditLogService auditLogService,
        ICurrentUserService currentUserService,
        IDataPermissionService permissionService,
        IUnitOfWork unitOfWork)
        : base(currentUserService, permissionService)
    {
        _categoryRepository = categoryRepository;
        _ruleRepository = ruleRepository;
        _referenceGuard = referenceGuard;
        _mapper = mapper;
        _logger = logger;
        _auditLogService = auditLogService;
        _unitOfWork = unitOfWork;
    }

    public async Task<PageResponse<CategoryDto>> GetPagedAsync(PageRequest request)
    {
        _logger.LogDebug("CategoryService.GetPagedAsync - Page={Page}, PageSize={PageSize}",
            request.Page, request.PageSize);

        try
        {
            var baseQuery = _categoryRepository.GetQueryable()
                .Include(c => c.Parent);

            // 应用权限过滤
            var query = ApplyPermissionFilter(baseQuery);

            // 按名称筛选
            if (!string.IsNullOrWhiteSpace(request.Name))
            {
                query = query.Where(c => c.Name.Contains(request.Name));
            }

            // 按分类类型筛选
            if (!string.IsNullOrWhiteSpace(request.CategoryType))
            {
                if (Enum.TryParse<CategoryType>(request.CategoryType, true, out var categoryType))
                {
                    query = query.Where(c => c.CategoryType == categoryType);
                }
            }

            IQueryable<Category> orderedQuery = query.OrderByDescending(c => c.CreatedAt);

            // 应用自定义排序
            var sortableFields = new Dictionary<string, System.Linq.Expressions.Expression<Func<Category, object>>>(StringComparer.OrdinalIgnoreCase)
            {
                ["name"] = c => c.Name,
                ["categoryType"] = c => c.CategoryType,
                ["sortOrder"] = c => c.SortOrder,
                ["createdAt"] = c => c.CreatedAt,
                ["isActive"] = c => c.IsActive
            };
            orderedQuery = SortingHelper.ApplySorting(orderedQuery, request.SortBy, request.SortOrder, sortableFields);

            var total = await orderedQuery.CountAsync();
            var items = await orderedQuery
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync();

            var dtos = items.Select(c => new CategoryDto
            {
                Id = c.Id,
                Name = c.Name,
                CategoryType = c.CategoryType.ToString(),
                ParentId = c.ParentId,
                ParentName = c.Parent?.Name,
                Description = c.Description,
                IsActive = c.IsActive,
                CreatedAt = c.CreatedAt
            }).ToList();

            _logger.LogInformation("分类分页查询成功: Total={Total}, ItemCount={ItemCount}",
                total, dtos.Count);

            return new PageResponse<CategoryDto>
            {
                Items = dtos,
                Page = request.Page,
                PageSize = request.PageSize,
                Total = total
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取分类分页列表失败");
            throw;
        }
    }

    public async Task<CategoryDto> GetByIdAsync(long id)
    {
        _logger.LogDebug("CategoryService.GetByIdAsync - Id={Id}", id);

        try
        {
            var category = await _categoryRepository.GetQueryable()
                .Include(c => c.Parent)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (category == null)
            {
                _logger.LogWarning("分类不存在: Id={Id}", id);
                throw new NotFoundException("分类不存在");
            }

            // 检查访问权限
            EnsureCanAccess(category);

            return new CategoryDto
            {
                Id = category.Id,
                Name = category.Name,
                CategoryType = category.CategoryType.ToString(),
                ParentId = category.ParentId,
                ParentName = category.Parent?.Name,
                Description = category.Description,
                IsActive = category.IsActive,
                CreatedAt = category.CreatedAt
            };
        }
        catch (NotFoundException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取分类详情失败: Id={Id}", id);
            throw;
        }
    }

    public async Task<CategoryDto> CreateAsync(CreateCategoryRequest request)
    {
        _logger.LogDebug("CategoryService.CreateAsync - Name={Name}, Type={Type}, ParentId={ParentId}",
            request.Name, request.CategoryType, request.ParentId);

        try
        {
            if (!Enum.TryParse<CategoryType>(request.CategoryType, true, out var categoryType))
            {
                _logger.LogWarning("分类类型验证失败: Type={Type}", request.CategoryType);
                throw new ValidationException($"Invalid category type: {request.CategoryType}");
            }

            if (request.ParentId.HasValue)
            {
                var parent = await _categoryRepository.GetByIdAsync(request.ParentId.Value);
                if (parent == null)
                {
                    _logger.LogWarning("父分类不存在: ParentId={ParentId}", request.ParentId);
                    throw new NotFoundException("父分类不存在");
                }
            }

            var category = new Category
            {
                Name = request.Name,
                CategoryType = categoryType,
                ParentId = request.ParentId,
                Description = request.Description,
                IsActive = true
            };

            await _categoryRepository.AddAsync(category);
            await _unitOfWork.SaveChangesAsync();

            var dto = new CategoryDto
            {
                Id = category.Id,
                Name = category.Name,
                CategoryType = category.CategoryType.ToString(),
                ParentId = category.ParentId,
                Description = category.Description,
                IsActive = category.IsActive,
                CreatedAt = category.CreatedAt
            };

            await _auditLogService.LogAsync("Create", "Category", category.Id, null, SerializeForAudit(dto));

            _logger.LogInformation("创建分类成功: Id={Id}, Name={Name}, Type={Type}",
                category.Id, category.Name, category.CategoryType);

            return dto;
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
            _logger.LogError(ex, "创建分类失败: Name={Name}, Type={Type}", request.Name, request.CategoryType);
            throw;
        }
    }

    public async Task<CategoryDto> UpdateAsync(long id, UpdateCategoryRequest request)
    {
        _logger.LogDebug("CategoryService.UpdateAsync - Id={Id}, Name={Name}", id, request.Name);

        try
        {
            var category = await _categoryRepository.GetByIdAsync(id);
            if (category == null)
            {
                _logger.LogWarning("分类不存在: Id={Id}", id);
                throw new NotFoundException("分类不存在");
            }

            // 检查编辑权限
            EnsureCanEdit(category);

            var oldDto = new CategoryDto
            {
                Id = category.Id,
                Name = category.Name,
                CategoryType = category.CategoryType.ToString(),
                ParentId = category.ParentId,
                Description = category.Description,
                IsActive = category.IsActive,
                CreatedAt = category.CreatedAt
            };

            category.Name = request.Name;
            category.Description = request.Description;
            category.ParentId = request.ParentId;
            category.IsActive = request.IsActive;

            _categoryRepository.Update(category);
            await _unitOfWork.SaveChangesAsync();

            var newDto = new CategoryDto
            {
                Id = category.Id,
                Name = category.Name,
                CategoryType = category.CategoryType.ToString(),
                ParentId = category.ParentId,
                Description = category.Description,
                IsActive = category.IsActive,
                CreatedAt = category.CreatedAt
            };

            await _auditLogService.LogAsync("Update", "Category", category.Id, SerializeForAudit(oldDto), SerializeForAudit(newDto));

            _logger.LogInformation("更新分类成功: Id={Id}, Name={Name}", id, category.Name);

            return newDto;
        }
        catch (NotFoundException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新分类失败: Id={Id}, Name={Name}", id, request.Name);
            throw;
        }
    }

    public async Task DeleteAsync(long id)
    {
        _logger.LogDebug("CategoryService.DeleteAsync - Id={Id}", id);

        var category = await _categoryRepository.GetByIdAsync(id);
        if (category == null)
        {
            _logger.LogWarning("分类不存在: Id={Id}", id);
            throw new NotFoundException("分类不存在");
        }

        EnsureCanDelete(category);

        var oldDto = new CategoryDto
        {
            Id = category.Id,
            Name = category.Name,
            CategoryType = category.CategoryType.ToString(),
            ParentId = category.ParentId,
            Description = category.Description,
            IsActive = category.IsActive,
            CreatedAt = category.CreatedAt
        };

        if (await _referenceGuard.HasCategoryReferencesAsync(id))
        {
            var activeRules = await _ruleRepository.GetQueryable()
                .Where(r => r.CategoryId == id && r.IsActive)
                .ToListAsync();

            if (!category.IsActive)
            {
                if (activeRules.Count > 0)
                {
                    foreach (var rule in activeRules)
                    {
                        rule.IsActive = false;
                        _ruleRepository.Update(rule);
                    }
                    await _unitOfWork.SaveChangesAsync();
                    _logger.LogInformation("分类已停用，同时停用了 {Count} 条关联规则: CategoryId={Id}", activeRules.Count, id);
                }
                else
                {
                    _logger.LogInformation("分类已停用且无活跃规则，跳过删除: CategoryId={Id}", id);
                }
                return;
            }

            category.IsActive = false;
            _categoryRepository.Update(category);

            foreach (var rule in activeRules)
            {
                rule.IsActive = false;
                _ruleRepository.Update(rule);
            }

            await _unitOfWork.SaveChangesAsync();
            await _auditLogService.LogAsync("Archive", "Category", id, null, null);
            if (activeRules.Count > 0)
            {
                _logger.LogInformation("分类存在历史引用，删除改为停用，同时停用了 {Count} 条关联规则: CategoryId={Id}", activeRules.Count, id);
            }
            else
            {
                _logger.LogInformation("分类存在历史引用，删除改为停用: CategoryId={Id}", id);
            }
            return;
        }

        _categoryRepository.Delete(category);
        await _unitOfWork.SaveChangesAsync();

        await _auditLogService.LogAsync("Delete", "Category", category.Id, SerializeForAudit(oldDto), null);

        _logger.LogInformation("删除分类成功: Id={Id}, Name={Name}", id, category.Name);
    }

    public async Task<List<CategoryDto>> GetActiveCategoriesAsync()
    {
        _logger.LogDebug("CategoryService.GetActiveCategoriesAsync");

        try
        {
            var categories = await _categoryRepository.GetQueryable()
                .Include(c => c.Parent)
                .Where(c => c.IsActive)
                .OrderBy(c => c.Name)
                .ToListAsync();

            var result = categories.Select(c => new CategoryDto
            {
                Id = c.Id,
                Name = c.Name,
                CategoryType = c.CategoryType.ToString(),
                ParentId = c.ParentId,
                ParentName = c.Parent?.Name,
                Description = c.Description,
                IsActive = c.IsActive,
                CreatedAt = c.CreatedAt
            }).ToList();

            _logger.LogInformation("获取活跃分类成功: 共 {Count} 条记录", result.Count);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取活跃分类失败");
            throw;
        }
    }

    public async Task<CategoryStatisticsDto> GetStatisticsAsync()
    {
        _logger.LogDebug("CategoryService.GetStatisticsAsync");

        try
        {
            var query = ApplyPermissionFilter(_categoryRepository.GetQueryable());
            var categories = await query.ToListAsync();

            var result = new CategoryStatisticsDto
            {
                TotalCount = categories.Count,
                IncomeCategoryCount = categories.Count(c => c.CategoryType == CategoryType.Income),
                ExpenseCategoryCount = categories.Count(c => c.CategoryType == CategoryType.Expense),
                ActiveCount = categories.Count(c => c.IsActive)
            };

            _logger.LogInformation("获取分类统计成功: Total={Total}, Income={Income}, Expense={Expense}, Active={Active}",
                result.TotalCount, result.IncomeCategoryCount, result.ExpenseCategoryCount, result.ActiveCount);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取分类统计失败");
            throw;
        }
    }

    public async Task<CategoryStatisticsDto> GetStatisticsAsync(PageRequest request)
    {
        _logger.LogDebug("CategoryService.GetStatisticsAsync with filters");

        try
        {
            var query = ApplyPermissionFilter(_categoryRepository.GetQueryable());

            if (!string.IsNullOrWhiteSpace(request.CategoryType))
            {
                if (Enum.TryParse<CategoryType>(request.CategoryType, true, out var categoryType))
                    query = query.Where(c => c.CategoryType == categoryType);
            }

            var categories = await query.ToListAsync();

            return new CategoryStatisticsDto
            {
                TotalCount = categories.Count,
                IncomeCategoryCount = categories.Count(c => c.CategoryType == CategoryType.Income),
                ExpenseCategoryCount = categories.Count(c => c.CategoryType == CategoryType.Expense),
                ActiveCount = categories.Count(c => c.IsActive)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取分类统计失败");
            throw;
        }
    }

    public async Task<List<CategoryDto>> GetCategoriesByTypeAsync(string type)
    {
        _logger.LogDebug("CategoryService.GetCategoriesByTypeAsync - Type={Type}", type);

        try
        {
            if (!Enum.TryParse<CategoryType>(type, true, out var categoryType))
            {
                _logger.LogWarning("分类类型验证失败: Type={Type}", type);
                throw new ValidationException($"Invalid category type: {type}");
            }

            var categories = await _categoryRepository.GetAllAsync();
            var filteredCategories = categories.Where(c => c.CategoryType == categoryType).ToList();

            var result = filteredCategories.Select(c => new CategoryDto
            {
                Id = c.Id,
                Name = c.Name,
                CategoryType = c.CategoryType.ToString(),
                ParentId = c.ParentId,
                ParentName = c.Parent?.Name,
                Description = c.Description,
                IsActive = c.IsActive,
                CreatedAt = c.CreatedAt
            }).ToList();

            _logger.LogInformation("按类型获取分类成功: Type={Type}, 共 {Count} 条记录", type, result.Count);

            return result;
        }
        catch (ValidationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "按类型获取分类失败: Type={Type}", type);
            throw;
        }
    }
}
