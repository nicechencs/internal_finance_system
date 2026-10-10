using FinanceApp.Application.Modules.MasterData.DTOs.Person;

namespace FinanceApp.Application.Modules.MasterData.Interfaces;

public interface IPersonDeliveryService
{
    Task<List<PersonDeliveryDto>> GetByPersonAsync(long personId);
    Task<PersonDeliveryDto> CreateAsync(long personId, CreatePersonDeliveryRequest request);
    Task<PersonDeliveryDto> UpdateAsync(long personId, long id, UpdatePersonDeliveryRequest request);
    Task DeleteAsync(long personId, long id);
}
