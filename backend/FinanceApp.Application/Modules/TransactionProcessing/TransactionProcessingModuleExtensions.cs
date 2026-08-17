using Microsoft.Extensions.DependencyInjection;
using FinanceApp.Application.Modules.TransactionProcessing.Interfaces;
using FinanceApp.Application.Modules.TransactionProcessing.Services;

namespace FinanceApp.Application.Modules.TransactionProcessing;

public static class TransactionProcessingModuleExtensions
{
    public static IServiceCollection AddTransactionProcessingModule(this IServiceCollection services)
    {
        // 注意注册顺序：先注册依赖服务，再注册 TransactionService
        services.AddScoped<IAllocationService, AllocationService>();
        services.AddScoped<IAccountBalanceService, AccountBalanceService>();
        services.AddScoped<ITransactionQueryService, TransactionQueryService>();
        services.AddScoped<ITransferService, TransferService>();
        services.AddScoped<ITransactionStatisticsService, TransactionStatisticsService>();
        services.AddScoped<ITransactionService, TransactionService>();

        return services;
    }
}
