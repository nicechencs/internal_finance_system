namespace FinanceApp.Domain.Constants;

/// <summary>
/// 站点品牌默认值与配置键。未配置或值为空时回退到这些默认值，保持与当前 UI 一致。
/// </summary>
public static class SiteBrandDefaults
{
    public const string SiteNameKey = "system_name";
    public const string SiteNameEnKey = "system_name_en";

    public const string SiteName = "财务管理系统";
    public const string SiteNameEn = "Finance Management System";

    public const string SiteNameDescription = "站点名称";
    public const string SiteNameEnDescription = "站点英文副标题";

    public const int SiteNameMaxLength = 50;
    public const int SiteNameEnMaxLength = 80;

    public const string PublicBrandCacheKey = "site-brand:public";
}
