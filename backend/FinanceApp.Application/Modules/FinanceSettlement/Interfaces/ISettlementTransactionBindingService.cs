namespace FinanceApp.Application.Modules.FinanceSettlement.Interfaces;

public interface ISettlementTransactionBindingService
{
    Task ValidateReceivableBindingAsync(long transactionId, decimal amount);
    Task ValidateReceivableBindingAsync(long transactionId, decimal amount, long? projectId, long? customerId, long? supplierId, long? personId);
    Task ValidatePayableBindingAsync(long transactionId, decimal amount);
    Task ValidatePayableBindingAsync(long transactionId, decimal amount, long? projectId, long? supplierId, long? customerId, long? personId);
}
