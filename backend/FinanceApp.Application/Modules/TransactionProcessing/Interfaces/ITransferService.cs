using FinanceApp.Application.Modules.TransactionProcessing.DTOs;

namespace FinanceApp.Application.Modules.TransactionProcessing.Interfaces;

public interface ITransferService
{
    Task<TransferResultDto> CreateTransferAsync(CreateTransferRequest request);
}
