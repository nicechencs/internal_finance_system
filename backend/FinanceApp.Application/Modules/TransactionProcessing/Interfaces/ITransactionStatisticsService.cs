using FinanceApp.Application.Common;
using FinanceApp.Application.Modules.TransactionProcessing.DTOs;

namespace FinanceApp.Application.Modules.TransactionProcessing.Interfaces;

public interface ITransactionStatisticsService
{
    Task<TransactionStatisticsDto> GetStatisticsAsync();
    Task<TransactionStatisticsDto> GetStatisticsAsync(PageRequest request);
    Task<TransactionStatisticsDto> GetAccountStatisticsAsync(long accountId);
    Task<TransactionStatisticsDto> GetCustomerStatisticsAsync(long customerId);
    Task<TransactionStatisticsDto> GetSupplierStatisticsAsync(long supplierId);
    Task<TransactionStatisticsDto> GetPersonStatisticsAsync(long personId);
    Task<RelatedFinanceRecordDto> GetRelatedFinanceRecordsAsync(long transactionId);
}
