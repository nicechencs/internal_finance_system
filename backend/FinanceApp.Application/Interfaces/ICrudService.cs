using FinanceApp.Application.Common;

namespace FinanceApp.Application.Interfaces;

/// <summary>
/// CRUD 服务接口，定义标准的增删改查操作
/// </summary>
/// <typeparam name="TDto">数据传输对象类型</typeparam>
/// <typeparam name="TCreateRequest">创建请求类型</typeparam>
/// <typeparam name="TUpdateRequest">更新请求类型</typeparam>
public interface ICrudService<TDto, TCreateRequest, TUpdateRequest>
    where TDto : class
    where TCreateRequest : class
    where TUpdateRequest : class
{
    /// <summary>
    /// 分页查询
    /// </summary>
    /// <param name="request">分页请求参数</param>
    /// <returns>分页结果</returns>
    Task<PageResponse<TDto>> GetPagedAsync(PageRequest request);

    /// <summary>
    /// 根据 ID 查询单个实体
    /// </summary>
    /// <param name="id">实体 ID</param>
    /// <returns>实体 DTO</returns>
    Task<TDto> GetByIdAsync(long id);

    /// <summary>
    /// 创建新实体
    /// </summary>
    /// <param name="request">创建请求</param>
    /// <returns>创建后的实体 DTO</returns>
    Task<TDto> CreateAsync(TCreateRequest request);

    /// <summary>
    /// 更新实体
    /// </summary>
    /// <param name="id">实体 ID</param>
    /// <param name="request">更新请求</param>
    /// <returns>更新后的实体 DTO</returns>
    Task<TDto> UpdateAsync(long id, TUpdateRequest request);

    /// <summary>
    /// 删除实体（软删除）
    /// </summary>
    /// <param name="id">实体 ID</param>
    Task DeleteAsync(long id);
}
