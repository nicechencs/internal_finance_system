using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Enums;

namespace FinanceApp.Application.Modules.TransactionProcessing.Interfaces;

public interface IAccountBalanceService
{
    Task<decimal> GetAccountBalanceAsync(long accountId);
    void AdjustBalanceWithoutSave(Account account, decimal amount, TransactionType type);
    void AdjustBalanceDirectWithoutSave(Account account, decimal delta);
}
