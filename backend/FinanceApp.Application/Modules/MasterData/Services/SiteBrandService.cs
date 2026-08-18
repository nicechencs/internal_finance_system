using System.Text.Encodings.Web;
using System.Text.Json;
using FinanceApp.Application.Common;
using FinanceApp.Application.Modules.Identity.Interfaces;
using FinanceApp.Application.Modules.MasterData.DTOs.Config;
using FinanceApp.Application.Modules.MasterData.Interfaces;
using FinanceApp.Domain.Constants;
using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace FinanceApp.Application.Modules.MasterData.Services;

public class SiteBrandService : ISiteBrandService
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);
    private static readonly JsonSerializerOptions AuditJsonOptions = new()
    {
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private readonly IRepository<SystemConfig> _configRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMemoryCache _cache;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<SiteBrandService> _logger;

    public SiteBrandService(
        IRepository<SystemConfig> configRepository,
        IUnitOfWork unitOfWork,
        IMemoryCache cache,
        IAuditLogService auditLogService,
        ILogger<SiteBrandService> logger)
    {
        _configRepository = configRepository;
        _unitOfWork = unitOfWork;
        _cache = cache;
        _auditLogService = auditLogService;
        _logger = logger;
    }

    public async Task<PublicBrandDto> GetPublicBrandAsync()
    {
        if (_cache.TryGetValue(SiteBrandDefaults.PublicBrandCacheKey, out PublicBrandDto? cached) && cached != null)
        {
            return cached;
        }

        try
        {
            var brand = await LoadBrandFromStoreAsync();
            _cache.Set(SiteBrandDefaults.PublicBrandCacheKey, brand, CacheTtl);
            return brand;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "读取公开站点品牌失败，回退到默认名称");
            return CreateDefaultBrand();
        }
    }

    public async Task<PublicBrandDto> UpdateSiteBrandAsync(UpdateSiteBrandRequest request)
    {
        var siteName = SiteBrandValidator.NormalizeRequiredName(
            request.SiteName, "站点名称", SiteBrandDefaults.SiteNameMaxLength);
        var siteNameEn = SiteBrandValidator.NormalizeOptionalName(
            request.SiteNameEn, "英文副标题", SiteBrandDefaults.SiteNameEnMaxLength);

        var configs = await _configRepository.GetQueryable()
            .Where(c => c.ConfigKey == SiteBrandDefaults.SiteNameKey || c.ConfigKey == SiteBrandDefaults.SiteNameEnKey)
            .ToListAsync();

        var oldBrand = ResolveBrand(configs);
        var nameConfig = await UpsertConfigAsync(
            configs,
            SiteBrandDefaults.SiteNameKey,
            siteName,
            SiteBrandDefaults.SiteNameDescription);
        await UpsertConfigAsync(
            configs,
            SiteBrandDefaults.SiteNameEnKey,
            siteNameEn,
            SiteBrandDefaults.SiteNameEnDescription);

        await _unitOfWork.SaveChangesAsync();
        InvalidateCache();

        var updated = new PublicBrandDto
        {
            SiteName = siteName,
            SiteNameEn = siteNameEn
        };

        await _auditLogService.LogAsync(
            "Update",
            "SystemConfig",
            nameConfig.Id,
            JsonSerializer.Serialize(oldBrand, AuditJsonOptions),
            JsonSerializer.Serialize(updated, AuditJsonOptions));

        _logger.LogInformation(
            "站点品牌已更新: SiteNameLength={SiteNameLength}, SiteNameEnLength={SiteNameEnLength}",
            siteName.Length,
            siteNameEn.Length);

        _cache.Set(SiteBrandDefaults.PublicBrandCacheKey, updated, CacheTtl);
        return updated;
    }

    public static void InvalidateCache(IMemoryCache cache)
    {
        cache.Remove(SiteBrandDefaults.PublicBrandCacheKey);
    }

    private void InvalidateCache() => InvalidateCache(_cache);

    private async Task<PublicBrandDto> LoadBrandFromStoreAsync()
    {
        var configs = await _configRepository.GetQueryable()
            .Where(c => c.IsActive &&
                        (c.ConfigKey == SiteBrandDefaults.SiteNameKey || c.ConfigKey == SiteBrandDefaults.SiteNameEnKey))
            .ToListAsync();

        return ResolveBrand(configs);
    }

    private static PublicBrandDto ResolveBrand(IEnumerable<SystemConfig> configs)
    {
        var map = configs
            .Where(c => c.IsActive)
            .GroupBy(c => c.ConfigKey)
            .ToDictionary(g => g.Key, g => g.First().ConfigValue);

        map.TryGetValue(SiteBrandDefaults.SiteNameKey, out var storedName);
        map.TryGetValue(SiteBrandDefaults.SiteNameEnKey, out var storedNameEn);

        return new PublicBrandDto
        {
            SiteName = string.IsNullOrWhiteSpace(storedName)
                ? SiteBrandDefaults.SiteName
                : storedName.Trim(),
            SiteNameEn = storedNameEn is null
                ? SiteBrandDefaults.SiteNameEn
                : storedNameEn.Trim()
        };
    }

    private static PublicBrandDto CreateDefaultBrand()
    {
        return new PublicBrandDto
        {
            SiteName = SiteBrandDefaults.SiteName,
            SiteNameEn = SiteBrandDefaults.SiteNameEn
        };
    }

    private async Task<SystemConfig> UpsertConfigAsync(
        List<SystemConfig> existing,
        string key,
        string value,
        string description)
    {
        var config = existing.FirstOrDefault(c => c.ConfigKey == key);
        var now = DateTime.UtcNow;

        if (config == null)
        {
            config = new SystemConfig
            {
                ConfigKey = key,
                ConfigValue = value,
                ConfigType = "string",
                Description = description,
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now
            };
            await _configRepository.AddAsync(config);
            existing.Add(config);
            return config;
        }

        config.ConfigValue = value;
        config.ConfigType = "string";
        config.Description = description;
        config.IsActive = true;
        config.IsDeleted = false;
        config.DeletedAt = null;
        config.UpdatedAt = now;
        _configRepository.Update(config);
        return config;
    }
}
