using Microsoft.Extensions.DependencyInjection;
using TradingApp.TradingEngine.Aggregation;
using TradingApp.TradingEngine.Indicators;
using TradingApp.TradingEngine.Risk;
using TradingApp.TradingEngine.Setup;

namespace TradingApp.TradingEngine.Extensions;

/// <summary>
/// DI registration for the modular trading engine.
/// </summary>
public static class TradingEngineServiceCollectionExtensions
{
    /// <summary>
    /// Registers broker-agnostic trading engine services.
    /// </summary>
    /// <param name="services">Service collection.</param>
    /// <returns>The same service collection for chaining.</returns>
    public static IServiceCollection AddTradingEngine(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<IPositionRiskEvaluator, StopLossTakeProfitEvaluator>();
        services.AddSingleton<ITradingSignalAggregator, ConsensusTradingSignalAggregator>();
        services.AddSingleton<ITradingEngine, TradingEngineService>();

        services.AddInstitutionalSetup();

        return services;
    }

    /// <summary>
    /// Registers the institutional setup strategy and its condition detectors.
    /// </summary>
    /// <param name="services">Service collection.</param>
    /// <returns>The same service collection for chaining.</returns>
    public static IServiceCollection AddInstitutionalSetup(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<IRsiCalculator, RsiCalculator>();
        services.AddSingleton<ITrendFilter, SwingTrendFilter>();
        services.AddSingleton<IExhaustionDetector, ThreePushExhaustionDetector>();
        services.AddSingleton<ILiquiditySweepDetector, LiquiditySweepDetector>();
        services.AddSingleton<IDisplacementDetector, DisplacementDetector>();
        services.AddSingleton<IFairValueGapDetector, FairValueGapDetector>();
        services.AddSingleton<IMacroConfirmationFilter, DxyVixConfirmationFilter>();

        services.AddSingleton<IInstitutionalSetupStrategy>(sp => new InstitutionalSetupStrategy(
            sp.GetRequiredService<ITrendFilter>(),
            sp.GetRequiredService<IExhaustionDetector>(),
            sp.GetRequiredService<ILiquiditySweepDetector>(),
            sp.GetRequiredService<IDisplacementDetector>(),
            sp.GetRequiredService<IFairValueGapDetector>(),
            sp.GetRequiredService<IMacroConfirmationFilter>(),
            sp.GetRequiredService<IRsiCalculator>(),
            timeProvider: sp.GetService<TimeProvider>() ?? TimeProvider.System));

        return services;
    }
}
