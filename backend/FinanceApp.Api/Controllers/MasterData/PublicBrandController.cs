using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FinanceApp.Application.Common;
using FinanceApp.Application.Modules.MasterData.DTOs.Config;
using FinanceApp.Application.Modules.MasterData.Interfaces;

using FinanceApp.Api.Controllers;

namespace FinanceApp.Api.Controllers.MasterData;

[ApiController]
[Route("api/public/brand")]
[AllowAnonymous]
public class PublicBrandController : BaseApiController
{
    private readonly ISiteBrandService _siteBrandService;
    private readonly ILogger<PublicBrandController> _logger;

    public PublicBrandController(ISiteBrandService siteBrandService, ILogger<PublicBrandController> logger)
    {
        _siteBrandService = siteBrandService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PublicBrandDto>>> GetPublicBrand()
    {
        _logger.LogDebug("[PublicBrandController.GetPublicBrand]");

        var result = await _siteBrandService.GetPublicBrandAsync();
        return Ok(ApiResponse<PublicBrandDto>.SuccessResponse(result));
    }
}
