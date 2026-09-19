using TradingApp.TradingEngine.Models;
using TradingApp.TradingEngine.Setup;

namespace TradingApp.Services.InstitutionalSetup;

/// <summary>
/// Deterministic mock candle source that yields a complete bullish institutional setup:
/// a clear H4 uptrend, a three-push corrective exhaustion with RSI divergence, a liquidity
/// sweep, a displacement candle, a Fair Value Gap, and falling DXY/VIX for confirmation.
/// </summary>
public sealed class MockSetupCandleProvider : IMockSetupCandleProvider, ISetupCandleProvider
{
    private static readonly DateTimeOffset Anchor = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    /// <inheritdoc />
    public InstitutionalSetupInput BuildInput(string symbol, string exchange, int rsiPeriod)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(symbol);
        ArgumentException.ThrowIfNullOrWhiteSpace(exchange);

        return new InstitutionalSetupInput(
            symbol,
            exchange,
            BuildHigherTimeframe(),
            BuildEntryTimeframe(),
            BuildDxy(),
            BuildVix(),
            rsiPeriod);
    }

    /// <inheritdoc />
    public Task<InstitutionalSetupInput> BuildInputAsync(
        string symbol,
        string exchange,
        int rsiPeriod,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(BuildInput(symbol, exchange, rsiPeriod));

    // Clear H4 uptrend: rising swing highs and rising swing lows.
    private static IReadOnlyList<Candle> BuildHigherTimeframe() =>
        FromLowHigh(TimeSpan.FromHours(4),
        [
            (105m, 107m), (106m, 115m), (104m, 109m), (107m, 118m), (106m, 111m),
            (109m, 121m), (108m, 113m), (111m, 124m), (110m, 115m)
        ]);

    // Entry timeframe: a three-push down-correction with diminishing pushes (lows 105 > 103 > 102)
    // and rising RSI lows (bullish divergence), a sweep of the 103 inducement, a displacement
    // candle and a Fair Value Gap (108.5 – 111). Pushes weaken (12 > 11 > 10) while bounces stay
    // strong, so momentum visibly wanes into the final low.
    private static IReadOnlyList<Candle> BuildEntryTimeframe() =>
        FromOhlc(TimeSpan.FromHours(1),
        [
            (109m, 111m, 108m, 110m),
            (110m, 111m, 109m, 110m),
            (110m, 112m, 109m, 111m),
            (111m, 117m, 111m, 116m),     // swing high (origin push 1)
            (107m, 107.5m, 105m, 106m),   // push 1 low = 105 (steep -> RSI low)
            (106m, 114m, 105.5m, 113m),   // swing high (origin push 2) + strong bounce
            (106m, 106.5m, 103m, 105m),   // push 2 low = 103 (weaker)
            (105m, 112m, 104.5m, 111m),   // swing high (origin push 3) + strong bounce
            (110m, 110.5m, 102m, 109m),   // push 3 low = 102 (sweeps 103, closes back -> reclaim)
            (108m, 108.5m, 106m, 107m),   // pullback (FVG anchor candle)
            (107m, 116m, 106.5m, 115m),   // displacement (body ~8, reclaims trend)
            (114m, 116.5m, 111m, 112m)    // forms FVG: prev high 108.5 < this low 111
        ]);

    // Falling DXY supports the long (risk-on).
    private static IReadOnlyList<Candle> BuildDxy() =>
        FromLowHigh(TimeSpan.FromDays(1),
        [
            (98m, 100m), (90m, 99m), (96m, 101m), (87m, 98m), (94m, 99m),
            (84m, 96m), (92m, 97m), (81m, 94m), (90m, 95m)
        ]);

    // Falling VIX supports the long (risk-on).
    private static IReadOnlyList<Candle> BuildVix() =>
        FromLowHigh(TimeSpan.FromDays(1),
        [
            (38m, 40m), (30m, 39m), (36m, 41m), (27m, 38m), (34m, 39m),
            (24m, 36m), (32m, 37m), (21m, 34m), (30m, 35m)
        ]);

    private static IReadOnlyList<Candle> FromLowHigh(TimeSpan step, (decimal Low, decimal High)[] bars)
    {
        var candles = new List<Candle>(bars.Length);
        for (var i = 0; i < bars.Length; i++)
        {
            var (low, high) = bars[i];
            var mid = (low + high) / 2m;
            candles.Add(new Candle(Anchor.Add(step * i), mid, high, low, mid));
        }

        return candles;
    }

    private static IReadOnlyList<Candle> FromOhlc(TimeSpan step, (decimal O, decimal H, decimal L, decimal C)[] bars)
    {
        var candles = new List<Candle>(bars.Length);
        for (var i = 0; i < bars.Length; i++)
        {
            var (o, h, l, c) = bars[i];
            candles.Add(new Candle(Anchor.Add(step * i), o, h, l, c));
        }

        return candles;
    }
}
