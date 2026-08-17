using FinanceApp.Application.Modules.FinanceSettlement.DTOs;

namespace FinanceApp.Application.Modules.FinanceSettlement.Interfaces;

public interface IDataMigrationService
{
    Task<DataMigrationIssuesDto> GetDataIssuesAsync();
    Task FixReceivableAmountAsync(long receivableId);
    Task FixPayableAmountAsync(long payableId);
    Task FixAllAmountIssuesAsync();
}
