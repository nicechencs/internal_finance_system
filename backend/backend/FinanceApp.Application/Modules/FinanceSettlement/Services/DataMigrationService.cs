using FinanceApp.Application.Modules.FinanceSettlement.DTOs;
using FinanceApp.Application.Modules.FinanceSettlement.Interfaces;
using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FinanceApp.Application.Modules.FinanceSettlement.Services;

/// <summary>
/// 数据迁移服务
/// </summary>
public class DataMigrationService : IDataMigrationService
{
    private readonly IRepository<Project> _projectRepository;
    private readonly IRepository<Receivable> _receivableRepository;
    private readonly IRepository<ReceivableDetail> _receivableDetailRepository;
    private readonly IRepository<Payable> _payableRepository;
    private readonly IRepository<PayableDetail> _payableDetailRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DataMigrationService> _logger;

    public DataMigrationService(
        IRepository<Project> projectRepository,
        IRepository<Receivable> receivableRepository,
        IRepository<ReceivableDetail> receivableDetailRepository,
        IRepository<Payable> payableRepository,
        IRepository<PayableDetail> payableDetailRepository,
        IUnitOfWork unitOfWork,
        ILogger<DataMigrationService> logger)
    {
        _projectRepository = projectRepository;
        _receivableRepository = receivableRepository;
        _receivableDetailRepository = receivableDetailRepository;
        _payableRepository = payableRepository;
        _payableDetailRepository = payableDetailRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<DataMigrationIssuesDto> GetDataIssuesAsync()
    {
        _logger.LogInformation("开始扫描数据一致性问题");

        var issues = new DataMigrationIssuesDto();

        // 1. 检查项目金额不一致
        var projects = await _projectRepository.GetQueryable()
            .Include(p => p.Customer)
            .Where(p => !p.IsDeleted)
            .ToListAsync();

        foreach (var project in projects)
        {
            var receivableTotal = await _receivableRepository.GetQueryable()
                .Where(r => r.ProjectId == project.Id && !r.IsDeleted)
                .SumAsync(r => r.TotalAmount);

            var difference = project.ContractAmount - receivableTotal;
            if (Math.Abs(difference) > 0.01m)
            {
                issues.ProjectAmountIssues.Add(new ProjectAmountIssue
                {
                    ProjectId = project.Id,
                    ProjectName = project.Name,
                    ContractAmount = project.ContractAmount,
                    ReceivableTotalAmount = receivableTotal,
                    Difference = difference
                });
            }
        }

        // 2. 检查应收款金额不一致
        var receivables = await _receivableRepository.GetQueryable()
            .Include(r => r.Project)
            .Include(r => r.Details)
            .Where(r => !r.IsDeleted)
            .ToListAsync();

        foreach (var receivable in receivables)
        {
            var detailTotal = receivable.Details
                .Where(d => !d.IsDeleted)
                .Sum(d => d.Amount);

            var difference = receivable.ReceivedAmount - detailTotal;
            if (Math.Abs(difference) > 0.01m)
            {
                issues.ReceivableAmountIssues.Add(new ReceivableAmountIssue
                {
                    ReceivableId = receivable.Id,
                    ProjectName = receivable.Project?.Name ?? "未知项目",
                    TotalAmount = receivable.TotalAmount,
                    ReceivedAmount = receivable.ReceivedAmount,
                    DetailTotalAmount = detailTotal,
                    Difference = difference
                });
            }
        }

        // 3. 检查应付款金额不一致
        var payables = await _payableRepository.GetQueryable()
            .Include(p => p.Project)
            .Include(p => p.Details)
            .Where(p => !p.IsDeleted)
            .ToListAsync();

        foreach (var payable in payables)
        {
            var detailTotal = payable.Details
                .Where(d => !d.IsDeleted)
                .Sum(d => d.Amount);

            var difference = payable.PaidAmount - detailTotal;
            if (Math.Abs(difference) > 0.01m)
            {
                issues.PayableAmountIssues.Add(new PayableAmountIssue
                {
                    PayableId = payable.Id,
                    ProjectName = payable.Project?.Name,
                    TotalAmount = payable.TotalAmount,
                    PaidAmount = payable.PaidAmount,
                    DetailTotalAmount = detailTotal,
                    Difference = difference
                });
            }
        }

        // 4. 检查未关联交易的收款记录
        var unlinkedReceivableDetails = await _receivableDetailRepository.GetQueryable()
            .Include(rd => rd.Receivable)
            .ThenInclude(r => r.Project)
            .Where(rd => rd.TransactionId == null && !rd.IsDeleted)
            .ToListAsync();

        issues.UnlinkedReceivableDetails = unlinkedReceivableDetails.Select(rd => new UnlinkedReceivableDetail
        {
            Id = rd.Id,
            ReceivableId = rd.ReceivableId,
            ProjectName = rd.Receivable?.Project?.Name ?? "未知项目",
            PaymentDate = rd.PaymentDate,
            Amount = rd.Amount
        }).ToList();

        // 5. 检查未关联交易的付款记录
        var unlinkedPayableDetails = await _payableDetailRepository.GetQueryable()
            .Include(pd => pd.Payable)
            .ThenInclude(p => p.Project)
            .Where(pd => pd.TransactionId == null && !pd.IsDeleted)
            .ToListAsync();

        issues.UnlinkedPayableDetails = unlinkedPayableDetails.Select(pd => new UnlinkedPayableDetail
        {
            Id = pd.Id,
            PayableId = pd.PayableId,
            ProjectName = pd.Payable?.Project?.Name,
            PaymentDate = pd.PaymentDate,
            Amount = pd.Amount
        }).ToList();

        _logger.LogInformation("数据一致性扫描完成: 项目问题={ProjectIssues}, 应收款问题={ReceivableIssues}, 应付款问题={PayableIssues}, 未关联收款={UnlinkedReceivable}, 未关联付款={UnlinkedPayable}",
            issues.ProjectAmountIssues.Count,
            issues.ReceivableAmountIssues.Count,
            issues.PayableAmountIssues.Count,
            issues.UnlinkedReceivableDetails.Count,
            issues.UnlinkedPayableDetails.Count);

        return issues;
    }

    public async Task FixReceivableAmountAsync(long receivableId)
    {
        _logger.LogInformation("修复应收款金额, ReceivableId={ReceivableId}", receivableId);

        var receivable = await _receivableRepository.GetQueryable()
            .Include(r => r.Details)
            .FirstOrDefaultAsync(r => r.Id == receivableId);

        if (receivable == null)
        {
            throw new Exception($"应收款不存在: {receivableId}");
        }

        var detailTotal = receivable.Details
            .Where(d => !d.IsDeleted)
            .Sum(d => d.Amount);

        receivable.ReceivedAmount = detailTotal;
        receivable.RemainingAmount = receivable.TotalAmount - detailTotal;

        // 更新状态
        if (receivable.RemainingAmount == 0)
        {
            receivable.Status = Domain.Enums.ReceivableStatus.Settled;
            receivable.SettledAt = DateTime.UtcNow;
        }
        else if (receivable.ReceivedAmount > 0)
        {
            receivable.Status = Domain.Enums.ReceivableStatus.Partial;
        }
        else
        {
            receivable.Status = Domain.Enums.ReceivableStatus.Pending;
        }

        _receivableRepository.Update(receivable);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("应收款金额修复完成, ReceivableId={ReceivableId}, ReceivedAmount={ReceivedAmount}",
            receivableId, receivable.ReceivedAmount);
    }

    public async Task FixPayableAmountAsync(long payableId)
    {
        _logger.LogInformation("修复应付款金额, PayableId={PayableId}", payableId);

        var payable = await _payableRepository.GetQueryable()
            .Include(p => p.Details)
            .FirstOrDefaultAsync(p => p.Id == payableId);

        if (payable == null)
        {
            throw new Exception($"应付款不存在: {payableId}");
        }

        var detailTotal = payable.Details
            .Where(d => !d.IsDeleted)
            .Sum(d => d.Amount);

        payable.PaidAmount = detailTotal;
        payable.RemainingAmount = payable.TotalAmount - detailTotal;

        // 更新状态
        if (payable.RemainingAmount == 0)
        {
            payable.Status = Domain.Enums.PayableStatus.Settled;
            payable.SettledAt = DateTime.UtcNow;
        }
        else if (payable.PaidAmount > 0)
        {
            payable.Status = Domain.Enums.PayableStatus.Partial;
        }
        else
        {
            payable.Status = Domain.Enums.PayableStatus.Pending;
        }

        _payableRepository.Update(payable);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("应付款金额修复完成, PayableId={PayableId}, PaidAmount={PaidAmount}",
            payableId, payable.PaidAmount);
    }

    public async Task FixAllAmountIssuesAsync()
    {
        _logger.LogInformation("开始批量修复所有金额不一致问题");

        var issues = await GetDataIssuesAsync();

        // 修复所有应收款
        foreach (var issue in issues.ReceivableAmountIssues)
        {
            await FixReceivableAmountAsync(issue.ReceivableId);
        }

        // 修复所有应付款
        foreach (var issue in issues.PayableAmountIssues)
        {
            await FixPayableAmountAsync(issue.PayableId);
        }

        _logger.LogInformation("批量修复完成, 应收款={ReceivableCount}, 应付款={PayableCount}",
            issues.ReceivableAmountIssues.Count,
            issues.PayableAmountIssues.Count);
    }
}
