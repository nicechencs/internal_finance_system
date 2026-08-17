using Microsoft.Extensions.DependencyInjection;
using FinanceApp.Application.Modules.FinanceSettlement.Interfaces;
using FinanceApp.Application.Modules.FinanceSettlement.Services;

namespace FinanceApp.Application.Modules.FinanceSettlement;

public static class FinanceSettlementModuleExtensions
{
    public static IServiceCollection AddFinanceSettlementModule(this IServiceCollection services)
    {
        services.AddScoped<ISettlementTransactionBindingService, SettlementTransactionBindingService>();
        services.AddScoped<IReceivableService, ReceivableService>();
        services.AddScoped<IPayableService, PayableService>();
        services.AddScoped<IPayableTypeService, PayableTypeService>();
        services.AddScoped<IReceivableTypeService, ReceivableTypeService>();
        services.AddScoped<IDataMigrationService, DataMigrationService>();
        services.AddScoped<TransactionAllocationHelper>();
        services.AddScoped<ILinkService, LinkService>();
        services.AddScoped<ISettlementCandidateService, SettlementCandidateService>();
        return services;
    }

}
