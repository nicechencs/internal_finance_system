using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FinanceApp.Application.Common;
using FinanceApp.Application.Modules.MasterData.DTOs.Supplier;
using FinanceApp.Application.Modules.MasterData.Interfaces;
using FinanceApp.Api.Controllers.Base;
using FinanceApp.Api.Helpers;

namespace FinanceApp.Api.Controllers.MasterData;

[ApiController]
[Route("api/suppliers")]
[Authorize]
public class SupplierController : CrudControllerBase<SupplierDto, CreateSupplierRequest, UpdateSupplierRequest>
{
    private readonly ISupplierService _supplierService;

    public SupplierController(ISupplierService supplierService, ILogger<SupplierController> logger)
        : base(supplierService, logger)
    {
        _supplierService = supplierService;
    }

    protected override string ControllerName => "SupplierController";
    protected override string EntityName => "Supplier";

    protected override string GetCreateSuccessMessage() => "供应商创建成功";
    protected override string GetUpdateSuccessMessage() => "供应商更新成功";
    protected override string GetDeleteSuccessMessage() => "供应商删除成功";

    [HttpGet("active")]
    [Authorize(Roles = "Admin,Accountant,Viewer")]
    public async Task<ActionResult<ApiResponse<List<SupplierDto>>>> GetActive()
    {
        Logger.LogInformation("[SupplierController.GetActive]");

        var result = await _supplierService.GetActiveSuppliersAsync();
        return Ok(ApiResponse<List<SupplierDto>>.SuccessResponse(result));
    }

    [HttpGet("statistics")]
    [Authorize(Roles = "Admin,Accountant,Viewer")]
    public async Task<ActionResult<ApiResponse<SupplierStatisticsDto>>> GetStatistics([FromQuery] PageRequest request)
    {
        Logger.LogInformation("[SupplierController.GetStatistics]");

        var result = await _supplierService.GetStatisticsAsync(request);
        return Ok(ApiResponse<SupplierStatisticsDto>.SuccessResponse(result));
    }

    [HttpGet("{id:long}/finance-summary")]
    [Authorize(Roles = "Admin,Accountant,Viewer")]
    public async Task<ActionResult<ApiResponse<SupplierFinanceSummaryDto>>> GetFinanceSummary(long id)
    {
        Logger.LogInformation("[SupplierController.GetFinanceSummary] SupplierId={SupplierId}", id);
        var result = await _supplierService.GetFinanceSummaryAsync(id);
        return Ok(ApiResponse<SupplierFinanceSummaryDto>.SuccessResponse(result));
    }

    [HttpPost("batch")]
    [Authorize(Roles = "Admin,Accountant")]
    public async Task<ActionResult<ApiResponse<BatchCreateResponse<SupplierDto>>>> BatchCreate([FromBody] BatchCreateRequest<CreateSupplierRequest> request)
    {
        var validationError = ValidateBatchRequest(request);
        if (validationError != null) return validationError;

        Logger.LogInformation("[SupplierController.BatchCreate] TotalCount={TotalCount}", request.Items.Count);

        var result = await _supplierService.BatchCreateAsync(request.Items);

        if (result.FailedCount > 0)
        {
            Logger.LogWarning("[SupplierController.BatchCreate] 部分失败, 成功={SuccessCount}, 失败={FailedCount}",
                result.SuccessCount, result.FailedCount);
        }

        return Ok(ApiResponse<BatchCreateResponse<SupplierDto>>.SuccessResponse(result, $"成功创建{result.SuccessCount}条，失败{result.FailedCount}条"));
    }

    [HttpPost("batch-import")]
    [Consumes("multipart/form-data")]
    [Authorize(Roles = "Admin,Accountant")]
    public async Task<ActionResult<ApiResponse<BatchCreateResponse<SupplierDto>>>> BatchImport(IFormFile file)
    {
        Logger.LogInformation("[SupplierController.BatchImport] FileName={FileName}, FileSize={FileSize}",
            file?.FileName ?? "null", file?.Length ?? 0);

        var (worksheet, package, error) = ExcelImportHelper.ValidateAndOpenExcel(file);
        if (error != null)
        {
            Logger.LogWarning("[SupplierController.BatchImport] 参数验证失败: {Error}", error);
            return BadRequest(ApiResponse<object>.ErrorResponse(error));
        }

        using (package!)
        {
            var suppliers = ExcelImportHelper.ReadRows(worksheet!, (ws, row) =>
            {
                var name = ws.Cells[row, 1].Text?.Trim();
                if (string.IsNullOrEmpty(name))
                    return null;

                return new CreateSupplierRequest
                {
                    Name = name,
                    ShortName = ws.Cells[row, 2].Text?.Trim(),
                    ContactPerson = ws.Cells[row, 3].Text?.Trim(),
                    ContactPhone = ws.Cells[row, 4].Text?.Trim(),
                    ContactEmail = ws.Cells[row, 5].Text?.Trim(),
                    Address = ws.Cells[row, 6].Text?.Trim(),
                    TaxNumber = ws.Cells[row, 7].Text?.Trim(),
                    BankAccount = ws.Cells[row, 8].Text?.Trim(),
                    BankName = ws.Cells[row, 9].Text?.Trim(),
                    Description = ws.Cells[row, 10].Text?.Trim()
                };
            });

            if (suppliers.Count == 0)
            {
                return BadRequest(ApiResponse<object>.ErrorResponse("没有找到有效的数据行"));
            }

            Logger.LogInformation("[SupplierController.BatchImport] 解析完成, TotalCount={TotalCount}", suppliers.Count);

            var result = await _supplierService.BatchCreateAsync(suppliers);

            if (result.FailedCount > 0)
            {
                Logger.LogWarning("[SupplierController.BatchImport] 部分失败, 成功={SuccessCount}, 失败={FailedCount}",
                    result.SuccessCount, result.FailedCount);
            }

            return Ok(ApiResponse<BatchCreateResponse<SupplierDto>>.SuccessResponse(result, $"成功导入{result.SuccessCount}条，失败{result.FailedCount}条"));
        }
    }
}
