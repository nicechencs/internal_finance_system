using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FinanceApp.Application.Common;
using FinanceApp.Application.Modules.MasterData.DTOs.Config;
using FinanceApp.Application.Modules.MasterData.Interfaces;

using FinanceApp.Api.Controllers;

namespace FinanceApp.Api.Controllers.MasterData;

[ApiController]
[Route("api/configs")]
[Authorize(Roles = "Admin")]
public class ConfigController : BaseApiController
{
    private readonly IConfigService _configService;
    private readonly ILogger<ConfigController> _logger;

    public ConfigController(IConfigService configService, ILogger<ConfigController> logger)
    {
        _configService = configService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<ConfigDto>>>> GetAllConfigs()
    {
        _logger.LogInformation("[ConfigController.GetAllConfigs]");

        var result = await _configService.GetAllConfigsAsync();
        return Ok(ApiResponse<List<ConfigDto>>.SuccessResponse(result));
    }

    [HttpGet("{key}")]
    public async Task<ActionResult<ApiResponse<ConfigDto>>> GetConfigByKey(string key)
    {
        _logger.LogInformation("[ConfigController.GetConfigByKey] Key={Key}", key);

        var result = await _configService.GetConfigByKeyAsync(key);
        return Ok(ApiResponse<ConfigDto>.SuccessResponse(result));
    }

    [HttpPut("{key}")]
    public async Task<ActionResult<ApiResponse<object>>> UpdateConfig(string key, [FromBody] UpdateConfigRequest request)
    {
        _logger.LogInformation("[ConfigController.UpdateConfig] Key={Key}", key);

        await _configService.UpdateConfigAsync(key, request.ConfigValue);
        return Ok(ApiResponse<object>.SuccessResponse(null, "配置更新成功"));
    }
}
