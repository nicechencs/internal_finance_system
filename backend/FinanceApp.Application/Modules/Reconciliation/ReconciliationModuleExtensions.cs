using Microsoft.Extensions.DependencyInjection;
using FinanceApp.Application.Modules.Reconciliation.Interfaces;
using FinanceApp.Application.Modules.Reconciliation.Services;

namespace FinanceApp.Application.Modules.Reconciliation;

public static class ReconciliationModuleExtensions
{
    public static IServiceCollection AddReconciliationModule(this IServiceCollection services)
    {
        services.AddScoped<IImportService, ImportService>();
        return services;
    }
}
