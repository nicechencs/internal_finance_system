namespace FinanceApp.Application.Modules.MasterData.Interfaces;

public interface IMasterDataReferenceGuard
{
    Task<bool> HasAccountReferencesAsync(long accountId);
    Task<bool> HasCategoryReferencesAsync(long categoryId);
    Task<bool> HasCustomerReferencesAsync(long customerId);
    Task<bool> HasSupplierReferencesAsync(long supplierId);
    Task<bool> HasPersonReferencesAsync(long personId);
    Task<bool> HasProjectReferencesAsync(long projectId);
}
