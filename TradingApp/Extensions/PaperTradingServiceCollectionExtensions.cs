using TradingApp.Configuration;
using TradingApp.Services.PaperTrading;
using TradingApp.Trading.Execution;

namespace TradingApp.Extensions;

/// <summary>
/// Registers paper trading and execution services.
/// </summary>
public static class PaperTradingServiceCollectionExtensions
{
    /// <summary>
    /// Adds paper trading, market processing, and order execution ports.
    /// </summary>
    public static IServiceCollection AddPaperTrading(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<PaperTradingSettings>(configuration.GetSection(PaperTradingSettings.SectionName));

        services.AddSingleton<IMarketQuoteStore, MarketQuoteStore>();
        services.AddScoped<ICommissionCalculator, CommissionCalculator>();
        services.AddScoped<IPnlCalculator, PnlCalculator>();
        services.AddScoped<IPortfolioSettlementService, PortfolioSettlementService>();
        services.AddScoped<IPortfolioValuationService, PortfolioValuationService>();
        services.AddScoped<IPositionRiskMonitor, PositionRiskMonitor>();
        services.AddScoped<IPaperRiskExitService, PaperRiskExitService>();
        services.AddScoped<IMarketQuoteProcessor, MarketQuoteProcessor>();
        services.AddScoped<IPaperTradeAccountService, PaperTradeAccountService>();
        services.AddScoped<IPaperOrderService, PaperOrderService>();
        services.AddScoped<IMarketDataService, MarketDataService>();
        services.AddScoped<IPaperTradingNotifier, PaperTradingNotifier>();

        services.AddScoped<IOrderExecutor, PaperOrderExecutor>();
        services.AddScoped<IOrderExecutor, BrokerOrderExecutor>();
        services.AddScoped<IOrderExecutorFactory, OrderExecutorFactory>();

        return services;
    }
}
