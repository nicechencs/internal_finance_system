using FinanceApp.Domain.Entities;

namespace FinanceApp.Domain.Interfaces;

/// <summary>
/// 数据权限服务接口
/// </summary>
public interface IDataPermissionService
{
    /// <summary>
    /// 检查是否有权限访问指定实体
    /// </summary>
    bool CanAccess<T>(T entity) where T : BaseEntity;

    /// <summary>
    /// 检查是否有权限修改指定实体
    /// </summary>
    bool CanModify<T>(T entity) where T : BaseEntity;

    /// <summary>
    /// 检查是否有权限删除指定实体
    /// </summary>
    bool CanDelete<T>(T entity) where T : BaseEntity;

    /// <summary>
    /// 对查询应用权限过滤
    /// </summary>
    IQueryable<T> ApplyPermissionFilter<T>(IQueryable<T> query) where T : BaseEntity;
}
