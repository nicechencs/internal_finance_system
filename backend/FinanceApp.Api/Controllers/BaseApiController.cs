using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using FinanceApp.Application.Common;

namespace FinanceApp.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public abstract class BaseApiController : ControllerBase
{
    protected long GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim))
            throw new UnauthorizedAccessException("用户未登录");

        return long.Parse(userIdClaim);
    }

    /// <summary>
    /// 验证批量创建请求的通用逻辑
    /// </summary>
    protected static ActionResult? ValidateBatchRequest<T>(BatchCreateRequest<T> request)
    {
        if (request.Items == null || request.Items.Count == 0)
            return new BadRequestObjectResult(ApiResponse<object>.ErrorResponse("请提供要创建的数据"));
        if (request.Items.Count > 500)
            return new BadRequestObjectResult(ApiResponse<object>.ErrorResponse("单次批量创建不能超过500条"));
        return null;
    }
}
