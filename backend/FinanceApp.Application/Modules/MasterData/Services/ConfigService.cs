using FinanceApp.Application.Common;
using FinanceApp.Application.Modules.MasterData.DTOs.Config;
using FinanceApp.Application.Modules.MasterData.Interfaces;
using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace FinanceApp.Application.Modules.MasterData.Services;

public class ConfigService : IConfigService
{
    private readonly IRepository<SystemConfig> _configRepository;
    private readonly ILogger<ConfigService> _logger;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMemoryCache _cache;

    public ConfigService(
        IRepository<SystemConfig> configRepository,
        ILogger<ConfigService> logger,
        IUnitOfWork unitOfWork,
        IMemoryCache cache)
    {
        _configRepository = configRepository;
        _logger = logger;
        _unitOfWork = unitOfWork;
        _cache = cache;
    }

    public async Task<List<ConfigDto>> GetAllConfigsAsync()
    {
        _logger.LogDebug("ConfigService.GetAllConfigsAsync");

        try
        {
            var configs = await _configRepository.GetQueryable()
                .Where(c => c.IsActive)
                .OrderBy(c => c.ConfigKey)
                .ToListAsync();

            _logger.LogInformation("获取系统配置列表成功，共 {Count} 条", configs.Count);

            return configs.Select(c => new ConfigDto
            {
                Id = c.Id,
                ConfigKey = c.ConfigKey,
                ConfigValue = c.ConfigValue ?? "",
                Description = c.Description,
                UpdatedAt = c.UpdatedAt
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取所有系统配置时发生异常");
            throw;
        }
    }

    public async Task<ConfigDto> GetConfigByKeyAsync(string key)
    {
        _logger.LogDebug("ConfigService.GetConfigByKeyAsync - Key: {Key}", key);

        try
        {
            var config = await _configRepository.GetQueryable()
                .Where(c => c.ConfigKey == key && c.IsActive)
                .FirstOrDefaultAsync();

            if (config == null)
            {
                _logger.LogWarning("未找到系统配置，Key: {Key}", key);
                throw new NotFoundException($"Config with key '{key}' not found");
            }

            _logger.LogDebug(
                "System config retrieved: Key={Key}, HasValue={HasValue}, ValueLength={ValueLength}",
                key,
                HasValue(config.ConfigValue),
                GetValueLength(config.ConfigValue));

            return new ConfigDto
            {
                Id = config.Id,
                ConfigKey = config.ConfigKey,
                ConfigValue = config.ConfigValue ?? "",
                Description = config.Description,
                UpdatedAt = config.UpdatedAt
            };
        }
        catch (NotFoundException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取系统配置时发生异常，Key: {Key}", key);
            throw;
        }
    }

    public async Task UpdateConfigAsync(string key, string value)
    {
        _logger.LogDebug("ConfigService.UpdateConfigAsync - Key: {Key}", key);

        try
        {
            var config = await _configRepository.GetQueryable()
                .Where(c => c.ConfigKey == key && c.IsActive)
                .FirstOrDefaultAsync();

            if (config == null)
            {
                _logger.LogWarning("更新系统配置失败，未找到配置项，Key: {Key}", key);
                throw new NotFoundException($"Config with key '{key}' not found");
            }

            if (SiteBrandValidator.IsBrandKey(key))
            {
                SiteBrandValidator.ValidateBrandValue(key, value);
                value = value?.Trim() ?? string.Empty;
            }

            var oldValue = config.ConfigValue;
            config.ConfigValue = value;
            config.UpdatedAt = DateTime.UtcNow;

            _configRepository.Update(config);
            await _unitOfWork.SaveChangesAsync();

            if (SiteBrandValidator.IsBrandKey(key))
            {
                SiteBrandService.InvalidateCache(_cache);
            }

            _logger.LogInformation(
                "System config updated: Key={Key}, OldHasValue={OldHasValue}, OldValueLength={OldValueLength}, NewHasValue={NewHasValue}, NewValueLength={NewValueLength}",
                key,
                HasValue(oldValue),
                GetValueLength(oldValue),
                HasValue(value),
                GetValueLength(value));
        }
        catch (NotFoundException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新系统配置时发生异常，Key: {Key}", key);
            throw;
        }
    }

    private static bool HasValue(string? value) => !string.IsNullOrWhiteSpace(value);

    private static int GetValueLength(string? value) => value?.Length ?? 0;
}
