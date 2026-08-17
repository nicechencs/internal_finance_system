using FinanceApp.Application.Modules.MasterData.DTOs.FixedDeposit;
using FinanceApp.Application.Modules.TransactionProcessing.DTOs;

namespace FinanceApp.Application.Modules.MasterData.Interfaces;

public interface IFixedDepositService
{
    Task<FixedDepositDto> CreateAsync(CreateFixedDepositRequest request);
    Task<FixedDepositDto> UpdateAsync(long id, UpdateFixedDepositRequest request);
    Task<List<FixedDepositDto>> GetAllAsync(GetFixedDepositsRequest request);
    Task<List<FixedDepositDto>> GetByAccountAsync(long accountId);
    Task<FixedDepositDto> GetByIdAsync(long id);
    Task<FixedDepositDto> WithdrawAsync(long id, WithdrawFixedDepositRequest request);
    Task<List<FixedDepositDto>> GetMaturingAsync(int days = 30);
    Task<FixedDepositStatisticsDto> GetStatisticsAsync(GetFixedDepositsRequest request);
    Task DeleteAsync(long id);
    Task<List<TransactionDto>> GetWithdrawalCandidatesAsync(long id);
}
