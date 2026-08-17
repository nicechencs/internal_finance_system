using FinanceApp.Application.Common;
using FinanceApp.Application.Modules.MasterData.DTOs.TagRule;

namespace FinanceApp.Application.Modules.MasterData.Interfaces;

public interface ITagRuleService
{
    Task<PageResponse<TagRuleDto>> GetPagedAsync(PageRequest request);
    Task<TagRuleDto> GetByIdAsync(long id);
    Task<TagRuleDto> CreateAsync(CreateTagRuleRequest request);
    Task<TagRuleDto> UpdateAsync(long id, UpdateTagRuleRequest request);
    Task DeleteAsync(long id);
    Task<RunTagRulesResult> RunRulesAsync(RunTagRulesRequest request);

    /// <summary>
    /// 预览规则重跑：执行匹配但不写入 TagBinding，返回受影响的交易明细供用户勾选
    /// </summary>
    Task<RerunPreviewResponse> PreviewRerunAsync(RerunPreviewRequest request);

    /// <summary>
    /// 确认执行规则重跑：按用户勾选的 transactionIds 执行匹配并写入 TagBinding
    /// </summary>
    Task<RerunConfirmResponse> ConfirmRerunAsync(RerunConfirmRequest request);
}
