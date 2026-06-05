using Microsoft.Extensions.Options;
using TradingApp.Configuration;
using TradingApp.DTOs.Setup;
using TradingApp.TradingEngine.Setup;

namespace TradingApp.Services.InstitutionalSetup;

/// <summary>
/// Orchestrates the institutional setup evaluation, de-duplicates opportunities,
/// stores them in memory and broadcasts them to subscribers.
/// </summary>
public sealed class InstitutionalSetupScanner(
    IInstitutionalSetupStrategy strategy,
    IMockSetupCandleProvider candleProvider,
    ITradeOpportunityStore opportunityStore,
    ISetupOpportunityNotifier notifier,
    IOptions<InstitutionalSetupSettings> options,
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
        var input = candleProvider.BuildInput(_settings.Symbol, _settings.Exchange, _settings.RsiPeriod);
        var result = strategy.Evaluate(input);
        if (!result.IsSetup)
        {
            return null;
        }

        var opportunity = SetupMapper.ToOpportunity(result);
        if (opportunityStore.HasActive(opportunity.Symbol, opportunity.Direction))
        {
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

        return opportunity;
    }
}
