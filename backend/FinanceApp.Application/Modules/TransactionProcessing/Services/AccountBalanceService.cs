using FinanceApp.Application.Common;
using FinanceApp.Application.Modules.TransactionProcessing.Interfaces;
using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Enums;
using FinanceApp.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace FinanceApp.Application.Modules.TransactionProcessing.Services;

public class AccountBalanceService : IAccountBalanceService
{
    private readonly IRepository<Account> _accountRepository;
    private readonly ILogger<AccountBalanceService> _logger;

    public AccountBalanceService(
        IRepository<Account> accountRepository,
        ILogger<AccountBalanceService> logger)
    {
    _accountRepository = accountRepository;
        _logger = logger;
    }

    public async Task<decimal> GetAccountBalanceAsync(long accountId)
    {
        _logger.LogDebug("AccountBalanceService.GetAccountBalanceAsync - 获取账户余额: AccountId={AccountId}", accountId);

        try
        {
            var account = await _accountRepository.GetByIdAsync(accountId);
            if (account == null)
            {
                _logger.LogWarning("账户不存在: AccountId={AccountId}", accountId);
                throw new NotFoundException("账户不存在");
            }

            _logger.LogDebug("账户余额: AccountId={AccountId}, Balance={Balance}",
                accountId, account.CurrentBalance);

            return account.CurrentBalance;
        }
        catch (NotFoundException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取账户余额失败: AccountId={AccountId}", accountId);
            throw;
        }
    }

    public void AdjustBalanceWithoutSave(Account account, decimal amount, TransactionType type)
    {
        var oldBalance = account.CurrentBalance;
        if (type == TransactionType.Income)
        {
            account.CurrentBalance += amount;
        }
        else if (type == TransactionType.Expense)
        {
            account.CurrentBalance -= amount;
        }

        _logger.LogDebug("账户余额原子调整: AccountId={AccountId}, 旧余额={Old}, 新余额={New}, 金额={Amount}, 类型={Type}",
            account.Id, oldBalance, account.CurrentBalance, amount, type);
    }

    public void AdjustBalanceDirectWithoutSave(Account account, decimal delta)
    {
        var oldBalance = account.CurrentBalance;
        account.CurrentBalance += delta;
        _logger.LogDebug("账户余额直接调整: AccountId={AccountId}, 旧余额={Old}, 新余额={New}, 增量={Delta}",
            account.Id, oldBalance, account.CurrentBalance, delta);
    }
}
