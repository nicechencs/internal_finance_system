using FinanceApp.Application.Modules.FinanceSettlement.DTOs;
using FinanceApp.Application.Modules.FinanceSettlement.Interfaces;
using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FinanceApp.Application.Modules.FinanceSettlement.Services;

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
        var issues = new DataMigrationIssuesDto();
        return issues;
    }

    public async Task FixReceivableAmountAsync(long receivableId)
    {
        await Task.CompletedTask;
    }

    public async Task FixPayableAmountAsync(long payableId)
    {
        await Task.CompletedTask;
    }

    public async Task FixAllAmountIssuesAsync()
    {
        await Task.CompletedTask;
    }
}
