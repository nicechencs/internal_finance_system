using FinanceApp.Application.Modules.MasterData.DTOs.Config;

namespace FinanceApp.Application.Modules.MasterData.Interfaces;

public interface ISiteBrandService
{
    Task<PublicBrandDto> GetPublicBrandAsync();
    Task<PublicBrandDto> UpdateSiteBrandAsync(UpdateSiteBrandRequest request);
}
