using FinanceApp.Application.Common;
using FinanceApp.Application.Interfaces;
using FinanceApp.Application.Modules.MasterData.DTOs.Customer;

namespace FinanceApp.Application.Modules.MasterData.Interfaces;

public interface ICustomerService : ICrudService<CustomerDto, CreateCustomerRequest, UpdateCustomerRequest>
{
    Task<List<CustomerDto>> GetActiveCustomersAsync();
    Task<BatchCreateResponse<CustomerDto>> BatchCreateAsync(List<CreateCustomerRequest> items);
    Task<CustomerStatisticsDto> GetStatisticsAsync();
    Task<CustomerStatisticsDto> GetStatisticsAsync(PageRequest request);
    Task<CustomerFinanceSummaryDto> GetFinanceSummaryAsync(long customerId);
}
