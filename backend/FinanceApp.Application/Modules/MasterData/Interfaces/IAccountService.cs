using FinanceApp.Application.Common;
using FinanceApp.Application.Interfaces;
using FinanceApp.Application.Modules.MasterData.DTOs.Account;

namespace FinanceApp.Application.Modules.MasterData.Interfaces;

public interface IAccountService : ICrudService<AccountDto, CreateAccountRequest, UpdateAccountRequest>
{
    Task<List<AccountDto>> GetActiveAccountsAsync();
    Task<List<AccountDto>> GetMaturingAccountsAsync(int days = 30);
    Task<BalanceTrendResponse> GetBalanceTrendAsync(long id, int months = 6);
    Task<AccountStatisticsDto> GetStatisticsAsync();
}
