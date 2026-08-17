using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FinanceApp.Application.Common;
using FinanceApp.Application.Modules.MasterData.DTOs.Customer;
using FinanceApp.Application.Modules.MasterData.Interfaces;
using FinanceApp.Api.Controllers.Base;
using FinanceApp.Api.Helpers;

namespace FinanceApp.Api.Controllers.MasterData;

[ApiController]
[Route("api/customers")]
[Authorize]
public class CustomerController : CrudControllerBase<CustomerDto, CreateCustomerRequest, UpdateCustomerRequest>
{
    private readonly ICustomerService _customerService;

    public CustomerController(ICustomerService customerService, ILogger<CustomerController> logger)
        : base(customerService, logger)
    {
        _customerService = customerService;
    }

    protected override string ControllerName => "CustomerController";
    protected override string EntityName => "Customer";

    protected override string GetCreateSuccessMessage() => "客户创建成功";
    protected override string GetUpdateSuccessMessage() => "客户更新成功";
    protected override string GetDeleteSuccessMessage() => "客户删除成功";

    [HttpGet("active")]
    [Authorize(Roles = "Admin,Accountant,Viewer")]
    public async Task<ActionResult<ApiResponse<List<CustomerDto>>>> GetActive()
    {
        Logger.LogInformation("[CustomerController.GetActive]");

        var result = await _customerService.GetActiveCustomersAsync();
        return Ok(ApiResponse<List<CustomerDto>>.SuccessResponse(result));
    }

    [HttpGet("statistics")]
    [Authorize(Roles = "Admin,Accountant,Viewer")]
    public async Task<ActionResult<ApiResponse<CustomerStatisticsDto>>> GetStatistics([FromQuery] PageRequest request)
    {
        Logger.LogInformation("[CustomerController.GetStatistics]");

        var result = await _customerService.GetStatisticsAsync(request);
        return Ok(ApiResponse<CustomerStatisticsDto>.SuccessResponse(result));
    }

    [HttpGet("{id:long}/finance-summary")]
    [Authorize(Roles = "Admin,Accountant,Viewer")]
    public async Task<ActionResult<ApiResponse<CustomerFinanceSummaryDto>>> GetFinanceSummary(long id)
    {
        Logger.LogInformation("[CustomerController.GetFinanceSummary] CustomerId={CustomerId}", id);
        var result = await _customerService.GetFinanceSummaryAsync(id);
        return Ok(ApiResponse<CustomerFinanceSummaryDto>.SuccessResponse(result));
    }

    [HttpPost("batch")]
    [Authorize(Roles = "Admin,Accountant")]
    public async Task<ActionResult<ApiResponse<BatchCreateResponse<CustomerDto>>>> BatchCreate([FromBody] BatchCreateRequest<CreateCustomerRequest> request)
    {
        var validationError = ValidateBatchRequest(request);
        if (validationError != null) return validationError;

        Logger.LogInformation("[CustomerController.BatchCreate] TotalCount={TotalCount}", request.Items.Count);

        var result = await _customerService.BatchCreateAsync(request.Items);

        if (result.FailedCount > 0)
        {
            Logger.LogWarning("[CustomerController.BatchCreate] 部分失败, 成功={SuccessCount}, 失败={FailedCount}",
                result.SuccessCount, result.FailedCount);
        }

        return Ok(ApiResponse<BatchCreateResponse<CustomerDto>>.SuccessResponse(result, $"成功创建{result.SuccessCount}条，失败{result.FailedCount}条"));
    }

    [HttpPost("batch-import")]
    [Consumes("multipart/form-data")]
    [Authorize(Roles = "Admin,Accountant")]
    public async Task<ActionResult<ApiResponse<BatchCreateResponse<CustomerDto>>>> BatchImport(IFormFile file)
    {
        Logger.LogInformation("[CustomerController.BatchImport] FileName={FileName}, FileSize={FileSize}",
            file?.FileName ?? "null", file?.Length ?? 0);

        var (worksheet, package, error) = ExcelImportHelper.ValidateAndOpenExcel(file);
        if (error != null)
        {
            Logger.LogWarning("[CustomerController.BatchImport] 参数验证失败: {Error}", error);
            return BadRequest(ApiResponse<object>.ErrorResponse(error));
        }

        using (package!)
        {
            var customers = ExcelImportHelper.ReadRows(worksheet!, (ws, row) =>
            {
                var name = ws.Cells[row, 1].Text?.Trim();
                if (string.IsNullOrEmpty(name))
                    return null;

                return new CreateCustomerRequest
                {
                    Name = name,
                    ShortName = ws.Cells[row, 2].Text?.Trim(),
                    ContactPerson = ws.Cells[row, 3].Text?.Trim(),
                    ContactPhone = ws.Cells[row, 4].Text?.Trim(),
                    ContactEmail = ws.Cells[row, 5].Text?.Trim(),
                    Address = ws.Cells[row, 6].Text?.Trim(),
                    TaxNumber = ws.Cells[row, 7].Text?.Trim(),
                    Description = ws.Cells[row, 8].Text?.Trim()
                };
            });

            if (customers.Count == 0)
            {
                return BadRequest(ApiResponse<object>.ErrorResponse("没有找到有效的数据行"));
            }

            Logger.LogInformation("[CustomerController.BatchImport] 解析完成, TotalCount={TotalCount}", customers.Count);

            var result = await _customerService.BatchCreateAsync(customers);

            if (result.FailedCount > 0)
            {
                Logger.LogWarning("[CustomerController.BatchImport] 部分失败, 成功={SuccessCount}, 失败={FailedCount}",
                    result.SuccessCount, result.FailedCount);
            }

            return Ok(ApiResponse<BatchCreateResponse<CustomerDto>>.SuccessResponse(result, $"成功导入{result.SuccessCount}条，失败{result.FailedCount}条"));
        }
    }
}
