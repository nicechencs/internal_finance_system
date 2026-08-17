using FinanceApp.Application.Common;
using FinanceApp.Application.Modules.Reconciliation.DTOs;

namespace FinanceApp.Application.Modules.Reconciliation.Interfaces;

public interface IImportService
{
    Task<ImportPreviewResponse> PreviewAsync(Stream fileStream, string fileName, long accountId);
    Task<ImportBatchDto> ConfirmAsync(ImportConfirmRequest request);
    Task<PageResponse<ImportBatchDto>> GetBatchesAsync(ImportBatchQueryRequest request);
    Task<ImportBatchDto> GetBatchByIdAsync(long id);
    Task<ImportPreviewResponse> GetCachedPreviewAsync(long batchId);
    Task DeleteBatchAsync(long id);
}
