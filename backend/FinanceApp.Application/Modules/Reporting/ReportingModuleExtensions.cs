using Microsoft.Extensions.DependencyInjection;
using FinanceApp.Application.Modules.Reporting.Interfaces;
using FinanceApp.Application.Modules.Reporting.Services;

namespace FinanceApp.Application.Modules.Reporting;

public static class ReportingModuleExtensions
{
    public static IServiceCollection AddReportingModule(this IServiceCollection services)
    {
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<IProjectFinancialSummaryService, ProjectFinancialSummaryService>();
        services.AddScoped<IReportService, ReportService>();
        return services;
    }
}
