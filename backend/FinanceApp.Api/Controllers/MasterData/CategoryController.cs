using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using FinanceApp.Application.Common;
using FinanceApp.Application.Modules.MasterData.DTOs.Category;
using FinanceApp.Application.Modules.MasterData.Interfaces;
using FinanceApp.Api.Controllers.Base;

namespace FinanceApp.Api.Controllers.MasterData;

[ApiController]
[Route("api/categories")]
[Authorize]
public class CategoryController : CrudControllerBase<CategoryDto, CreateCategoryRequest, UpdateCategoryRequest>
{
    private readonly ICategoryService _categoryService;

    public CategoryController(ICategoryService categoryService, ILogger<CategoryController> logger)
        : base(categoryService, logger)
    {
        _categoryService = categoryService;
    }

    protected override string ControllerName => "CategoryController";
    protected override string EntityName => "Category";

    protected override string GetCreateSuccessMessage() => "创建成功";
    protected override string GetUpdateSuccessMessage() => "更新成功";
    protected override string GetDeleteSuccessMessage() => "删除成功";

    [HttpGet("active")]
    [Authorize(Roles = "Admin,Accountant,Viewer")]
    public async Task<ActionResult<ApiResponse<List<CategoryDto>>>> GetActive()
    {
        Logger.LogInformation("[CategoryController.GetActive]");

        var result = await _categoryService.GetActiveCategoriesAsync();
        return Ok(ApiResponse<List<CategoryDto>>.SuccessResponse(result));
    }

    [HttpGet("statistics")]
    [Authorize(Roles = "Admin,Accountant,Viewer")]
    public async Task<ActionResult<ApiResponse<CategoryStatisticsDto>>> GetStatistics([FromQuery] PageRequest request)
    {
        Logger.LogInformation("[CategoryController.GetStatistics]");

        var result = await _categoryService.GetStatisticsAsync(request);
        return Ok(ApiResponse<CategoryStatisticsDto>.SuccessResponse(result));
    }

    [HttpGet("by-type/{type}")]
    [Authorize(Roles = "Admin,Accountant,Viewer")]
    public async Task<ActionResult<ApiResponse<List<CategoryDto>>>> GetByType(string type)
    {
        Logger.LogInformation("[CategoryController.GetByType] Type={Type}", type);

        var result = await _categoryService.GetCategoriesByTypeAsync(type);
        return Ok(ApiResponse<List<CategoryDto>>.SuccessResponse(result));
    }

    // 重写 Create 方法以使用 Admin 角色
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public override async Task<ActionResult<ApiResponse<CategoryDto>>> Create([FromBody] CreateCategoryRequest request)
    {
        return await base.Create(request);
    }

    // 重写 Update 方法以使用 Admin 角色
    [HttpPut("{id:long}")]
    [Authorize(Roles = "Admin")]
    public override async Task<ActionResult<ApiResponse<CategoryDto>>> Update(long id, [FromBody] UpdateCategoryRequest request)
    {
        return await base.Update(id, request);
    }
}
