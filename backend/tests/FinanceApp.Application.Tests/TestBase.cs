using Mapster;
using MapsterMapper;
using FinanceApp.Application.Mappings;
using FinanceApp.Application.Modules.Identity.Interfaces;
using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Enums;
using FinanceApp.Domain.Interfaces;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace FinanceApp.Application.Tests;

/// <summary>
/// 测试基类，提供通用测试设置
/// </summary>
public abstract class TestBase
{
    protected IMapper Mapper { get; }
    protected Mock<IUnitOfWork> UnitOfWorkMock { get; }
    protected Mock<IAuditLogService> AuditLogServiceMock { get; }

    protected TestBase()
    {
        var config = MappingConfig.Configure();
        Mapper = new Mapper(config);

        UnitOfWorkMock = new Mock<IUnitOfWork>();
        UnitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(0);

        AuditLogServiceMock = new Mock<IAuditLogService>();
        AuditLogServiceMock.Setup(a => a.LogAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<long>(),
            It.IsAny<string?>(), It.IsAny<string?>()))
            .Returns(Task.CompletedTask);
    }

    /// <summary>
    /// 创建默认的 Admin 角色 ICurrentUserService Mock
    /// </summary>
    protected static ICurrentUserService CreateAdminCurrentUserService()
    {
        var mock = new Mock<ICurrentUserService>();
        mock.Setup(x => x.UserId).Returns(1L);
        mock.Setup(x => x.Username).Returns("admin");
        mock.Setup(x => x.Role).Returns(UserRole.Admin);
        mock.Setup(x => x.IsAdmin).Returns(true);
        mock.Setup(x => x.IsAccountant).Returns(false);
        mock.Setup(x => x.IsViewer).Returns(false);
        return mock.Object;
    }

    /// <summary>
    /// 创建默认的 IDataPermissionService Mock（Admin 拥有全部权限）
    /// </summary>
    protected static IDataPermissionService CreateAdminDataPermissionService()
    {
        return new AllPermissionsDataPermissionService();
    }
}

/// <summary>
/// 全权限的 IDataPermissionService 实现，用于测试
/// Admin 角色：所有权限检查返回 true，查询不做过滤
/// </summary>
internal class AllPermissionsDataPermissionService : IDataPermissionService
{
    public bool CanAccess<T>(T entity) where T : BaseEntity => true;
    public bool CanModify<T>(T entity) where T : BaseEntity => true;
    public bool CanDelete<T>(T entity) where T : BaseEntity => true;
    public IQueryable<T> ApplyPermissionFilter<T>(IQueryable<T> query) where T : BaseEntity => query;
}

/// <summary>
/// 按 CreatedBy 过滤的 IDataPermissionService 实现，用于模拟 Viewer 权限测试
/// 只返回 CreatedBy == userId 的记录
/// </summary>
internal class CreatedByFilterDataPermissionService : IDataPermissionService
{
    private readonly long _userId;

    public CreatedByFilterDataPermissionService(long userId)
    {
        _userId = userId;
    }

    public bool CanAccess<T>(T entity) where T : BaseEntity => entity.CreatedBy == _userId;
    public bool CanModify<T>(T entity) where T : BaseEntity => entity.CreatedBy == _userId;
    public bool CanDelete<T>(T entity) where T : BaseEntity => entity.CreatedBy == _userId;
    public IQueryable<T> ApplyPermissionFilter<T>(IQueryable<T> query) where T : BaseEntity
        => query.Where(e => e.CreatedBy == _userId);
}
