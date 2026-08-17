using FinanceApp.Application.Common;
using FinanceApp.Application.Interfaces;
using FinanceApp.Application.Modules.FinanceSettlement.DTOs.Receivable;

namespace FinanceApp.Application.Modules.FinanceSettlement.Interfaces;

/// <summary>
/// 应收款业务类型服务接口
/// </summary>
public interface IReceivableTypeService : ICrudService<ReceivableTypeDto, CreateReceivableTypeRequest, UpdateReceivableTypeRequest>
{
    /// <summary>
    /// 获取所有启用的应收款类型
    /// </summary>
    /// <returns>启用的应收款类型列表</returns>
    Task<List<ReceivableTypeDto>> GetAllActiveAsync();
}
