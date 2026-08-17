using Moq;

namespace FinanceApp.Infrastructure.Tests;

/// <summary>
/// Mock 对象创建辅助类
/// </summary>
public static class MockHelper
{
    /// <summary>
    /// 创建 Mock 对象
    /// </summary>
    public static Mock<T> CreateMock<T>() where T : class
    {
        return new Mock<T>();
    }

    /// <summary>
    /// 创建 Mock 对象并设置为 Strict 模式
    /// </summary>
    public static Mock<T> CreateStrictMock<T>() where T : class
    {
        return new Mock<T>(MockBehavior.Strict);
    }
}
