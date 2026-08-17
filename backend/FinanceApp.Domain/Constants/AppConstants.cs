namespace FinanceApp.Domain.Constants;

/// <summary>
/// 应用常量定义
/// </summary>
public static class AppConstants
{
    /// <summary>分页相关常量</summary>
    public static class Pagination
    {
        /// <summary>默认页码</summary>
        public const int DefaultPage = 1;
        /// <summary>默认每页大小</summary>
        public const int DefaultPageSize = 20;
        /// <summary>最小每页大小</summary>
        public const int MinPageSize = 1;
        /// <summary>最大每页大小</summary>
        public const int MaxPageSize = 100;
    }

    /// <summary>金额相关常量</summary>
    public static class Amount
    {
        /// <summary>最小金额</summary>
        public const decimal MinAmount = 0.01m;
        /// <summary>最大金额</summary>
        public const decimal MaxAmount = 999999999999.99m;
        /// <summary>金额精度（小数位数）</summary>
        public const int Precision = 2;
        /// <summary>默认货币代码</summary>
        public const string DefaultCurrency = "CNY";
    }

    /// <summary>日期时间格式常量</summary>
    public static class DateTime
    {
        /// <summary>日期格式</summary>
        public const string DateFormat = "yyyy-MM-dd";
        /// <summary>日期时间格式</summary>
        public const string DateTimeFormat = "yyyy-MM-dd HH:mm:ss";
        /// <summary>时间格式</summary>
        public const string TimeFormat = "HH:mm:ss";
    }

    /// <summary>文件相关常量</summary>
    public static class File
    {
        /// <summary>Excel 文件扩展名</summary>
        public static readonly string[] ExcelExtensions = { ".xlsx", ".xls" };
        /// <summary>最大文件大小（字节）- 10MB</summary>
        public const long MaxFileSize = 10 * 1024 * 1024;
        /// <summary>Excel MIME 类型</summary>
        public static readonly string[] ExcelMimeTypes =
        {
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "application/vnd.ms-excel"
        };
    }

    /// <summary>哈希相关常量</summary>
    public static class Hash
    {
        /// <summary>MD5 哈希长度</summary>
        public const int Md5Length = 32;
        /// <summary>银行流水唯一哈希分隔符</summary>
        public const string BankTransactionHashSeparator = "|";
    }

    /// <summary>缓存相关常量</summary>
    public static class Cache
    {
        /// <summary>默认缓存过期时间（分钟）</summary>
        public const int DefaultExpirationMinutes = 30;
        /// <summary>短期缓存过期时间（分钟）</summary>
        public const int ShortExpirationMinutes = 5;
        /// <summary>长期缓存过期时间（分钟）</summary>
        public const int LongExpirationMinutes = 120;
    }

    /// <summary>认证相关常量</summary>
    public static class Auth
    {
        /// <summary>密码最小长度</summary>
        public const int MinPasswordLength = 10;
        /// <summary>密码最大长度</summary>
        public const int MaxPasswordLength = 128;
        /// <summary>SecurityStamp 声明类型</summary>
        public const string SecurityStampClaimType = "ifs_security_stamp";
    }

    /// <summary>系统相关常量</summary>
    public static class System
    {
        /// <summary>系统管理员用户名</summary>
        public const string AdminUsername = "admin";
        /// <summary>系统默认时区</summary>
        public const string DefaultTimeZone = "Asia/Shanghai";
    }
}
