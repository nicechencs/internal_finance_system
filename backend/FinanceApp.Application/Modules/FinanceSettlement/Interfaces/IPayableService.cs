using FinanceApp.Application.Common;
using FinanceApp.Application.Interfaces;
using FinanceApp.Application.Modules.FinanceSettlement.DTOs.Payable;

namespace FinanceApp.Application.Modules.FinanceSettlement.Interfaces;

public interface IPayableService : ICrudService<PayableDto, CreatePayableRequest, UpdatePayableRequest>
{
    Task<PayableDto> PayPaymentAsync(long payableId, PayPaymentRequest request);
    Task<PayableSummaryDto> GetPayableSummaryAsync();
    Task<PayableTrendDto> GetTrendAsync(DateTime? startDate, DateTime? endDate);
    Task<PayableAgingDto> GetAgingAsync();
    Task<List<PayableDto>> GetByCustomerIdAsync(long customerId);
    Task<List<PayableDto>> GetBySupplierIdAsync(long supplierId);
    Task<List<PayableDto>> GetByPersonIdAsync(long personId);
    Task<List<PayableDto>> GetByProjectIdAsync(long projectId);
    Task<PayableStatisticsDto> GetStatisticsAsync(PageRequest request);
}
