using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FinanceApp.Application.Modules.FinanceSettlement.Services;

public class TransactionAllocationHelper
{
    private readonly IRepository<Transaction> _transactionRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<TransactionAllocationHelper> _logger;

    public TransactionAllocationHelper(
        IRepository<Transaction> transactionRepository,
        IUnitOfWork unitOfWork,
        ILogger<TransactionAllocationHelper> logger)
    {
        _transactionRepository = transactionRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Transaction?> UpdateAllocationStatusAsync(long transactionId, bool saveChanges = false)
    {
        var transaction = await _transactionRepository.GetQueryable()
            .Include(t => t.ReceivableDetails)
            .Include(t => t.PayableDetails)
            .FirstOrDefaultAsync(t => t.Id == transactionId);

        if (transaction != null)
        {
            transaction.UpdateAllocationStatus();
            _transactionRepository.Update(transaction);

            if (saveChanges)
            {
                await _unitOfWork.SaveChangesAsync();
            }

            _logger.LogInformation("更新交易分配状态, TransactionId={TransactionId}, Status={Status}",
                transaction.Id, transaction.AllocationStatus);
        }

        return transaction;
    }
}
