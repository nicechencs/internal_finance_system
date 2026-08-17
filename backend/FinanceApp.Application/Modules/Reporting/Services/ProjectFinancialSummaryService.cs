using FinanceApp.Application.Modules.Reporting.Interfaces;
using FinanceApp.Application.Modules.Reporting.Models;
using FinanceApp.Application.Services;
using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FinanceApp.Application.Modules.Reporting.Services;

public class ProjectFinancialSummaryService : ServiceBase, IProjectFinancialSummaryService
{
    private readonly IRepository<Project> _projectRepository;
    private readonly IRepository<Receivable> _receivableRepository;
    private readonly IRepository<PayableDetail> _payableDetailRepository;
    private readonly ILogger<ProjectFinancialSummaryService> _logger;

    public ProjectFinancialSummaryService(
        IRepository<Project> projectRepository,
        IRepository<Receivable> receivableRepository,
        IRepository<PayableDetail> payableDetailRepository,
        ILogger<ProjectFinancialSummaryService> logger,
        ICurrentUserService currentUserService,
        IDataPermissionService permissionService)
        : base(currentUserService, permissionService)
    {
        _projectRepository = projectRepository;
        _receivableRepository = receivableRepository;
        _payableDetailRepository = payableDetailRepository;
        _logger = logger;
    }

    public async Task<ProjectFinancialSummary> GetProjectSummaryAsync(long projectId)
    {
        var summaries = await GetProjectSummariesAsync(new[] { projectId });
        if (!summaries.TryGetValue(projectId, out var summary))
        {
            _logger.LogDebug("Project summary fallback for missing or inaccessible project: ProjectId={ProjectId}", projectId);
            return new ProjectFinancialSummary { ProjectId = projectId };
        }

        return summary;
    }

    public async Task<IReadOnlyDictionary<long, ProjectFinancialSummary>> GetProjectSummariesAsync(
        IReadOnlyCollection<long> projectIds)
    {
        if (projectIds == null || projectIds.Count == 0)
            return new Dictionary<long, ProjectFinancialSummary>();

        var idList = projectIds.Distinct().ToList();

        var projects = await ApplyPermissionFilter(_projectRepository.GetQueryable())
            .Where(p => idList.Contains(p.Id))
            .Select(p => new { p.Id, p.ContractAmount })
            .ToListAsync();

        if (projects.Count == 0)
            return new Dictionary<long, ProjectFinancialSummary>();

        var foundIds = projects.Select(p => p.Id).ToList();

        var receivableGroups = await ApplyPermissionFilter(_receivableRepository.GetQueryable())
            .Where(r => !r.IsDeleted && foundIds.Contains(r.ProjectId))
            .GroupBy(r => r.ProjectId)
            .Select(g => new
            {
                ProjectId = g.Key,
                ReceivedAmount = g.Sum(r => r.ReceivedAmount),
                RemainingAmount = g.Sum(r => r.RemainingAmount)
            })
            .ToListAsync();

        var costGroups = await ApplyPermissionFilter(_payableDetailRepository.GetQueryable())
            .Where(pd => !pd.IsDeleted
                         && pd.Payable != null
                         && !pd.Payable.IsDeleted
                         && pd.Payable.ProjectId != null
                         && foundIds.Contains(pd.Payable.ProjectId.Value))
            .GroupBy(pd => pd.Payable!.ProjectId!.Value)
            .Select(g => new
            {
                ProjectId = g.Key,
                Amount = g.Sum(pd => pd.Amount)
            })
            .ToListAsync();

        var receivedByProject = receivableGroups.ToDictionary(x => x.ProjectId, x => x.ReceivedAmount);
        var remainingByProject = receivableGroups.ToDictionary(x => x.ProjectId, x => x.RemainingAmount);
        var costByProject = costGroups.ToDictionary(x => x.ProjectId, x => x.Amount);

        var result = new Dictionary<long, ProjectFinancialSummary>(projects.Count);
        foreach (var project in projects)
        {
            receivedByProject.TryGetValue(project.Id, out var receivedAmount);
            remainingByProject.TryGetValue(project.Id, out var receivableAmount);
            costByProject.TryGetValue(project.Id, out var totalCost);
            result[project.Id] = BuildSummary(project.Id, project.ContractAmount, receivedAmount, receivableAmount, totalCost);
        }

        return result;
    }

    private static ProjectFinancialSummary BuildSummary(
        long projectId,
        decimal contractAmount,
        decimal receivedAmount,
        decimal receivableAmount,
        decimal totalCost)
    {
        var profitAmount = receivedAmount - totalCost;
        var profitRate = contractAmount > 0
            ? Math.Round(profitAmount / contractAmount * 100, 2)
            : 0m;

        return new ProjectFinancialSummary
        {
            ProjectId = projectId,
            ContractAmount = contractAmount,
            ReceivedAmount = receivedAmount,
            ReceivableAmount = receivableAmount,
            DirectCost = 0m,
            AllocatedCost = 0m,
            TotalCost = totalCost,
            ProfitAmount = profitAmount,
            ProfitRate = profitRate
        };
    }
}
