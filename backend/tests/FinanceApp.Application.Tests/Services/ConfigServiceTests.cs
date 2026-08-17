using FluentAssertions;
using FinanceApp.Application.Common;
using FinanceApp.Application.Modules.MasterData.DTOs.Config;
using FinanceApp.Application.Modules.MasterData.Services;
using FinanceApp.Application.Tests.Helpers;
using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;

namespace FinanceApp.Application.Tests.Services;

public class ConfigServiceTests : TestBase
{
    private readonly Mock<IRepository<SystemConfig>> _repositoryMock;
    private readonly Mock<ILogger<ConfigService>> _loggerMock;
    private readonly ConfigService _service;

    public ConfigServiceTests()
    {
        _repositoryMock = new Mock<IRepository<SystemConfig>>();
        _loggerMock = new Mock<ILogger<ConfigService>>();
        _service = new ConfigService(_repositoryMock.Object, _loggerMock.Object, UnitOfWorkMock.Object);
    }

    [Fact]
    public async Task GetAllConfigsAsync_ShouldReturnAllActiveConfigs()
    {
        // Arrange
        var configs = new List<SystemConfig>
        {
            new() { Id = 1, ConfigKey = "app.name", ConfigValue = "财务系统", IsActive = true, UpdatedAt = DateTime.UtcNow },
            new() { Id = 2, ConfigKey = "app.version", ConfigValue = "1.0.0", IsActive = true, UpdatedAt = DateTime.UtcNow },
            new() { Id = 3, ConfigKey = "app.disabled", ConfigValue = "test", IsActive = false, UpdatedAt = DateTime.UtcNow }
        };

        var queryableMock = configs.AsQueryable().BuildMock();
        _repositoryMock.Setup(r => r.GetQueryable()).Returns(queryableMock.Object);

        // Act
        var result = await _service.GetAllConfigsAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.Should().AllSatisfy(c => c.ConfigKey.Should().NotBeNullOrEmpty());
        result.Select(c => c.ConfigKey).Should().BeInAscendingOrder();
    }

    [Fact]
    public async Task GetAllConfigsAsync_WithNoActiveConfigs_ShouldReturnEmptyList()
    {
        // Arrange
        var configs = new List<SystemConfig>
        {
            new() { Id = 1, ConfigKey = "disabled.key", ConfigValue = "value", IsActive = false }
        };

        var queryableMock = configs.AsQueryable().BuildMock();
        _repositoryMock.Setup(r => r.GetQueryable()).Returns(queryableMock.Object);

        // Act
        var result = await _service.GetAllConfigsAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllConfigsAsync_WithNullConfigValue_ShouldReturnEmptyString()
    {
        // Arrange
        var configs = new List<SystemConfig>
        {
            new() { Id = 1, ConfigKey = "null.key", ConfigValue = null, IsActive = true, UpdatedAt = DateTime.UtcNow }
        };

        var queryableMock = configs.AsQueryable().BuildMock();
        _repositoryMock.Setup(r => r.GetQueryable()).Returns(queryableMock.Object);

        // Act
        var result = await _service.GetAllConfigsAsync();

        // Assert
        result.Should().HaveCount(1);
        result[0].ConfigValue.Should().Be("");
    }

    [Fact]
    public async Task GetConfigByKeyAsync_WithExistingKey_ShouldReturnConfig()
    {
        // Arrange
        var config = new SystemConfig
        {
            Id = 1,
            ConfigKey = "app.name",
            ConfigValue = "财务系统",
            Description = "应用名称",
            IsActive = true,
            UpdatedAt = DateTime.UtcNow
        };

        var configs = new List<SystemConfig> { config };
        var queryableMock = configs.AsQueryable().BuildMock();
        _repositoryMock.Setup(r => r.GetQueryable()).Returns(queryableMock.Object);

        // Act
        var result = await _service.GetConfigByKeyAsync("app.name");

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(1);
        result.ConfigKey.Should().Be("app.name");
        result.ConfigValue.Should().Be("财务系统");
        result.Description.Should().Be("应用名称");
    }

    [Fact]
    public async Task GetConfigByKeyAsync_WithNonExistingKey_ShouldThrowNotFoundException()
    {
        // Arrange
        var configs = new List<SystemConfig>();
        var queryableMock = configs.AsQueryable().BuildMock();
        _repositoryMock.Setup(r => r.GetQueryable()).Returns(queryableMock.Object);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => _service.GetConfigByKeyAsync("non.existing.key"));
    }

    [Fact]
    public async Task GetConfigByKeyAsync_WithInactiveConfig_ShouldThrowNotFoundException()
    {
        // Arrange
        var config = new SystemConfig
        {
            Id = 1,
            ConfigKey = "inactive.key",
            ConfigValue = "value",
            IsActive = false
        };

        var configs = new List<SystemConfig> { config };
        var queryableMock = configs.AsQueryable().BuildMock();
        _repositoryMock.Setup(r => r.GetQueryable()).Returns(queryableMock.Object);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => _service.GetConfigByKeyAsync("inactive.key"));
    }

    [Fact]
    public async Task GetConfigByKeyAsync_WithNullValue_ShouldReturnEmptyString()
    {
        // Arrange
        var config = new SystemConfig
        {
            Id = 1,
            ConfigKey = "null.key",
            ConfigValue = null,
            IsActive = true,
            UpdatedAt = DateTime.UtcNow
        };

        var configs = new List<SystemConfig> { config };
        var queryableMock = configs.AsQueryable().BuildMock();
        _repositoryMock.Setup(r => r.GetQueryable()).Returns(queryableMock.Object);

        // Act
        var result = await _service.GetConfigByKeyAsync("null.key");

        // Assert
        result.ConfigValue.Should().Be("");
    }

    [Fact]
    public async Task UpdateConfigAsync_WithExistingKey_ShouldUpdateSuccessfully()
    {
        // Arrange
        var config = new SystemConfig
        {
            Id = 1,
            ConfigKey = "app.name",
            ConfigValue = "旧名称",
            IsActive = true,
            UpdatedAt = DateTime.UtcNow.AddDays(-1)
        };

        var configs = new List<SystemConfig> { config };
        var queryableMock = configs.AsQueryable().BuildMock();
        _repositoryMock.Setup(r => r.GetQueryable()).Returns(queryableMock.Object);

        // Act
        await _service.UpdateConfigAsync("app.name", "新名称");

        // Assert
        config.ConfigValue.Should().Be("新名称");
        config.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task UpdateConfigAsync_WithNonExistingKey_ShouldThrowNotFoundException()
    {
        // Arrange
        var configs = new List<SystemConfig>();
        var queryableMock = configs.AsQueryable().BuildMock();
        _repositoryMock.Setup(r => r.GetQueryable()).Returns(queryableMock.Object);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => _service.UpdateConfigAsync("non.existing.key", "value"));
    }

    [Fact]
    public async Task UpdateConfigAsync_WithInactiveConfig_ShouldThrowNotFoundException()
    {
        // Arrange
        var config = new SystemConfig
        {
            Id = 1,
            ConfigKey = "inactive.key",
            ConfigValue = "old",
            IsActive = false
        };

        var configs = new List<SystemConfig> { config };
        var queryableMock = configs.AsQueryable().BuildMock();
        _repositoryMock.Setup(r => r.GetQueryable()).Returns(queryableMock.Object);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => _service.UpdateConfigAsync("inactive.key", "new"));
    }

    [Fact]
    public async Task UpdateConfigAsync_WithEmptyValue_ShouldUpdateToEmptyString()
    {
        // Arrange
        var config = new SystemConfig
        {
            Id = 1,
            ConfigKey = "test.key",
            ConfigValue = "old value",
            IsActive = true
        };

        var configs = new List<SystemConfig> { config };
        var queryableMock = configs.AsQueryable().BuildMock();
        _repositoryMock.Setup(r => r.GetQueryable()).Returns(queryableMock.Object);

        // Act
        await _service.UpdateConfigAsync("test.key", "");

        // Assert
        config.ConfigValue.Should().Be("");
    }
}
