using Microsoft.Extensions.DependencyInjection;
using FinanceApp.Application.Modules.Identity.Interfaces;
using FinanceApp.Application.Modules.Identity.Services;

namespace FinanceApp.Application.Modules.Identity;

public static class IdentityModuleExtensions
{
    public static IServiceCollection AddIdentityModule(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserManagementService, UserManagementService>();
        services.AddScoped<IAuditLogService, AuditLogService>();

        return services;
    }
}
