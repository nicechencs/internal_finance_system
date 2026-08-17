using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using FinanceApp.Application.Common;
using FinanceApp.Application.Interfaces;

namespace FinanceApp.Api.Controllers.Base;

/// <summary>
/// CRUD 控制器基类，提供标准的增删改查端点
/// </summary>
/// <typeparam name="TDto">数据传输对象类型</typeparam>
/// <typeparam name="TCreateRequest">创建请求类型</typeparam>
/// <typeparam name="TUpdateRequest">更新请求类型</typeparam>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public abstract class CrudControllerBase<TDto, TCreateRequest, TUpdateRequest> : BaseApiController
    where TDto : class
    where TCreateRequest : class
    where TUpdateRequest : class
{
    protected readonly ICrudService<TDto, TCreateRequest, TUpdateRequest> Service;
    protected readonly ILogger Logger;

    protected CrudControllerBase(
        ICrudService<TDto, TCreateRequest, TUpdateRequest> service,
        ILogger logger)
    {
        Service = service;
        Logger = logger;
    }

    /// <summary>
    /// 获取控制器名称（用于日志）
    /// </summary>
    protected abstract string ControllerName { get; }

    /// <summary>
    /// 获取实体名称（用于日志）
    /// </summary>
    protected abstract string EntityName { get; }

    /// <summary>
    /// 分页查询
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "Admin,Accountant,Viewer")]
    public virtual async Task<ActionResult<ApiResponse<PageResponse<TDto>>>> GetPaged([FromQuery] PageRequest request)
    {
        Logger.LogInformation("[{ControllerName}.GetPaged] Page={Page}, PageSize={PageSize}",
            ControllerName, request.Page, request.PageSize);

        var result = await Service.GetPagedAsync(request);
        return Ok(ApiResponse<PageResponse<TDto>>.SuccessResponse(result));
    }

    /// <summary>
    /// 根据 ID 查询
    /// </summary>
    [HttpGet("{id:long}")]
    [Authorize(Roles = "Admin,Accountant,Viewer")]
    public virtual async Task<ActionResult<ApiResponse<TDto>>> GetById(long id)
    {
        Logger.LogInformation("[{ControllerName}.GetById] {EntityName}Id={Id}",
            ControllerName, EntityName, id);

        var result = await Service.GetByIdAsync(id);
        return Ok(ApiResponse<TDto>.SuccessResponse(result));
    }

    /// <summary>
    /// 创建
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin,Accountant")]
    public virtual async Task<ActionResult<ApiResponse<TDto>>> Create([FromBody] TCreateRequest request)
    {
        Logger.LogInformation("[{ControllerName}.Create] creating {EntityName}",
            ControllerName, EntityName);

        var result = await Service.CreateAsync(request);
        return StatusCode(201, ApiResponse<TDto>.CreatedResponse(result, GetCreateSuccessMessage()));
    }

    /// <summary>
    /// 更新
    /// </summary>
    [HttpPut("{id:long}")]
    [Authorize(Roles = "Admin,Accountant")]
    public virtual async Task<ActionResult<ApiResponse<TDto>>> Update(long id, [FromBody] TUpdateRequest request)
    {
        Logger.LogInformation("[{ControllerName}.Update] {EntityName}Id={Id}",
            ControllerName, EntityName, id);

        var result = await Service.UpdateAsync(id, request);
        return Ok(ApiResponse<TDto>.SuccessResponse(result, GetUpdateSuccessMessage()));
    }

    /// <summary>
    /// 删除
    /// </summary>
    [HttpDelete("{id:long}")]
    [Authorize(Roles = "Admin")]
    public virtual async Task<ActionResult<ApiResponse<object>>> Delete(long id)
    {
        Logger.LogInformation("[{ControllerName}.Delete] {EntityName}Id={Id}",
            ControllerName, EntityName, id);

        await Service.DeleteAsync(id);
        return Ok(ApiResponse<object>.SuccessResponse(null, GetDeleteSuccessMessage()));
    }

    /// <summary>
    /// 获取创建成功消息（子类可重写）
    /// </summary>
    protected virtual string GetCreateSuccessMessage() => "创建成功";

    /// <summary>
    /// 获取更新成功消息（子类可重写）
    /// </summary>
    protected virtual string GetUpdateSuccessMessage() => "更新成功";

    /// <summary>
    /// 获取删除成功消息（子类可重写）
    /// </summary>
    protected virtual string GetDeleteSuccessMessage() => "删除成功";
}
