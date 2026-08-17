using FinanceApp.Domain.Entities;

namespace FinanceApp.Domain.Interfaces;

public interface IRepository<T> where T : BaseEntity
{
    Task<T?> GetByIdAsync(long id);
    Task<List<T>> GetAllAsync();
    Task<(List<T> Items, int Total)> GetPagedAsync(int page, int pageSize);

    /// <summary>
    /// 将实体添加到 EF 变更追踪，不自动保存。需配合 IUnitOfWork.SaveChangesAsync 使用。
    /// </summary>
    Task<T> AddAsync(T entity);

    /// <summary>
    /// 将实体标记为已修改，不自动保存。需配合 IUnitOfWork.SaveChangesAsync 使用。
    /// </summary>
    void Update(T entity);

    /// <summary>
    /// 软删除实体（标记 IsDeleted），不自动保存。需配合 IUnitOfWork.SaveChangesAsync 使用。
    /// </summary>
    void Delete(long id);

    /// <summary>
    /// 软删除实体（标记 IsDeleted），不自动保存。需配合 IUnitOfWork.SaveChangesAsync 使用。
    /// </summary>
    void Delete(T entity);

    Task<bool> ExistsAsync(long id);
    IQueryable<T> GetQueryable();
}
