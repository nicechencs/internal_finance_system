using FinanceApp.Application.Modules.FinanceSettlement.DTOs.Link;

namespace FinanceApp.Application.Modules.FinanceSettlement.Interfaces;

public interface ILinkService
{
    Task<LinkPreviewResponse> PreviewLinkAsync(LinkPreviewRequest request);
    Task<LinkConfirmResponse> ConfirmLinkAsync(LinkConfirmRequest request);
    Task<RuleRerunPreviewResponse> PreviewRuleRerunAsync(RuleRerunPreviewRequest request);
    Task<RuleRerunConfirmResponse> ConfirmRuleRerunAsync(RuleRerunConfirmRequest request);
    Task<BatchLinkPreviewResponse> PreviewBatchLinkAsync();
    Task<BatchLinkConfirmResponse> ConfirmBatchLinkAsync(BatchLinkConfirmRequest request);
}
