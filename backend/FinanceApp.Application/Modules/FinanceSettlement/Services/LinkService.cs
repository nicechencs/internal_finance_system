using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using FinanceApp.Application.Common;
using FinanceApp.Application.Modules.FinanceSettlement.DTOs.Link;
using FinanceApp.Application.Modules.FinanceSettlement.Interfaces;
using FinanceApp.Application.Modules.Identity.Interfaces;
using FinanceApp.Application.Modules.MasterData.Interfaces;
using FinanceApp.Application.Services;
using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Enums;
using FinanceApp.Domain.Interfaces;

namespace FinanceApp.Application.Modules.FinanceSettlement.Services;

public class LinkService : ServiceBase, ILinkService
{
    private readonly IRepository<Transaction> _transactionRepository;
    private readonly IRepository<BankTransaction> _bankTransactionRepository;
    private readonly IRepository<Customer> _customerRepository;
    private readonly IRepository<Supplier> _supplierRepository;
    private readonly IRepository<Person> _personRepository;
    private readonly IRepository<Project> _projectRepository;
    private readonly IRepository<Account> _accountRepository;
    private readonly IRepository<Category> _categoryRepository;
    private readonly IRuleService _ruleService;
    private readonly IAuditLogService _auditLogService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<LinkService> _logger;

    public LinkService(
        IRepository<Transaction> transactionRepository,
        IRepository<BankTransaction> bankTransactionRepository,
        IRepository<Customer> customerRepository,
        IRepository<Supplier> supplierRepository,
        IRepository<Person> personRepository,
        IRepository<Project> projectRepository,
        IRepository<Account> accountRepository,
        IRepository<Category> categoryRepository,
        IRuleService ruleService,
        IAuditLogService auditLogService,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IDataPermissionService permissionService,
        ILogger<LinkService> logger)
        : base(currentUserService, permissionService)
    {
        _transactionRepository = transactionRepository;
        _bankTransactionRepository = bankTransactionRepository;
        _customerRepository = customerRepository;
        _supplierRepository = supplierRepository;
        _personRepository = personRepository;
        _projectRepository = projectRepository;
        _accountRepository = accountRepository;
        _categoryRepository = categoryRepository;
        _ruleService = ruleService;
        _auditLogService = auditLogService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<LinkPreviewResponse> PreviewLinkAsync(LinkPreviewRequest request)
    {
        _logger.LogInformation("[LinkService.PreviewLink] LinkType={LinkType}, EntityId={EntityId}",
            request.LinkType, request.EntityId);

        var (entityName, candidates) = request.LinkType switch
        {
            LinkType.Customer => await PreviewCustomerLinkAsync(request.EntityId),
            LinkType.Supplier => await PreviewSupplierLinkAsync(request.EntityId),
            LinkType.Person => await PreviewPersonLinkAsync(request.EntityId),
            LinkType.Project => await PreviewProjectLinkAsync(request.EntityId),
            LinkType.Account => await PreviewAccountLinkAsync(request.EntityId),
            _ => throw new ArgumentException($"不支持的关联类型: {request.LinkType}")
        };

        return new LinkPreviewResponse
        {
            LinkType = request.LinkType,
            EntityId = request.EntityId,
            EntityName = entityName,
            TotalMatched = candidates.Count,
            Candidates = candidates
        };
    }

    public async Task<LinkConfirmResponse> ConfirmLinkAsync(LinkConfirmRequest request)
    {
        _logger.LogInformation("[LinkService.ConfirmLink] LinkType={LinkType}, EntityId={EntityId}, Count={Count}",
            request.LinkType, request.EntityId, request.TransactionIds.Count);

        if (request.TransactionIds.Count == 0)
            return new LinkConfirmResponse { LinkedCount = 0, Message = "未选择任何交易记录" };

        await using var transaction = await _unitOfWork.BeginTransactionAsync();
        try
        {
            var linkedCount = request.LinkType switch
            {
                LinkType.Customer => await ConfirmCustomerLinkAsync(request.EntityId, request.TransactionIds),
                LinkType.Supplier => await ConfirmSupplierLinkAsync(request.EntityId, request.TransactionIds),
                LinkType.Person => await ConfirmPersonLinkAsync(request.EntityId, request.TransactionIds),
                LinkType.Project => await ConfirmProjectLinkAsync(request.EntityId, request.TransactionIds),
                LinkType.Account => await ConfirmAccountLinkAsync(request.EntityId, request.TransactionIds),
                _ => throw new ArgumentException($"不支持的关联类型: {request.LinkType}")
            };

            await _unitOfWork.SaveChangesAsync();
            if (transaction != null) await transaction.CommitAsync();

            var typeName = request.LinkType.ToString();
            await _auditLogService.LogAsync("BatchLink", typeName, request.EntityId,
                null, SerializeForAudit(new { linkedCount, entityId = request.EntityId, entityType = typeName }));

            _logger.LogInformation("[LinkService.ConfirmLink] 成功关联 {Count} 条记录", linkedCount);

            return new LinkConfirmResponse
            {
                LinkedCount = linkedCount,
                Message = $"成功关联 {linkedCount} 条交易记录"
            };
        }
        catch
        {
            if (transaction != null) await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<RuleRerunPreviewResponse> PreviewRuleRerunAsync(RuleRerunPreviewRequest request)
    {
        _logger.LogInformation("[LinkService.PreviewRuleRerun] Strategy={Strategy}, Start={Start}, End={End}",
            request.Strategy, request.StartDate, request.EndDate);

        var query = BuildRuleRerunQuery(request.StartDate, request.EndDate, request.Strategy);

        // 先获取真实总数，再取预览数据
        var totalCount = await query.CountAsync();

        var transactions = await query
            .Include(t => t.BankTransaction)
            .Include(t => t.Category)
            .Include(t => t.Account)
            .OrderByDescending(t => t.TransactionDate)
            .Take(500)
            .ToListAsync();

        // 批量匹配分类（避免 N+1）
        var matchItems = transactions
            .Select(tx => (
                tx.BankTransaction?.Counterparty ?? string.Empty,
                tx.Description ?? string.Empty,
                tx.Amount,
                tx.BankTransaction?.Memo))
            .ToList();
        var matchResults = await _ruleService.MatchCategoriesBatchAsync(matchItems);

        // 一次加载所有需要的分类名
        var newCategoryIds = matchResults.Where(id => id.HasValue).Select(id => id!.Value).Distinct().ToList();
        var categoryNames = new Dictionary<long, string>();
        foreach (var cid in newCategoryIds)
        {
            var category = await _categoryRepository.GetByIdAsync(cid);
            if (category != null) categoryNames[cid] = category.Name;
        }

        var candidates = new List<RuleRerunCandidateDto>();
        for (int i = 0; i < transactions.Count; i++)
        {
            var tx = transactions[i];
            var newCategoryId = matchResults[i];

            var willChange = newCategoryId.HasValue && newCategoryId != tx.CategoryId;
            if (request.Strategy == RuleRerunStrategy.Conservative && tx.CategoryId.HasValue)
                willChange = false;

            string? newCategoryName = null;
            if (newCategoryId.HasValue)
                categoryNames.TryGetValue(newCategoryId.Value, out newCategoryName);

            candidates.Add(new RuleRerunCandidateDto
            {
                TransactionId = tx.Id,
                TransactionDate = tx.TransactionDate,
                Amount = tx.Amount,
                TransactionType = tx.TransactionType.ToString(),
                Counterparty = tx.BankTransaction?.Counterparty ?? string.Empty,
                Description = tx.Description ?? string.Empty,
                CurrentCategoryName = tx.Category?.Name,
                NewCategoryName = newCategoryName,
                NewCategoryId = newCategoryId,
                WillChange = willChange
            });
        }

        var wouldUpdate = candidates.Count(c => c.WillChange);

        return new RuleRerunPreviewResponse
        {
            TotalAffected = totalCount,
            WouldUpdate = wouldUpdate,
            Strategy = request.Strategy,
            Candidates = candidates
        };
    }

    public async Task<RuleRerunConfirmResponse> ConfirmRuleRerunAsync(RuleRerunConfirmRequest request)
    {
        _logger.LogInformation("[LinkService.ConfirmRuleRerun] Strategy={Strategy}, SelectedIds={Count}",
            request.Strategy, request.TransactionIds?.Count ?? -1);

        IQueryable<Transaction> query;
        if (request.TransactionIds != null && request.TransactionIds.Count > 0)
        {
            query = _transactionRepository.GetQueryable()
                .Where(t => request.TransactionIds.Contains(t.Id));
        }
        else
        {
            query = BuildRuleRerunQuery(request.StartDate, request.EndDate, request.Strategy);
        }

        var transactions = await query
            .Include(t => t.BankTransaction)
            .ToListAsync();

        // 批量匹配分类（避免 N+1）
        var matchItems = transactions
            .Select(tx => (
                tx.BankTransaction?.Counterparty ?? string.Empty,
                tx.Description ?? string.Empty,
                tx.Amount,
                tx.BankTransaction?.Memo))
            .ToList();
        var matchResults = await _ruleService.MatchCategoriesBatchAsync(matchItems);

        await using var dbTransaction = await _unitOfWork.BeginTransactionAsync();
        try
        {
            int updatedCount = 0;
            int skippedCount = 0;

            for (int i = 0; i < transactions.Count; i++)
            {
                var tx = transactions[i];
                var newCategoryId = matchResults[i];

                if (!newCategoryId.HasValue)
                {
                    skippedCount++;
                    continue;
                }

                if (request.Strategy == RuleRerunStrategy.Conservative && tx.CategoryId.HasValue)
                {
                    skippedCount++;
                    continue;
                }

                if (newCategoryId == tx.CategoryId)
                {
                    skippedCount++;
                    continue;
                }

                tx.CategoryId = newCategoryId.Value;
                _transactionRepository.Update(tx);
                updatedCount++;
            }

            await _unitOfWork.SaveChangesAsync();
            if (dbTransaction != null) await dbTransaction.CommitAsync();

            var affectedIds = transactions.Where(t => t.CategoryId != null).Select(t => t.Id).ToList();
            await _auditLogService.LogAsync("RuleRerun", "Transaction", 0,
                null, System.Text.Json.JsonSerializer.Serialize(new
                {
                    updatedCount,
                    skippedCount,
                    strategy = request.Strategy.ToString(),
                    transactionIds = affectedIds.Take(100)
                }));

            _logger.LogInformation("[LinkService.ConfirmRuleRerun] 完成, Updated={Updated}, Skipped={Skipped}",
                updatedCount, skippedCount);

            return new RuleRerunConfirmResponse
            {
                UpdatedCount = updatedCount,
                SkippedCount = skippedCount,
                Message = $"规则重跑完成：更新 {updatedCount} 条，跳过 {skippedCount} 条"
            };
        }
        catch
        {
            if (dbTransaction != null) await dbTransaction.RollbackAsync();
            throw;
        }
    }

    #region 一键关联 - 预览

    private async Task<(string EntityName, List<LinkCandidateDto> Candidates)> PreviewCustomerLinkAsync(long customerId)
    {
        var customer = await _customerRepository.GetByIdAsync(customerId)
            ?? throw new NotFoundException($"客户不存在: {customerId}");

        var names = BuildMatchNames(customer.Name, customer.ShortName);
        var candidates = await FindUnlinkedTransactionsByCounterparty(
            names, t => t.CustomerId == null && t.TransactionType == TransactionType.Income);

        return (customer.Name, candidates);
    }

    private async Task<(string EntityName, List<LinkCandidateDto> Candidates)> PreviewSupplierLinkAsync(long supplierId)
    {
        var supplier = await _supplierRepository.GetByIdAsync(supplierId)
            ?? throw new NotFoundException($"供应商不存在: {supplierId}");

        var names = BuildMatchNames(supplier.Name, supplier.ShortName);
        var candidates = await FindUnlinkedTransactionsByCounterparty(
            names, t => t.SupplierId == null && t.TransactionType == TransactionType.Expense);

        return (supplier.Name, candidates);
    }

    private async Task<(string EntityName, List<LinkCandidateDto> Candidates)> PreviewPersonLinkAsync(long personId)
    {
        var person = await _personRepository.GetByIdAsync(personId)
            ?? throw new NotFoundException($"人员不存在: {personId}");

        var names = new List<string> { person.Name };
        var candidates = await FindUnlinkedTransactionsByCounterparty(
            names, t => t.PersonId == null);

        return (person.Name, candidates);
    }

    private async Task<(string EntityName, List<LinkCandidateDto> Candidates)> PreviewProjectLinkAsync(long projectId)
    {
        var project = await _projectRepository.GetByIdAsync(projectId)
            ?? throw new NotFoundException($"项目不存在: {projectId}");

        var matchTerms = new List<string> { project.Name };
        if (!string.IsNullOrWhiteSpace(project.ProjectCode))
            matchTerms.Add(project.ProjectCode);

        var query = ApplyPermissionFilter(_transactionRepository.GetQueryable())
            .Where(t => t.ProjectId == null && !t.IsAllocated);

        var transactions = await query
            .Include(t => t.BankTransaction)
            .Include(t => t.Account)
            .Include(t => t.Category)
            .OrderByDescending(t => t.TransactionDate)
            .ToListAsync();

        var candidates = new List<LinkCandidateDto>();
        foreach (var tx in transactions)
        {
            var desc = tx.Description ?? string.Empty;
            var memo = tx.BankTransaction?.Memo ?? string.Empty;
            var counterparty = tx.BankTransaction?.Counterparty ?? string.Empty;
            var combined = $"{desc} {memo} {counterparty}";

            foreach (var term in matchTerms)
            {
                if (combined.Contains(term, StringComparison.OrdinalIgnoreCase))
                {
                    candidates.Add(MapToCandidate(tx, $"描述/备注中包含\"{term}\""));
                    break;
                }
            }
        }

        return (project.Name, candidates);
    }

    private async Task<(string EntityName, List<LinkCandidateDto> Candidates)> PreviewAccountLinkAsync(long accountId)
    {
        var account = await _accountRepository.GetByIdAsync(accountId)
            ?? throw new NotFoundException($"账户不存在: {accountId}");

        // 账户关联较少见：查找银行流水对方账号匹配但交易未关联到此账户的情况
        // 这里主要用于修正错误关联，场景有限
        var candidates = new List<LinkCandidateDto>();

        if (!string.IsNullOrWhiteSpace(account.AccountNumber))
        {
            var query = ApplyPermissionFilter(_transactionRepository.GetQueryable())
                .Where(t => t.BankTransactionId != null);

            var transactions = await query
                .Include(t => t.BankTransaction)
                .Include(t => t.Account)
                .Include(t => t.Category)
                .Where(t => t.BankTransaction != null
                    && t.BankTransaction.CounterpartyAccount != null
                    && t.BankTransaction.CounterpartyAccount.Contains(account.AccountNumber))
                .OrderByDescending(t => t.TransactionDate)
                .Take(200)
                .ToListAsync();

            foreach (var tx in transactions)
            {
                candidates.Add(MapToCandidate(tx, $"对方账号匹配\"{account.AccountNumber}\""));
            }
        }

        return (account.Name, candidates);
    }

    #endregion

    #region 一键关联 - 确认

    private async Task<int> ConfirmCustomerLinkAsync(long customerId, List<long> transactionIds)
    {
        var customer = await _customerRepository.GetByIdAsync(customerId)
            ?? throw new NotFoundException($"客户不存在: {customerId}");

        // 服务端重新校验：只允许未关联客户的收入交易
        var validIds = await _transactionRepository.GetQueryable()
            .Where(t => transactionIds.Contains(t.Id))
            .Where(t => t.CustomerId == null && t.TransactionType == TransactionType.Income)
            .Select(t => t.Id)
            .ToListAsync();

        return await BatchUpdateForeignKey(validIds, tx =>
        {
            tx.CustomerId = customerId;
        });
    }

    private async Task<int> ConfirmSupplierLinkAsync(long supplierId, List<long> transactionIds)
    {
        var supplier = await _supplierRepository.GetByIdAsync(supplierId)
            ?? throw new NotFoundException($"供应商不存在: {supplierId}");

        // 服务端重新校验：只允许未关联供应商的支出交易
        var validIds = await _transactionRepository.GetQueryable()
            .Where(t => transactionIds.Contains(t.Id))
            .Where(t => t.SupplierId == null && t.TransactionType == TransactionType.Expense)
            .Select(t => t.Id)
            .ToListAsync();

        return await BatchUpdateForeignKey(validIds, tx =>
        {
            tx.SupplierId = supplierId;
        });
    }

    private async Task<int> ConfirmPersonLinkAsync(long personId, List<long> transactionIds)
    {
        var person = await _personRepository.GetByIdAsync(personId)
            ?? throw new NotFoundException($"人员不存在: {personId}");

        // 服务端重新校验：只允许未关联人员的交易
        var validIds = await _transactionRepository.GetQueryable()
            .Where(t => transactionIds.Contains(t.Id))
            .Where(t => t.PersonId == null)
            .Select(t => t.Id)
            .ToListAsync();

        return await BatchUpdateForeignKey(validIds, tx =>
        {
            tx.PersonId = personId;
        });
    }

    private async Task<int> ConfirmProjectLinkAsync(long projectId, List<long> transactionIds)
    {
        var project = await _projectRepository.GetByIdAsync(projectId)
            ?? throw new NotFoundException($"项目不存在: {projectId}");

        // 服务端重新校验：只允许未关联项目且未分摊的交易
        var validIds = await _transactionRepository.GetQueryable()
            .Where(t => transactionIds.Contains(t.Id))
            .Where(t => t.ProjectId == null && !t.IsAllocated)
            .Select(t => t.Id)
            .ToListAsync();

        return await BatchUpdateForeignKey(validIds, tx =>
        {
            tx.ProjectId = projectId;
        });
    }

    private Task<int> ConfirmAccountLinkAsync(long accountId, List<long> transactionIds)
    {
        // 账户关联功能尚未实现，明确拒绝请求
        throw new InvalidOperationException("账户关联功能暂不支持，请使用其他关联方式");
    }

    #endregion

    #region 批量智能关联

    public async Task<BatchLinkPreviewResponse> PreviewBatchLinkAsync()
    {
        _logger.LogInformation("[LinkService.PreviewBatchLink] 开始扫描未关联交易");

        // 查询所有来自银行流水、且未关联任何实体的交易
        var unlinkedQuery = ApplyPermissionFilter(_transactionRepository.GetQueryable())
            .Where(t => t.BankTransactionId != null
                && t.CustomerId == null
                && t.SupplierId == null
                && t.PersonId == null
                && t.ProjectId == null
                && t.TransactionType != TransactionType.Transfer);

        var totalUnlinked = await unlinkedQuery.CountAsync();

        var transactions = await unlinkedQuery
            .Include(t => t.BankTransaction)
            .Include(t => t.Account)
            .OrderByDescending(t => t.TransactionDate)
            .Take(500)
            .ToListAsync();

        // 一次性加载所有活跃实体，避免 N+1
        var customers = await _customerRepository.GetQueryable()
            .Where(c => c.IsActive).ToListAsync();
        var suppliers = await _supplierRepository.GetQueryable()
            .Where(s => s.IsActive).ToListAsync();
        var persons = await _personRepository.GetQueryable()
            .Where(p => p.IsActive).ToListAsync();
        var projects = await _projectRepository.GetQueryable()
            .Where(p => !p.IsDeleted).ToListAsync();
        var accounts = await _accountRepository.GetQueryable()
            .Where(a => a.IsActive).ToListAsync();

        var candidates = new List<BatchLinkCandidateDto>();

        foreach (var tx in transactions)
        {
            var counterparty = tx.BankTransaction?.Counterparty ?? string.Empty;
            var description = tx.Description ?? string.Empty;
            var memo = tx.BankTransaction?.Memo ?? string.Empty;
            var combined = $"{description} {memo} {counterparty}";

            var matches = new List<EntityMatchDto>();

            // 匹配客户（收入交易优先，但不排除支出）
            foreach (var c in customers)
            {
                var names = BuildMatchNames(c.Name, c.ShortName);
                foreach (var name in names)
                {
                    if (!string.IsNullOrEmpty(name) && counterparty.Contains(name, StringComparison.OrdinalIgnoreCase))
                    {
                        matches.Add(new EntityMatchDto
                        {
                            EntityType = BatchLinkEntityType.Customer,
                            EntityId = c.Id,
                            EntityName = c.Name,
                            MatchReason = $"对方名称含\"{name}\"",
                            ExtraInfo = BuildCustomerExtraInfo(c)
                        });
                        break;
                    }
                }
            }

            // 匹配供应商
            foreach (var s in suppliers)
            {
                var names = BuildMatchNames(s.Name, s.ShortName);
                foreach (var name in names)
                {
                    if (!string.IsNullOrEmpty(name) && counterparty.Contains(name, StringComparison.OrdinalIgnoreCase))
                    {
                        matches.Add(new EntityMatchDto
                        {
                            EntityType = BatchLinkEntityType.Supplier,
                            EntityId = s.Id,
                            EntityName = s.Name,
                            MatchReason = $"对方名称含\"{name}\"",
                            ExtraInfo = BuildSupplierExtraInfo(s)
                        });
                        break;
                    }
                }
            }

            // 匹配人员
            foreach (var p in persons)
            {
                if (!string.IsNullOrEmpty(p.Name) && counterparty.Contains(p.Name, StringComparison.OrdinalIgnoreCase))
                {
                    matches.Add(new EntityMatchDto
                    {
                        EntityType = BatchLinkEntityType.Person,
                        EntityId = p.Id,
                        EntityName = p.Name,
                        MatchReason = $"对方名称含\"{p.Name}\"",
                        ExtraInfo = BuildPersonExtraInfo(p)
                    });
                }
            }

            // 匹配项目（按描述/备注/对方字段）
            foreach (var proj in projects)
            {
                var matchTerms = new List<string> { proj.Name };
                if (!string.IsNullOrWhiteSpace(proj.ProjectCode))
                    matchTerms.Add(proj.ProjectCode);

                foreach (var term in matchTerms)
                {
                    if (!string.IsNullOrEmpty(term) && combined.Contains(term, StringComparison.OrdinalIgnoreCase))
                    {
                        matches.Add(new EntityMatchDto
                        {
                            EntityType = BatchLinkEntityType.Project,
                            EntityId = proj.Id,
                            EntityName = proj.Name,
                            MatchReason = term == proj.ProjectCode
                                ? $"含项目编号\"{term}\""
                                : $"描述/备注含\"{term}\"",
                            ExtraInfo = string.IsNullOrWhiteSpace(proj.ProjectCode)
                                ? null
                                : $"编号：{proj.ProjectCode}"
                        });
                        break;
                    }
                }
            }

            // 匹配账户（按对方名称匹配账户名称/银行名称/账号，排除交易本身所属账户）
            foreach (var acc in accounts)
            {
                if (acc.Id == tx.AccountId) continue; // 跳过交易自身账户

                var matchNames = new List<string> { acc.Name };
                if (!string.IsNullOrWhiteSpace(acc.BankName)) matchNames.Add(acc.BankName);
                if (!string.IsNullOrWhiteSpace(acc.AccountNumber)) matchNames.Add(acc.AccountNumber);

                foreach (var name in matchNames)
                {
                    if (!string.IsNullOrEmpty(name) && counterparty.Contains(name, StringComparison.OrdinalIgnoreCase))
                    {
                        matches.Add(new EntityMatchDto
                        {
                            EntityType = BatchLinkEntityType.Account,
                            EntityId = acc.Id,
                            EntityName = acc.Name,
                            MatchReason = $"对方名称含\"{name}\"",
                            ExtraInfo = BuildAccountExtraInfo(acc)
                        });
                        break;
                    }
                }
            }

            if (matches.Count > 0)
            {
                candidates.Add(new BatchLinkCandidateDto
                {
                    TransactionId = tx.Id,
                    TransactionDate = tx.TransactionDate,
                    Amount = tx.Amount,
                    TransactionType = tx.TransactionType.ToString(),
                    AccountName = tx.Account?.Name ?? string.Empty,
                    Counterparty = tx.BankTransaction?.Counterparty,
                    Description = tx.Description,
                    Matches = matches
                });
            }
        }

        _logger.LogInformation("[LinkService.PreviewBatchLink] 扫描完成, 未关联={Total}, 匹配={Matched}",
            totalUnlinked, candidates.Count);

        return new BatchLinkPreviewResponse
        {
            TotalUnlinked = totalUnlinked,
            TotalMatched = candidates.Count,
            Candidates = candidates
        };
    }

    public async Task<BatchLinkConfirmResponse> ConfirmBatchLinkAsync(BatchLinkConfirmRequest request)
    {
        _logger.LogInformation("[LinkService.ConfirmBatchLink] 确认关联 {Count} 条", request.Items.Count);

        if (request.Items.Count == 0)
            return new BatchLinkConfirmResponse { LinkedCount = 0, Message = "未选择任何关联项" };

        var transactionIds = request.Items.Select(i => i.TransactionId).Distinct().ToList();
        var transactions = await _transactionRepository.GetQueryable()
            .Where(t => transactionIds.Contains(t.Id))
            .ToListAsync();

        var txMap = transactions.ToDictionary(t => t.Id);

        await using var dbTransaction = await _unitOfWork.BeginTransactionAsync();
        try
        {
            int linkedCount = 0;
            foreach (var item in request.Items)
            {
                if (!txMap.TryGetValue(item.TransactionId, out var tx))
                    continue;

                EnsureCanEdit(tx);

                switch (item.EntityType)
                {
                    case BatchLinkEntityType.Customer:
                        tx.CustomerId = item.EntityId;
                        break;
                    case BatchLinkEntityType.Supplier:
                        tx.SupplierId = item.EntityId;
                        break;
                    case BatchLinkEntityType.Person:
                        tx.PersonId = item.EntityId;
                        break;
                    case BatchLinkEntityType.Project:
                        tx.ProjectId = item.EntityId;
                        break;
                    case BatchLinkEntityType.Account:
                        tx.AccountId = item.EntityId;
                        break;
                }

                _transactionRepository.Update(tx);
                linkedCount++;
            }

            await _unitOfWork.SaveChangesAsync();
            if (dbTransaction != null) await dbTransaction.CommitAsync();

            await _auditLogService.LogAsync("BatchLinkConfirm", "Transaction", 0,
                null, SerializeForAudit(new { linkedCount }));

            _logger.LogInformation("[LinkService.ConfirmBatchLink] 成功关联 {Count} 条", linkedCount);

            return new BatchLinkConfirmResponse
            {
                LinkedCount = linkedCount,
                Message = $"成功关联 {linkedCount} 条交易记录"
            };
        }
        catch
        {
            if (dbTransaction != null) await dbTransaction.RollbackAsync();
            throw;
        }
    }

    private static string? BuildCustomerExtraInfo(Customer c)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(c.ShortName)) parts.Add($"简称：{c.ShortName}");
        if (!string.IsNullOrWhiteSpace(c.ContactPhone)) parts.Add($"电话：{c.ContactPhone}");
        return parts.Count > 0 ? string.Join("，", parts) : null;
    }

    private static string? BuildSupplierExtraInfo(Supplier s)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(s.ShortName)) parts.Add($"简称：{s.ShortName}");
        if (!string.IsNullOrWhiteSpace(s.ContactPhone)) parts.Add($"电话：{s.ContactPhone}");
        return parts.Count > 0 ? string.Join("，", parts) : null;
    }

    private static string? BuildPersonExtraInfo(Person p)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(p.Phone)) parts.Add($"电话：{p.Phone}");
        parts.Add($"类型：{p.PersonType}");
        return parts.Count > 0 ? string.Join("，", parts) : null;
    }

    private static string? BuildAccountExtraInfo(Account a)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(a.BankName)) parts.Add($"银行：{a.BankName}");
        if (!string.IsNullOrWhiteSpace(a.AccountNumber)) parts.Add($"账号：{a.AccountNumber}");
        return parts.Count > 0 ? string.Join("，", parts) : null;
    }

    #endregion

    #region 辅助方法

    private List<string> BuildMatchNames(string name, string? shortName)
    {
        var names = new List<string> { name };
        if (!string.IsNullOrWhiteSpace(shortName) && !shortName.Equals(name, StringComparison.OrdinalIgnoreCase))
            names.Add(shortName);
        return names;
    }

    private async Task<List<LinkCandidateDto>> FindUnlinkedTransactionsByCounterparty(
        List<string> names, System.Linq.Expressions.Expression<Func<Transaction, bool>> filter)
    {
        var query = ApplyPermissionFilter(_transactionRepository.GetQueryable())
            .Where(filter)
            .Where(t => t.BankTransactionId != null);

        var transactions = await query
            .Include(t => t.BankTransaction)
            .Include(t => t.Account)
            .Include(t => t.Category)
            .OrderByDescending(t => t.TransactionDate)
            .ToListAsync();

        var candidates = new List<LinkCandidateDto>();
        foreach (var tx in transactions)
        {
            var counterparty = tx.BankTransaction?.Counterparty ?? string.Empty;
            foreach (var name in names)
            {
                if (counterparty.Contains(name, StringComparison.OrdinalIgnoreCase))
                {
                    candidates.Add(MapToCandidate(tx, $"对方名称匹配\"{name}\""));
                    break;
                }
            }
        }

        return candidates;
    }

    private async Task<int> BatchUpdateForeignKey(List<long> transactionIds, Action<Transaction> updateAction)
    {
        var transactions = await _transactionRepository.GetQueryable()
            .Where(t => transactionIds.Contains(t.Id))
            .ToListAsync();

        int count = 0;
        foreach (var tx in transactions)
        {
            EnsureCanEdit(tx);
            updateAction(tx);
            _transactionRepository.Update(tx);
            count++;
        }

        return count;
    }

    private static LinkCandidateDto MapToCandidate(Transaction tx, string matchReason)
    {
        return new LinkCandidateDto
        {
            TransactionId = tx.Id,
            TransactionDate = tx.TransactionDate,
            Amount = tx.Amount,
            TransactionType = tx.TransactionType.ToString(),
            AccountName = tx.Account?.Name ?? string.Empty,
            Counterparty = tx.BankTransaction?.Counterparty,
            Description = tx.Description,
            CategoryName = tx.Category?.Name,
            MatchReason = matchReason
        };
    }

    private IQueryable<Transaction> BuildRuleRerunQuery(DateTime? startDate, DateTime? endDate, RuleRerunStrategy strategy)
    {
        var query = ApplyPermissionFilter(_transactionRepository.GetQueryable())
            .Where(t => t.BankTransactionId != null);

        if (strategy == RuleRerunStrategy.Conservative)
            query = query.Where(t => t.CategoryId == null);

        if (startDate.HasValue)
            query = query.Where(t => t.TransactionDate >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(t => t.TransactionDate <= endDate.Value);

        return query;
    }

    #endregion
}
