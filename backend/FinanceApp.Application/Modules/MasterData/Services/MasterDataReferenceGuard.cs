using System.Linq.Expressions;
using FinanceApp.Application.Modules.MasterData.Interfaces;
using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FinanceApp.Application.Modules.MasterData.Services;

public class MasterDataReferenceGuard : IMasterDataReferenceGuard
{
    private readonly IRepository<Transaction> _transactionRepository;
    private readonly IRepository<BankTransaction> _bankTransactionRepository;
    private readonly IRepository<ImportBatch> _importBatchRepository;
    private readonly IRepository<ClassificationRule> _ruleRepository;
    private readonly IRepository<TransactionAllocation> _allocationRepository;
    private readonly IRepository<Receivable> _receivableRepository;
    private readonly IRepository<Payable> _payableRepository;
    private readonly IRepository<Project> _projectRepository;
    private readonly IRepository<Category> _categoryRepository;

    public MasterDataReferenceGuard(
        IRepository<Transaction> transactionRepository,
        IRepository<BankTransaction> bankTransactionRepository,
        IRepository<ImportBatch> importBatchRepository,
        IRepository<ClassificationRule> ruleRepository,
        IRepository<TransactionAllocation> allocationRepository,
        IRepository<Receivable> receivableRepository,
        IRepository<Payable> payableRepository,
        IRepository<Project> projectRepository,
        IRepository<Category> categoryRepository)
    {
        _transactionRepository = transactionRepository;
        _bankTransactionRepository = bankTransactionRepository;
        _importBatchRepository = importBatchRepository;
        _ruleRepository = ruleRepository;
        _allocationRepository = allocationRepository;
        _receivableRepository = receivableRepository;
        _payableRepository = payableRepository;
        _projectRepository = projectRepository;
        _categoryRepository = categoryRepository;
    }

    public async Task<bool> HasAccountReferencesAsync(long accountId)
    {
        return await AnyActiveAsync(_transactionRepository, t => t.AccountId == accountId)
            || await AnyActiveAsync(_bankTransactionRepository, t => t.AccountId == accountId)
            || await AnyActiveAsync(_importBatchRepository, b => b.AccountId == accountId);
    }

    public async Task<bool> HasCategoryReferencesAsync(long categoryId)
    {
        return await AnyActiveAsync(_transactionRepository, t => t.CategoryId == categoryId)
            || await AnyActiveAsync(_ruleRepository, r => r.CategoryId == categoryId)
            || await AnyActiveAsync(_categoryRepository, c => c.ParentId == categoryId);
    }

    public async Task<bool> HasCustomerReferencesAsync(long customerId)
    {
        return await AnyActiveAsync(_projectRepository, p => p.CustomerId == customerId)
            || await AnyActiveAsync(_transactionRepository, t => t.CustomerId == customerId)
            || await AnyActiveAsync(_receivableRepository, r => r.CustomerId == customerId)
            || await AnyActiveAsync(_payableRepository, p => p.CustomerId == customerId)
            || await AnyActiveAsync(_ruleRepository, r => r.CustomerId == customerId);
    }

    public async Task<bool> HasSupplierReferencesAsync(long supplierId)
    {
        return await AnyActiveAsync(_transactionRepository, t => t.SupplierId == supplierId)
            || await AnyActiveAsync(_receivableRepository, r => r.SupplierId == supplierId)
            || await AnyActiveAsync(_payableRepository, p => p.SupplierId == supplierId)
            || await AnyActiveAsync(_ruleRepository, r => r.SupplierId == supplierId);
    }

    public async Task<bool> HasPersonReferencesAsync(long personId)
    {
        return await AnyActiveAsync(_transactionRepository, t => t.PersonId == personId)
            || await AnyActiveAsync(_allocationRepository, a => a.PersonId == personId)
            || await AnyActiveAsync(_receivableRepository, r => r.PersonId == personId)
            || await AnyActiveAsync(_payableRepository, p => p.PersonId == personId)
            || await AnyActiveAsync(_ruleRepository, r => r.PersonId == personId);
    }

    public async Task<bool> HasProjectReferencesAsync(long projectId)
    {
        return await AnyActiveAsync(_transactionRepository, t => t.ProjectId == projectId)
            || await AnyActiveAsync(_allocationRepository, a => a.ProjectId == projectId)
            || await AnyActiveAsync(_receivableRepository, r => r.ProjectId == projectId)
            || await AnyActiveAsync(_payableRepository, p => p.ProjectId == projectId)
            || await AnyActiveAsync(_ruleRepository, r => r.ProjectId == projectId);
    }

    private static Task<bool> AnyActiveAsync<T>(
        IRepository<T> repository,
        Expression<Func<T, bool>> predicate) where T : BaseEntity
    {
        return repository.GetQueryable()
            .Where(e => !e.IsDeleted)
            .AnyAsync(predicate);
    }
}
