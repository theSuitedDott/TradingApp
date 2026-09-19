using Microsoft.Extensions.Options;
using TradingApp.Configuration;
using TradingApp.TradingEngine.Models;

namespace TradingApp.Services.HistoricalData;

/// <summary>
/// Routes historical candle requests: Twelve Data for forex (primary),
/// Yahoo Finance as fallback and for indices. OANDA only when explicitly configured.
/// </summary>
public sealed class RoutingHistoricalDataService(
    TwelveDataCandleService twelveDataService,
    OandaHistoricalDataService oandaService,
    YahooFinanceHistoricalDataService yahooService,
    IOptions<OandaSettings> oandaOptions,
    ILogger<RoutingHistoricalDataService> logger) : IHistoricalDataService
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<Candle>> GetHistoricalCandlesAsync(
        string symbol,
        string interval,
        string range,
        int? candleCount = null,
        CancellationToken cancellationToken = default)
    {
        if (!ForexSymbolNormalizer.IsOandaForex(symbol))
        {
            return await yahooService.GetHistoricalCandlesAsync(symbol, interval, range, candleCount, cancellationToken);
        }

        if (candleCount is not null)
        {
            return await FetchForexFromYahooAsync(symbol, interval, range, candleCount, cancellationToken);
        }

        var oandaSymbol = ForexSymbolNormalizer.ToOandaInstrument(symbol);

        if (twelveDataService.IsConfigured)
        {
            var targetCount = EstimateCandleCount(range, interval);
            var twelveData = await twelveDataService.GetCandlesAsync(
                oandaSymbol, interval, targetCount, cancellationToken);
            if (twelveData.Count > 0)
            {
                return twelveData;
            }

            logger.LogWarning(
                "Twelve Data returned no candles for {Symbol} — trying fallback providers.",
                oandaSymbol);
        }

        if (oandaOptions.Value.IsConfigured)
        {
            try
            {
                var oanda = await oandaService.GetHistoricalCandlesAsync(
                    symbol, interval, range, cancellationToken: cancellationToken);
                if (oanda.Count > 0)
                {
                    return oanda;
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "OANDA historical fetch failed for {Symbol}, falling back to Yahoo.", symbol);
            }
        }

        return await FetchForexFromYahooAsync(symbol, interval, range, candleCount: null, cancellationToken);
    }

    private async Task<IReadOnlyList<Candle>> FetchForexFromYahooAsync(
        string symbol,
        string interval,
        string range,
        int? candleCount,
        CancellationToken cancellationToken)
    {
        var yahooSymbol = ForexSymbolNormalizer.ToYahooSymbol(symbol);
        var fetchRange = WidenRange(range, minDays: 7);
        return await yahooService.GetHistoricalCandlesAsync(
            yahooSymbol, interval, fetchRange, candleCount, cancellationToken);
    }

    private static int EstimateCandleCount(string range, string interval)
    {
        if (!range.EndsWith("d", StringComparison.OrdinalIgnoreCase) ||
            !int.TryParse(range.AsSpan(0, range.Length - 1), out var days))
        {
            return ChartHistoricalSettings.DefaultCandleCount;
        }

        return interval.Trim().ToLowerInvariant() switch
        {
            "15m" => days * 24 * 4,
            "1h" => days * 24,
            "4h" => days * 6,
            "1d" => days,
            _ => days * 24
        };
    }

    private static string WidenRange(string range, int minDays = 5)
    {
        if (range.EndsWith('d') && int.TryParse(range[..^1], out var days) && days < minDays)
        {
            return $"{minDays}d";
        }

        return range;
    }
}
