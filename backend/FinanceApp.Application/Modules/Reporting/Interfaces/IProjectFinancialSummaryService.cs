using FinanceApp.Application.Modules.Reporting.Models;

namespace FinanceApp.Application.Modules.Reporting.Interfaces;

public interface IProjectFinancialSummaryService
{
    Task<ProjectFinancialSummary> GetProjectSummaryAsync(long projectId);

    Task<IReadOnlyDictionary<long, ProjectFinancialSummary>> GetProjectSummariesAsync(
        IReadOnlyCollection<long> projectIds);
}
