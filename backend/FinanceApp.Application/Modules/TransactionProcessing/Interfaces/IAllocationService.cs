using FinanceApp.Application.Modules.TransactionProcessing.DTOs;
using FinanceApp.Domain.Entities;

namespace FinanceApp.Application.Modules.TransactionProcessing.Interfaces;

public interface IAllocationService
{
    void ValidateAllocations(List<CreateAllocationRequest> allocations, decimal totalAmount);
    decimal CalculateAmountFromRate(decimal totalAmount, decimal rate);
    Task CreateAllocationsAsync(long transactionId, List<CreateAllocationRequest> allocations, decimal totalAmount);
    Task ReplaceAllocationsAsync(Transaction transaction, List<CreateAllocationRequest>? allocations);
}
