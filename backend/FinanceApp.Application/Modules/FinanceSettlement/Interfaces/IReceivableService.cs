using FinanceApp.Application.Common;
using FinanceApp.Application.Interfaces;
using FinanceApp.Application.Modules.FinanceSettlement.DTOs.Receivable;

namespace FinanceApp.Application.Modules.FinanceSettlement.Interfaces;

public interface IReceivableService : ICrudService<ReceivableDto, CreateReceivableRequest, UpdateReceivableRequest>
{
    Task<ReceivableDto> ReceivePaymentAsync(long receivableId, ReceivePaymentRequest request);
    Task<ReceivableSummaryDto> GetReceivableSummaryAsync();
    Task<ReceivableTrendDto> GetTrendAsync(DateTime? startDate, DateTime? endDate);
    Task<ReceivableAgingDto> GetAgingAsync();
    Task<List<ReceivableDto>> GetByProjectIdAsync(long projectId);
    Task<List<ReceivableDto>> GetByCustomerIdAsync(long customerId);
    Task<List<ReceivableDto>> GetBySupplierIdAsync(long supplierId);
    Task<List<ReceivableDto>> GetByPersonIdAsync(long personId);
    Task<ReceivableStatisticsDto> GetStatisticsAsync(PageRequest request);
}
