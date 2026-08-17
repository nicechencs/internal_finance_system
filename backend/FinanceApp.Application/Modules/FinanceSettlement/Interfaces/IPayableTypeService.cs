using FinanceApp.Application.Common;
using FinanceApp.Application.Interfaces;
using FinanceApp.Application.Modules.FinanceSettlement.DTOs.Payable;

namespace FinanceApp.Application.Modules.FinanceSettlement.Interfaces;

/// <summary>
/// 应付款业务类型服务接口
/// </summary>
public interface IPayableTypeService : ICrudService<PayableTypeDto, CreatePayableTypeRequest, UpdatePayableTypeRequest>
{
    /// <summary>
    /// 获取所有启用的应付款类型
    /// </summary>
    /// <returns>启用的应付款类型列表</returns>
    Task<List<PayableTypeDto>> GetAllActiveAsync();
}
