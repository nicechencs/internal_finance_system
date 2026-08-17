using FinanceApp.Domain.Enums;

namespace FinanceApp.Domain.Interfaces;

/// <summary>
/// 当前用户服务接口
/// </summary>
public interface ICurrentUserService
{
    /// <summary>
    /// 当前用户 ID
    /// </summary>
    long UserId { get; }

    /// <summary>
    /// 当前用户名
    /// </summary>
    string Username { get; }

    /// <summary>
    /// 当前用户角色
    /// </summary>
    UserRole Role { get; }

    /// <summary>
    /// 是否为管理员
    /// </summary>
    bool IsAdmin { get; }

    /// <summary>
    /// 是否为会计
    /// </summary>
    bool IsAccountant { get; }

    /// <summary>
    /// 是否为查看者
    /// </summary>
    bool IsViewer { get; }
}
