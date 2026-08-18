using FluentAssertions;
using FinanceApp.Application.Common;
using FinanceApp.Application.Modules.MasterData.DTOs.Config;
using FinanceApp.Application.Modules.MasterData.Services;
using FinanceApp.Application.Tests.Helpers;
using FinanceApp.Domain.Constants;
using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;

namespace FinanceApp.Application.Tests.Services;

public class SiteBrandServiceTests : TestBase
{
    private readonly Mock<IRepository<SystemConfig>> _repositoryMock;
    private readonly IMemoryCache _cache;
    private readonly SiteBrandService _service;

    public SiteBrandServiceTests()
    {
        _repositoryMock = new Mock<IRepository<SystemConfig>>();
        _cache = new MemoryCache(new MemoryCacheOptions());
        _service = new SiteBrandService(
            _repositoryMock.Object,
            UnitOfWorkMock.Object,
            _cache,
            AuditLogServiceMock.Object,
            Mock.Of<ILogger<SiteBrandService>>());
    }

    [Fact]
    public async Task GetPublicBrandAsync_WhenNoConfigs_ReturnsDefaults()
    {
        SetupConfigs(new List<SystemConfig>());

        var result = await _service.GetPublicBrandAsync();

        result.SiteName.Should().Be(SiteBrandDefaults.SiteName);
        result.SiteNameEn.Should().Be(SiteBrandDefaults.SiteNameEn);
    }

    [Fact]
    public async Task GetPublicBrandAsync_WhenBlankSiteName_FallsBackToDefault()
    {
        SetupConfigs(new List<SystemConfig>
        {
            ActiveConfig(SiteBrandDefaults.SiteNameKey, "   "),
            ActiveConfig(SiteBrandDefaults.SiteNameEnKey, "Custom English")
        });

        var result = await _service.GetPublicBrandAsync();

        result.SiteName.Should().Be(SiteBrandDefaults.SiteName);
        result.SiteNameEn.Should().Be("Custom English");
    }

    [Fact]
    public async Task GetPublicBrandAsync_WhenStoredValuesExist_ReturnsStoredValues()
    {
        SetupConfigs(new List<SystemConfig>
        {
            ActiveConfig(SiteBrandDefaults.SiteNameKey, "  自定义站点  "),
            ActiveConfig(SiteBrandDefaults.SiteNameEnKey, "Custom Brand")
        });

        var result = await _service.GetPublicBrandAsync();

        result.SiteName.Should().Be("自定义站点");
        result.SiteNameEn.Should().Be("Custom Brand");
    }

    [Fact]
    public async Task GetPublicBrandAsync_WhenEnglishExplicitlyCleared_ReturnsEmptyEnglish()
    {
        SetupConfigs(new List<SystemConfig>
        {
            ActiveConfig(SiteBrandDefaults.SiteNameKey, "自定义站点"),
            ActiveConfig(SiteBrandDefaults.SiteNameEnKey, "")
        });

        var result = await _service.GetPublicBrandAsync();

        result.SiteName.Should().Be("自定义站点");
        result.SiteNameEn.Should().BeEmpty();
    }

    [Fact]
    public async Task GetPublicBrandAsync_UsesCacheOnSecondCall()
    {
        SetupConfigs(new List<SystemConfig>
        {
            ActiveConfig(SiteBrandDefaults.SiteNameKey, "缓存站点")
        });

        var first = await _service.GetPublicBrandAsync();
        first.SiteName.Should().Be("缓存站点");

        SetupConfigs(new List<SystemConfig>
        {
            ActiveConfig(SiteBrandDefaults.SiteNameKey, "已变更")
        });

        var second = await _service.GetPublicBrandAsync();
        second.SiteName.Should().Be("缓存站点");
        _repositoryMock.Verify(r => r.GetQueryable(), Times.Once);
    }

    [Fact]
    public async Task UpdateSiteBrandAsync_PersistsTrimmedValuesAndInvalidatesCache()
    {
        var nameConfig = ActiveConfig(SiteBrandDefaults.SiteNameKey, "旧名称");
        var enConfig = ActiveConfig(SiteBrandDefaults.SiteNameEnKey, "Old English");
        SetupConfigs(new List<SystemConfig> { nameConfig, enConfig });
        _cache.Set(SiteBrandDefaults.PublicBrandCacheKey, new PublicBrandDto
        {
            SiteName = "旧名称",
            SiteNameEn = "Old English"
        });

        var result = await _service.UpdateSiteBrandAsync(new UpdateSiteBrandRequest
        {
            SiteName = "  新站点  ",
            SiteNameEn = "  New Brand  "
        });

        result.SiteName.Should().Be("新站点");
        result.SiteNameEn.Should().Be("New Brand");
        nameConfig.ConfigValue.Should().Be("新站点");
        enConfig.ConfigValue.Should().Be("New Brand");
        UnitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        AuditLogServiceMock.Verify(
            a => a.LogAsync("Update", "SystemConfig", nameConfig.Id, It.IsAny<string?>(), It.IsAny<string?>()),
            Times.Once);

        var cached = await _service.GetPublicBrandAsync();
        cached.SiteName.Should().Be("新站点");
    }

    [Fact]
    public async Task UpdateSiteBrandAsync_WhenMissingRows_CreatesConfigs()
    {
        SetupConfigs(new List<SystemConfig>());
        _repositoryMock
            .Setup(r => r.AddAsync(It.IsAny<SystemConfig>()))
            .ReturnsAsync((SystemConfig config) =>
            {
                config.Id = config.ConfigKey == SiteBrandDefaults.SiteNameKey ? 11 : 12;
                return config;
            });

        var result = await _service.UpdateSiteBrandAsync(new UpdateSiteBrandRequest
        {
            SiteName = "新建站点",
            SiteNameEn = "Created Brand"
        });

        result.SiteName.Should().Be("新建站点");
        result.SiteNameEn.Should().Be("Created Brand");
        _repositoryMock.Verify(r => r.AddAsync(It.Is<SystemConfig>(c => c.ConfigKey == SiteBrandDefaults.SiteNameKey)), Times.Once);
        _repositoryMock.Verify(r => r.AddAsync(It.Is<SystemConfig>(c => c.ConfigKey == SiteBrandDefaults.SiteNameEnKey)), Times.Once);
    }

    [Fact]
    public async Task UpdateSiteBrandAsync_WithBlankName_ThrowsValidationException()
    {
        SetupConfigs(new List<SystemConfig>());

        var act = () => _service.UpdateSiteBrandAsync(new UpdateSiteBrandRequest
        {
            SiteName = "   "
        });

        await act.Should().ThrowAsync<ValidationException>().WithMessage("*站点名称不能为空*");
    }

    [Fact]
    public async Task UpdateSiteBrandAsync_WithHtml_ThrowsValidationException()
    {
        SetupConfigs(new List<SystemConfig>());

        var act = () => _service.UpdateSiteBrandAsync(new UpdateSiteBrandRequest
        {
            SiteName = "<script>alert(1)</script>"
        });

        await act.Should().ThrowAsync<ValidationException>().WithMessage("*HTML*");
    }

    [Fact]
    public async Task UpdateSiteBrandAsync_WithTooLongName_ThrowsValidationException()
    {
        SetupConfigs(new List<SystemConfig>());

        var act = () => _service.UpdateSiteBrandAsync(new UpdateSiteBrandRequest
        {
            SiteName = new string('站', SiteBrandDefaults.SiteNameMaxLength + 1)
        });

        await act.Should().ThrowAsync<ValidationException>().WithMessage("*长度不能超过*");
    }

    [Fact]
    public async Task GetPublicBrandAsync_WhenRepositoryThrows_ReturnsDefaults()
    {
        _repositoryMock.Setup(r => r.GetQueryable()).Throws(new InvalidOperationException("db down"));

        var result = await _service.GetPublicBrandAsync();

        result.SiteName.Should().Be(SiteBrandDefaults.SiteName);
        result.SiteNameEn.Should().Be(SiteBrandDefaults.SiteNameEn);
    }

    private void SetupConfigs(List<SystemConfig> configs)
    {
        var queryableMock = configs.AsQueryable().BuildMock();
        _repositoryMock.Setup(r => r.GetQueryable()).Returns(queryableMock.Object);
    }

    private static SystemConfig ActiveConfig(string key, string? value)
    {
        return new SystemConfig
        {
            Id = key == SiteBrandDefaults.SiteNameKey ? 1 : 2,
            ConfigKey = key,
            ConfigValue = value,
            IsActive = true,
            UpdatedAt = DateTime.UtcNow
        };
    }
}
