namespace FinanceApp.Application.Modules.MasterData.DTOs.Category;

public class CategoryStatisticsDto
{
    public int TotalCount { get; set; }
    public int IncomeCategoryCount { get; set; }
    public int ExpenseCategoryCount { get; set; }
    public int ActiveCount { get; set; }
}
