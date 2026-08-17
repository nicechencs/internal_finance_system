using FinanceApp.Application.Common;
using FinanceApp.Application.Modules.Identity.Interfaces;
using FinanceApp.Application.Modules.MasterData.DTOs.TagRule;
using FinanceApp.Application.Modules.MasterData.Interfaces;
using FinanceApp.Application.Services;
using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Enums;
using FinanceApp.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace FinanceApp.Application.Modules.MasterData.Services;

public class TagRuleService : ServiceBase, ITagRuleService
{
    private readonly IRepository<TagRule> _tagRuleRepository;
    private readonly IRepository<Tag> _tagRepository;
    private readonly IRepository<TagBinding> _tagBindingRepository;
    private readonly IRepository<Transaction> _transactionRepository;
    private readonly ILogger<TagRuleService> _logger;
    private readonly IAuditLogService _auditLogService;
    private readonly IUnitOfWork _unitOfWork;

    public TagRuleService(
        IRepository<TagRule> tagRuleRepository,
        IRepository<Tag> tagRepository,
        IRepository<TagBinding> tagBindingRepository,
        IRepository<Transaction> transactionRepository,
        ILogger<TagRuleService> logger,
        IAuditLogService auditLogService,
        ICurrentUserService currentUserService,
        IDataPermissionService permissionService,
        IUnitOfWork unitOfWork)
        : base(currentUserService, permissionService)
    {
        _tagRuleRepository = tagRuleRepository;
        _tagRepository = tagRepository;
        _tagBindingRepository = tagBindingRepository;
        _transactionRepository = transactionRepository;
        _logger = logger;
        _auditLogService = auditLogService;
        _unitOfWork = unitOfWork;
    }

    public async Task<PageResponse<TagRuleDto>> GetPagedAsync(PageRequest request)
    {
        _logger.LogDebug("TagRuleService.GetPagedAsync: 获取标签规则分页列表, Page={Page}, PageSize={PageSize}", request.Page, request.PageSize);

        var sortableFields = SortingHelper.Merge(
            SortingHelper.GetBaseFields<TagRule>(),
            new Dictionary<string, System.Linq.Expressions.Expression<Func<TagRule, object>>>(StringComparer.OrdinalIgnoreCase)
            {
                ["ruleName"] = r => r.RuleName,
                ["priority"] = r => r.Priority,
                ["targetScope"] = r => r.TargetScope,
                ["isActive"] = r => r.IsActive,
            });

        var query = _tagRuleRepository.GetQueryable()
            .Include(r => r.TagRuleTags).ThenInclude(rt => rt.Tag)
            .OrderByDescending(r => r.CreatedAt);

        var sortedQuery = SortingHelper.ApplySorting(query, request.SortBy, request.SortOrder, sortableFields);
        var total = await sortedQuery.CountAsync();
        var items = await sortedQuery
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync();

        var dtos = items.Select(MapToDto).ToList();

        _logger.LogInformation("获取标签规则分页列表成功, 返回 {Count} 条记录, 总计 {Total} 条", dtos.Count, total);

        return new PageResponse<TagRuleDto>
        {
            Items = dtos,
            Page = request.Page,
            PageSize = request.PageSize,
            Total = total
        };
    }

    public async Task<TagRuleDto> GetByIdAsync(long id)
    {
        _logger.LogDebug("TagRuleService.GetByIdAsync: 获取标签规则详情, TagRuleId={TagRuleId}", id);

        var rule = await _tagRuleRepository.GetQueryable()
            .Include(r => r.TagRuleTags).ThenInclude(rt => rt.Tag)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (rule == null)
        {
            _logger.LogWarning("获取标签规则详情失败, 规则不存在, TagRuleId={TagRuleId}", id);
            throw new NotFoundException($"TagRule with ID {id} not found");
        }

        EnsureCanAccess(rule);

        _logger.LogInformation("获取标签规则详情成功, TagRuleId={TagRuleId}, RuleName={RuleName}", id, rule.RuleName);

        return MapToDto(rule);
    }

    public async Task<TagRuleDto> CreateAsync(CreateTagRuleRequest request)
    {
        _logger.LogDebug("TagRuleService.CreateAsync: 开始创建标签规则, RuleName={RuleName}, TargetScope={TargetScope}, MatchField={MatchField}, MatchOperator={MatchOperator}, Priority={Priority}",
            request.RuleName, request.TargetScope, request.MatchField, request.MatchOperator, request.Priority);

        // Validate TargetScope
        if (!Enum.TryParse<TagScope>(request.TargetScope, true, out var targetScope))
        {
            _logger.LogWarning("创建标签规则失败, 无效的 TargetScope, TargetScope={TargetScope}", request.TargetScope);
            throw new ValidationException($"Invalid target scope: {request.TargetScope}");
        }

        // Validate MatchField
        if (!Enum.TryParse<RuleMatchField>(request.MatchField, true, out var matchField))
        {
            _logger.LogWarning("创建标签规则失败, 无效的匹配字段, MatchField={MatchField}", request.MatchField);
            throw new ValidationException($"Invalid match field: {request.MatchField}");
        }

        // Validate MatchOperator
        if (!Enum.TryParse<RuleMatchOperator>(request.MatchOperator, true, out var matchOperator))
        {
            _logger.LogWarning("创建标签规则失败, 无效的匹配操作符, MatchOperator={MatchOperator}", request.MatchOperator);
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
                _logger.LogWarning("创建标签规则失败, 正则表达式验证失败, Pattern={Pattern}, Error={Error}",
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
            _logger.LogWarning("创建标签规则失败, 组合校验不通过, Error={Error}", ex.Message);
            throw new ValidationException(ex.Message);
        }

        // Resolve and validate tags
        var allTagIds = await ResolveTagIdsAsync(request.TagIds, request.NewTagNames, targetScope);

        // Create the rule
        var rule = new TagRule
        {
            RuleName = request.RuleName,
            Priority = request.Priority,
            TargetScope = targetScope,
            MatchField = matchField,
            MatchOperator = matchOperator,
            MatchValue = request.MatchValue,
            MatchValueMax = matchOperator == RuleMatchOperator.Range
                ? (string.IsNullOrWhiteSpace(request.MatchValueMax) ? null : request.MatchValueMax)
                : null,
            IsActive = true
        };

        // Add tag associations
        foreach (var tagId in allTagIds)
        {
            rule.TagRuleTags.Add(new TagRuleTag { TagId = tagId });
        }

        var createdRule = await _tagRuleRepository.AddAsync(rule);
        await _unitOfWork.SaveChangesAsync();

        // Reload with navigation for DTO mapping
        var reloaded = await _tagRuleRepository.GetQueryable()
            .Include(r => r.TagRuleTags).ThenInclude(rt => rt.Tag)
            .FirstAsync(r => r.Id == createdRule.Id);

        var dto = MapToDto(reloaded);

        await _auditLogService.LogAsync("Create", "TagRule", reloaded.Id, null, SerializeForAudit(dto));
        _logger.LogInformation("创建标签规则成功, TagRuleId={TagRuleId}, RuleName={RuleName}, TagCount={TagCount}",
            reloaded.Id, reloaded.RuleName, allTagIds.Count);

        return dto;
    }

    public async Task<TagRuleDto> UpdateAsync(long id, UpdateTagRuleRequest request)
    {
        _logger.LogDebug("TagRuleService.UpdateAsync: 开始更新标签规则, TagRuleId={TagRuleId}, RuleName={RuleName}", id, request.RuleName);

        var rule = await _tagRuleRepository.GetQueryable()
            .Include(r => r.TagRuleTags).ThenInclude(rt => rt.Tag)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (rule == null)
        {
            _logger.LogWarning("更新标签规则失败, 规则不存在, TagRuleId={TagRuleId}", id);
            throw new NotFoundException($"TagRule with ID {id} not found");
        }

        EnsureCanEdit(rule);

        var oldDto = MapToDto(rule);

        // Validate TargetScope
        if (!Enum.TryParse<TagScope>(request.TargetScope, true, out var targetScope))
        {
            _logger.LogWarning("更新标签规则失败, 无效的 TargetScope, TargetScope={TargetScope}", request.TargetScope);
            throw new ValidationException($"Invalid target scope: {request.TargetScope}");
        }

        // Validate MatchField
        if (!Enum.TryParse<RuleMatchField>(request.MatchField, true, out var matchField))
        {
            _logger.LogWarning("更新标签规则失败, 无效的匹配字段, MatchField={MatchField}", request.MatchField);
            throw new ValidationException($"Invalid match field: {request.MatchField}");
        }

        // Validate MatchOperator
        if (!Enum.TryParse<RuleMatchOperator>(request.MatchOperator, true, out var matchOperator))
        {
            _logger.LogWarning("更新标签规则失败, 无效的匹配操作符, MatchOperator={MatchOperator}", request.MatchOperator);
            throw new ValidationException($"Invalid match operator: {request.MatchOperator}");
        }

        // Validate regex if operator is Regex
        if (matchOperator == RuleMatchOperator.Regex)
        {
            try
            {
                _ = new Regex(request.MatchValue);
                _logger.LogDebug("正则表达式验证通过, TagRuleId={TagRuleId}, Pattern={Pattern}", id, request.MatchValue);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning("更新标签规则失败, 正则表达式验证失败, TagRuleId={TagRuleId}, Pattern={Pattern}, Error={Error}",
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
            _logger.LogWarning("更新标签规则失败, 组合校验不通过, TagRuleId={TagRuleId}, Error={Error}", id, ex.Message);
            throw new ValidationException(ex.Message);
        }

        // Resolve and validate tags
        var allTagIds = await ResolveTagIdsAsync(request.TagIds, request.NewTagNames, targetScope);

        // Update rule fields
        rule.RuleName = request.RuleName;
        rule.Priority = request.Priority;
        rule.TargetScope = targetScope;
        rule.MatchField = matchField;
        rule.MatchOperator = matchOperator;
        rule.MatchValue = request.MatchValue;
        rule.MatchValueMax = matchOperator == RuleMatchOperator.Range
            ? (string.IsNullOrWhiteSpace(request.MatchValueMax) ? null : request.MatchValueMax)
            : null;
        rule.IsActive = request.IsActive;

        // Replace tag associations: clear old, add new
        rule.TagRuleTags.Clear();
        foreach (var tagId in allTagIds)
        {
            rule.TagRuleTags.Add(new TagRuleTag { TagRuleId = rule.Id, TagId = tagId });
        }

        _tagRuleRepository.Update(rule);
        await _unitOfWork.SaveChangesAsync();

        // Reload with navigation for DTO mapping
        var reloaded = await _tagRuleRepository.GetQueryable()
            .Include(r => r.TagRuleTags).ThenInclude(rt => rt.Tag)
            .FirstAsync(r => r.Id == id);

        var newDto = MapToDto(reloaded);

        await _auditLogService.LogAsync("Update", "TagRule", rule.Id, SerializeForAudit(oldDto), SerializeForAudit(newDto));
        _logger.LogInformation("更新标签规则成功, TagRuleId={TagRuleId}, RuleName={RuleName}, TagCount={TagCount}",
            rule.Id, rule.RuleName, allTagIds.Count);

        return newDto;
    }

    public async Task DeleteAsync(long id)
    {
        _logger.LogDebug("TagRuleService.DeleteAsync: 开始删除标签规则, TagRuleId={TagRuleId}", id);

        var rule = await _tagRuleRepository.GetQueryable()
            .Include(r => r.TagRuleTags).ThenInclude(rt => rt.Tag)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (rule == null)
        {
            _logger.LogWarning("删除标签规则失败, 规则不存在, TagRuleId={TagRuleId}", id);
            throw new NotFoundException($"TagRule with ID {id} not found");
        }

        EnsureCanDelete(rule);

        var oldDto = MapToDto(rule);

        _tagRuleRepository.Delete(rule);
        await _unitOfWork.SaveChangesAsync();

        await _auditLogService.LogAsync("Delete", "TagRule", id, SerializeForAudit(oldDto), null);
        _logger.LogInformation("删除标签规则成功, TagRuleId={TagRuleId}", id);
    }

    public async Task<RunTagRulesResult> RunRulesAsync(RunTagRulesRequest request)
    {
        _logger.LogDebug("TagRuleService.RunRulesAsync: 开始执行标签规则, TargetScope={TargetScope}", request.TargetScope);

        // Phase 1: only Transaction scope
        if (!Enum.TryParse<TagScope>(request.TargetScope, true, out var targetScope))
        {
            _logger.LogWarning("执行标签规则失败, 无效的 TargetScope, TargetScope={TargetScope}", request.TargetScope);
            throw new ValidationException($"Invalid target scope: {request.TargetScope}");
        }

        if (targetScope != TagScope.Transaction)
        {
            _logger.LogWarning("执行标签规则失败, Phase 1 仅支持 Transaction 范围, TargetScope={TargetScope}", request.TargetScope);
            throw new ValidationException("Phase 1 only supports Transaction scope");
        }

        // Load active rules ordered by priority DESC then ID ASC
        var rules = await _tagRuleRepository.GetQueryable()
            .Include(r => r.TagRuleTags)
            .Where(r => r.IsActive && r.TargetScope == targetScope)
            .OrderByDescending(r => r.Priority)
            .ThenBy(r => r.Id)
            .ToListAsync();

        if (rules.Count == 0)
        {
            _logger.LogInformation("没有活跃的标签规则, TargetScope={TargetScope}", targetScope);
            return new RunTagRulesResult { ScannedCount = 0, AddedCount = 0, SkippedCount = 0 };
        }

        // 分批处理交易，避免一次性加载过多数据到内存
        const int batchSize = 1000;
        IQueryable<Transaction> baseQuery = ApplyPermissionFilter(_transactionRepository.GetQueryable())
            .Include(t => t.BankTransaction);

        if (request.EntityIds?.Count > 0)
        {
            baseQuery = baseQuery.Where(t => request.EntityIds.Contains(t.Id));
        }

        var totalCount = await baseQuery.CountAsync();
        int scannedCount = 0;
        int addedCount = 0;
        int skippedCount = 0;

        for (int offset = 0; offset < totalCount; offset += batchSize)
        {
            var batch = await baseQuery
                .OrderBy(t => t.Id)
                .Skip(offset)
                .Take(batchSize)
                .ToListAsync();

            if (batch.Count == 0) break;

            // 加载本批交易的已有绑定
            var batchIds = batch.Select(t => t.Id).ToList();
            var existingBindings = await _tagBindingRepository.GetQueryable()
                .Where(b => b.OwnerType == TagScope.Transaction && batchIds.Contains(b.OwnerId))
                .Select(b => new { b.OwnerId, b.TagId })
                .ToListAsync();

            var existingBindingSet = new HashSet<(long OwnerId, long TagId)>(
                existingBindings.Select(b => (b.OwnerId, b.TagId)));

            foreach (var transaction in batch)
            {
                var input = BuildMatchInput(transaction);
                foreach (var rule in rules)
                {
                    bool isMatch = RuleMatchingHelper.Match(
                        rule.MatchField,
                        rule.MatchOperator,
                        rule.MatchValue,
                        rule.MatchValueMax,
                        input,
                        _logger,
                        rule.Id);

                    if (isMatch)
                    {
                        foreach (var ruleTag in rule.TagRuleTags)
                        {
                            var key = (transaction.Id, ruleTag.TagId);
                            if (existingBindingSet.Contains(key))
                            {
                                skippedCount++;
                            }
                            else
                            {
                                await _tagBindingRepository.AddAsync(new TagBinding
                                {
                                    TagId = ruleTag.TagId,
                                    OwnerType = TagScope.Transaction,
                                    OwnerId = transaction.Id
                                });
                                existingBindingSet.Add(key);
                                addedCount++;
                            }
                        }
                    }
                }
            }

            await _unitOfWork.SaveChangesAsync();
            scannedCount += batch.Count;
        }

        _logger.LogInformation("执行标签规则完成, TargetScope={TargetScope}, ScannedCount={ScannedCount}, AddedCount={AddedCount}, SkippedCount={SkippedCount}",
            targetScope, scannedCount, addedCount, skippedCount);

        return new RunTagRulesResult
        {
            ScannedCount = scannedCount,
            AddedCount = addedCount,
            SkippedCount = skippedCount
        };
    }

    public async Task<RerunPreviewResponse> PreviewRerunAsync(RerunPreviewRequest request)
    {
        _logger.LogDebug("TagRuleService.PreviewRerunAsync: 开始预览规则重跑, TargetScope={TargetScope}", request.TargetScope);

        if (!Enum.TryParse<TagScope>(request.TargetScope, true, out var targetScope))
        {
            throw new ValidationException($"Invalid target scope: {request.TargetScope}");
        }
        if (targetScope != TagScope.Transaction)
        {
            throw new ValidationException("Phase 1 only supports Transaction scope");
        }

        var rules = await _tagRuleRepository.GetQueryable()
            .Include(r => r.TagRuleTags).ThenInclude(rt => rt.Tag)
            .Where(r => r.IsActive && r.TargetScope == targetScope)
            .OrderByDescending(r => r.Priority).ThenBy(r => r.Id)
            .ToListAsync();

        var response = new RerunPreviewResponse();
        if (rules.Count == 0) return response;

        IQueryable<Transaction> baseQuery = ApplyPermissionFilter(_transactionRepository.GetQueryable())
            .Include(t => t.BankTransaction);
        if (request.EntityIds?.Count > 0)
        {
            baseQuery = baseQuery.Where(t => request.EntityIds.Contains(t.Id));
        }

        var totalCount = await baseQuery.CountAsync();
        response.TotalScanned = totalCount;
        if (totalCount == 0) return response;

        const int batchSize = 1000;
        for (int offset = 0; offset < totalCount; offset += batchSize)
        {
            var batch = await baseQuery.OrderBy(t => t.Id).Skip(offset).Take(batchSize).ToListAsync();
            if (batch.Count == 0) break;

            var batchIds = batch.Select(t => t.Id).ToList();
            var existingBindings = await _tagBindingRepository.GetQueryable()
                .Where(b => b.OwnerType == TagScope.Transaction && batchIds.Contains(b.OwnerId))
                .Select(b => new { b.OwnerId, b.TagId })
                .ToListAsync();
            var existingByOwner = existingBindings
                .GroupBy(b => b.OwnerId)
                .ToDictionary(g => g.Key, g => g.Select(b => b.TagId).ToHashSet());

            foreach (var transaction in batch)
            {
                var input = BuildMatchInput(transaction);

                var matchedRules = rules
                    .Where(r => RuleMatchingHelper.Match(
                        r.MatchField, r.MatchOperator, r.MatchValue, r.MatchValueMax,
                        input, _logger, r.Id))
                    .ToList();

                if (matchedRules.Count == 0) continue;

                var existing = existingByOwner.GetValueOrDefault(transaction.Id) ?? new HashSet<long>();
                var ruleTagPairs = matchedRules.SelectMany(r => r.TagRuleTags).ToList();
                var tagsToAdd = ruleTagPairs
                    .Where(rt => !existing.Contains(rt.TagId))
                    .GroupBy(rt => rt.TagId)
                    .Select(g => new TagToAddDto
                    {
                        TagId = g.Key,
                        TagName = g.First().Tag?.Name ?? string.Empty,
                        TagColor = g.First().Tag?.Color
                    })
                    .ToList();

                if (tagsToAdd.Count == 0) continue; // 全部已存在，不计入 candidates

                response.Candidates.Add(new RerunCandidateDto
                {
                    TransactionId = transaction.Id,
                    TransactionDate = transaction.TransactionDate,
                    Amount = transaction.Amount,
                    Counterparty = transaction.BankTransaction?.Counterparty,
                    Description = transaction.Description ?? transaction.BankTransaction?.Description,
                    MatchedRules = matchedRules.Select(r => new MatchedRuleDto
                    {
                        RuleId = r.Id,
                        RuleName = r.RuleName,
                        Priority = r.Priority
                    }).ToList(),
                    TagsToAdd = tagsToAdd
                });
                response.TotalAffected++;
                response.TotalTagsToAdd += tagsToAdd.Count;
            }
        }

        _logger.LogInformation("预览规则重跑完成, TotalScanned={TotalScanned}, TotalAffected={TotalAffected}, TotalTagsToAdd={TotalTagsToAdd}",
            response.TotalScanned, response.TotalAffected, response.TotalTagsToAdd);

        return response;
    }

    public async Task<RerunConfirmResponse> ConfirmRerunAsync(RerunConfirmRequest request)
    {
        _logger.LogDebug("TagRuleService.ConfirmRerunAsync: 确认执行规则重跑, TargetScope={TargetScope}, TransactionCount={Count}",
            request.TargetScope, request.TransactionIds?.Count ?? 0);

        if (request.TransactionIds == null || request.TransactionIds.Count == 0)
        {
            throw new ValidationException("TransactionIds 不能为空");
        }

        var runResult = await RunRulesAsync(new RunTagRulesRequest
        {
            TargetScope = request.TargetScope,
            EntityIds = request.TransactionIds
        });

        return new RerunConfirmResponse
        {
            ScannedCount = runResult.ScannedCount,
            AddedCount = runResult.AddedCount,
            SkippedCount = runResult.SkippedCount,
            Message = $"规则重跑完成: 扫描 {runResult.ScannedCount} 条, 新增 {runResult.AddedCount} 个标签, 跳过 {runResult.SkippedCount} 个"
        };
    }

    // ────────────────────────── 私有辅助 ──────────────────────────

    /// <summary>
    /// 解析并验证标签 ID 列表：处理 NewTagNames（get-or-create）+ 验证所有标签存在且 scope 匹配
    /// </summary>
    private async Task<List<long>> ResolveTagIdsAsync(List<long> tagIds, List<string>? newTagNames, TagScope targetScope)
    {
        var allTagIds = new List<long>(tagIds);

        if (newTagNames?.Count > 0)
        {
            foreach (var tagName in newTagNames)
            {
                var trimmedName = tagName.Trim();
                if (string.IsNullOrEmpty(trimmedName)) continue;

                var existingTag = await _tagRepository.GetQueryable()
                    .FirstOrDefaultAsync(t => t.Scope == targetScope && t.Name == trimmedName);

                if (existingTag != null)
                {
                    allTagIds.Add(existingTag.Id);
                }
                else
                {
                    var newTag = new Tag
                    {
                        Scope = targetScope,
                        Name = trimmedName,
                        IsActive = true,
                        IsSystem = false
                    };
                    var created = await _tagRepository.AddAsync(newTag);
                    await _unitOfWork.SaveChangesAsync();
                    allTagIds.Add(created.Id);
                    _logger.LogDebug("自动创建标签, TagName={TagName}, TagScope={TagScope}, TagId={TagId}",
                        trimmedName, targetScope, created.Id);
                }
            }
        }

        allTagIds = allTagIds.Distinct().ToList();

        if (allTagIds.Count == 0) return allTagIds;

        var tags = await _tagRepository.GetQueryable()
            .Where(t => allTagIds.Contains(t.Id))
            .ToListAsync();

        if (tags.Count != allTagIds.Count)
        {
            var missingIds = allTagIds.Except(tags.Select(t => t.Id)).ToList();
            throw new ValidationException($"Tags not found: {string.Join(", ", missingIds)}");
        }

        var mismatchedTag = tags.FirstOrDefault(t => t.Scope != targetScope);
        if (mismatchedTag != null)
        {
            throw new ValidationException(
                $"Tag scope mismatch: {mismatchedTag.Name} is {mismatchedTag.Scope}, expected {targetScope}");
        }

        return allTagIds;
    }

    private static RuleMatchInput BuildMatchInput(Transaction t) => new(
        Counterparty: t.BankTransaction?.Counterparty ?? string.Empty,
        Description: t.Description ?? t.BankTransaction?.Description ?? string.Empty,
        Amount: t.Amount,
        Memo: t.BankTransaction?.Memo);

    private static TagRuleDto MapToDto(TagRule rule) => new TagRuleDto
    {
        Id = rule.Id,
        RuleName = rule.RuleName,
        Priority = rule.Priority,
        TargetScope = rule.TargetScope.ToString(),
        MatchField = rule.MatchField.ToString(),
        MatchOperator = rule.MatchOperator.ToString(),
        MatchValue = rule.MatchValue,
        MatchValueMax = rule.MatchValueMax,
        IsActive = rule.IsActive,
        Tags = rule.TagRuleTags.Select(rt => new TagRuleTagItemDto
        {
            TagId = rt.TagId,
            TagName = rt.Tag?.Name ?? string.Empty,
            TagColor = rt.Tag?.Color
        }).ToList(),
        CreatedAt = rule.CreatedAt
    };
}
