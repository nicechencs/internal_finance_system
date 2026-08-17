using Mapster;
using MapsterMapper;
using Microsoft.Extensions.DependencyInjection;
using FinanceApp.Application.Mappings;
using FinanceApp.Application.Modules.Identity;
using FinanceApp.Application.Modules.MasterData;
using FinanceApp.Application.Modules.TransactionProcessing;
using FinanceApp.Application.Modules.Reconciliation;
using FinanceApp.Application.Modules.FinanceSettlement;
using FinanceApp.Application.Modules.Reporting;

namespace FinanceApp.Application.Extensions;

/// <summary>
/// Application 层 DI 注册扩展方法
/// </summary>
public static class ApplicationServiceExtensions
{
    /// <summary>
    /// 注册应用层服务：Mapster + 所有业务模块
    /// </summary>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // ── 跨模块基础设施 ──
        var config = MappingConfig.Configure();
        services.AddSingleton(config);
        services.AddScoped<IMapper, ServiceMapper>();

        // ── 业务模块注册 ──
        services.AddIdentityModule();
        services.AddMasterDataModule();
        services.AddTransactionProcessingModule();
        services.AddReconciliationModule();
        services.AddFinanceSettlementModule();
        services.AddReportingModule();

        return services;
    }
}
