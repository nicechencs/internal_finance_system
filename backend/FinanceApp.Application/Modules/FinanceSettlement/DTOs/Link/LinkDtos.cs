using FinanceApp.Domain.Enums;

namespace FinanceApp.Application.Modules.FinanceSettlement.DTOs.Link;

public class LinkPreviewRequest
{
    public LinkType LinkType { get; set; }
    public long EntityId { get; set; }
}

public class LinkPreviewResponse
{
    public LinkType LinkType { get; set; }
    public long EntityId { get; set; }
    public string EntityName { get; set; } = string.Empty;
    public int TotalMatched { get; set; }
    public List<LinkCandidateDto> Candidates { get; set; } = new();
}

public class LinkCandidateDto
{
    public long TransactionId { get; set; }
    public DateTime TransactionDate { get; set; }
    public decimal Amount { get; set; }
    public string TransactionType { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public string? Counterparty { get; set; }
    public string? Description { get; set; }
    public string? CategoryName { get; set; }
    public string MatchReason { get; set; } = string.Empty;
}

public class LinkConfirmRequest
{
    public LinkType LinkType { get; set; }
    public long EntityId { get; set; }
    public List<long> TransactionIds { get; set; } = new();
}

public class LinkConfirmResponse
{
    public int LinkedCount { get; set; }
    public string Message { get; set; } = string.Empty;
}

// ===== 批量智能关联 =====

/// <summary>实体类型（用于批量关联确认）</summary>
public enum BatchLinkEntityType
{
    Customer = 1,
    Supplier = 2,
    Person = 3,
    Project = 4,
    Account = 5
}

/// <summary>一个可选实体匹配项</summary>
public class EntityMatchDto
{
    public BatchLinkEntityType EntityType { get; set; }
    public long EntityId { get; set; }
    public string EntityName { get; set; } = string.Empty;
    /// <summary>匹配依据说明</summary>
    public string MatchReason { get; set; } = string.Empty;
    /// <summary>辅助区分信息（如联系电话、简称、项目编号等）</summary>
    public string? ExtraInfo { get; set; }
}

/// <summary>一条待匹配的交易及其所有候选实体</summary>
public class BatchLinkCandidateDto
{
    public long TransactionId { get; set; }
    public DateTime TransactionDate { get; set; }
    public decimal Amount { get; set; }
    public string TransactionType { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public string? Counterparty { get; set; }
    public string? Description { get; set; }
    /// <summary>所有命中的候选实体（可能来自不同类型）</summary>
    public List<EntityMatchDto> Matches { get; set; } = new();
}

public class BatchLinkPreviewResponse
{
    /// <summary>系统内未关联实体的交易总数</summary>
    public int TotalUnlinked { get; set; }
    /// <summary>找到至少一个候选匹配的交易数量</summary>
    public int TotalMatched { get; set; }
    public List<BatchLinkCandidateDto> Candidates { get; set; } = new();
}

/// <summary>用户确认的一条关联操作</summary>
public class BatchLinkConfirmItem
{
    public long TransactionId { get; set; }
    public BatchLinkEntityType EntityType { get; set; }
    public long EntityId { get; set; }
}

public class BatchLinkConfirmRequest
{
    public List<BatchLinkConfirmItem> Items { get; set; } = new();
}

public class BatchLinkConfirmResponse
{
    public int LinkedCount { get; set; }
    public string Message { get; set; } = string.Empty;
}

// ===== 规则重跑 =====

public class RuleRerunPreviewRequest
{
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public RuleRerunStrategy Strategy { get; set; } = RuleRerunStrategy.Conservative;
}

public class RuleRerunPreviewResponse
{
    public int TotalAffected { get; set; }
    public int WouldUpdate { get; set; }
    public RuleRerunStrategy Strategy { get; set; }
    public List<RuleRerunCandidateDto> Candidates { get; set; } = new();
}

public class RuleRerunCandidateDto
{
    public long TransactionId { get; set; }
    public DateTime TransactionDate { get; set; }
    public decimal Amount { get; set; }
    public string TransactionType { get; set; } = string.Empty;
    public string? Counterparty { get; set; }
    public string? Description { get; set; }
    public string? CurrentCategoryName { get; set; }
    public string? NewCategoryName { get; set; }
    public long? NewCategoryId { get; set; }
    public bool WillChange { get; set; }
}

public class RuleRerunConfirmRequest
{
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public RuleRerunStrategy Strategy { get; set; } = RuleRerunStrategy.Conservative;
    public List<long>? TransactionIds { get; set; }
}

public class RuleRerunConfirmResponse
{
    public int UpdatedCount { get; set; }
    public int SkippedCount { get; set; }
    public string Message { get; set; } = string.Empty;
}
