using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using TradingApp.Configuration;
using TradingApp.DTOs.Setup;
using TradingApp.Services.OandaOrder;
using TradingApp.Services.PaperTrading;
using TradingApp.TradingEngine.Setup;

namespace TradingApp.Services.InstitutionalSetup;

/// <summary>
/// Orchestrates the institutional setup evaluation, de-duplicates opportunities,
/// stores them in memory and broadcasts them to subscribers.
/// When <c>Oanda:LiveOrderEnabled</c> is true, each 6/6 setup also places a live
/// market order via the OANDA v20 REST API.
/// </summary>
public sealed class InstitutionalSetupScanner(
    IInstitutionalSetupStrategy strategy,
    IMockSetupCandleProvider candleProvider,
    ITradeOpportunityStore opportunityStore,
    ISetupOpportunityNotifier notifier,
    IServiceScopeFactory scopeFactory,
    IOptions<InstitutionalSetupSettings> options,
    IOptions<OandaSettings> oandaOptions,
    ILogger<InstitutionalSetupScanner> logger) : IInstitutionalSetupScanner
{
    private readonly InstitutionalSetupSettings _settings = options.Value;

    /// <inheritdoc />
    public SetupAnalysisDto Analyze(string symbol, string exchange)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(symbol);
        ArgumentException.ThrowIfNullOrWhiteSpace(exchange);

        var input = candleProvider.BuildInput(symbol, exchange, _settings.RsiPeriod);
        var result = strategy.Evaluate(input);
        return SetupMapper.ToAnalysis(result);
    }

    /// <inheritdoc />
    public async Task<TradeOpportunityDto?> ScanAsync(CancellationToken cancellationToken = default)
    {
        // Single scope for all scoped services in this scan cycle.
        await using var scope = scopeFactory.CreateAsyncScope();

        var provider = scope.ServiceProvider.GetRequiredService<ISetupCandleProvider>();
        var input = await provider.BuildInputAsync(
            _settings.Symbol, _settings.Exchange, _settings.RsiPeriod, cancellationToken);

        var result = strategy.Evaluate(input);
        if (!result.IsSetup)
        {
            return null;
        }

        var opportunity = SetupMapper.ToOpportunity(result);
        if (opportunityStore.HasActive(opportunity.Symbol, opportunity.Direction))
        {
            logger.LogDebug(
                "Skipping setup broadcast for {Symbol} — active opportunity already pending.",
                opportunity.Symbol);
            return null;
        }

        var positionLookup = scope.ServiceProvider.GetRequiredService<IOpenPositionLookup>();
        if (await positionLookup.HasOpenPositionAsync(opportunity.Symbol, opportunity.Exchange, cancellationToken))
        {
            logger.LogDebug(
                "Skipping setup broadcast for {Symbol} — open position exists (no repeat buy signals).",
                opportunity.Symbol);
            return null;
        }

        opportunityStore.Add(opportunity);
        await notifier.NotifyOpportunityAsync(opportunity, cancellationToken);

        logger.LogInformation(
            "Institutional setup detected for {Symbol} ({Exchange}) – {Direction}. Entry {Entry}, SL {Stop}, TP {Target}.",
            opportunity.Symbol,
            opportunity.Exchange,
            opportunity.Direction,
            opportunity.EntryPrice,
            opportunity.StopLossPrice,
            opportunity.TakeProfitPrice);

        await TryPlaceLiveOrderAsync(scope, opportunity, cancellationToken);

        return opportunity;
    }

    private async Task TryPlaceLiveOrderAsync(
        IServiceScope scope,
        TradeOpportunityDto opportunity,
        CancellationToken cancellationToken)
    {
        var oanda = oandaOptions.Value;
        if (!oanda.LiveOrderEnabled || !oanda.IsConfigured)
        {
            return;
        }

        var orderService = scope.ServiceProvider.GetRequiredService<IOandaOrderService>();

        var lotsInUnits = Math.Round(oanda.DefaultLots * 100_000m, 0);
        var units = opportunity.Side.Equals("Buy", StringComparison.OrdinalIgnoreCase)
            ? lotsInUnits
            : -lotsInUnits;

        var instrument = TradingApp.Services.HistoricalData.ForexSymbolNormalizer
            .ToOandaInstrument(opportunity.Symbol);

        var result = await orderService.PlaceMarketOrderAsync(
            instrument,
            units,
            opportunity.StopLossPrice,
            opportunity.TakeProfitPrice,
            cancellationToken);

        if (result.IsSuccess)
        {
            logger.LogInformation(
                "Live OANDA order placed for {Symbol}: TradeId={TradeId}, Price={Price}.",
                instrument, result.Value!.TradeId, result.Value.OpenPrice);
        }
        else
        {
            logger.LogError(
                "Live OANDA order failed for {Symbol}: [{Code}] {Message}.",
                instrument, result.ErrorCode, result.ErrorMessage);
        }
    }
}
