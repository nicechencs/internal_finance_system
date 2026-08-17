using FinanceApp.Application.Common;
using FinanceApp.Application.Modules.Identity.Interfaces;
using FinanceApp.Application.Modules.MasterData.DTOs.FixedDeposit;
using FinanceApp.Application.Modules.MasterData.Interfaces;
using FinanceApp.Application.Modules.TransactionProcessing.DTOs;
using FinanceApp.Application.Modules.TransactionProcessing.Interfaces;
using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Enums;
using FinanceApp.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FinanceApp.Application.Modules.TransactionProcessing.Services;

public class TransferService : ITransferService
{
    private readonly IRepository<Transaction> _transactionRepository;
    private readonly IRepository<Account> _accountRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITransactionQueryService _queryService;
    private readonly IAuditLogService _auditLogService;
    private readonly IFixedDepositService _fixedDepositService;
    private readonly ILogger<TransferService> _logger;

    public TransferService(
        IRepository<Transaction> transactionRepository,
        IRepository<Account> accountRepository,
        IUnitOfWork unitOfWork,
        ITransactionQueryService queryService,
        IAuditLogService auditLogService,
        IFixedDepositService fixedDepositService,
        ILogger<TransferService> logger)
    {
        _transactionRepository = transactionRepository;
        _accountRepository = accountRepository;
        _unitOfWork = unitOfWork;
        _queryService = queryService;
        _auditLogService = auditLogService;
        _fixedDepositService = fixedDepositService;
        _logger = logger;
    }

    public async Task<TransferResultDto> CreateTransferAsync(CreateTransferRequest request)
    {
        _logger.LogInformation(
            "开始创建转账: FromAccount={FromAccountId}, ToAccount={ToAccountId}, Amount={Amount}, Date={Date}",
            request.FromAccountId,
            request.ToAccountId,
            request.Amount,
            request.TransactionDate);

        if (request.FromAccountId == request.ToAccountId)
        {
            throw new ValidationException("转出和转入账户不能相同");
        }

        if (request.Amount <= 0)
        {
            throw new ValidationException("转账金额必须大于0");
        }

        await using var dbTransaction = await _unitOfWork.BeginTransactionAsync();

        try
        {
            // 账户在事务开启后加载，配合 Account 的乐观并发版本令牌，
            // 保证余额读改写在提交时若被其他操作抢先修改会触发并发冲突而非丢失更新。
            var fromAccount = await _accountRepository.GetByIdAsync(request.FromAccountId)
                ?? throw new NotFoundException("转出账户不存在");
            var toAccount = await _accountRepository.GetByIdAsync(request.ToAccountId)
                ?? throw new NotFoundException("转入账户不存在");

            // 校验转出账户余额是否充足
            if (fromAccount.CurrentBalance < request.Amount)
            {
                _logger.LogWarning("转账失败，余额不足: Account={AccountId}, CurrentBalance={Balance}, RequestAmount={Amount}",
                    fromAccount.Id, fromAccount.CurrentBalance, request.Amount);
                throw new ValidationException($"转出账户余额不足，当前余额 {fromAccount.CurrentBalance}");
            }

            var outTransaction = new Transaction
            {
                TransactionDate = request.TransactionDate,
                Amount = request.Amount,
                TransactionType = TransactionType.Transfer,
                TransferDirection = TransferDirection.Out,
                AccountId = request.FromAccountId,
                Description = BuildDescription(request.Description, toAccount, TransferDirection.Out),
                Status = TransactionStatus.Confirmed,
                IsAllocated = false
            };

            await _transactionRepository.AddAsync(outTransaction);
            await _unitOfWork.SaveChangesAsync();

            var inTransaction = new Transaction
            {
                TransactionDate = request.TransactionDate,
                Amount = request.Amount,
                TransactionType = TransactionType.Transfer,
                TransferDirection = TransferDirection.In,
                AccountId = request.ToAccountId,
                Description = BuildDescription(request.Description, fromAccount, TransferDirection.In),
                Status = TransactionStatus.Confirmed,
                IsAllocated = false,
                RelatedTransactionId = outTransaction.Id
            };

            await _transactionRepository.AddAsync(inTransaction);
            await _unitOfWork.SaveChangesAsync();

            outTransaction.RelatedTransactionId = inTransaction.Id;
            _transactionRepository.Update(outTransaction);

            fromAccount.CurrentBalance -= request.Amount;
            toAccount.CurrentBalance += request.Amount;
            _accountRepository.Update(fromAccount);
            _accountRepository.Update(toAccount);

            await _unitOfWork.SaveChangesAsync();
            if (dbTransaction != null)
            {
                await dbTransaction.CommitAsync();
            }

            _logger.LogInformation("转账成功: OutTxId={OutTxId}, InTxId={InTxId}, Amount={Amount}",
                outTransaction.Id, inTransaction.Id, request.Amount);

            var outSnapshot = System.Text.Json.JsonSerializer.Serialize(new { outTransaction.Id, outTransaction.Amount, outTransaction.AccountId, Direction = "Out", PairId = inTransaction.Id });
            var inSnapshot = System.Text.Json.JsonSerializer.Serialize(new { inTransaction.Id, inTransaction.Amount, inTransaction.AccountId, Direction = "In", PairId = outTransaction.Id });
            await _auditLogService.LogAsync("Transfer", "Transaction", outTransaction.Id, null, outSnapshot);
            await _auditLogService.LogAsync("Transfer", "Transaction", inTransaction.Id, null, inSnapshot);

            var outDto = await _queryService.GetByIdAsync(outTransaction.Id);
            var inDto = await _queryService.GetByIdAsync(inTransaction.Id);

            // 定期存款自动联动
            FixedDepositLinkageInfo? linkage = null;

            if (toAccount.AccountType == AccountType.FixedDeposit)
            {
                // 转入定期账户 → 自动创建定期存款记录
                if (request.TermMonths.HasValue && request.InterestRate.HasValue)
                {
                    try
                    {
                        var createRequest = new CreateFixedDepositRequest
                        {
                            AccountId = toAccount.Id,
                            Principal = request.Amount,
                            TermMonths = request.TermMonths.Value,
                            InterestRate = request.InterestRate.Value,
                            DepositDate = request.TransactionDate,
                            DepositTransactionId = inTransaction.Id,
                            Notes = $"由转账自动创建（交易 #{inTransaction.Id}）"
                        };
                        var depositDto = await _fixedDepositService.CreateAsync(createRequest);
                        linkage = new FixedDepositLinkageInfo
                        {
                            Action = "Created",
                            FixedDepositId = depositDto.Id,
                            Message = $"已自动创建定期存款记录，本金 {request.Amount:N2}，期限 {request.TermMonths} 个月"
                        };
                        _logger.LogInformation("转账联动：自动创建定期存款 Id={DepositId}", depositDto.Id);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "转账联动：自动创建定期存款失败，转账本身已成功");
                    }
                }
                else
                {
                    _logger.LogInformation("转入定期账户但未提供期限/利率参数，跳过自动创建定期记录");
                }
            }
            else if (fromAccount.AccountType == AccountType.FixedDeposit)
            {
                // 从定期账户转出 → 自动匹配并支取
                try
                {
                    var activeRecords = await _fixedDepositService.GetByAccountAsync(fromAccount.Id);
                    var candidate = activeRecords
                        .Where(r => r.Status == "Active" || r.Status == "Matured")
                        .OrderBy(r => Math.Abs(r.Principal - request.Amount))
                        .FirstOrDefault();

                    if (candidate != null && Math.Abs(candidate.Principal - request.Amount) <= Math.Min(candidate.Principal * 0.01m, 100m))
                    {
                        var withdrawRequest = new WithdrawFixedDepositRequest
                        {
                            TransactionId = outTransaction.Id,
                            WithdrawalDate = request.TransactionDate
                        };
                        var withdrawnDto = await _fixedDepositService.WithdrawAsync(candidate.Id, withdrawRequest);
                        linkage = new FixedDepositLinkageInfo
                        {
                            Action = "Withdrawn",
                            FixedDepositId = candidate.Id,
                            Message = $"已自动支取定期存款，本金 {candidate.Principal:N2}，实际利息 {withdrawnDto.ActualInterest:N2}"
                        };
                        _logger.LogInformation("转账联动：自动支取定期存款 Id={DepositId}", candidate.Id);
                    }
                    else
                    {
                        _logger.LogInformation("从定期账户转出但未找到匹配的定期记录，跳过自动支取");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "转账联动：自动支取定期存款失败，转账本身已成功");
                }
            }

            return new TransferResultDto
            {
                OutTransaction = outDto,
                InTransaction = inDto,
                FixedDepositLinkage = linkage
            };
        }
        catch (DbUpdateConcurrencyException ex)
        {
            if (dbTransaction != null)
            {
                await dbTransaction.RollbackAsync();
            }

            _logger.LogWarning(ex, "转账失败：检测到账户并发更新冲突，事务已回滚: FromAccount={FromAccountId}, ToAccount={ToAccountId}",
                request.FromAccountId, request.ToAccountId);
            throw new ValidationException("账户正在被其他操作更新，请稍后重试");
        }
        catch (Exception ex)
        {
            if (dbTransaction != null)
            {
                _logger.LogWarning(ex, "转账过程中发生异常，事务已回滚: {Message}", ex.Message);
                await dbTransaction.RollbackAsync();
            }

            if (ex is ValidationException or NotFoundException) throw;

            _logger.LogError(ex, "创建转账失败: FromAccount={FromAccountId}, ToAccount={ToAccountId}",
                request.FromAccountId, request.ToAccountId);
            throw;
        }
    }

    private static string BuildDescription(string? description, Account counterpartAccount, TransferDirection direction)
    {
        return string.IsNullOrWhiteSpace(description)
            ? TransactionBalanceHelper.BuildDefaultTransferDescription(counterpartAccount, direction)
            : description;
    }
}
