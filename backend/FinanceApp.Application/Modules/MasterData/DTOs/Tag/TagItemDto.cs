namespace FinanceApp.Application.Modules.MasterData.DTOs.Tag;

/// <summary>
/// 标签项 DTO - 用于在列表中显示实体关联的标签
/// </summary>
public class TagItemDto
{
    public long TagId { get; set; }
    public string TagName { get; set; } = string.Empty;
    public string? TagColor { get; set; }
}
