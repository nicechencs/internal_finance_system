using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using FluentAssertions;
using FinanceApp.Api.Controllers.MasterData;
using FinanceApp.Application.Common;
using FinanceApp.Application.Modules.MasterData.DTOs.Config;
using FinanceApp.Application.Modules.MasterData.Interfaces;
using FinanceApp.Domain.Constants;

namespace FinanceApp.Api.Tests.Controllers;

public class PublicBrandControllerTests
{
    private readonly Mock<ISiteBrandService> _siteBrandServiceMock;
    private readonly PublicBrandController _controller;

    public PublicBrandControllerTests()
    {
        _siteBrandServiceMock = new Mock<ISiteBrandService>();
        _controller = new PublicBrandController(
            _siteBrandServiceMock.Object,
            Mock.Of<ILogger<PublicBrandController>>());
    }

    [Fact]
    public async Task GetPublicBrand_ReturnsOnlyBrandFields()
    {
        _siteBrandServiceMock
            .Setup(x => x.GetPublicBrandAsync())
            .ReturnsAsync(new PublicBrandDto
            {
                SiteName = SiteBrandDefaults.SiteName,
                SiteNameEn = SiteBrandDefaults.SiteNameEn
            });

        var result = await _controller.GetPublicBrand();

        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<PublicBrandDto>>().Subject;
        apiResponse.Success.Should().BeTrue();
        apiResponse.Data.Should().NotBeNull();
        apiResponse.Data!.SiteName.Should().Be(SiteBrandDefaults.SiteName);
        apiResponse.Data.SiteNameEn.Should().Be(SiteBrandDefaults.SiteNameEn);
        apiResponse.Data.GetType().GetProperties().Select(p => p.Name)
            .Should().BeEquivalentTo("SiteName", "SiteNameEn");
    }
}
