using FinanceApp.Application.Modules.MasterData.Interfaces;
using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FinanceApp.Application.Modules.MasterData.Services;

public class ProjectFinancialRecalculationService : IProjectFinancialRecalculationService
{
    private readonly IRepository<Project> _projectRepository;
    private readonly IRepository<Receivable> _receivableRepository;
    private readonly IRepository<PayableDetail> _payableDetailRepository;
    private readonly ILogger<ProjectFinancialRecalculationService> _logger;

    public ProjectFinancialRecalculationService(
        IRepository<Project> projectRepository,
        IRepository<Receivable> receivableRepository,
        IRepository<PayableDetail> payableDetailRepository,
        ILogger<ProjectFinancialRecalculationService> logger)
    {
        _projectRepository = projectRepository;
        _receivableRepository = receivableRepository;
        _payableDetailRepository = payableDetailRepository;
        _logger = logger;
    }

    public async Task RecalculateAsync(long projectId)
    {
        var project = await _projectRepository.GetByIdAsync(projectId);
        if (project == null) return;

        // 从应收表重新计算已收款和应收款
        var receivedAmount = await _receivableRepository.GetQueryable()
            .Where(r => !r.IsDeleted && r.ProjectId == projectId)
            .SumAsync(r => (decimal?)r.ReceivedAmount) ?? 0m;

        var receivableAmount = await _receivableRepository.GetQueryable()
            .Where(r => !r.IsDeleted && r.ProjectId == projectId)
            .SumAsync(r => (decimal?)r.RemainingAmount) ?? 0m;

        project.ReceivedAmount = receivedAmount;
        project.ReceivableAmount = receivableAmount;

        // 从应付明细重新计算总成本
        var totalCost = await _payableDetailRepository.GetQueryable()
            .Include(pd => pd.Payable)
            .Where(pd => !pd.IsDeleted && pd.Payable != null && !pd.Payable.IsDeleted && pd.Payable.ProjectId == projectId)
            .SumAsync(pd => (decimal?)pd.Amount) ?? 0m;

        project.TotalCost = totalCost;
        project.ProfitAmount = project.ReceivedAmount - project.TotalCost;
        project.ProfitRate = project.ContractAmount > 0
            ? Math.Round(project.ProfitAmount / project.ContractAmount * 100, 2)
            : 0;

        _projectRepository.Update(project);

        _logger.LogDebug("重算项目财务汇总: ProjectId={ProjectId}, 已收款={ReceivedAmount}, 应收款={ReceivableAmount}, 总成本={TotalCost}, 利润={ProfitAmount}, 利润率={ProfitRate}%",
            projectId, project.ReceivedAmount, project.ReceivableAmount, project.TotalCost, project.ProfitAmount, project.ProfitRate);
    }
}
