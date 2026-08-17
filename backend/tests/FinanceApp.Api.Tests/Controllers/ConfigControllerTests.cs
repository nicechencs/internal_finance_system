using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using FluentAssertions;
using FinanceApp.Api.Controllers.MasterData;
using FinanceApp.Application.Common;
using FinanceApp.Application.Modules.MasterData.DTOs.Config;
using FinanceApp.Application.Modules.MasterData.Interfaces;

namespace FinanceApp.Api.Tests.Controllers;

public class ConfigControllerTests
{
    private readonly Mock<IConfigService> _configServiceMock;
    private readonly Mock<ILogger<ConfigController>> _loggerMock;
    private readonly ConfigController _controller;

    public ConfigControllerTests()
    {
        _configServiceMock = new Mock<IConfigService>();
        _loggerMock = new Mock<ILogger<ConfigController>>();
        _controller = new ConfigController(_configServiceMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task GetAllConfigs_ReturnsOkWithAllConfigs()
    {
        // Arrange
        var expectedConfigs = new List<ConfigDto>
        {
            new ConfigDto
            {
                Id = 1,
                ConfigKey = "system.currency",
                ConfigValue = "CNY",
                Description = "系统默认货币",
                UpdatedAt = DateTime.UtcNow
            },
            new ConfigDto
            {
                Id = 2,
                ConfigKey = "system.timezone",
                ConfigValue = "Asia/Shanghai",
                Description = "系统时区",
                UpdatedAt = DateTime.UtcNow
            }
        };

        _configServiceMock
            .Setup(x => x.GetAllConfigsAsync())
            .ReturnsAsync(expectedConfigs);

        // Act
        var result = await _controller.GetAllConfigs();

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);

        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<List<ConfigDto>>>().Subject;
        apiResponse.Success.Should().BeTrue();
        apiResponse.Data.Should().NotBeNull();
        apiResponse.Data!.Should().HaveCount(2);
        apiResponse.Data[0].ConfigKey.Should().Be("system.currency");
        apiResponse.Data[1].ConfigKey.Should().Be("system.timezone");

        _configServiceMock.Verify(x => x.GetAllConfigsAsync(), Times.Once);
    }

    [Fact]
    public async Task GetConfigByKey_ExistingKey_ReturnsOkWithConfig()
    {
        // Arrange
        var configKey = "system.currency";
        var expectedConfig = new ConfigDto
        {
            Id = 1,
            ConfigKey = configKey,
            ConfigValue = "CNY",
            Description = "系统默认货币",
            UpdatedAt = DateTime.UtcNow
        };

        _configServiceMock
            .Setup(x => x.GetConfigByKeyAsync(configKey))
            .ReturnsAsync(expectedConfig);

        // Act
        var result = await _controller.GetConfigByKey(configKey);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);

        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<ConfigDto>>().Subject;
        apiResponse.Success.Should().BeTrue();
        apiResponse.Data.Should().NotBeNull();
        apiResponse.Data!.ConfigKey.Should().Be(configKey);
        apiResponse.Data.ConfigValue.Should().Be("CNY");

        _configServiceMock.Verify(x => x.GetConfigByKeyAsync(configKey), Times.Once);
    }

    [Fact]
    public async Task UpdateConfig_ValidRequest_ReturnsOkWithSuccessMessage()
    {
        // Arrange
        var configKey = "system.currency";
        var request = new UpdateConfigRequest
        {
            ConfigValue = "USD"
        };

        var oldConfig = new ConfigDto
        {
            Id = 1,
            ConfigKey = configKey,
            ConfigValue = "CNY",
            Description = "系统默认货币",
            UpdatedAt = DateTime.UtcNow
        };

        _configServiceMock
            .Setup(x => x.GetConfigByKeyAsync(configKey))
            .ReturnsAsync(oldConfig);

        _configServiceMock
            .Setup(x => x.UpdateConfigAsync(configKey, request.ConfigValue))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.UpdateConfig(configKey, request);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);

        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<object>>().Subject;
        apiResponse.Success.Should().BeTrue();
        apiResponse.Message.Should().Be("配置更新成功");

        _configServiceMock.Verify(x => x.UpdateConfigAsync(configKey, request.ConfigValue), Times.Once);
    }

    [Fact]
    public async Task UpdateConfig_EmptyValue_ReturnsOkWithSuccessMessage()
    {
        // Arrange
        var configKey = "system.optional_setting";
        var request = new UpdateConfigRequest
        {
            ConfigValue = ""
        };

        var oldConfig = new ConfigDto
        {
            Id = 1,
            ConfigKey = configKey,
            ConfigValue = "old_value",
            Description = "可选配置项",
            UpdatedAt = DateTime.UtcNow
        };

        _configServiceMock
            .Setup(x => x.GetConfigByKeyAsync(configKey))
            .ReturnsAsync(oldConfig);

        _configServiceMock
            .Setup(x => x.UpdateConfigAsync(configKey, request.ConfigValue))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.UpdateConfig(configKey, request);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);

        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<object>>().Subject;
        apiResponse.Success.Should().BeTrue();
        apiResponse.Message.Should().Be("配置更新成功");

        _configServiceMock.Verify(x => x.UpdateConfigAsync(configKey, ""), Times.Once);
    }
}
