using FinanceApp.Application.Modules.FinanceSettlement.DTOs.Payable;
using FinanceApp.Application.Modules.FinanceSettlement.DTOs.Receivable;

namespace FinanceApp.Application.Modules.FinanceSettlement.Interfaces;

public interface ISettlementCandidateService
{
    Task<List<ReceivableDto>> GetAvailableReceivablesForTransactionAsync(long transactionId, string? keyword = null);
    Task<List<PayableDto>> GetAvailablePayablesForTransactionAsync(long transactionId, string? keyword = null);
}
