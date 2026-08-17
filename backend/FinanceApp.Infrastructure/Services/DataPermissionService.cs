using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Enums;
using FinanceApp.Domain.Interfaces;

namespace FinanceApp.Infrastructure.Services;

/// <summary>
/// 数据权限服务实现
/// </summary>
public class DataPermissionService : IDataPermissionService
{
    private readonly ICurrentUserService _currentUserService;

    public DataPermissionService(ICurrentUserService currentUserService)
    {
        _currentUserService = currentUserService;
    }

    public bool CanAccess<T>(T entity) where T : BaseEntity
    {
        // Admin 和 Accountant 可以访问所有数据
        if (_currentUserService.IsAdmin || _currentUserService.IsAccountant)
            return true;

        // Viewer 只能访问自己创建的数据
        // BaseEntity 已包含 CreatedBy 属性，直接使用
        var createdByProperty = typeof(T).GetProperty("CreatedBy");
        if (createdByProperty != null)
        {
            var createdBy = createdByProperty.GetValue(entity);
            if (createdBy is long createdById)
            {
                return createdById == _currentUserService.UserId;
            }
        }

        // CreatedBy 为 null 或不存在时，Viewer 不允许访问
        return false;
    }

    public bool CanModify<T>(T entity) where T : BaseEntity
    {
        // Viewer 不能修改任何数据
        if (_currentUserService.IsViewer)
            return false;

        // Admin 可以修改所有数据
        if (_currentUserService.IsAdmin)
            return true;

        // Accountant 可以修改财务相关数据
        if (_currentUserService.IsAccountant)
        {
            return entity is Transaction or Receivable or Payable or
                   Account or Project or Customer or Supplier or Person;
        }

        return false;
    }

    public bool CanDelete<T>(T entity) where T : BaseEntity
    {
        // 只有 Admin 可以删除数据
        return _currentUserService.IsAdmin;
    }

    public IQueryable<T> ApplyPermissionFilter<T>(IQueryable<T> query) where T : BaseEntity
    {
        // Admin 和 Accountant 可以看到所有数据
        if (_currentUserService.IsAdmin || _currentUserService.IsAccountant)
            return query;

        // Viewer 只能看到自己创建的数据
        // BaseEntity 已包含 CreatedBy 属性，直接使用
        var createdByProperty = typeof(T).GetProperty("CreatedBy");
        if (createdByProperty != null)
        {
            var userId = _currentUserService.UserId;
            // 使用动态 LINQ 表达式过滤: e.CreatedBy == userId
            var parameter = System.Linq.Expressions.Expression.Parameter(typeof(T), "e");
            var property = System.Linq.Expressions.Expression.Property(parameter, "CreatedBy");
            var constant = System.Linq.Expressions.Expression.Constant(userId, typeof(long?));
            var equality = System.Linq.Expressions.Expression.Equal(property, constant);
            var lambda = System.Linq.Expressions.Expression.Lambda<Func<T, bool>>(equality, parameter);
            return query.Where(lambda);
        }

        // 没有 CreatedBy 字段时，Viewer 看不到任何数据
        return query.Where(e => false);
    }
}
