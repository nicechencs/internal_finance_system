using FinanceApp.Application.Common;
using FinanceApp.Application.Interfaces;
using FinanceApp.Application.Modules.MasterData.DTOs.Supplier;

namespace FinanceApp.Application.Modules.MasterData.Interfaces;

public interface ISupplierService : ICrudService<SupplierDto, CreateSupplierRequest, UpdateSupplierRequest>
{
    Task<List<SupplierDto>> GetActiveSuppliersAsync();
    Task<BatchCreateResponse<SupplierDto>> BatchCreateAsync(List<CreateSupplierRequest> items);
    Task<SupplierStatisticsDto> GetStatisticsAsync();
    Task<SupplierStatisticsDto> GetStatisticsAsync(PageRequest request);
    Task<SupplierFinanceSummaryDto> GetFinanceSummaryAsync(long supplierId);
}
