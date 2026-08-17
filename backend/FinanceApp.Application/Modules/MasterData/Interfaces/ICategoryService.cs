using FinanceApp.Application.Common;
using FinanceApp.Application.Interfaces;
using FinanceApp.Application.Modules.MasterData.DTOs.Category;

namespace FinanceApp.Application.Modules.MasterData.Interfaces;

public interface ICategoryService : ICrudService<CategoryDto, CreateCategoryRequest, UpdateCategoryRequest>
{
    Task<List<CategoryDto>> GetActiveCategoriesAsync();
    Task<List<CategoryDto>> GetCategoriesByTypeAsync(string type);
    Task<CategoryStatisticsDto> GetStatisticsAsync();
    Task<CategoryStatisticsDto> GetStatisticsAsync(PageRequest request);
}
