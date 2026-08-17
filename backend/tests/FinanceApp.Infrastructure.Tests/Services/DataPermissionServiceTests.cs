using FluentAssertions;
using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Enums;
using FinanceApp.Domain.Interfaces;
using FinanceApp.Infrastructure.Services;
using Moq;

namespace FinanceApp.Infrastructure.Tests.Services;

/// <summary>
/// DataPermissionService 单元测试
/// 覆盖 CanAccess 和 ApplyPermissionFilter 的权限过滤逻辑
/// </summary>
public class DataPermissionServiceTests
{
    /// <summary>
    /// 创建指定角色的 ICurrentUserService Mock
    /// </summary>
    private static ICurrentUserService CreateCurrentUserService(UserRole role, long userId = 1L)
    {
        var mock = new Mock<ICurrentUserService>();
        mock.Setup(x => x.UserId).Returns(userId);
        mock.Setup(x => x.Username).Returns(role.ToString().ToLower());
        mock.Setup(x => x.Role).Returns(role);
        mock.Setup(x => x.IsAdmin).Returns(role == UserRole.Admin);
        mock.Setup(x => x.IsAccountant).Returns(role == UserRole.Accountant);
        mock.Setup(x => x.IsViewer).Returns(role == UserRole.Viewer);
        return mock.Object;
    }

    #region CanAccess 测试

    [Fact]
    public void CanAccess_Admin角色_不管CreatedBy是否为null都应返回true()
    {
        // Arrange - Admin 角色
        var userService = CreateCurrentUserService(UserRole.Admin, userId: 1L);
        var service = new DataPermissionService(userService);

        // CreatedBy 为 null 的实体
        var entityWithNullCreatedBy = new Account
        {
            Id = 1, Name = "测试账户", AccountType = AccountType.Bank, CreatedBy = null
        };

        // CreatedBy 指向其他用户的实体
        var entityWithOtherCreatedBy = new Account
        {
            Id = 2, Name = "其他账户", AccountType = AccountType.Bank, CreatedBy = 999L
        };

        // Act & Assert - Admin 对任何实体都有访问权限
        service.CanAccess(entityWithNullCreatedBy).Should().BeTrue("Admin 应可访问 CreatedBy=null 的实体");
        service.CanAccess(entityWithOtherCreatedBy).Should().BeTrue("Admin 应可访问其他用户创建的实体");
    }

    [Fact]
    public void CanAccess_Viewer角色且CreatedBy为null_应返回false()
    {
        // Arrange - Viewer 角色
        var userService = CreateCurrentUserService(UserRole.Viewer, userId: 1L);
        var service = new DataPermissionService(userService);

        var entity = new Account
        {
            Id = 1, Name = "测试账户", AccountType = AccountType.Bank, CreatedBy = null
        };

        // Act
        var result = service.CanAccess(entity);

        // Assert - CreatedBy 为 null 时，Viewer 不允许访问
        result.Should().BeFalse("Viewer 不应访问 CreatedBy=null 的实体");
    }

    [Fact]
    public void CanAccess_Viewer角色且CreatedBy匹配当前用户_应返回true()
    {
        // Arrange - Viewer 角色，UserId=42
        var userService = CreateCurrentUserService(UserRole.Viewer, userId: 42L);
        var service = new DataPermissionService(userService);

        var entity = new Account
        {
            Id = 1, Name = "我的账户", AccountType = AccountType.Bank, CreatedBy = 42L
        };

        // Act
        var result = service.CanAccess(entity);

        // Assert - CreatedBy 匹配当前用户，Viewer 可以访问
        result.Should().BeTrue("Viewer 应可访问自己创建的实体");
    }

    [Fact]
    public void CanAccess_Viewer角色且CreatedBy不匹配当前用户_应返回false()
    {
        // Arrange - Viewer 角色，UserId=1
        var userService = CreateCurrentUserService(UserRole.Viewer, userId: 1L);
        var service = new DataPermissionService(userService);

        var entity = new Account
        {
            Id = 1, Name = "他人账户", AccountType = AccountType.Bank, CreatedBy = 999L
        };

        // Act
        var result = service.CanAccess(entity);

        // Assert - CreatedBy 不匹配，Viewer 无权访问
        result.Should().BeFalse("Viewer 不应访问其他用户创建的实体");
    }

    [Fact]
    public void CanAccess_Accountant角色_不管CreatedBy都应返回true()
    {
        // Arrange - Accountant 角色
        var userService = CreateCurrentUserService(UserRole.Accountant, userId: 1L);
        var service = new DataPermissionService(userService);

        var entityWithNull = new Account
        {
            Id = 1, Name = "测试", AccountType = AccountType.Bank, CreatedBy = null
        };
        var entityWithOther = new Account
        {
            Id = 2, Name = "他人", AccountType = AccountType.Bank, CreatedBy = 999L
        };

        // Act & Assert - Accountant 拥有全部访问权限
        service.CanAccess(entityWithNull).Should().BeTrue("Accountant 应可访问 CreatedBy=null 的实体");
        service.CanAccess(entityWithOther).Should().BeTrue("Accountant 应可访问其他用户创建的实体");
    }

    #endregion

    #region ApplyPermissionFilter 测试

    [Fact]
    public void ApplyPermissionFilter_Viewer角色_应只返回自己创建的数据()
    {
        // Arrange - Viewer 角色，UserId=1
        var userService = CreateCurrentUserService(UserRole.Viewer, userId: 1L);
        var service = new DataPermissionService(userService);

        var accounts = new List<Account>
        {
            new() { Id = 1, Name = "我的账户", AccountType = AccountType.Bank, CreatedBy = 1L },
            new() { Id = 2, Name = "他人账户", AccountType = AccountType.Bank, CreatedBy = 999L },
            new() { Id = 3, Name = "无创建者", AccountType = AccountType.Bank, CreatedBy = null }
        }.AsQueryable();

        // Act
        var result = service.ApplyPermissionFilter(accounts).ToList();

        // Assert - 只返回 CreatedBy == 当前用户的数据
        result.Should().HaveCount(1);
        result[0].Name.Should().Be("我的账户");
    }

    [Fact]
    public void ApplyPermissionFilter_Admin角色_应返回全部数据()
    {
        // Arrange - Admin 角色
        var userService = CreateCurrentUserService(UserRole.Admin, userId: 1L);
        var service = new DataPermissionService(userService);

        var accounts = new List<Account>
        {
            new() { Id = 1, Name = "账户1", AccountType = AccountType.Bank, CreatedBy = 1L },
            new() { Id = 2, Name = "账户2", AccountType = AccountType.Bank, CreatedBy = 999L },
            new() { Id = 3, Name = "账户3", AccountType = AccountType.Bank, CreatedBy = null }
        }.AsQueryable();

        // Act
        var result = service.ApplyPermissionFilter(accounts).ToList();

        // Assert - Admin 能看到所有数据
        result.Should().HaveCount(3);
    }

    #endregion
}
