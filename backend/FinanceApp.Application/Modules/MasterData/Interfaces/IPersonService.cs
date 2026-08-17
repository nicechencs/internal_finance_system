using FinanceApp.Application.Common;
using FinanceApp.Application.Interfaces;
using FinanceApp.Application.Modules.MasterData.DTOs.Person;

namespace FinanceApp.Application.Modules.MasterData.Interfaces;

public interface IPersonService : ICrudService<PersonDto, CreatePersonRequest, UpdatePersonRequest>
{
    Task<PersonCostSummaryDto> GetPersonCostSummaryAsync(long personId);
    Task<BatchCreateResponse<PersonDto>> BatchCreateAsync(List<CreatePersonRequest> items);
    Task<List<PersonDto>> GetActivePersonsAsync();
    Task<PersonStatisticsDto> GetStatisticsAsync();
    Task<PersonStatisticsDto> GetStatisticsAsync(PageRequest request);
    Task<PersonFinanceSummaryDto> GetFinanceSummaryAsync(long personId);
}
