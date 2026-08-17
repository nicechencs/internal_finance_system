using MapsterMapper;
using FinanceApp.Application.Common;
using FinanceApp.Application.Modules.Identity.Interfaces;
using FinanceApp.Application.Modules.MasterData.DTOs.Rule;
using FinanceApp.Application.Modules.MasterData.Interfaces;
using FinanceApp.Application.Services;
using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Enums;
using FinanceApp.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace FinanceApp.Application.Modules.MasterData.Services;

public class RuleService : ServiceBase, IRuleService
{
    private readonly IRepository<ClassificationRule> _ruleRepository;
    private readonly IRepository<Category> _categoryRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<RuleService> _logger;
    private readonly IAuditLogService _auditLogService;
    private readonly IUnitOfWork _unitOfWork;

    public RuleService(
        IRepository<ClassificationRule> ruleRepository,
        IRepository<Category> categoryRepository,
        IMapper mapper,
        ILogger<RuleService> logger,
        IAuditLogService auditLogService,
        ICurrentUserService currentUserService,
        IDataPermissionService permissionService,
        IUnitOfWork unitOfWork)
        : base(currentUserService, permissionService)
    {
        _ruleRepository = ruleRepository;
        _categoryRepository = categoryRepository;
        _mapper = mapper;
        _logger = logger;
        _auditLogService = auditLogService;
        _unitOfWork = unitOfWork;
    }

    public async Task<PageResponse<RuleDto>> GetPagedAsync(PageRequest request)
    {
        _logger.LogDebug("RuleService.GetPagedAsync: 获取规则分页列表, Page={Page}, PageSize={PageSize}", request.Page, request.PageSize);

        var sortableFields = SortingHelper.Merge(
            SortingHelper.GetBaseFields<ClassificationRule>(),
            new Dictionary<string, System.Linq.Expressions.Expression<Func<ClassificationRule, object>>>(StringComparer.OrdinalIgnoreCase)
            {
                ["name"] = r => r.RuleName,
                ["priority"] = r => r.Priority,
                ["categoryName"] = r => r.Category != null ? r.Category.Name : string.Empty,
                ["isActive"] = r => r.IsActive,
            });

        var query = _ruleRepository.GetQueryable()
            .Include(r => r.Category)
            .OrderByDescending(r => r.CreatedAt);

        var sortedQuery = SortingHelper.ApplySorting(query, request.SortBy, request.SortOrder, sortableFields);
        var total = await sortedQuery.CountAsync();
        var items = await sortedQuery
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync();

        var dtos = items.Select(r => new RuleDto
        {
            Id = r.Id,
            Name = r.RuleName,
            CategoryId = r.CategoryId ?? 0,
            CategoryName = r.Category?.Name ?? string.Empty,
            MatchField = r.MatchField.ToString(),
            MatchOperator = r.MatchOperator.ToString(),
            MatchValue = r.MatchValue,
            MatchValueMax = r.MatchValueMax,
            Priority = r.Priority,
            IsActive = r.IsActive,
            CreatedAt = r.CreatedAt
        }).ToList();

        _logger.LogInformation("获取规则分页列表成功, 返回 {Count} 条记录, 总计 {Total} 条", dtos.Count, total);

        return new PageResponse<RuleDto>
        {
            Items = dtos,
            Page = request.Page,
            PageSize = request.PageSize,
            Total = total
        };
    }

    public async Task<RuleDto> GetByIdAsync(long id)
    {
        _logger.LogDebug("RuleService.GetByIdAsync: 获取规则详情, RuleId={RuleId}", id);

        var rule = await _ruleRepository.GetByIdAsync(id);
        if (rule == null)
        {
            _logger.LogWarning("获取规则详情失败, 规则不存在, RuleId={RuleId}", id);
            throw new NotFoundException($"Rule with ID {id} not found");
        }

        // 检查访问权限
        EnsureCanAccess(rule);

        _logger.LogInformation("获取规则详情成功, RuleId={RuleId}, RuleName={RuleName}", id, rule.RuleName);

        return new RuleDto
        {
            Id = rule.Id,
            Name = rule.RuleName,
            CategoryId = rule.CategoryId ?? 0,
            CategoryName = rule.Category?.Name ?? string.Empty,
            MatchField = rule.MatchField.ToString(),
            MatchOperator = rule.MatchOperator.ToString(),
            MatchValue = rule.MatchValue,
            MatchValueMax = rule.MatchValueMax,
            Priority = rule.Priority,
            IsActive = rule.IsActive,
            CreatedAt = rule.CreatedAt
        };
    }

    public async Task<RuleDto> CreateAsync(CreateRuleRequest request)
    {
        _logger.LogDebug("RuleService.CreateAsync: 开始创建规则, 名称={Name}, 分类ID={CategoryId}, 匹配字段={MatchField}, 匹配操作符={MatchOperator}, 优先级={Priority}",
            request.Name, request.CategoryId, request.MatchField, request.MatchOperator, request.Priority);

        // Validate category exists
        var categoryExists = await _categoryRepository.ExistsAsync(request.CategoryId);
        if (!categoryExists)
        {
            _logger.LogWarning("创建规则失败, 分类不存在, CategoryId={CategoryId}", request.CategoryId);
            throw new ValidationException($"Category with ID {request.CategoryId} not found");
        }

        // Validate match field
        if (!Enum.TryParse<RuleMatchField>(request.MatchField, true, out var matchField))
        {
            _logger.LogWarning("创建规则失败, 无效的匹配字段, MatchField={MatchField}", request.MatchField);
            throw new ValidationException($"Invalid match field: {request.MatchField}");
        }

        // Validate match operator
        if (!Enum.TryParse<RuleMatchOperator>(request.MatchOperator, true, out var matchOperator))
        {
            _logger.LogWarning("创建规则失败, 无效的匹配操作符, MatchOperator={MatchOperator}", request.MatchOperator);
            throw new ValidationException($"Invalid match operator: {request.MatchOperator}");
        }

        // Validate regex if operator is Regex
        if (matchOperator == RuleMatchOperator.Regex)
        {
            try
            {
                _ = new Regex(request.MatchValue);
                _logger.LogDebug("正则表达式验证通过, Pattern={Pattern}", request.MatchValue);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning("创建规则失败, 正则表达式验证失败, Pattern={Pattern}, Error={Error}",
                    request.MatchValue, ex.Message);
                throw new ValidationException($"Invalid regular expression: {request.MatchValue}");
            }
        }

        // 校验字段/操作符组合合法性，以及 Range 的数值与区间
        try
        {
            RuleMatchingHelper.ValidateFieldOperatorCombination(
                matchField, matchOperator, request.MatchValue, request.MatchValueMax);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("创建规则失败, 组合校验不通过, Error={Error}", ex.Message);
            throw new ValidationException(ex.Message);
        }

        var rule = new ClassificationRule
        {
            RuleName = request.Name,
            CategoryId = request.CategoryId,
            MatchField = matchField,
            MatchOperator = matchOperator,
            MatchValue = request.MatchValue,
            MatchValueMax = matchOperator == RuleMatchOperator.Range
                ? (string.IsNullOrWhiteSpace(request.MatchValueMax) ? null : request.MatchValueMax)
                : null,
            Priority = request.Priority,
            IsActive = true
        };

        var created = await _ruleRepository.AddAsync(rule);
        await _unitOfWork.SaveChangesAsync();
        var category = await _categoryRepository.GetByIdAsync(request.CategoryId);

        var dto = new RuleDto
        {
            Id = created.Id,
            Name = created.RuleName,
            CategoryId = created.CategoryId ?? 0,
            CategoryName = category?.Name ?? string.Empty,
            MatchField = created.MatchField.ToString(),
            MatchOperator = created.MatchOperator.ToString(),
            MatchValue = created.MatchValue,
            MatchValueMax = created.MatchValueMax,
            Priority = created.Priority,
            IsActive = created.IsActive,
            CreatedAt = created.CreatedAt
        };

        await _auditLogService.LogAsync("Create", "ClassificationRule", created.Id, null, SerializeForAudit(dto));
        _logger.LogInformation("创建规则成功, RuleId={RuleId}, RuleName={RuleName}, CategoryId={CategoryId}, MatchField={MatchField}, MatchOperator={MatchOperator}, Priority={Priority}",
            created.Id, created.RuleName, created.CategoryId, matchField, matchOperator, created.Priority);

        return dto;
    }

    public async Task<RuleDto> UpdateAsync(long id, UpdateRuleRequest request)
    {
        _logger.LogDebug("RuleService.UpdateAsync: 开始更新规则, RuleId={RuleId}, 名称={Name}, 分类ID={CategoryId}, 匹配字段={MatchField}, 匹配操作符={MatchOperator}, 优先级={Priority}, 是否启用={IsActive}",
            id, request.Name, request.CategoryId, request.MatchField, request.MatchOperator, request.Priority, request.IsActive);

        var rule = await _ruleRepository.GetByIdAsync(id);
        if (rule == null)
        {
            _logger.LogWarning("更新规则失败, 规则不存在, RuleId={RuleId}", id);
            throw new NotFoundException($"Rule with ID {id} not found");
        }

        // 检查编辑权限
        EnsureCanEdit(rule);

        var oldDto = new RuleDto
        {
            Id = rule.Id,
            Name = rule.RuleName,
            CategoryId = rule.CategoryId ?? 0,
            CategoryName = rule.Category?.Name ?? string.Empty,
            MatchField = rule.MatchField.ToString(),
            MatchOperator = rule.MatchOperator.ToString(),
            MatchValue = rule.MatchValue,
            MatchValueMax = rule.MatchValueMax,
            Priority = rule.Priority,
            IsActive = rule.IsActive,
            CreatedAt = rule.CreatedAt
        };

        // Validate category exists
        var categoryExists = await _categoryRepository.ExistsAsync(request.CategoryId);
        if (!categoryExists)
        {
            _logger.LogWarning("更新规则失败, 分类不存在, RuleId={RuleId}, CategoryId={CategoryId}", id, request.CategoryId);
            throw new ValidationException($"Category with ID {request.CategoryId} not found");
        }

        // Validate match field
        if (!Enum.TryParse<RuleMatchField>(request.MatchField, true, out var matchField))
        {
            _logger.LogWarning("更新规则失败, 无效的匹配字段, RuleId={RuleId}, MatchField={MatchField}", id, request.MatchField);
            throw new ValidationException($"Invalid match field: {request.MatchField}");
        }

        // Validate match operator
        if (!Enum.TryParse<RuleMatchOperator>(request.MatchOperator, true, out var matchOperator))
        {
            _logger.LogWarning("更新规则失败, 无效的匹配操作符, RuleId={RuleId}, MatchOperator={MatchOperator}", id, request.MatchOperator);
            throw new ValidationException($"Invalid match operator: {request.MatchOperator}");
        }

        // Validate regex if operator is Regex
        if (matchOperator == RuleMatchOperator.Regex)
        {
            try
            {
                _ = new Regex(request.MatchValue);
                _logger.LogDebug("正则表达式验证通过, RuleId={RuleId}, Pattern={Pattern}", id, request.MatchValue);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning("更新规则失败, 正则表达式验证失败, RuleId={RuleId}, Pattern={Pattern}, Error={Error}",
                    id, request.MatchValue, ex.Message);
                throw new ValidationException($"Invalid regular expression: {request.MatchValue}");
            }
        }

        // 校验字段/操作符组合合法性，以及 Range 的数值与区间
        try
        {
            RuleMatchingHelper.ValidateFieldOperatorCombination(
                matchField, matchOperator, request.MatchValue, request.MatchValueMax);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("更新规则失败, 组合校验不通过, RuleId={RuleId}, Error={Error}", id, ex.Message);
            throw new ValidationException(ex.Message);
        }

        rule.RuleName = request.Name;
        rule.CategoryId = request.CategoryId;
        rule.MatchField = matchField;
        rule.MatchOperator = matchOperator;
        rule.MatchValue = request.MatchValue;
        rule.MatchValueMax = matchOperator == RuleMatchOperator.Range
            ? (string.IsNullOrWhiteSpace(request.MatchValueMax) ? null : request.MatchValueMax)
            : null;
        rule.Priority = request.Priority;
        rule.IsActive = request.IsActive;

        _ruleRepository.Update(rule);
        await _unitOfWork.SaveChangesAsync();
        var category = await _categoryRepository.GetByIdAsync(request.CategoryId);

        var newDto = new RuleDto
        {
            Id = rule.Id,
            Name = rule.RuleName,
            CategoryId = rule.CategoryId ?? 0,
            CategoryName = category?.Name ?? string.Empty,
            MatchField = rule.MatchField.ToString(),
            MatchOperator = rule.MatchOperator.ToString(),
            MatchValue = rule.MatchValue,
            MatchValueMax = rule.MatchValueMax,
            Priority = rule.Priority,
            IsActive = rule.IsActive,
            CreatedAt = rule.CreatedAt
        };

        await _auditLogService.LogAsync("Update", "ClassificationRule", rule.Id, SerializeForAudit(oldDto), SerializeForAudit(newDto));
        _logger.LogInformation("更新规则成功, RuleId={RuleId}, RuleName={RuleName}, CategoryId={CategoryId}, MatchField={MatchField}, MatchOperator={MatchOperator}, Priority={Priority}, IsActive={IsActive}",
            rule.Id, rule.RuleName, rule.CategoryId, matchField, matchOperator, rule.Priority, rule.IsActive);

        return newDto;
    }

    public async Task DeleteAsync(long id)
    {
        _logger.LogDebug("RuleService.DeleteAsync: 开始删除规则, RuleId={RuleId}", id);

        var rule = await _ruleRepository.GetByIdAsync(id);
        if (rule == null)
        {
            _logger.LogWarning("删除规则失败, 规则不存在, RuleId={RuleId}", id);
            throw new NotFoundException($"Rule with ID {id} not found");
        }

        // 检查删除权限
        EnsureCanDelete(rule);

        var oldDto = new RuleDto
        {
            Id = rule.Id,
            Name = rule.RuleName,
            CategoryId = rule.CategoryId ?? 0,
            CategoryName = rule.Category?.Name ?? string.Empty,
            MatchField = rule.MatchField.ToString(),
            MatchOperator = rule.MatchOperator.ToString(),
            MatchValue = rule.MatchValue,
            MatchValueMax = rule.MatchValueMax,
            Priority = rule.Priority,
            IsActive = rule.IsActive,
            CreatedAt = rule.CreatedAt
        };

        _ruleRepository.Delete(rule);
        await _unitOfWork.SaveChangesAsync();

        await _auditLogService.LogAsync("Delete", "ClassificationRule", id, SerializeForAudit(oldDto), null);
        _logger.LogInformation("删除规则成功, RuleId={RuleId}", id);
    }

    public async Task<List<RuleDto>> GetActiveRulesAsync()
    {
        _logger.LogDebug("RuleService.GetActiveRulesAsync: 获取所有活跃规则");

        var rules = await _ruleRepository.GetAllAsync();
        var activeRules = rules.Where(r => r.IsActive)
                               .OrderByDescending(r => r.Priority)
                               .ThenBy(r => r.Id)
                               .ToList();

        _logger.LogInformation("获取活跃规则成功, 共 {Count} 条活跃规则", activeRules.Count);

        return activeRules.Select(r => new RuleDto
        {
            Id = r.Id,
            Name = r.RuleName,
            CategoryId = r.CategoryId ?? 0,
            CategoryName = r.Category?.Name ?? string.Empty,
            MatchField = r.MatchField.ToString(),
            MatchOperator = r.MatchOperator.ToString(),
            MatchValue = r.MatchValue,
            MatchValueMax = r.MatchValueMax,
            Priority = r.Priority,
            IsActive = r.IsActive,
            CreatedAt = r.CreatedAt
        }).ToList();
    }

    public async Task<long?> MatchCategoryAsync(string counterpartyName, string description, decimal amount, string? memo = null)
    {
        _logger.LogDebug("RuleService.MatchCategoryAsync: 开始规则匹配, 描述={Description}, 对方={Counterparty}, 金额={Amount}, 摘要={Memo}",
            description, counterpartyName, amount, memo);

        var rules = await _ruleRepository.GetAllAsync();
        var activeRules = rules.Where(r => r.IsActive && r.CategoryId.HasValue)
                               .OrderByDescending(r => r.Priority)
                               .ThenBy(r => r.Id)
                               .ToList();

        _logger.LogDebug("获取到 {Count} 条活跃规则", activeRules.Count);

        var input = new RuleMatchInput(
            Counterparty: counterpartyName ?? string.Empty,
            Description: description ?? string.Empty,
            Amount: amount,
            Memo: memo);

        foreach (var rule in activeRules)
        {
            _logger.LogDebug("测试规则, RuleId={RuleId}, RuleName={RuleName}, MatchField={MatchField}, MatchOperator={MatchOperator}, MatchValue={MatchValue}",
                rule.Id, rule.RuleName, rule.MatchField, rule.MatchOperator, rule.MatchValue);

            bool isMatch = RuleMatchingHelper.Match(
                rule.MatchField, rule.MatchOperator, rule.MatchValue, rule.MatchValueMax,
                input, _logger, rule.Id);

            if (isMatch)
            {
                _logger.LogInformation("规则匹配成功, RuleId={RuleId}, RuleName={RuleName}, CategoryId={CategoryId}, Priority={Priority}",
                    rule.Id, rule.RuleName, rule.CategoryId, rule.Priority);
                return rule.CategoryId;
            }
            else
            {
                _logger.LogDebug("规则不匹配, RuleId={RuleId}, RuleName={RuleName}", rule.Id, rule.RuleName);
            }
        }

        _logger.LogInformation("未找到匹配的规则");
        return null;
    }

    public async Task<List<long?>> MatchCategoriesBatchAsync(List<(string CounterpartyName, string Description, decimal Amount, string? Memo)> items)
    {
        _logger.LogDebug("RuleService.MatchCategoriesBatchAsync: 批量匹配 {Count} 条记录", items.Count);

        // 一次加载全量活跃规则
        var rules = await _ruleRepository.GetAllAsync();
        var activeRules = rules.Where(r => r.IsActive && r.CategoryId.HasValue)
                               .OrderByDescending(r => r.Priority)
                               .ThenBy(r => r.Id)
                               .ToList();

        var results = new List<long?>(items.Count);
        foreach (var (counterpartyName, description, amount, memo) in items)
        {
            var input = new RuleMatchInput(
                Counterparty: counterpartyName ?? string.Empty,
                Description: description ?? string.Empty,
                Amount: amount,
                Memo: memo);

            long? matchedCategoryId = null;
            foreach (var rule in activeRules)
            {
                bool isMatch = RuleMatchingHelper.Match(
                    rule.MatchField, rule.MatchOperator, rule.MatchValue, rule.MatchValueMax,
                    input, _logger, rule.Id);

                if (isMatch)
                {
                    matchedCategoryId = rule.CategoryId;
                    break;
                }
            }
            results.Add(matchedCategoryId);
        }

        _logger.LogInformation("批量匹配完成: 总数={Total}, 匹配成功={Matched}",
            items.Count, results.Count(r => r.HasValue));
        return results;
    }

}

