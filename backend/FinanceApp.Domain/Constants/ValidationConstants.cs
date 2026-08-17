namespace FinanceApp.Domain.Constants;

/// <summary>
/// 验证常量定义
/// </summary>
public static class ValidationConstants
{
    /// <summary>通用字段长度限制</summary>
    public static class Length
    {
        /// <summary>名称最小长度</summary>
        public const int NameMinLength = 1;
        /// <summary>名称最大长度</summary>
        public const int NameMaxLength = 100;
        /// <summary>代码最大长度</summary>
        public const int CodeMaxLength = 50;
        /// <summary>描述最大长度</summary>
        public const int DescriptionMaxLength = 500;
        /// <summary>备注最大长度</summary>
        public const int RemarksMaxLength = 1000;
        /// <summary>地址最大长度</summary>
        public const int AddressMaxLength = 200;
        /// <summary>电话号码最大长度</summary>
        public const int PhoneMaxLength = 20;
        /// <summary>邮箱最大长度</summary>
        public const int EmailMaxLength = 100;
    }

    /// <summary>账户相关验证</summary>
    public static class Account
    {
        /// <summary>账户名称最小长度</summary>
        public const int NameMinLength = 2;
        /// <summary>账户名称最大长度</summary>
        public const int NameMaxLength = 100;
        /// <summary>账号最大长度</summary>
        public const int AccountNumberMaxLength = 50;
        /// <summary>银行名称最大长度</summary>
        public const int BankNameMaxLength = 100;
    }

    /// <summary>分类相关验证</summary>
    public static class Category
    {
        /// <summary>分类名称最小长度</summary>
        public const int NameMinLength = 1;
        /// <summary>分类名称最大长度</summary>
        public const int NameMaxLength = 50;
        /// <summary>最大层级深度</summary>
        public const int MaxDepth = 5;
    }

    /// <summary>项目相关验证</summary>
    public static class Project
    {
        /// <summary>项目名称最小长度</summary>
        public const int NameMinLength = 2;
        /// <summary>项目名称最大长度</summary>
        public const int NameMaxLength = 100;
        /// <summary>项目代码最大长度</summary>
        public const int CodeMaxLength = 50;
    }

    /// <summary>客户/供应商相关验证</summary>
    public static class Partner
    {
        /// <summary>名称最小长度</summary>
        public const int NameMinLength = 2;
        /// <summary>名称最大长度</summary>
        public const int NameMaxLength = 100;
        /// <summary>联系人最大长度</summary>
        public const int ContactMaxLength = 50;
    }

    /// <summary>人员相关验证</summary>
    public static class Person
    {
        /// <summary>姓名最小长度</summary>
        public const int NameMinLength = 2;
        /// <summary>姓名最大长度</summary>
        public const int NameMaxLength = 50;
        /// <summary>工号最大长度</summary>
        public const int EmployeeIdMaxLength = 50;
        /// <summary>部门/职位最大长度</summary>
        public const int DeptPositionMaxLength = 100;
    }

    /// <summary>交易相关验证</summary>
    public static class Transaction
    {
        /// <summary>描述最大长度</summary>
        public const int DescriptionMaxLength = 500;
        /// <summary>最小交易金额</summary>
        public const decimal MinAmount = 0.01m;
        /// <summary>最大交易金额</summary>
        public const decimal MaxAmount = 999999999.99m;
    }

    /// <summary>用户相关验证</summary>
    public static class User
    {
        /// <summary>用户名最小长度</summary>
        public const int UsernameMinLength = 3;
        /// <summary>用户名最大长度</summary>
        public const int UsernameMaxLength = 50;
        /// <summary>密码最小长度</summary>
        public const int PasswordMinLength = 10;
        /// <summary>密码最大长度</summary>
        public const int PasswordMaxLength = 128;
        /// <summary>真实姓名最大长度</summary>
        public const int RealNameMaxLength = 100;
    }

    /// <summary>正则表达式模式</summary>
    public static class Patterns
    {
        /// <summary>邮箱格式</summary>
        public const string Email = @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$";
        /// <summary>手机号格式（中国大陆）</summary>
        public const string PhoneCN = @"^1[3-9]\d{9}$";
        /// <summary>固定电话格式（中国）</summary>
        public const string TelephoneCN = @"^0\d{2,3}-?\d{7,8}$";
        /// <summary>银行账号格式（纯数字）</summary>
        public const string BankAccount = @"^\d{10,30}$";
        /// <summary>项目代码格式（字母数字下划线横线）</summary>
        public const string ProjectCode = @"^[A-Za-z0-9_-]+$";
    }

    /// <summary>错误消息模板</summary>
    public static class ErrorMessages
    {
        /// <summary>必填字段</summary>
        public const string Required = "{0}不能为空";
        /// <summary>长度超出限制</summary>
        public const string MaxLength = "{0}长度不能超过{1}个字符";
        /// <summary>长度不足</summary>
        public const string MinLength = "{0}长度不能少于{1}个字符";
        /// <summary>格式不正确</summary>
        public const string InvalidFormat = "{0}格式不正确";
        /// <summary>数值超出范围</summary>
        public const string OutOfRange = "{0}必须在{1}到{2}之间";
    }
}
