using Mapster;
using MapsterMapper;
using FinanceApp.Application.Mappings;
using FinanceApp.Application.Modules.Identity.Interfaces;
using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Enums;
using FinanceApp.Domain.Interfaces;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace FinanceApp.Application.Tests.Helpers;

public abstract class TestBase
{
    protected readonly IMapper Mapper;
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
    /// 创建 Admin 角色 ICurrentUserService Mock（返回 Mock 对象）
    /// </summary>
    protected static Mock<ICurrentUserService> CreateAdminCurrentUserMock()
    {
        var mock = new Mock<ICurrentUserService>();
        mock.Setup(x => x.UserId).Returns(1L);
        mock.Setup(x => x.Username).Returns("admin");
        mock.Setup(x => x.Role).Returns(UserRole.Admin);
        mock.Setup(x => x.IsAdmin).Returns(true);
        mock.Setup(x => x.IsAccountant).Returns(false);
        mock.Setup(x => x.IsViewer).Returns(false);
        return mock;
    }

    /// <summary>
    /// 创建 Viewer 角色 ICurrentUserService Mock（返回 Mock 对象）
    /// </summary>
    protected static Mock<ICurrentUserService> CreateViewerCurrentUserMock(long userId = 2)
    {
        var mock = new Mock<ICurrentUserService>();
        mock.Setup(x => x.UserId).Returns(userId);
        mock.Setup(x => x.Role).Returns(UserRole.Viewer);
        mock.Setup(x => x.IsAdmin).Returns(false);
        mock.Setup(x => x.IsViewer).Returns(true);
        return mock;
    }

    /// <summary>
    /// 创建默认的 IDataPermissionService Mock（Admin 拥有全部权限）
    /// </summary>
    protected static IDataPermissionService CreateAdminDataPermissionService()
    {
        return new Application.Tests.AllPermissionsDataPermissionService();
    }
}
