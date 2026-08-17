using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using FinanceApp.Application.Common;
using FinanceApp.Application.Modules.Identity.Interfaces;
using FinanceApp.Application.Modules.MasterData.Interfaces;
using FinanceApp.Application.Modules.Reconciliation.DTOs;
using FinanceApp.Application.Modules.Reconciliation.Parsers;
using FinanceApp.Application.Modules.Reconciliation.Interfaces;
using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Enums;
using FinanceApp.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OfficeOpenXml;

namespace FinanceApp.Application.Modules.Reconciliation.Services;

public class ImportService : IImportService
{
    private readonly IRepository<ImportBatch> _batchRepository;
    private readonly IRepository<Account> _accountRepository;
    private readonly IRepository<Category> _categoryRepository;
    private readonly IRepository<BankTransaction> _bankTransactionRepository;
    private readonly IRepository<Transaction> _transactionRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IRuleService _ruleService;
    private readonly ILogger<ImportService> _logger;
    private readonly IAuditLogService _auditLogService;

    // In-memory cache for preview data (keyed by batch Id)
    // In production, this could be stored in Redis or a temp table
    private static readonly ConcurrentDictionary<long, (List<BankTransactionPreviewDto> Previews, DateTime ExpireAt)> _previewCache = new();

    public ImportService(
        IRepository<ImportBatch> batchRepository,
        IRepository<Account> accountRepository,
        IRepository<Category> categoryRepository,
        IRepository<BankTransaction> bankTransactionRepository,
        IRepository<Transaction> transactionRepository,
        IUnitOfWork unitOfWork,
        IRuleService ruleService,
        ILogger<ImportService> logger,
        IAuditLogService auditLogService)
    {
        _batchRepository = batchRepository;
        _accountRepository = accountRepository;
        _categoryRepository = categoryRepository;
        _bankTransactionRepository = bankTransactionRepository;
        _transactionRepository = transactionRepository;
        _unitOfWork = unitOfWork;
        _ruleService = ruleService;
        _logger = logger;
        _auditLogService = auditLogService;
    }

    public async Task<ImportPreviewResponse> PreviewAsync(Stream fileStream, string fileName, long accountId)
    {
        _logger.LogDebug("ImportService.PreviewAsync - 开始预览导入: 文件名={FileName}, 账户={AccountId}, 文件大小={FileSize}字节",
            fileName, accountId, fileStream.CanSeek ? fileStream.Length : -1);

        try
        {
            // Validate account exists
            _logger.LogDebug("验证账户存在性: AccountId={AccountId}", accountId);
            var account = await _accountRepository.GetByIdAsync(accountId);
            if (account == null)
            {
                _logger.LogWarning("账户不存在: AccountId={AccountId}", accountId);
                throw new NotFoundException("账户不存在");
            }
            _logger.LogDebug("账户验证通过: AccountId={AccountId}, AccountName={AccountName}", accountId, account.Name);

            // 检测文件格式并解析
            var format = DetectFileFormat(fileStream, fileName);
            _logger.LogInformation("文件格式检测结果: {FileName} → {Format}", fileName, format);

            var parser = CreateParser(format);
            var parsedRows = parser.Parse(fileStream);

            if (parsedRows.Count == 0)
            {
                _logger.LogError("文件中没有可解析的有效数据行: {FileName}", fileName);
                throw new ValidationException("文件中没有可解析的有效数据行");
            }

            // 转换为 PreviewDto
            var previews = new List<BankTransactionPreviewDto>();
            for (int i = 0; i < parsedRows.Count; i++)
            {
                var parsed = parsedRows[i];
                var uniqueHash = CalculateUniqueHash(format, parsed);

                previews.Add(new BankTransactionPreviewDto
                {
                    RowNumber = i + 1,
                    TransactionDate = parsed.TransactionDate,
                    TransactionTime = parsed.TransactionTime,
                    Amount = parsed.Amount,
                    Direction = parsed.Direction,
                    Balance = parsed.Balance,
                    CounterpartyName = parsed.Counterparty,
                    CounterpartyAccount = parsed.CounterpartyAccount,
                    CounterpartyBank = parsed.CounterpartyBank,
                    TransactionNumber = parsed.TransactionNumber,
                    Description = parsed.Description,
                    Memo = parsed.ExtendedInfo,
                    UniqueHash = uniqueHash,
                    IsDuplicate = false,
                    MatchedCategoryId = null,
                    MatchedCategoryName = null
                });
            }

            _logger.LogInformation("文件解析完成: {FileName}, 格式={Format}, 有效行数={Count}", fileName, format, previews.Count);

            // Step 1: 文件内部 hash 冲突检测
            _logger.LogDebug("开始检测文件内部 hash 冲突");
            var hashGroups = previews.GroupBy(p => p.UniqueHash).Where(g => g.Count() > 1).ToList();
            var fileConflictHashes = new HashSet<string>(hashGroups.Select(g => g.Key));
            foreach (var preview in previews)
            {
                if (fileConflictHashes.Contains(preview.UniqueHash))
                {
                    preview.IsFileConflict = true;
                    preview.ConflictReason = "文件内存在多条相同记录";
                }
            }
            _logger.LogDebug("文件内冲突检测完成: 冲突组数={GroupCount}, 涉及行数={RowCount}",
                hashGroups.Count, previews.Count(p => p.IsFileConflict));

            // Step 2: 数据库重复检查（批量查询）
            _logger.LogDebug("开始批量检查重复记录: 待检查数量={Count}", previews.Count);
            var hashes = previews.Select(p => p.UniqueHash).Distinct().ToList();

            // 查询已存在的银行流水及其关联交易的状态
            var existingBankTxs = await _bankTransactionRepository.GetQueryable()
                .Where(bt => hashes.Contains(bt.UniqueHash))
                .Select(bt => new
                {
                    bt.UniqueHash,
                    bt.Id,
                    // 检查是否有未删除的关联业务交易
                    HasActiveTransaction = _transactionRepository.GetQueryable()
                        .Any(t => t.BankTransactionId == bt.Id)
                })
                .ToListAsync();

            var duplicateHashSet = new HashSet<string>();
            var recoverableHashSet = new HashSet<string>();

            foreach (var existing in existingBankTxs)
            {
                if (existing.HasActiveTransaction)
                {
                    duplicateHashSet.Add(existing.UniqueHash);
                }
                else
                {
                    recoverableHashSet.Add(existing.UniqueHash);
                }
            }

            _logger.LogDebug("重复检查完成: 真正重复={DuplicateCount}, 可恢复={RecoverableCount}",
                duplicateHashSet.Count, recoverableHashSet.Count);

            foreach (var preview in previews)
            {
                if (preview.IsFileConflict) continue; // 文件内冲突优先

                if (duplicateHashSet.Contains(preview.UniqueHash))
                {
                    preview.IsDuplicate = true;
                    preview.ConflictReason = "数据库中已存在相同记录";
                }
                else if (recoverableHashSet.Contains(preview.UniqueHash))
                {
                    preview.IsRecoverable = true;
                    preview.ConflictReason = "银行流水已存在，业务交易已删除，可恢复";
                }
            }

            // Match categories using rule engine for importable rows (batch to avoid N+1)
            var importableRows = previews.Where(p => !p.IsDuplicate && !p.IsFileConflict).ToList();
            _logger.LogDebug("开始规则引擎分类匹配: 可导入记录数={Count}", importableRows.Count);
            var allCategories = await _categoryRepository.GetAllAsync();
            var categoryDict = allCategories.ToDictionary(c => c.Id, c => c.Name);

            var matchItems = importableRows
                .Select(p => (p.CounterpartyName, p.Description ?? string.Empty, p.Amount, p.Memo))
                .ToList();
            var matchResults = await _ruleService.MatchCategoriesBatchAsync(matchItems);

            int matchedCount = 0;
            for (int idx = 0; idx < importableRows.Count; idx++)
            {
                var preview = importableRows[idx];
                var matchedCategoryId = matchResults[idx];

                if (matchedCategoryId.HasValue)
                {
                    preview.MatchedCategoryId = matchedCategoryId.Value;
                    if (categoryDict.TryGetValue(matchedCategoryId.Value, out var categoryName))
                    {
                        preview.MatchedCategoryName = categoryName;
                    }
                    matchedCount++;
                    _logger.LogDebug("规则匹配成功: RowNumber={Row}, CategoryId={CategoryId}, CategoryName={CategoryName}",
                        preview.RowNumber, matchedCategoryId.Value, preview.MatchedCategoryName);
                }
            }
            _logger.LogInformation("分类匹配完成: 成功匹配={MatchedCount}, 总非重复记录={Total}",
                matchedCount, previews.Count(p => !p.IsDuplicate));

            // Create ImportBatch record with Pending status
            _logger.LogDebug("创建导入批次记录: AccountId={AccountId}, FileName={FileName}", accountId, fileName);
            var batch = new ImportBatch
            {
                AccountId = accountId,
                FileName = fileName,
                FileSize = fileStream.CanSeek ? fileStream.Length : 0,
                RecordCount = previews.Count,
                SuccessCount = 0,
                DuplicateCount = previews.Count(p => p.IsDuplicate),
                Status = ImportBatchStatus.Pending
            };

            await _batchRepository.AddAsync(batch);
            await _unitOfWork.SaveChangesAsync();
            _logger.LogInformation("导入批次已保存到数据库: BatchId={BatchId}, RecordCount={RecordCount}",
                batch.Id, batch.RecordCount);

            // Cache preview data for later confirmation (30 minutes TTL)
            CleanExpiredPreviewCache();
            _previewCache[batch.Id] = (previews, DateTime.UtcNow.AddMinutes(30));
            _logger.LogDebug("预览数据已缓存: BatchId={BatchId}, CacheSize={CacheSize}, ExpireAt={ExpireAt}",
                batch.Id, _previewCache.Count, DateTime.UtcNow.AddMinutes(30));

            var duplicateCount = previews.Count(p => p.IsDuplicate);
            var fileConflictCount = previews.Count(p => p.IsFileConflict);
            var recoverableCount = previews.Count(p => p.IsRecoverable);
            var newCount = previews.Count - duplicateCount - fileConflictCount - recoverableCount;

            _logger.LogInformation(
                "预览完成: 文件名={FileName}, 格式={Format}, 总行数={Total}, 重复={Duplicate}, 文件内冲突={FileConflict}, 可恢复={Recoverable}, 新增={New}, BatchId={BatchId}",
                fileName, format, previews.Count, duplicateCount, fileConflictCount, recoverableCount, newCount, batch.Id);

            return new ImportPreviewResponse
            {
                BatchId = batch.Id,
                FileName = fileName,
                TotalRows = previews.Count,
                DuplicateRows = duplicateCount,
                FileConflictRows = fileConflictCount,
                RecoverableRows = recoverableCount,
                NewRows = newCount,
                DetectedFormat = format.ToString(),
                Previews = previews
            };
        }
        catch (ValidationException)
        {
            throw;
        }
        catch (NotFoundException)
        {
            throw;
        }
        catch (InvalidDataException ex)
        {
            _logger.LogWarning(ex, "文件格式无效: 文件名={FileName}, 账户={AccountId}, 错误={Message}",
                fileName, accountId, ex.Message);
            throw new ValidationException($"上传的文件格式无效或已损坏，请确保文件是有效的 Excel (.xlsx) 格式: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "预览失败: 文件名={FileName}, 账户={AccountId}, 错误={Message}",
                fileName, accountId, ex.Message);
            throw;
        }
    }

    public async Task<ImportBatchDto> ConfirmAsync(ImportConfirmRequest request)
    {
        _logger.LogDebug("ImportService.ConfirmAsync - 开始确认导入: BatchId={BatchId}, 选中行数={SelectedCount}",
            request.BatchId, request.SelectedRowNumbers.Count);

        try
        {
            // Get batch
            _logger.LogDebug("获取导入批次: BatchId={BatchId}", request.BatchId);
            var batch = await _batchRepository.GetByIdAsync(request.BatchId);
            if (batch == null)
            {
                _logger.LogWarning("导入批次不存在: BatchId={BatchId}", request.BatchId);
                throw new NotFoundException("导入批次不存在");
            }

            if (batch.Status != ImportBatchStatus.Pending)
            {
                _logger.LogWarning("批次状态无效: BatchId={BatchId}, Status={Status}", request.BatchId, batch.Status);
                throw new ValidationException("该批次已处理，不能重复确认");
            }

            // Get preview data from cache and check expiration
            if (!_previewCache.TryGetValue(request.BatchId, out var cached) || cached.ExpireAt < DateTime.UtcNow)
            {
                _logger.LogWarning("预览数据不存在或已过期: BatchId={BatchId}", request.BatchId);
                throw new ValidationException("预览数据已过期，请重新上传文件");
            }
            var previews = cached.Previews;
            _logger.LogDebug("预览数据已从缓存获取: BatchId={BatchId}, PreviewCount={Count}", request.BatchId, previews.Count);

            // Update batch status to Processing
            batch.Status = ImportBatchStatus.Processing;
            _batchRepository.Update(batch);
            await _unitOfWork.SaveChangesAsync();
            _logger.LogDebug("批次状态已更新为处理中: BatchId={BatchId}", request.BatchId);

            // Get account for the batch
            var account = await _accountRepository.GetByIdAsync(batch.AccountId);
            if (account == null)
            {
                _logger.LogError("账户不存在: AccountId={AccountId}, BatchId={BatchId}", batch.AccountId, request.BatchId);
                throw new NotFoundException("账户不存在");
            }
            _logger.LogDebug("账户信息已获取: AccountId={AccountId}, AccountName={AccountName}, 初始余额={Balance}",
                account.Id, account.Name, account.CurrentBalance);

            var selectedSet = new HashSet<int>(request.SelectedRowNumbers);
            var successCount = 0;
            var errorCount = 0;
            var errorDetails = new List<string>();

            _logger.LogDebug("开始数据库事务: BatchId={BatchId}, 待处理行数={SelectedCount}",
                request.BatchId, selectedSet.Count);
            await using var dbTransaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                var candidateRows = previews
                    .Where(p => selectedSet.Contains(p.RowNumber) && !p.IsDuplicate && !p.IsFileConflict)
                    .ToList();

                var recoverableIdByHash = await LoadRecoverableBankTransactionIdsAsync(candidateRows);
                var originalBalance = account.CurrentBalance;

                // 批量写入前打 savepoint：任一次 SaveChanges 失败时可撤掉已写入的流水，再走逐行路径
                string? batchSavepoint = null;
                if (dbTransaction != null)
                {
                    try
                    {
                        batchSavepoint = "batch_insert";
                        await dbTransaction.CreateSavepointAsync(batchSavepoint);
                    }
                    catch (Exception spEx)
                    {
                        _logger.LogWarning(spEx, "创建批量写入 savepoint 失败（可能不支持），继续处理: BatchId={BatchId}", request.BatchId);
                        batchSavepoint = null;
                    }
                }

                try
                {
                    var batchResult = await ConfirmRowsInBatchAsync(
                        candidateRows, recoverableIdByHash, batch, account, errorDetails);
                    successCount = batchResult.SuccessCount;
                    errorCount = batchResult.ErrorCount;

                    if (batchSavepoint != null && dbTransaction != null)
                    {
                        try
                        {
                            await dbTransaction.ReleaseSavepointAsync(batchSavepoint);
                        }
                        catch (Exception spEx)
                        {
                            _logger.LogWarning(spEx, "释放批量写入 savepoint 失败（可能不支持），继续处理: BatchId={BatchId}", request.BatchId);
                        }
                    }
                }
                catch (DbUpdateException ex) when (ex is not DbUpdateConcurrencyException)
                {
                    _logger.LogWarning(ex, "批量写入失败，回退到逐行 savepoint 路径: BatchId={BatchId}", batch.Id);

                    if (batchSavepoint != null && dbTransaction != null)
                    {
                        try
                        {
                            await dbTransaction.RollbackToSavepointAsync(batchSavepoint);
                            _logger.LogDebug("已回滚批量写入 savepoint: BatchId={BatchId}", batch.Id);
                        }
                        catch (Exception spEx)
                        {
                            _logger.LogWarning(spEx, "回滚批量写入 savepoint 失败（可能不支持）: BatchId={BatchId}", batch.Id);
                        }
                    }

                    _unitOfWork.ClearChangeTracker();
                    account.CurrentBalance = originalBalance;
                    errorDetails.Clear();

                    var fallback = await ConfirmRowsWithSavepointsAsync(
                        previews, selectedSet, batch, account, dbTransaction, errorDetails);
                    successCount = fallback.SuccessCount;
                    errorCount = fallback.ErrorCount;
                }

                // Update account balance
                _accountRepository.Update(account);
                _logger.LogDebug("账户余额已更新: AccountId={AccountId}, 新余额={NewBalance}",
                    account.Id, account.CurrentBalance);

                // Update batch
                batch.SuccessCount = successCount;
                batch.ErrorCount = errorCount;
                batch.DuplicateCount = previews.Count(p => p.IsDuplicate);
                batch.ErrorMessage = errorDetails.Count > 0
                    ? string.Join("\n", errorDetails)
                    : null;
                batch.Status = errorCount > 0 && successCount == 0
                    ? ImportBatchStatus.Failed
                    : errorCount > 0
                        ? ImportBatchStatus.PartialCompleted
                        : ImportBatchStatus.Completed;

                _batchRepository.Update(batch);
                await _unitOfWork.SaveChangesAsync();

                if (dbTransaction != null) await dbTransaction.CommitAsync();
                _logger.LogInformation("数据库事务提交成功: BatchId={BatchId}, 成功={Success}, 失败={Error}",
                    request.BatchId, successCount, errorCount);

                _logger.LogInformation(
                    "导入批次处理完成: BatchId={BatchId}, 成功={Success}, 失败={Error}, 重复={Duplicate}, 状态={Status}",
                    batch.Id, successCount, errorCount, batch.DuplicateCount, batch.Status);
            }
            catch (Exception ex)
            {
                try
                {
                    if (dbTransaction != null) await dbTransaction.RollbackAsync();
                }
                catch (Exception rollbackEx)
                {
                    _logger.LogWarning(rollbackEx, "事务回滚失败（可能已自动回滚）: BatchId={BatchId}", request.BatchId);
                }

                _logger.LogError(ex, "导入确认失败，事务已回滚: BatchId={BatchId}", request.BatchId);

                // 在新的数据库上下文状态中更新批次状态
                try
                {
                    // 清理所有被追踪的已修改实体（包括 account.CurrentBalance），
                    // 防止事务回滚后余额变更被意外持久化
                    _unitOfWork.ClearChangeTracker();
                    batch.Status = ImportBatchStatus.Failed;
                    batch.ErrorMessage = ex is DbUpdateConcurrencyException
                        ? "导入期间账户余额被其他操作更新，本批次已终止"
                        : ex.Message;
                    _batchRepository.Update(batch);
                    await _unitOfWork.SaveChangesAsync();
                }
                catch (Exception updateEx)
                {
                    _logger.LogWarning(updateEx, "更新批次失败状态时出错: BatchId={BatchId}", request.BatchId);
                }

                throw;
            }
            finally
            {
                // Clean up preview cache
                _previewCache.TryRemove(request.BatchId, out _);
                _logger.LogDebug("预览缓存已清理: BatchId={BatchId}", request.BatchId);
            }

            // 审计日志放在事务外，失败不影响导入结果
            try
            {
                await _auditLogService.LogAsync("Confirm", "ImportBatch", batch.Id, null,
                    System.Text.Json.JsonSerializer.Serialize(new { successCount, errorCount, duplicateCount = batch.DuplicateCount, batchId = batch.Id }));
            }
            catch (Exception auditEx)
            {
                _logger.LogWarning(auditEx, "审计日志记录失败（不影响导入结果）: BatchId={BatchId}", request.BatchId);
            }

            return MapBatchToDto(batch, account.Name);
        }
        catch (ValidationException)
        {
            throw;
        }
        catch (NotFoundException)
        {
            throw;
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogWarning(ex, "确认导入失败：检测到账户并发更新冲突, BatchId={BatchId}", request.BatchId);
            throw new ValidationException("导入期间账户余额被其他操作更新，请重新上传并预览后再试");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "确认导入失败: BatchId={BatchId}, 错误={Message}",
                request.BatchId, ex.Message);
            throw;
        }
    }

    /// <summary>
    /// 一次预加载可恢复行对应的银行流水 Id（UniqueHash IN ...）。
    /// </summary>
    private async Task<Dictionary<string, long>> LoadRecoverableBankTransactionIdsAsync(
        IReadOnlyList<BankTransactionPreviewDto> candidateRows)
    {
        var hashes = candidateRows
            .Where(p => p.IsRecoverable)
            .Select(p => p.UniqueHash)
            .Where(h => !string.IsNullOrEmpty(h))
            .Distinct()
            .ToList();

        if (hashes.Count == 0)
            return new Dictionary<string, long>();

        var existing = await _bankTransactionRepository.GetQueryable()
            .Where(bt => hashes.Contains(bt.UniqueHash))
            .Select(bt => new { bt.UniqueHash, bt.Id })
            .ToListAsync();

        _logger.LogDebug("可恢复银行流水预加载完成: 待查找={Requested}, 已找到={Found}",
            hashes.Count, existing.Count);

        return existing
            .GroupBy(x => x.UniqueHash)
            .ToDictionary(g => g.Key, g => g.First().Id);
    }

    /// <summary>
    /// 成功路径：新流水一次 SaveChanges，业务交易一次 SaveChanges，账户余额仅在两次提交成功后累加。
    /// 可恢复行不插入 BankTransaction；找不到对应流水的记入 errorDetails，不进入插入集合。
    /// </summary>
    private async Task<(int SuccessCount, int ErrorCount)> ConfirmRowsInBatchAsync(
        IReadOnlyList<BankTransactionPreviewDto> candidateRows,
        IReadOnlyDictionary<string, long> recoverableIdByHash,
        ImportBatch batch,
        Account account,
        List<string> errorDetails)
    {
        var successCount = 0;
        var errorCount = 0;
        var newBankRows = new List<(BankTransactionPreviewDto Preview, BankTransaction BankTx)>();
        var recoverableRows = new List<(BankTransactionPreviewDto Preview, long BankTransactionId)>();

        foreach (var preview in candidateRows)
        {
            if (preview.IsRecoverable)
            {
                if (!recoverableIdByHash.TryGetValue(preview.UniqueHash, out var existingId))
                {
                    _logger.LogError(
                        "导入行处理失败: BatchId={BatchId}, RowNumber={Row}, 日期={Date}, 金额={Amount}, 对方={Counterparty}",
                        batch.Id, preview.RowNumber, preview.TransactionDate, preview.Amount, preview.CounterpartyName);
                    errorDetails.Add($"行{preview.RowNumber}: 标记为可恢复但未找到对应银行流水");
                    errorCount++;
                    continue;
                }

                recoverableRows.Add((preview, existingId));
                _logger.LogDebug("恢复已存在银行流水: RowNumber={Row}, BankTransactionId={BankTxId}",
                    preview.RowNumber, existingId);
            }
            else
            {
                var bankTransaction = CreateBankTransaction(batch, preview);
                await _bankTransactionRepository.AddAsync(bankTransaction);
                newBankRows.Add((preview, bankTransaction));
            }
        }

        if (newBankRows.Count > 0)
        {
            await _unitOfWork.SaveChangesAsync();
        }

        var insertedRows = new List<(BankTransactionPreviewDto Preview, long BankTransactionId, Transaction Transaction)>();

        foreach (var (preview, bankTx) in newBankRows)
        {
            var transaction = CreateBusinessTransaction(batch, preview, bankTx.Id);
            await _transactionRepository.AddAsync(transaction);
            insertedRows.Add((preview, bankTx.Id, transaction));
        }

        foreach (var (preview, bankTransactionId) in recoverableRows)
        {
            var transaction = CreateBusinessTransaction(batch, preview, bankTransactionId);
            await _transactionRepository.AddAsync(transaction);
            insertedRows.Add((preview, bankTransactionId, transaction));
        }

        if (insertedRows.Count > 0)
        {
            await _unitOfWork.SaveChangesAsync();
        }

        foreach (var (preview, bankTransactionId, transaction) in insertedRows)
        {
            ApplyAccountBalance(account, preview);
            successCount++;
            _logger.LogDebug(
                "交易记录已创建: RowNumber={Row}, BankTransactionId={BankTxId}, TransactionId={TxId}, 金额={Amount}, 方向={Direction}, 可恢复={IsRecoverable}",
                preview.RowNumber, bankTransactionId, transaction.Id, preview.Amount, preview.Direction, preview.IsRecoverable);
        }

        return (successCount, errorCount);
    }

    /// <summary>
    /// 逐行 savepoint 回退路径：单行失败不影响其他行，保留 PartialCompleted 语义。
    /// </summary>
    private async Task<(int SuccessCount, int ErrorCount)> ConfirmRowsWithSavepointsAsync(
        IReadOnlyList<BankTransactionPreviewDto> previews,
        HashSet<int> selectedSet,
        ImportBatch batch,
        Account account,
        ITransactionScope? dbTransaction,
        List<string> errorDetails)
    {
        var successCount = 0;
        var errorCount = 0;
        int rowIndex = 0;

        foreach (var preview in previews)
        {
            if (!selectedSet.Contains(preview.RowNumber))
                continue;

            if (preview.IsDuplicate || preview.IsFileConflict)
                continue;

            string? savepointName = null;
            if (dbTransaction != null)
            {
                savepointName = $"row_{rowIndex}";
                try
                {
                    await dbTransaction.CreateSavepointAsync(savepointName);
                }
                catch (Exception spEx)
                {
                    _logger.LogWarning(spEx, "创建 savepoint 失败（可能不支持），继续处理: RowNumber={Row}", preview.RowNumber);
                    savepointName = null;
                }
            }

            try
            {
                long bankTransactionId;

                if (preview.IsRecoverable)
                {
                    var existingBankTx = await _bankTransactionRepository.GetQueryable()
                        .FirstOrDefaultAsync(bt => bt.UniqueHash == preview.UniqueHash);

                    if (existingBankTx == null)
                    {
                        throw new ValidationException($"行{preview.RowNumber}: 标记为可恢复但未找到对应银行流水");
                    }

                    bankTransactionId = existingBankTx.Id;
                    _logger.LogDebug("恢复已存在银行流水: RowNumber={Row}, BankTransactionId={BankTxId}",
                        preview.RowNumber, bankTransactionId);
                }
                else
                {
                    var bankTransaction = CreateBankTransaction(batch, preview);
                    await _bankTransactionRepository.AddAsync(bankTransaction);
                    await _unitOfWork.SaveChangesAsync();
                    bankTransactionId = bankTransaction.Id;
                }

                var transaction = CreateBusinessTransaction(batch, preview, bankTransactionId);
                await _transactionRepository.AddAsync(transaction);
                await _unitOfWork.SaveChangesAsync();

                ApplyAccountBalance(account, preview);

                if (savepointName != null && dbTransaction != null)
                {
                    try
                    {
                        await dbTransaction.ReleaseSavepointAsync(savepointName);
                    }
                    catch (Exception spEx)
                    {
                        _logger.LogWarning(spEx, "释放 savepoint 失败（可能不支持），继续处理: RowNumber={Row}", preview.RowNumber);
                    }
                }

                successCount++;
                _logger.LogDebug(
                    "交易记录已创建: RowNumber={Row}, BankTransactionId={BankTxId}, TransactionId={TxId}, 金额={Amount}, 方向={Direction}, 可恢复={IsRecoverable}",
                    preview.RowNumber, bankTransactionId, transaction.Id, preview.Amount, preview.Direction, preview.IsRecoverable);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "导入行处理失败: BatchId={BatchId}, RowNumber={Row}, 日期={Date}, 金额={Amount}, 对方={Counterparty}",
                    batch.Id, preview.RowNumber, preview.TransactionDate, preview.Amount, preview.CounterpartyName);

                if (savepointName != null && dbTransaction != null)
                {
                    try
                    {
                        await dbTransaction.RollbackToSavepointAsync(savepointName);
                        _logger.LogDebug("已回滚到 savepoint: RowNumber={Row}, Savepoint={Name}", preview.RowNumber, savepointName);
                    }
                    catch (Exception spEx)
                    {
                        _logger.LogWarning(spEx, "回滚 savepoint 失败（可能不支持），清理 ChangeTracker: RowNumber={Row}", preview.RowNumber);
                        _unitOfWork.DetachAddedEntities();
                    }
                }
                else
                {
                    _unitOfWork.DetachAddedEntities();
                }

                var errorMessage = ExtractDetailedErrorMessage(ex);
                errorDetails.Add($"行{preview.RowNumber}: {errorMessage}");
                errorCount++;
            }

            rowIndex++;
        }

        return (successCount, errorCount);
    }

    private static BankTransaction CreateBankTransaction(ImportBatch batch, BankTransactionPreviewDto preview)
    {
        return new BankTransaction
        {
            AccountId = batch.AccountId,
            ImportBatchId = batch.Id,
            TransactionDate = preview.TransactionDate,
            TransactionTime = preview.TransactionTime,
            Amount = preview.Amount,
            Balance = preview.Balance,
            Direction = preview.Direction == "in"
                ? BankTransactionDirection.In
                : BankTransactionDirection.Out,
            Counterparty = preview.CounterpartyName,
            CounterpartyAccount = preview.CounterpartyAccount,
            CounterpartyBank = preview.CounterpartyBank,
            TransactionNumber = preview.TransactionNumber,
            Description = preview.Description,
            Memo = preview.Memo,
            UniqueHash = preview.UniqueHash,
            IsProcessed = true
        };
    }

    private static Transaction CreateBusinessTransaction(ImportBatch batch, BankTransactionPreviewDto preview, long bankTransactionId)
    {
        return new Transaction
        {
            BankTransactionId = bankTransactionId,
            TransactionDate = preview.TransactionDate,
            Amount = preview.Amount,
            TransactionType = preview.Direction == "in"
                ? TransactionType.Income
                : TransactionType.Expense,
            CategoryId = preview.MatchedCategoryId,
            AccountId = batch.AccountId,
            Description = preview.Description,
            Status = TransactionStatus.Confirmed,
            IsAllocated = false
        };
    }

    private static void ApplyAccountBalance(Account account, BankTransactionPreviewDto preview)
    {
        if (preview.Direction == "in")
        {
            account.CurrentBalance += preview.Amount;
        }
        else
        {
            account.CurrentBalance -= preview.Amount;
        }
    }

    public async Task<PageResponse<ImportBatchDto>> GetBatchesAsync(ImportBatchQueryRequest request)
    {
        _logger.LogDebug("ImportService.GetBatchesAsync - 获取导入批次列表: Page={Page}, PageSize={PageSize}, AccountId={AccountId}, Status={Status}",
            request.Page, request.PageSize, request.AccountId, request.Status);

        var query = _batchRepository.GetQueryable()
            .Include(b => b.Account)
            .AsQueryable();

        // 按账户筛选
        if (request.AccountId.HasValue)
        {
            query = query.Where(b => b.AccountId == request.AccountId.Value);
        }

        // 按日期范围筛选
        if (request.StartDate.HasValue)
        {
            query = query.Where(b => b.CreatedAt >= request.StartDate.Value);
        }
        if (request.EndDate.HasValue)
        {
            var endDate = request.EndDate.Value.Date.AddDays(1);
            query = query.Where(b => b.CreatedAt < endDate);
        }

        // 按状态筛选
        if (!string.IsNullOrEmpty(request.Status) && Enum.TryParse<ImportBatchStatus>(request.Status, out var status))
        {
            query = query.Where(b => b.Status == status);
        }

        // 按文件名模糊搜索
        if (!string.IsNullOrEmpty(request.FileName))
        {
            query = query.Where(b => b.FileName.Contains(request.FileName));
        }

        var orderedQuery = query.OrderByDescending(b => b.CreatedAt);

        var total = await orderedQuery.CountAsync();
        var items = await orderedQuery
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync();

        var dtos = items.Select(b => MapBatchToDto(b, b.Account?.Name ?? string.Empty)).ToList();

        _logger.LogInformation("导入批次列表查询成功: Total={Total}, ReturnedCount={Count}", total, dtos.Count);

        return new PageResponse<ImportBatchDto>
        {
            Items = dtos,
            Page = request.Page,
            PageSize = request.PageSize,
            Total = total
        };
    }

    public async Task<ImportBatchDto> GetBatchByIdAsync(long id)
    {
        _logger.LogDebug("ImportService.GetBatchByIdAsync - 获取导入批次详情: BatchId={BatchId}", id);

        var batch = await _batchRepository.GetQueryable()
            .Include(b => b.Account)
            .FirstOrDefaultAsync(b => b.Id == id);

        if (batch == null)
        {
            _logger.LogWarning("导入批次不存在: BatchId={BatchId}", id);
            throw new NotFoundException("导入批次不存在");
        }

        _logger.LogDebug("导入批次详情已获取: BatchId={BatchId}, Status={Status}, RecordCount={RecordCount}",
            id, batch.Status, batch.RecordCount);

        return MapBatchToDto(batch, batch.Account?.Name ?? string.Empty);
    }

    public async Task DeleteBatchAsync(long id)
    {
        _logger.LogDebug("ImportService.DeleteBatchAsync - 开始删除导入批次: BatchId={BatchId}", id);

        var batch = await _batchRepository.GetByIdAsync(id);
        if (batch == null)
        {
            _logger.LogWarning("导入批次不存在: BatchId={BatchId}", id);
            throw new NotFoundException("导入批次不存在");
        }

        if (batch.Status != ImportBatchStatus.Pending && batch.Status != ImportBatchStatus.Failed)
        {
            _logger.LogWarning("批次状态不允许删除: BatchId={BatchId}, Status={Status}", id, batch.Status);
            throw new ValidationException("只能删除待处理或失败状态的批次");
        }

        batch.IsDeleted = true;
        batch.DeletedAt = DateTime.UtcNow;
        _batchRepository.Update(batch);
        await _unitOfWork.SaveChangesAsync();

        // 清理可能残留的预览缓存
        _previewCache.TryRemove(id, out _);

        var oldSnapshot = System.Text.Json.JsonSerializer.Serialize(new { id, batch.FileName, batch.AccountId, batch.RecordCount });
        await _auditLogService.LogAsync("Delete", "ImportBatch", id, oldSnapshot, null);
        _logger.LogInformation("导入批次已删除: BatchId={BatchId}, Status={Status}", id, batch.Status);
    }

    public async Task<ImportPreviewResponse> GetCachedPreviewAsync(long batchId)
    {
        _logger.LogDebug("ImportService.GetCachedPreviewAsync - 获取缓存预览: BatchId={BatchId}", batchId);

        var batch = await _batchRepository.GetQueryable()
            .Include(b => b.Account)
            .FirstOrDefaultAsync(b => b.Id == batchId);

        if (batch == null)
        {
            _logger.LogWarning("导入批次不存在: BatchId={BatchId}", batchId);
            throw new NotFoundException("导入批次不存在");
        }

        if (batch.Status != ImportBatchStatus.Pending)
            throw new ValidationException("只有待处理状态的批次可以继续处理");

        if (!_previewCache.TryGetValue(batchId, out var cached) || cached.ExpireAt < DateTime.UtcNow)
        {
            _logger.LogWarning("预览数据已过期: BatchId={BatchId}", batchId);
            throw new ValidationException("预览数据已过期，请删除此批次后重新上传文件");
        }

        _logger.LogInformation("缓存预览数据已获取: BatchId={BatchId}, TotalRows={Total}", batchId, cached.Previews.Count);

        return new ImportPreviewResponse
        {
            BatchId = batch.Id,
            FileName = batch.FileName,
            TotalRows = cached.Previews.Count,
            DuplicateRows = cached.Previews.Count(p => p.IsDuplicate),
            FileConflictRows = cached.Previews.Count(p => p.IsFileConflict),
            RecoverableRows = cached.Previews.Count(p => p.IsRecoverable),
            NewRows = cached.Previews.Count(p => !p.IsDuplicate && !p.IsFileConflict && !p.IsRecoverable),
            DetectedFormat = "Cached",
            Previews = cached.Previews
        };
    }

    /// <summary>
    /// 自动检测文件格式
    /// </summary>
    private FileFormat DetectFileFormat(Stream stream, string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();

        // .xml → AlipayBusiness
        if (extension == ".xml")
        {
            _logger.LogDebug("通过扩展名识别为支付宝格式: {FileName}", fileName);
            return FileFormat.AlipayBusiness;
        }

        // .xls/.xlsx → 先尝试 EPPlus 解析检测华夏银行
        if (extension == ".xls" || extension == ".xlsx")
        {
            try
            {
                if (stream.CanSeek) stream.Seek(0, SeekOrigin.Begin);

                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                using var package = new ExcelPackage(stream);
                var worksheet = package.Workbook.Worksheets.FirstOrDefault();
                if (worksheet?.Dimension != null)
                {
                    var maxRow = Math.Min(15, worksheet.Dimension.Rows);
                    var maxCol = Math.Min(worksheet.Dimension.Columns, 15);

                    for (int row = 1; row <= maxRow; row++)
                    {
                        bool hasSequence = false, hasDate = false, hasExpense = false;
                        for (int col = 1; col <= maxCol; col++)
                        {
                            var val = worksheet.Cells[row, col].GetValue<string>()?.Trim();
                            if (val == "序号") hasSequence = true;
                            if (val == "交易日期") hasDate = true;
                            if (val == "支出金额") hasExpense = true;
                        }
                        if (hasSequence && hasDate && hasExpense)
                        {
                            _logger.LogDebug("通过表头内容识别为华夏银行格式: {FileName}, 表头行={Row}", fileName, row);
                            if (stream.CanSeek) stream.Seek(0, SeekOrigin.Begin);
                            return FileFormat.HuaxiaBank;
                        }
                    }
                }

                // EPPlus 成功打开但不是华夏银行 → Simple
                if (stream.CanSeek) stream.Seek(0, SeekOrigin.Begin);
            }
            catch (Exception)
            {
                // EPPlus 无法解析 → 可能是 SpreadsheetML XML（支付宝 .xls）
                _logger.LogDebug("EPPlus 无法解析，尝试检测 XML 格式: {FileName}", fileName);
                try
                {
                    if (stream.CanSeek) stream.Seek(0, SeekOrigin.Begin);
                    var doc = System.Xml.Linq.XDocument.Load(stream);
                    var ssNs = (System.Xml.Linq.XNamespace)"urn:schemas-microsoft-com:office:spreadsheet";
                    if (doc.Root?.Name == ssNs + "Workbook")
                    {
                        _logger.LogDebug("通过 XML 内容识别为支付宝格式: {FileName}", fileName);
                        if (stream.CanSeek) stream.Seek(0, SeekOrigin.Begin);
                        return FileFormat.AlipayBusiness;
                    }
                }
                catch (Exception xmlEx)
                {
                    _logger.LogWarning(xmlEx, "XML 格式检测也失败，回退到简单格式: {FileName}", fileName);
                }

                if (stream.CanSeek) stream.Seek(0, SeekOrigin.Begin);
            }
        }

        _logger.LogDebug("使用默认简单格式: {FileName}", fileName);
        return FileFormat.Simple;
    }

    /// <summary>
    /// 根据格式创建解析器
    /// </summary>
    private IFileParser CreateParser(FileFormat format)
    {
        return format switch
        {
            FileFormat.HuaxiaBank => new HuaxiaBankParser(_logger),
            FileFormat.AlipayBusiness => new AlipayBusinessParser(_logger),
            _ => new SimpleFileParser(_logger)
        };
    }

    /// <summary>
    /// 按格式差异化计算唯一哈希
    /// </summary>
    private static string CalculateUniqueHash(FileFormat format, ParsedBankRow row)
    {
        // 字段规范化辅助函数
        static string Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? "" : value.Trim();
        static string NormalizeTime(TimeSpan? time) => time.HasValue ? time.Value.ToString(@"hh\:mm\:ss") : "";

        string raw;

        switch (format)
        {
            case FileFormat.HuaxiaBank:
                // 华夏：交易号 + 日期 + 时间 + 方向 + 金额 + 对方账号/名称 + 描述 + 余额
                raw = $"HX|{Normalize(row.TransactionNumber)}|{row.TransactionDate:yyyy-MM-dd}|{NormalizeTime(row.TransactionTime)}|{row.Direction}|{row.Amount}|{Normalize(row.CounterpartyAccount)}|{Normalize(row.Counterparty)}|{Normalize(row.Description)}|{(row.Balance?.ToString() ?? "")}";
                break;
            case FileFormat.AlipayBusiness:
                // 支付宝：交易号 + 日期 + 时间 + 方向 + 金额 + 对方账号/名称 + 描述 + 余额
                raw = $"ZFB|{Normalize(row.TransactionNumber)}|{row.TransactionDate:yyyy-MM-dd}|{NormalizeTime(row.TransactionTime)}|{row.Direction}|{row.Amount}|{Normalize(row.CounterpartyAccount)}|{Normalize(row.Counterparty)}|{Normalize(row.Description)}|{(row.Balance?.ToString() ?? "")}";
                break;
            default:
                // Simple 格式保持原有 hash 算法，不破坏已有数据去重
                raw = $"{row.TransactionDate:yyyy-MM-dd}|{row.Amount}|{row.Counterparty}|{row.Description}";
                break;
        }

        using var md5 = MD5.Create();
        var bytes = md5.ComputeHash(Encoding.UTF8.GetBytes(raw));
        return BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant();
    }

    private static void CleanExpiredPreviewCache()
    {
        var now = DateTime.UtcNow;
        var expiredKeys = _previewCache
            .Where(kvp => kvp.Value.ExpireAt < now)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in expiredKeys)
        {
            _previewCache.TryRemove(key, out _);
        }
    }

    /// <summary>
    /// 提取详细错误信息，优先展开 DbUpdateException
    /// </summary>
    private static string ExtractDetailedErrorMessage(Exception ex)
    {
        if (ex is Microsoft.EntityFrameworkCore.DbUpdateException dbEx)
        {
            var baseEx = dbEx.GetBaseException();
            var baseMessage = baseEx.Message;

            // 尝试提取约束名或关键错误信息
            if (baseMessage.Contains("unique", StringComparison.OrdinalIgnoreCase) ||
                baseMessage.Contains("duplicate", StringComparison.OrdinalIgnoreCase))
            {
                return $"唯一约束冲突 - {baseMessage}";
            }
            else if (baseMessage.Contains("foreign key", StringComparison.OrdinalIgnoreCase) ||
                     baseMessage.Contains("constraint", StringComparison.OrdinalIgnoreCase))
            {
                return $"约束违反 - {baseMessage}";
            }

            return baseMessage;
        }

        return ex.Message;
    }

    private static ImportBatchDto MapBatchToDto(ImportBatch batch, string accountName)
    {
        return new ImportBatchDto
        {
            Id = batch.Id,
            FileName = batch.FileName,
            AccountId = batch.AccountId,
            AccountName = accountName,
            TotalCount = batch.RecordCount,
            SuccessCount = batch.SuccessCount,
            DuplicateCount = batch.DuplicateCount,
            ErrorCount = batch.ErrorCount,
            ErrorMessage = batch.ErrorMessage,
            Status = batch.Status.ToString(),
            CreatedAt = batch.CreatedAt
        };
    }
}
