using Microsoft.EntityFrameworkCore;
using FinanceApp.Domain.Entities;

namespace FinanceApp.Application.Extensions;

/// <summary>
/// Transaction 查询扩展方法，提取常用 Include 链
/// </summary>
public static class TransactionQueryExtensions
{
    /// <summary>
    /// 包含所有导航属性（用于详情展示）
    /// Include: Account, Category, Project, Customer, Supplier, Person,
    ///          RelatedTransaction->Account, Allocations->Project, Allocations->Person,
    /// </summary>
    public static IQueryable<Transaction> IncludeFullDetails(this IQueryable<Transaction> query)
    {
        return query
            .Include(t => t.BankTransaction)
            .Include(t => t.Account)
            .Include(t => t.Category)
            .Include(t => t.Project)
            .Include(t => t.Customer)
            .Include(t => t.Supplier)
            .Include(t => t.Person)
            .Include(t => t.RelatedTransaction)
                .ThenInclude(rt => rt!.Account)
            .Include(t => t.Allocations)
                .ThenInclude(a => a.Project)
            .Include(t => t.Allocations)
                .ThenInclude(a => a.Person);
    }

    /// <summary>
    /// 包含结算核销明细，用于计算 AvailableAmount / AllocationStatus。
    /// 不并入 IncludeFullDetails，避免加重其他列表查询。
    /// </summary>
    public static IQueryable<Transaction> IncludeSettlementDetails(this IQueryable<Transaction> query)
    {
        return query
            .Include(t => t.ReceivableDetails)
            .Include(t => t.PayableDetails);
    }

    /// <summary>
    /// 包含编辑所需的导航属性（轻量级）
    /// Include: Account, Allocations
    /// </summary>
    public static IQueryable<Transaction> IncludeForEdit(this IQueryable<Transaction> query)
    {
        return query
            .Include(t => t.BankTransaction)
            .Include(t => t.Account)
            .Include(t => t.Allocations);
    }
}
