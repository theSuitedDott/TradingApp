using TradingApp.Configuration;
using TradingApp.Services.HistoricalData;
using TradingApp.Services.InstitutionalSetup;
using TradingApp.Services.PaperTrading;
using TradingApp.TradingEngine.Indicators;
using TradingApp.TradingEngine.Setup;

namespace TradingApp.Extensions;

/// <summary>
/// DI registration for the institutional setup scanner feature.
/// </summary>
public static class InstitutionalSetupServiceCollectionExtensions
{
    /// <summary>
    /// Registers the institutional setup strategy, the demo mock data source, the opportunity
    /// store, the SignalR notifier and the background scanner.
    /// </summary>
    /// <param name="services">Service collection.</param>
    /// <param name="configuration">Application configuration.</param>
    /// <returns>The same service collection for chaining.</returns>
    public static IServiceCollection AddInstitutionalSetupScanner(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<InstitutionalSetupSettings>(
            configuration.GetSection(InstitutionalSetupSettings.SectionName));

        var settings = configuration
            .GetSection(InstitutionalSetupSettings.SectionName)
            .Get<InstitutionalSetupSettings>() ?? new InstitutionalSetupSettings();

        services.AddSingleton<IRsiCalculator, RsiCalculator>();

        // The detectors use swing strength 1 to match the granularity of the demo mock data.
        services.AddSingleton<IInstitutionalSetupStrategy>(sp => new InstitutionalSetupStrategy(
            new SwingTrendFilter(swingStrength: 1),
            new ThreePushExhaustionDetector(swingStrength: 1),
            new LiquiditySweepDetector(swingStrength: 1),
            new DisplacementDetector(bodyMultiplier: 1.5m, lookback: 10),
            new FairValueGapDetector(),
            new DxyVixConfirmationFilter(new SwingTrendFilter(swingStrength: 1)),
            sp.GetRequiredService<IRsiCalculator>()));

        services.AddSingleton<MockSetupCandleProvider>();
        services.AddSingleton<IMockSetupCandleProvider>(sp => sp.GetRequiredService<MockSetupCandleProvider>());

        // ISetupCandleProvider: real Yahoo data when UseLiveData=true, mock data otherwise.
        if (settings.UseLiveData)
        {
            services.AddScoped<ISetupCandleProvider, RealSetupCandleProvider>();
        }
        else
        {
            services.AddSingleton<ISetupCandleProvider>(sp => sp.GetRequiredService<MockSetupCandleProvider>());
        }

        services.AddSingleton<ITradeOpportunityStore>(_ => new TradeOpportunityStore(settings.MaxOpportunities));
        services.AddSingleton<ISetupOpportunityNotifier, SetupOpportunityNotifier>();
        services.AddSingleton<IExitAlertTracker, ExitAlertTracker>();
        services.AddSingleton<IExitSignalNotifier, ExitSignalNotifier>();
        services.AddSingleton<IInstitutionalSetupScanner, InstitutionalSetupScanner>();
        services.AddScoped<IPositionExitScanner, PositionExitScanner>();
        services.AddScoped<ISetupBacktestService, SetupBacktestService>();
        services.AddScoped<TwelveDataCandleService>();
        services.AddScoped<AlphaVantageCandleService>();
        services.AddScoped<ISetupChartService, SetupChartService>();

        services.AddScoped<ISetupExecutionService, SetupExecutionService>();
        services.AddScoped<IOpenPositionLookup, OpenPositionLookup>();

        services.AddHostedService<InstitutionalSetupScannerWorker>();
        services.AddHostedService<PositionExitScannerWorker>();

        return services;
    }
}
