namespace FinanceApp.Domain.Constants;

/// <summary>
/// 错误码常量定义
/// </summary>
public static class ErrorCodes
{
    /// <summary>
    /// 通用错误码
    /// </summary>
    public static class Common
    {
        public const string InternalError = "COMMON_001";
        public const string InvalidParameter = "COMMON_002";
        public const string OperationFailed = "COMMON_003";
    }

    /// <summary>
    /// 资源相关错误码
    /// </summary>
    public static class Resource
    {
        public const string NotFound = "RESOURCE_001";
        public const string AlreadyExists = "RESOURCE_002";
        public const string Conflict = "RESOURCE_003";
    }

    /// <summary>
    /// 验证相关错误码
    /// </summary>
    public static class Validation
    {
        public const string Required = "VALIDATION_001";
        public const string InvalidFormat = "VALIDATION_002";
        public const string OutOfRange = "VALIDATION_003";
        public const string DuplicateValue = "VALIDATION_004";
    }

    /// <summary>
    /// 认证授权相关错误码
    /// </summary>
    public static class Auth
    {
        public const string Unauthorized = "AUTH_001";
        public const string Forbidden = "AUTH_002";
        public const string InvalidCredentials = "AUTH_003";
        public const string TokenExpired = "AUTH_004";
    }

    /// <summary>
    /// 业务逻辑相关错误码
    /// </summary>
    public static class Business
    {
        public const string InvalidOperation = "BUSINESS_001";
        public const string StateConflict = "BUSINESS_002";
        public const string RuleViolation = "BUSINESS_003";
    }
}
