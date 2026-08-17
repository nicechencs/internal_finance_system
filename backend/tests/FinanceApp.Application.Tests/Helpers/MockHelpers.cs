using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Interfaces;
using MockQueryable.Moq;
using Moq;

namespace FinanceApp.Application.Tests.Helpers;

public static class MockHelpers
{
    /// <summary>
    /// 创建 GetQueryable() 返回空集合的 Repository Mock
    /// </summary>
    public static Mock<IRepository<T>> CreateEmptyRepoMock<T>() where T : BaseEntity
    {
        var mock = new Mock<IRepository<T>>();
        mock.Setup(r => r.GetQueryable())
            .Returns(new List<T>().AsQueryable().BuildMock().Object);
        return mock;
    }

    /// <summary>
    /// 设置 Repository Mock 的 GetQueryable() 返回指定数据
    /// </summary>
    public static void SetupRepo<T>(Mock<IRepository<T>> mock, IEnumerable<T> data) where T : BaseEntity
    {
        mock.Setup(r => r.GetQueryable())
            .Returns(data.AsQueryable().BuildMock().Object);
    }

    /// <summary>
    /// 设置 Repository Mock 的 GetQueryable() 返回指定数据（params 重载）
    /// </summary>
    public static void SetupRepo<T>(Mock<IRepository<T>> mock, params T[] data) where T : BaseEntity
    {
        mock.Setup(r => r.GetQueryable())
            .Returns(data.AsQueryable().BuildMock().Object);
    }
}
