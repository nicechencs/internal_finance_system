using FinanceApp.Application.Common;
using FinanceApp.Application.Modules.FinanceSettlement.Interfaces;
using FinanceApp.Application.Modules.Identity.Interfaces;
using FinanceApp.Application.Services;
using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Enums;
using FinanceApp.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FinanceApp.Application.Modules.FinanceSettlement.Services;

public class SettlementTransactionBindingService : ServiceBase, ISettlementTransactionBindingService
{
    private readonly IRepository<Transaction> _transactionRepository;
    private readonly IRepository<ReceivableDetail> _receivableDetailRepository;
    private readonly IRepository<PayableDetail> _payableDetailRepository;

    public SettlementTransactionBindingService(
        IRepository<Transaction> transactionRepository,
        IRepository<ReceivableDetail> receivableDetailRepository,
        IRepository<PayableDetail> payableDetailRepository,
        ICurrentUserService currentUserService,
        IDataPermissionService permissionService)
        : base(currentUserService, permissionService)
    {
        _transactionRepository = transactionRepository;
        _receivableDetailRepository = receivableDetailRepository;
        _payableDetailRepository = payableDetailRepository;
    }

    public async Task ValidateReceivableBindingAsync(long transactionId, decimal amount)
    {
        await ValidateReceivableBindingAsync(transactionId, amount, null, null, null, null);
    }

    public async Task ValidateReceivableBindingAsync(
        long transactionId,
        decimal amount,
        long? projectId,
        long? customerId,
        long? supplierId,
        long? personId)
    {
        if (transactionId <= 0)
        {
            throw new ValidationException("收款登记必须关联交易记录");
        }

        var transaction = await LoadTransactionForBindingAsync(transactionId);

        if (transaction.TransactionType != TransactionType.Income)
        {
            throw new ValidationException("应收明细只能关联收入交易");
        }

        var hasPayableLinks = await _payableDetailRepository.GetQueryable()
            .AnyAsync(x => !x.IsDeleted && x.TransactionId == transaction.Id);
        if (hasPayableLinks)
        {
            throw new ValidationException("该交易已关联应付明细，不能再关联应收明细");
        }

        BackfillOrValidateProject(
            transaction,
            projectId,
            "交易的项目与应收款的项目不一致");

        if (customerId.HasValue)
        {
            BackfillOrValidateCounterparty(
                transaction,
                expectedId: customerId,
                currentId: transaction.CustomerId,
                assign: value => transaction.CustomerId = value,
                mismatchMessage: "交易的客户与应收款的客户不一致",
                mutuallyExclusiveId: transaction.SupplierId);
        }
        else if (supplierId.HasValue)
        {
            BackfillOrValidateCounterparty(
                transaction,
                expectedId: supplierId,
                currentId: transaction.SupplierId,
                assign: value => transaction.SupplierId = value,
                mismatchMessage: "交易的供应商与应收款的供应商不一致",
                mutuallyExclusiveId: transaction.CustomerId);
        }
        if (personId.HasValue)
        {
            // 人员不一致仅作提示，不阻止提交；仅在交易无人员时回填
            if (!transaction.PersonId.HasValue)
            {
                transaction.PersonId = personId.Value;
                _transactionRepository.Update(transaction);
            }
        }

        var linkedAmount = await _receivableDetailRepository.GetQueryable()
            .Where(x => !x.IsDeleted && x.TransactionId == transaction.Id)
            .SumAsync(x => (decimal?)x.Amount) ?? 0m;

        if (linkedAmount + amount > transaction.Amount)
        {
            throw new ValidationException("应收核销金额合计不能超过交易金额");
        }
    }

    public async Task ValidatePayableBindingAsync(long transactionId, decimal amount)
    {
        await ValidatePayableBindingAsync(transactionId, amount, null, null, null, null);
    }

    public async Task ValidatePayableBindingAsync(
        long transactionId,
        decimal amount,
        long? projectId,
        long? supplierId,
        long? customerId,
        long? personId)
    {
        if (transactionId <= 0)
        {
            throw new ValidationException("付款登记必须关联交易记录");
        }

        var transaction = await LoadTransactionForBindingAsync(transactionId);

        if (transaction.TransactionType != TransactionType.Expense)
        {
            throw new ValidationException("应付明细只能关联支出交易");
        }

        var hasReceivableLinks = await _receivableDetailRepository.GetQueryable()
            .AnyAsync(x => !x.IsDeleted && x.TransactionId == transaction.Id);
        if (hasReceivableLinks)
        {
            throw new ValidationException("该交易已关联应收明细，不能再关联应付明细");
        }

        BackfillOrValidateProject(
            transaction,
            projectId,
            "交易的项目与应付款的项目不一致");

        if (supplierId.HasValue)
        {
            BackfillOrValidateCounterparty(
                transaction,
                expectedId: supplierId,
                currentId: transaction.SupplierId,
                assign: value => transaction.SupplierId = value,
                mismatchMessage: "交易的供应商与应付款的供应商不一致",
                mutuallyExclusiveId: transaction.CustomerId);
        }
        else if (customerId.HasValue)
        {
            BackfillOrValidateCounterparty(
                transaction,
                expectedId: customerId,
                currentId: transaction.CustomerId,
                assign: value => transaction.CustomerId = value,
                mismatchMessage: "交易的客户与应付款的客户不一致",
                mutuallyExclusiveId: transaction.SupplierId);
        }
        if (personId.HasValue)
        {
            // 人员不一致仅作提示，不阻止提交；仅在交易无人员时回填
            if (!transaction.PersonId.HasValue)
            {
                transaction.PersonId = personId.Value;
                _transactionRepository.Update(transaction);
            }
        }

        var linkedAmount = await _payableDetailRepository.GetQueryable()
            .Where(x => !x.IsDeleted && x.TransactionId == transaction.Id)
            .SumAsync(x => (decimal?)x.Amount) ?? 0m;

        if (linkedAmount + amount > transaction.Amount)
        {
            throw new ValidationException("应付核销金额合计不能超过交易金额");
        }
    }

    private void BackfillOrValidateProject(Transaction transaction, long? expectedProjectId, string mismatchMessage)
    {
        if (!expectedProjectId.HasValue)
        {
            return;
        }

        // 项目不一致时仅跳过回填，不阻止绑定（允许跨项目分配）
        if (transaction.ProjectId.HasValue && transaction.ProjectId.Value != expectedProjectId.Value)
        {
            return;
        }

        if (!transaction.ProjectId.HasValue)
        {
            transaction.ProjectId = expectedProjectId.Value;
            _transactionRepository.Update(transaction);
        }
    }

    private void BackfillOrValidateCounterparty(
        Transaction transaction,
        long? expectedId,
        long? currentId,
        Action<long> assign,
        string mismatchMessage,
        long? mutuallyExclusiveId = null)
    {
        if (!expectedId.HasValue)
        {
            return;
        }

        // 供应商与客户互斥：不允许同一交易同时关联两种对手方
        if (mutuallyExclusiveId.HasValue)
        {
            throw new ValidationException("该交易已关联其他对手方类型（供应商/客户互斥），不能同时关联");
        }

        if (currentId.HasValue && currentId.Value != expectedId.Value)
        {
            throw new ValidationException(mismatchMessage);
        }

        if (!currentId.HasValue)
        {
            assign(expectedId.Value);
            _transactionRepository.Update(transaction);
        }
    }

    private async Task<Transaction> LoadTransactionForBindingAsync(long transactionId)
    {
        var transaction = await _transactionRepository.GetQueryable()
            .FirstOrDefaultAsync(x => x.Id == transactionId);

        if (transaction == null || transaction.IsDeleted)
        {
            throw new NotFoundException("关联的交易记录不存在");
        }

        EnsureCanEdit(transaction);
        return transaction;
    }
}
