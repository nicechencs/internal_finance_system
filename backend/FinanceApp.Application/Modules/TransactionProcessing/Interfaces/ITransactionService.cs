using FinanceApp.Application.Common;
using FinanceApp.Application.Interfaces;
using FinanceApp.Application.Modules.TransactionProcessing.DTOs;

namespace FinanceApp.Application.Modules.TransactionProcessing.Interfaces;

public interface ITransactionService : ICrudService<TransactionDto, CreateTransactionRequest, UpdateTransactionRequest>
{
    Task<List<TransactionDto>> GetByAccountAsync(long accountId);
    Task<List<TransactionDto>> GetByProjectAsync(long projectId);
    Task<List<TransactionDto>> GetByCategoryAsync(long categoryId);
    Task<List<TransactionDto>> GetByCustomerAsync(long customerId);
    Task<List<TransactionDto>> GetBySupplierAsync(long supplierId);
    Task<List<TransactionDto>> GetByPersonAsync(long personId);
    Task<List<TransactionDto>> GetTransferCandidatesAsync(long transactionId, long targetAccountId);
    Task<decimal> GetAccountBalanceAsync(long accountId);
    Task<TransferResultDto> CreateTransferAsync(CreateTransferRequest request);
    Task<TransferResultDto> ConvertToTransferAsync(long transactionId, ConvertTransactionToTransferRequest request);
    Task<TransactionStatisticsDto> GetStatisticsAsync();
    Task<TransactionStatisticsDto> GetStatisticsAsync(PageRequest request);
    Task<TransactionStatisticsDto> GetAccountStatisticsAsync(long accountId);
    Task<TransactionStatisticsDto> GetCustomerStatisticsAsync(long customerId);
    Task<TransactionStatisticsDto> GetSupplierStatisticsAsync(long supplierId);
    Task<TransactionStatisticsDto> GetPersonStatisticsAsync(long personId);
    Task<RelatedFinanceRecordDto> GetRelatedFinanceRecordsAsync(long transactionId);
    Task<List<TransactionDto>> GetAvailableForReceivableAsync(long? projectId = null, long? customerId = null, long? supplierId = null, long? personId = null, bool showAll = false, string? keyword = null);
    Task<List<TransactionDto>> GetAvailableForPayableAsync(long? projectId = null, long? supplierId = null, long? customerId = null, long? personId = null, bool showAll = false, string? keyword = null);
}
