using FinanceApp.Application.Common;
using FinanceApp.Application.Interfaces;
using FinanceApp.Application.Modules.MasterData.DTOs.Project;

namespace FinanceApp.Application.Modules.MasterData.Interfaces;

public interface IProjectService : ICrudService<ProjectDto, CreateProjectRequest, UpdateProjectRequest>
{
    Task<string> GenerateProjectCodeAsync();
    Task<List<ProjectProfitReportDto>> GetProjectProfitReportAsync();
    Task<List<ProjectDto>> GetActiveProjectsAsync();
    Task<BatchCreateResponse<ProjectDto>> BatchCreateAsync(List<CreateProjectRequest> items);
    Task<ProfitAnalysisResponse> GetProfitAnalysisAsync(long id, int months = 12);
    Task<ProjectStatisticsDto> GetStatisticsAsync();
    Task<ProjectStatisticsDto> GetStatisticsAsync(PageRequest request);
    Task InitializeReceivablesAsync(long projectId, InitializeReceivablesRequest request);
    Task RecalculateProjectFinancialsAsync(long projectId);
}
