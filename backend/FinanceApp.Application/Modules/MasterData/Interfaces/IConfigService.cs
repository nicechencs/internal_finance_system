using FinanceApp.Application.Modules.MasterData.DTOs.Config;

namespace FinanceApp.Application.Modules.MasterData.Interfaces;

public interface IConfigService
{
    Task<List<ConfigDto>> GetAllConfigsAsync();
    Task<ConfigDto> GetConfigByKeyAsync(string key);
    Task UpdateConfigAsync(string key, string value);
}
