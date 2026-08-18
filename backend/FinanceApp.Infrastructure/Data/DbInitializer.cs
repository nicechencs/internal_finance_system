using FinanceApp.Domain.Configuration;
using FinanceApp.Domain.Constants;
using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Enums;
using FinanceApp.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FinanceApp.Infrastructure.Data;

public static class DbInitializer
{
    public static async Task SeedAsync(AppDbContext context, ILogger logger, IServiceProvider serviceProvider)
    {
        try
        {
            await SeedBootstrapAdminAsync(context, logger, serviceProvider);
            await SeedDefaultCategoriesAsync(context, logger);
            await SeedDefaultSiteBrandAsync(context, logger);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Database seeding failed.");
            throw;
        }
    }

    private static async Task SeedBootstrapAdminAsync(AppDbContext context, ILogger logger, IServiceProvider serviceProvider)
    {
        var authOptions = serviceProvider.GetRequiredService<IOptions<AuthOptions>>().Value;
        var bootstrapAdmin = authOptions.BootstrapAdmin;

        if (!bootstrapAdmin.Enabled)
        {
            logger.LogInformation("Bootstrap admin is disabled. Skipping administrator seeding.");
            return;
        }

        var environment = serviceProvider.GetService<IHostEnvironment>();
        ValidateBootstrapAdmin(bootstrapAdmin, authOptions, environment);

        var normalizedUsername = NormalizeUsername(bootstrapAdmin.Username);
        var exists = await context.Users
            .AnyAsync(u =>
                u.NormalizedUsername == normalizedUsername
                || u.Username.ToUpper() == normalizedUsername);

        if (exists)
        {
            logger.LogInformation("Bootstrap admin already exists. Skipping creation.");
            return;
        }

        var passwordService = serviceProvider.GetRequiredService<IPasswordService>();
        var now = DateTime.UtcNow;
        var user = new User
        {
            Username = bootstrapAdmin.Username.Trim(),
            NormalizedUsername = normalizedUsername,
            PasswordHash = passwordService.HashPassword(bootstrapAdmin.Password),
            SecurityStamp = CreateSecurityStamp(),
            FullName = bootstrapAdmin.FullName.Trim(),
            Email = string.IsNullOrWhiteSpace(bootstrapAdmin.Email) ? null : bootstrapAdmin.Email.Trim(),
            Role = UserRole.Admin,
            IsActive = true,
            MustChangePassword = false,
            PasswordChangedAt = now,
            CreatedAt = now,
            UpdatedAt = now
        };

        context.Users.Add(user);
        await context.SaveChangesAsync();
        logger.LogInformation("Bootstrap admin created successfully, Username={Username}", user.Username);
    }

    private static async Task SeedDefaultCategoriesAsync(AppDbContext context, ILogger logger)
    {
        if (await context.Categories.AnyAsync())
        {
            logger.LogInformation("Default categories already exist. Skipping category seeding.");
            return;
        }

        var adminUserId = await context.Users
            .Where(u => u.Role == UserRole.Admin && u.IsActive)
            .OrderBy(u => u.Id)
            .Select(u => (long?)u.Id)
            .FirstOrDefaultAsync();

        var categories = new List<Category>
        {
            new()
            {
                Name = "项目收入",
                CategoryType = CategoryType.Income,
                Level = 1,
                SortOrder = 1,
                Description = "项目合同收入",
                CreatedBy = adminUserId
            },
            new()
            {
                Name = "咨询收入",
                CategoryType = CategoryType.Income,
                Level = 1,
                SortOrder = 2,
                Description = "咨询服务收入",
                CreatedBy = adminUserId
            },
            new()
            {
                Name = "技术服务费",
                CategoryType = CategoryType.Income,
                Level = 1,
                SortOrder = 3,
                Description = "技术服务费收入",
                CreatedBy = adminUserId
            },
            new()
            {
                Name = "利息收入",
                CategoryType = CategoryType.Income,
                Level = 1,
                SortOrder = 4,
                Description = "银行存款利息",
                CreatedBy = adminUserId
            },
            new()
            {
                Name = "其他收入",
                CategoryType = CategoryType.Income,
                Level = 1,
                SortOrder = 5,
                Description = "其他杂项收入",
                CreatedBy = adminUserId
            },
            new()
            {
                Name = "人员工资",
                CategoryType = CategoryType.Expense,
                Level = 1,
                SortOrder = 10,
                Description = "员工薪资支出",
                CreatedBy = adminUserId
            },
            new()
            {
                Name = "社保公积金",
                CategoryType = CategoryType.Expense,
                Level = 1,
                SortOrder = 11,
                Description = "社保和住房公积金",
                CreatedBy = adminUserId
            },
            new()
            {
                Name = "办公租金",
                CategoryType = CategoryType.Expense,
                Level = 1,
                SortOrder = 12,
                Description = "办公室租金",
                CreatedBy = adminUserId
            },
            new()
            {
                Name = "办公用品",
                CategoryType = CategoryType.Expense,
                Level = 1,
                SortOrder = 13,
                Description = "日常办公用品采购",
                CreatedBy = adminUserId
            },
            new()
            {
                Name = "差旅费",
                CategoryType = CategoryType.Expense,
                Level = 1,
                SortOrder = 14,
                Description = "出差差旅费用",
                CreatedBy = adminUserId
            },
            new()
            {
                Name = "招待费",
                CategoryType = CategoryType.Expense,
                Level = 1,
                SortOrder = 15,
                Description = "商务招待费",
                CreatedBy = adminUserId
            },
            new()
            {
                Name = "服务器/云服务",
                CategoryType = CategoryType.Expense,
                Level = 1,
                SortOrder = 16,
                Description = "服务器及云计算服务费用",
                CreatedBy = adminUserId
            },
            new()
            {
                Name = "软件/工具订阅",
                CategoryType = CategoryType.Expense,
                Level = 1,
                SortOrder = 17,
                Description = "软件许可及工具订阅费",
                CreatedBy = adminUserId
            },
            new()
            {
                Name = "外包费用",
                CategoryType = CategoryType.Expense,
                Level = 1,
                SortOrder = 18,
                Description = "外包项目费用",
                CreatedBy = adminUserId
            },
            new()
            {
                Name = "税费",
                CategoryType = CategoryType.Expense,
                Level = 1,
                SortOrder = 19,
                Description = "各类税费",
                CreatedBy = adminUserId
            },
            new()
            {
                Name = "水电物业",
                CategoryType = CategoryType.Expense,
                Level = 1,
                SortOrder = 20,
                Description = "水电费及物业管理费",
                CreatedBy = adminUserId
            },
            new()
            {
                Name = "其他支出",
                CategoryType = CategoryType.Expense,
                Level = 1,
                SortOrder = 99,
                Description = "其他杂项支出",
                CreatedBy = adminUserId
            }
        };

        context.Categories.AddRange(categories);
        await context.SaveChangesAsync();
        logger.LogInformation("Default categories created successfully, Count={Count}", categories.Count);
    }

    private static async Task SeedDefaultSiteBrandAsync(AppDbContext context, ILogger logger)
    {
        var existingKeys = await context.SystemConfigs
            .Where(c => c.ConfigKey == SiteBrandDefaults.SiteNameKey || c.ConfigKey == SiteBrandDefaults.SiteNameEnKey)
            .Select(c => c.ConfigKey)
            .ToListAsync();

        var now = DateTime.UtcNow;
        var added = 0;

        if (!existingKeys.Contains(SiteBrandDefaults.SiteNameKey))
        {
            context.SystemConfigs.Add(new SystemConfig
            {
                ConfigKey = SiteBrandDefaults.SiteNameKey,
                ConfigValue = SiteBrandDefaults.SiteName,
                ConfigType = "string",
                Description = SiteBrandDefaults.SiteNameDescription,
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now
            });
            added++;
        }

        if (!existingKeys.Contains(SiteBrandDefaults.SiteNameEnKey))
        {
            context.SystemConfigs.Add(new SystemConfig
            {
                ConfigKey = SiteBrandDefaults.SiteNameEnKey,
                ConfigValue = SiteBrandDefaults.SiteNameEn,
                ConfigType = "string",
                Description = SiteBrandDefaults.SiteNameEnDescription,
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now
            });
            added++;
        }

        if (added == 0)
        {
            logger.LogInformation("Default site brand configs already exist. Skipping site brand seeding.");
            return;
        }

        await context.SaveChangesAsync();
        logger.LogInformation("Default site brand configs created successfully, Count={Count}", added);
    }

    private static string NormalizeUsername(string username)
    {
        return username.Trim().ToUpperInvariant();
    }

    private static string CreateSecurityStamp()
    {
        return Guid.NewGuid().ToString("N");
    }

    private static void ValidateBootstrapAdmin(
        BootstrapAdminOptions bootstrapAdmin,
        AuthOptions authOptions,
        IHostEnvironment? environment)
    {
        var username = bootstrapAdmin.Username?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(username) || username.Length is < 3 or > 50)
        {
            throw new InvalidOperationException("BootstrapAdmin 用户名长度必须在 3 到 50 位之间");
        }

        var fullName = bootstrapAdmin.FullName?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(fullName) || fullName.Length > 100)
        {
            throw new InvalidOperationException("BootstrapAdmin 姓名不能为空且不能超过 100 个字符");
        }

        var password = bootstrapAdmin.Password ?? string.Empty;
        if (string.IsNullOrWhiteSpace(password))
        {
            throw new InvalidOperationException("BootstrapAdmin 密码不能为空");
        }

        if (password.Length < authOptions.MinPasswordLength || password.Length > authOptions.MaxPasswordLength)
        {
            throw new InvalidOperationException(
                $"BootstrapAdmin 密码长度必须在 {authOptions.MinPasswordLength} 到 {authOptions.MaxPasswordLength} 位之间");
        }

        var isDevelopment = environment?.IsDevelopment() == true;
        if (!isDevelopment && DemoCredentials.IsPublishedDemoPassword(password))
        {
            throw new InvalidOperationException(
                "Refusing to seed a bootstrap admin with a published demo password outside Development. Set a unique BOOTSTRAP_ADMIN_PASSWORD.");
        }
    }
}
