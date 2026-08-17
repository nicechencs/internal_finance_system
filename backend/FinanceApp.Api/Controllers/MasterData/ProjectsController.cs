using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using FinanceApp.Application.Common;
using FinanceApp.Application.Modules.MasterData.DTOs.Project;
using FinanceApp.Application.Modules.MasterData.Interfaces;
using FinanceApp.Api.Controllers.Base;
using FinanceApp.Api.Helpers;

namespace FinanceApp.Api.Controllers.MasterData;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProjectsController : CrudControllerBase<ProjectDto, CreateProjectRequest, UpdateProjectRequest>
{
    private readonly IProjectService _projectService;

    public ProjectsController(IProjectService projectService, ILogger<ProjectsController> logger)
        : base(projectService, logger)
    {
        _projectService = projectService;
    }

    protected override string ControllerName => "ProjectsController";
    protected override string EntityName => "Project";

    [HttpGet("generate-code")]
    [Authorize(Roles = "Admin,Accountant")]
    public async Task<ActionResult<ApiResponse<string>>> GenerateCode()
    {
        Logger.LogInformation("[ProjectsController.GenerateCode]");

        var result = await _projectService.GenerateProjectCodeAsync();
        return Ok(ApiResponse<string>.SuccessResponse(result));
    }

    [HttpGet("active")]
    [Authorize(Roles = "Admin,Accountant,Viewer")]
    public async Task<ActionResult<ApiResponse<List<ProjectDto>>>> GetActive()
    {
        Logger.LogInformation("[ProjectsController.GetActive]");

        var result = await _projectService.GetActiveProjectsAsync();
        return Ok(ApiResponse<List<ProjectDto>>.SuccessResponse(result));
    }

    [HttpGet("statistics")]
    [Authorize(Roles = "Admin,Accountant,Viewer")]
    public async Task<ActionResult<ApiResponse<ProjectStatisticsDto>>> GetStatistics([FromQuery] PageRequest request)
    {
        Logger.LogInformation("[ProjectsController.GetStatistics]");

        var result = await _projectService.GetStatisticsAsync(request);
        return Ok(ApiResponse<ProjectStatisticsDto>.SuccessResponse(result));
    }

    [HttpGet("profit-report")]
    [Authorize(Roles = "Admin,Accountant,Viewer")]
    public async Task<ActionResult<ApiResponse<List<ProjectProfitReportDto>>>> GetProjectProfitReport()
    {
        Logger.LogInformation("[ProjectsController.GetProjectProfitReport]");

        var result = await _projectService.GetProjectProfitReportAsync();
        return Ok(ApiResponse<List<ProjectProfitReportDto>>.SuccessResponse(result));
    }

    [HttpPost("batch")]
    [Authorize(Roles = "Admin,Accountant")]
    public async Task<ActionResult<ApiResponse<BatchCreateResponse<ProjectDto>>>> BatchCreate([FromBody] BatchCreateRequest<CreateProjectRequest> request)
    {
        var validationError = ValidateBatchRequest(request);
        if (validationError != null) return validationError;

        Logger.LogInformation("[ProjectsController.BatchCreate] TotalCount={TotalCount}", request.Items.Count);

        var result = await _projectService.BatchCreateAsync(request.Items);

        if (result.FailedCount > 0)
        {
            Logger.LogWarning("[ProjectsController.BatchCreate] 部分失败, 成功={SuccessCount}, 失败={FailedCount}",
                result.SuccessCount, result.FailedCount);
        }

        return Ok(ApiResponse<BatchCreateResponse<ProjectDto>>.SuccessResponse(result, $"成功创建{result.SuccessCount}条，失败{result.FailedCount}条"));
    }

    [HttpPost("batch-import")]
    [Consumes("multipart/form-data")]
    [Authorize(Roles = "Admin,Accountant")]
    public async Task<ActionResult<ApiResponse<BatchCreateResponse<ProjectDto>>>> BatchImport(IFormFile file)
    {
        Logger.LogInformation("[ProjectsController.BatchImport] FileName={FileName}, FileSize={FileSize}",
            file?.FileName ?? "null", file?.Length ?? 0);

        var (worksheet, package, error) = ExcelImportHelper.ValidateAndOpenExcel(file);
        if (error != null)
        {
            Logger.LogWarning("[ProjectsController.BatchImport] 参数验证失败: {Error}", error);
            return BadRequest(ApiResponse<object>.ErrorResponse(error));
        }

        using (package!)
        {
            var projects = ExcelImportHelper.ReadRows(worksheet!, (ws, row) =>
            {
                var name = ws.Cells[row, 1].Text?.Trim();
                var contractAmountText = ws.Cells[row, 2].Text?.Trim();

                if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(contractAmountText))
                    return null;

                if (!decimal.TryParse(contractAmountText, out var contractAmount))
                    return null;

                var customerIdText = ws.Cells[row, 4].Text?.Trim();
                long customerId = 0;
                if (!string.IsNullOrEmpty(customerIdText) && long.TryParse(customerIdText, out var cid))
                {
                    customerId = cid;
                }

                var startDateText = ws.Cells[row, 5].Text?.Trim();
                if (string.IsNullOrEmpty(startDateText) || !DateTime.TryParse(startDateText, out var startDate))
                {
                    startDate = DateTime.Now;
                }

                var endDateText = ws.Cells[row, 6].Text?.Trim();
                DateTime? endDate = null;
                if (!string.IsNullOrEmpty(endDateText) && DateTime.TryParse(endDateText, out var ed))
                {
                    endDate = ed;
                }

                return new CreateProjectRequest
                {
                    Name = name,
                    ContractAmount = contractAmount,
                    ProjectCode = ws.Cells[row, 3].Text?.Trim() ?? string.Empty,
                    CustomerId = customerId,
                    StartDate = startDate,
                    EndDate = endDate,
                    Description = ws.Cells[row, 7].Text?.Trim()
                };
            });

            if (projects.Count == 0)
            {
                return BadRequest(ApiResponse<object>.ErrorResponse("没有找到有效的数据行"));
            }

            Logger.LogInformation("[ProjectsController.BatchImport] 解析完成, TotalCount={TotalCount}", projects.Count);

            var result = await _projectService.BatchCreateAsync(projects);

            if (result.FailedCount > 0)
            {
                Logger.LogWarning("[ProjectsController.BatchImport] 部分失败, 成功={SuccessCount}, 失败={FailedCount}",
                    result.SuccessCount, result.FailedCount);
            }

            return Ok(ApiResponse<BatchCreateResponse<ProjectDto>>.SuccessResponse(result, $"成功导入{result.SuccessCount}条，失败{result.FailedCount}条"));
        }
    }

    [HttpGet("{id:long}/profit-analysis")]
    [Authorize(Roles = "Admin,Accountant,Viewer")]
    public async Task<ActionResult<ApiResponse<ProfitAnalysisResponse>>> GetProfitAnalysis(long id, [FromQuery] int months = 12)
    {
        Logger.LogInformation("[ProjectsController.GetProfitAnalysis] ProjectId={ProjectId}, Months={Months}", id, months);

        var result = await _projectService.GetProfitAnalysisAsync(id, months);
        return Ok(ApiResponse<ProfitAnalysisResponse>.SuccessResponse(result));
    }

    [HttpPost("{id:long}/initialize-receivables")]
    [Authorize(Roles = "Admin,Accountant")]
    public async Task<ActionResult<ApiResponse<object>>> InitializeReceivables(long id, [FromBody] InitializeReceivablesRequest request)
    {
        Logger.LogInformation("[ProjectsController.InitializeReceivables] ProjectId={ProjectId}, Mode={Mode}",
            id, request.Mode);

        await _projectService.InitializeReceivablesAsync(id, request);
        return Ok(ApiResponse<object>.SuccessResponse(null, "收款计划初始化成功"));
    }
}
