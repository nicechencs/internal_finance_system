namespace FinanceApp.Application.Common;

public class PageRequest
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? SortBy { get; set; }
    public string? SortOrder { get; set; } // "asc" or "desc"
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? TransactionType { get; set; }
    public string? Name { get; set; }
    public long? AccountId { get; set; }
    public long? CategoryId { get; set; }
    public long? ProjectId { get; set; }
    public long? CustomerId { get; set; }
    public long? SupplierId { get; set; }
    public string? Status { get; set; }
    public string? ContactPerson { get; set; }
    public string? ContactPhone { get; set; }
    public string? PersonType { get; set; }
    public string? Phone { get; set; }
    public string? CategoryType { get; set; }
    public List<TagFilterGroup>? TagFilters { get; set; }

    /// <summary>
    /// 交易核销状态，支持逗号分隔多值，如 Unallocated,PartiallyAllocated。
    /// </summary>
    public string? AllocationStatus { get; set; }

    /// <summary>按交易金额下限筛选。</summary>
    public decimal? MinAmount { get; set; }

    /// <summary>按交易金额上限筛选。</summary>
    public decimal? MaxAmount { get; set; }

    /// <summary>
    /// 为 true 时排除转账。待分配核销列表应显式传入，避免共享过滤隐式丢数据。
    /// </summary>
    public bool? ExcludeTransfer { get; set; }
}

public class PageResponse<T>
{
    public List<T> Items { get; set; } = new();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int Total { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)Total / PageSize);
}
