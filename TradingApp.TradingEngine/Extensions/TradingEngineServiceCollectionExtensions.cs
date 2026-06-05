using Microsoft.Extensions.DependencyInjection;
using TradingApp.TradingEngine.Aggregation;
using TradingApp.TradingEngine.Risk;

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

        return services;
    }
}
