namespace FinanceApp.Domain.Enums;

/// <summary>
/// 用户角色枚举
/// </summary>
public enum UserRole
{
    /// <summary>
    /// 管理员：全部权限
    /// </summary>
    Admin = 1,

    /// <summary>
    /// 会计：财务数据权限
    /// </summary>
    Accountant = 2,

    /// <summary>
    /// 查看者：只读权限，仅看自己的数据
    /// </summary>
    Viewer = 3
}
