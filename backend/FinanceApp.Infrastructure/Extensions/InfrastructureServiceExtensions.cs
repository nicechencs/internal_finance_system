using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using FinanceApp.Application.Modules.Identity.Interfaces;
using FinanceApp.Domain.Configuration;
using FinanceApp.Domain.Interfaces;
using FinanceApp.Infrastructure.Data;
using FinanceApp.Infrastructure.Repositories;
using FinanceApp.Infrastructure.Services;

namespace FinanceApp.Infrastructure.Extensions;

/// <summary>
/// Infrastructure 层 DI 注册扩展方法
/// </summary>
public static class InfrastructureServiceExtensions
{
    /// <summary>
    /// 注册基础设施层服务：DbContext、Repository、UnitOfWork、CurrentUserService、DataPermissionService
    /// </summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<AuthOptions>(configuration.GetSection(AuthOptions.SectionName));

        // Configure DbContext
        var dbProvider = configuration.GetValue<string>("DatabaseProvider") ?? "PostgreSQL";

        if (dbProvider.Equals("SQLite", StringComparison.OrdinalIgnoreCase))
        {
            services.AddDbContext<AppDbContext>(options =>
                options.UseSqlite(configuration.GetConnectionString("DefaultConnection")));
        }
        else
        {
            services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));
        }

        // Register Generic Repository
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

        // Register Unit of Work
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Register Current User Service and Data Permission Service
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IDataPermissionService, DataPermissionService>();
        services.AddScoped<IPasswordService, PasswordService>();
        services.AddScoped<IAuthSessionService, AuthSessionService>();
        services.AddScoped<CookieSessionValidationEvents>();

        return services;
    }
}
