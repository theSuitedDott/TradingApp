using TradingApp.TradingEngine.Models;

namespace TradingApp.TradingEngine.Setup;

/// <summary>
/// Detects a three-push corrective exhaustion without RSI divergence.
/// For a bullish bias the correction forms three lower swing lows with diminishing push sizes;
/// entry is at the third push low, take-profit at the prior swing high.
/// The mirror logic applies for a bearish bias.
/// </summary>
public sealed class ThreePushExhaustionDetector : IExhaustionDetector
{
    private readonly int _swingStrength;

    /// <summary>
    /// Creates a three-push exhaustion detector.
    /// </summary>
    /// <param name="swingStrength">Neighbour count used for fractal swing detection.</param>
    public ThreePushExhaustionDetector(int swingStrength = 2)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(swingStrength, 1);
        _swingStrength = swingStrength;
    }

    /// <inheritdoc />
    public ExhaustionResult? Detect(IReadOnlyList<Candle> candles, MarketBias bias, IReadOnlyList<decimal?> rsi)
    {
        ArgumentNullException.ThrowIfNull(candles);
        ArgumentNullException.ThrowIfNull(rsi);
        if (rsi.Count != candles.Count)
        {
            throw new ArgumentException("RSI series must align with candles.", nameof(rsi));
        }

        if (bias == MarketBias.Neutral)
        {
            return null;
        }

        var swings = SwingScanner.FindSwings(candles, _swingStrength);
        return bias == MarketBias.Bullish
            ? DetectBullish(swings)
            : DetectBearish(swings);
    }

    private static ExhaustionResult? DetectBullish(IReadOnlyList<SwingPoint> swings)
    {
        var lows = swings.Where(s => !s.IsHigh).TakeLast(3).ToList();
        if (lows.Count < 3)
        {
            return null;
        }

        if (!(lows[0].Price > lows[1].Price && lows[1].Price > lows[2].Price))
        {
            return null;
        }

        if (!PushesAreDiminishing(swings, lows, isHighPush: false))
        {
            return null;
        }

        var priorPeak = FindPrecedingOpposite(swings, lows[0].Index, wantHigh: true);
        if (priorPeak is null || priorPeak.Price <= lows[2].Price)
        {
            return null;
        }

        return new ExhaustionResult(
            lows,
            priorPeak.Price,
            $"3-Push-Korrektur abgeschlossen: Einstieg am 3. Tief {lows[2].Price:F4}, " +
            $"Ziel vorheriges Hoch {priorPeak.Price:F4}.");
    }

    private static ExhaustionResult? DetectBearish(IReadOnlyList<SwingPoint> swings)
    {
        var highs = swings.Where(s => s.IsHigh).TakeLast(3).ToList();
        if (highs.Count < 3)
        {
            return null;
        }

        if (!(highs[0].Price < highs[1].Price && highs[1].Price < highs[2].Price))
        {
            return null;
        }

        if (!PushesAreDiminishing(swings, highs, isHighPush: true))
        {
            return null;
        }

        var priorTrough = FindPrecedingOpposite(swings, highs[0].Index, wantHigh: false);
        if (priorTrough is null || priorTrough.Price >= highs[2].Price)
        {
            return null;
        }

        return new ExhaustionResult(
            highs,
            priorTrough.Price,
            $"3-Push-Korrektur abgeschlossen: Einstieg am 3. Hoch {highs[2].Price:F4}, " +
            $"Ziel vorheriges Tief {priorTrough.Price:F4}.");
    }

    private static bool PushesAreDiminishing(
        IReadOnlyList<SwingPoint> swings,
        IReadOnlyList<SwingPoint> pushPoints,
        bool isHighPush)
    {
        var sizes = new List<decimal>(pushPoints.Count);
        foreach (var push in pushPoints)
        {
            var origin = FindPrecedingOpposite(swings, push.Index, wantHigh: !isHighPush);
            if (origin is null)
            {
                return false;
            }

            var size = isHighPush ? push.Price - origin.Price : origin.Price - push.Price;
            if (size <= 0)
            {
                return false;
            }

            sizes.Add(size);
        }

        for (var i = 1; i < sizes.Count; i++)
        {
            if (sizes[i] >= sizes[i - 1])
            {
                return false;
            }
        }

        return true;
    }

    private static SwingPoint? FindPrecedingOpposite(
        IReadOnlyList<SwingPoint> swings,
        int beforeIndex,
        bool wantHigh)
    {
        SwingPoint? found = null;
        foreach (var swing in swings)
        {
            if (swing.Index >= beforeIndex)
            {
                break;
            }

            if (swing.IsHigh == wantHigh)
            {
                found = swing;
            }
        }

        return found;
    }
}
