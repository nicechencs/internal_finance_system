namespace FinanceApp.Application.Modules.MasterData.DTOs.Config;

/// <summary>
/// 可公开返回的站点品牌字段。不含内部配置键、描述或其他系统配置。
/// </summary>
public class PublicBrandDto
{
    public string SiteName { get; set; } = string.Empty;
    public string SiteNameEn { get; set; } = string.Empty;
}
