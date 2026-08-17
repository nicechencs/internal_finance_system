using FinanceApp.Application.Common;
using FinanceApp.Application.Modules.MasterData.DTOs.Rule;

namespace FinanceApp.Application.Modules.MasterData.Interfaces;

public interface IRuleService
{
    Task<PageResponse<RuleDto>> GetPagedAsync(PageRequest request);
    Task<RuleDto> GetByIdAsync(long id);
    Task<RuleDto> CreateAsync(CreateRuleRequest request);
    Task<RuleDto> UpdateAsync(long id, UpdateRuleRequest request);
    Task DeleteAsync(long id);
    Task<List<RuleDto>> GetActiveRulesAsync();
    Task<long?> MatchCategoryAsync(string counterpartyName, string description, decimal amount, string? memo = null);

    /// <summary>
    /// 批量匹配分类：一次加载规则，对多条交易逐一匹配，避免 N+1 查询
    /// </summary>
    Task<List<long?>> MatchCategoriesBatchAsync(List<(string CounterpartyName, string Description, decimal Amount, string? Memo)> items);
}
