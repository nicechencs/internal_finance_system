namespace FinanceApp.Domain.Interfaces;

/// <summary>
/// 标记实体拥有提供商无关的显式并发版本列。
/// 实现该接口的实体会在 <c>AppDbContext.SaveChanges</c> 中自动递增 <see cref="Version"/>，
/// 并将该列配置为并发令牌（乐观锁），从而在 PostgreSQL 与 SQLite 上都能防止读改写场景下的丢失更新。
/// 约束：版本化实体必须先经跟踪查询加载后再修改保存，禁止用分离实体（DTO 直接构造）调用 Update()，
/// 否则 OriginalValue 不可靠会导致误报并发冲突。
/// </summary>
public interface IConcurrencyVersioned
{
    /// <summary>
    /// 乐观并发版本号。每次成功更新自增 1，WHERE 条件使用更新前的原值。
    /// </summary>
    long Version { get; set; }
}
