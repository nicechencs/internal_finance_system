using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FinanceApp.Application.Common;
using FinanceApp.Application.Modules.MasterData.DTOs.Person;
using FinanceApp.Application.Modules.MasterData.Interfaces;
using FinanceApp.Api.Controllers.Base;
using FinanceApp.Api.Helpers;

namespace FinanceApp.Api.Controllers.MasterData;

[ApiController]
[Route("api/persons")]
[Authorize]
public class PersonController : CrudControllerBase<PersonDto, CreatePersonRequest, UpdatePersonRequest>
{
    private readonly IPersonService _personService;

    public PersonController(IPersonService personService, ILogger<PersonController> logger)
        : base(personService, logger)
    {
        _personService = personService;
    }

    protected override string ControllerName => "PersonController";
    protected override string EntityName => "Person";

    protected override string GetCreateSuccessMessage() => "人员创建成功";
    protected override string GetUpdateSuccessMessage() => "人员更新成功";
    protected override string GetDeleteSuccessMessage() => "人员删除成功";

    [HttpGet("active")]
    [Authorize(Roles = "Admin,Accountant,Viewer")]
    public async Task<ActionResult<ApiResponse<List<PersonDto>>>> GetActive()
    {
        Logger.LogInformation("[PersonController.GetActive]");

        var result = await _personService.GetActivePersonsAsync();
        return Ok(ApiResponse<List<PersonDto>>.SuccessResponse(result));
    }

    [HttpGet("statistics")]
    [Authorize(Roles = "Admin,Accountant,Viewer")]
    public async Task<ActionResult<ApiResponse<PersonStatisticsDto>>> GetStatistics([FromQuery] PageRequest request)
    {
        Logger.LogInformation("[PersonController.GetStatistics]");

        var result = await _personService.GetStatisticsAsync(request);
        return Ok(ApiResponse<PersonStatisticsDto>.SuccessResponse(result));
    }

    [HttpGet("{id:long}/cost-summary")]
    [Authorize(Roles = "Admin,Accountant,Viewer")]
    public async Task<ActionResult<ApiResponse<PersonCostSummaryDto>>> GetCostSummary(long id)
    {
        Logger.LogInformation("[PersonController.GetCostSummary] PersonId={PersonId}", id);

        var result = await _personService.GetPersonCostSummaryAsync(id);
        return Ok(ApiResponse<PersonCostSummaryDto>.SuccessResponse(result));
    }

    [HttpGet("{id:long}/finance-summary")]
    [Authorize(Roles = "Admin,Accountant,Viewer")]
    public async Task<ActionResult<ApiResponse<PersonFinanceSummaryDto>>> GetFinanceSummary(long id)
    {
        Logger.LogInformation("[PersonController.GetFinanceSummary] PersonId={PersonId}", id);
        var result = await _personService.GetFinanceSummaryAsync(id);
        return Ok(ApiResponse<PersonFinanceSummaryDto>.SuccessResponse(result));
    }

    [HttpPost("batch")]
    [Authorize(Roles = "Admin,Accountant")]
    public async Task<ActionResult<ApiResponse<BatchCreateResponse<PersonDto>>>> BatchCreate([FromBody] BatchCreateRequest<CreatePersonRequest> request)
    {
        var validationError = ValidateBatchRequest(request);
        if (validationError != null) return validationError;

        Logger.LogInformation("[PersonController.BatchCreate] TotalCount={TotalCount}", request.Items.Count);

        var result = await _personService.BatchCreateAsync(request.Items);

        if (result.FailedCount > 0)
        {
            Logger.LogWarning("[PersonController.BatchCreate] 部分失败, 成功={SuccessCount}, 失败={FailedCount}",
                result.SuccessCount, result.FailedCount);
        }

        return Ok(ApiResponse<BatchCreateResponse<PersonDto>>.SuccessResponse(result, $"成功创建{result.SuccessCount}条，失败{result.FailedCount}条"));
    }

    [HttpPost("batch-import")]
    [Consumes("multipart/form-data")]
    [Authorize(Roles = "Admin,Accountant")]
    public async Task<ActionResult<ApiResponse<BatchCreateResponse<PersonDto>>>> BatchImport(IFormFile file)
    {
        Logger.LogInformation("[PersonController.BatchImport] FileName={FileName}, FileSize={FileSize}",
            file?.FileName ?? "null", file?.Length ?? 0);

        var (worksheet, package, error) = ExcelImportHelper.ValidateAndOpenExcel(file);
        if (error != null)
        {
            Logger.LogWarning("[PersonController.BatchImport] 参数验证失败: {Error}", error);
            return BadRequest(ApiResponse<object>.ErrorResponse(error));
        }

        using (package!)
        {
            var persons = ExcelImportHelper.ReadRows(worksheet!, (ws, row) =>
            {
                var name = ws.Cells[row, 1].Text?.Trim();
                var personType = ws.Cells[row, 2].Text?.Trim();

                if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(personType))
                    return null;

                var joinDateText = ws.Cells[row, 8].Text?.Trim();
                DateTime? joinDate = null;
                if (!string.IsNullOrEmpty(joinDateText) && DateTime.TryParse(joinDateText, out var jd))
                {
                    joinDate = jd;
                }

                return new CreatePersonRequest
                {
                    Name = name,
                    PersonType = personType,
                    IdNumber = ws.Cells[row, 3].Text?.Trim(),
                    Phone = ws.Cells[row, 4].Text?.Trim(),
                    Email = ws.Cells[row, 5].Text?.Trim(),
                    BankAccount = ws.Cells[row, 6].Text?.Trim(),
                    BankName = ws.Cells[row, 7].Text?.Trim(),
                    JoinDate = joinDate
                };
            });

            if (persons.Count == 0)
            {
                return BadRequest(ApiResponse<object>.ErrorResponse("没有找到有效的数据行"));
            }

            Logger.LogInformation("[PersonController.BatchImport] 解析完成, TotalCount={TotalCount}", persons.Count);

            var result = await _personService.BatchCreateAsync(persons);

            if (result.FailedCount > 0)
            {
                Logger.LogWarning("[PersonController.BatchImport] 部分失败, 成功={SuccessCount}, 失败={FailedCount}",
                    result.SuccessCount, result.FailedCount);
            }

            return Ok(ApiResponse<BatchCreateResponse<PersonDto>>.SuccessResponse(result, $"成功导入{result.SuccessCount}条，失败{result.FailedCount}条"));
        }
    }
}
