using Microsoft.Extensions.DependencyInjection;
using FinanceApp.Application.Modules.MasterData.Interfaces;
using FinanceApp.Application.Modules.MasterData.Services;

namespace FinanceApp.Application.Modules.MasterData;

public static class MasterDataModuleExtensions
{
    public static IServiceCollection AddMasterDataModule(this IServiceCollection services)
    {
        services.AddScoped<IMasterDataReferenceGuard, MasterDataReferenceGuard>();
        services.AddScoped<IAccountService, AccountService>();
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<IRuleService, RuleService>();
        services.AddScoped<ICustomerService, CustomerService>();
        services.AddScoped<ISupplierService, SupplierService>();
        services.AddScoped<IPersonService, PersonService>();
        services.AddScoped<IProjectFinancialRecalculationService, ProjectFinancialRecalculationService>();
        services.AddScoped<IProjectService, ProjectService>();
        services.AddScoped<IConfigService, ConfigService>();
        services.AddScoped<ITagService, TagService>();
        services.AddScoped<ITagAnalyticsService, TagAnalyticsService>();
        services.AddScoped<ITagRuleService, TagRuleService>();
        services.AddScoped<IFixedDepositService, FixedDepositService>();

        return services;
    }
}
