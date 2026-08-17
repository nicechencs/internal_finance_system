using FinanceApp.Application.Common;
using FinanceApp.Application.Modules.TransactionProcessing.DTOs;

namespace FinanceApp.Application.Modules.TransactionProcessing.Interfaces;

public interface ITransactionQueryService
{
    Task<PageResponse<TransactionDto>> GetPagedAsync(PageRequest request);
    Task<TransactionDto> GetByIdAsync(long id);
    Task<List<TransactionDto>> GetByAccountAsync(long accountId);
    Task<List<TransactionDto>> GetByProjectAsync(long projectId);
    Task<List<TransactionDto>> GetByCategoryAsync(long categoryId);
    Task<List<TransactionDto>> GetByCustomerAsync(long customerId);
    Task<List<TransactionDto>> GetBySupplierAsync(long supplierId);
    Task<List<TransactionDto>> GetByPersonAsync(long personId);
}
