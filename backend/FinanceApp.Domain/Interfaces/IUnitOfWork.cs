namespace FinanceApp.Domain.Interfaces;

/// <summary>
/// 工作单元接口，统一管理事务和保存操作
/// </summary>
public interface IUnitOfWork : IDisposable, IAsyncDisposable
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task<ITransactionScope?> BeginTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 清理 ChangeTracker 中所有处于 Added 状态的实体，防止失败实体污染后续操作
    /// </summary>
    void DetachAddedEntities();

    /// <summary>
    /// 清理 ChangeTracker 中所有被追踪的实体（Added/Modified/Deleted），
    /// 用于事务回滚后防止已修改实体状态被意外持久化
    /// </summary>
    void ClearChangeTracker();
}

/// <summary>
/// 事务包装器
/// </summary>
public interface ITransactionScope : IDisposable, IAsyncDisposable
{
    Task CommitAsync(CancellationToken cancellationToken = default);
    Task RollbackAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 创建保存点（仅关系型数据库支持）
    /// </summary>
    Task CreateSavepointAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// 回滚到指定保存点
    /// </summary>
    Task RollbackToSavepointAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// 释放保存点
    /// </summary>
    Task ReleaseSavepointAsync(string name, CancellationToken cancellationToken = default);
}
