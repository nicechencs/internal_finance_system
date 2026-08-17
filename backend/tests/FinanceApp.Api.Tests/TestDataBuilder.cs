using FinanceApp.Application.Modules.Identity.DTOs;

namespace FinanceApp.Api.Tests;

/// <summary>
/// 测试数据构建器
/// </summary>
public static class TestDataBuilder
{
    /// <summary>
    /// 创建登录请求 DTO
    /// </summary>
    public static LoginRequest CreateLoginRequest(
        string username = "testuser",
        string password = "Test@123")
    {
        return new LoginRequest
        {
            Username = username,
            Password = password
        };
    }

    /// <summary>
    /// 创建注册请求 DTO
    /// </summary>
    public static RegisterRequest CreateRegisterRequest(
        string username = "testuser",
        string email = "test@example.com",
        string password = "Test@123")
    {
        return new RegisterRequest
        {
            Username = username,
            Email = email,
            Password = password
        };
    }
}
