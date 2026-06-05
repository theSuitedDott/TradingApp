using TradingApp.TradingEngine.Models;

namespace TradingApp.TradingEngine.Setup;

/// <summary>
/// Detects a three-push corrective exhaustion with RSI divergence.
/// For a bullish bias the correction is a down-move forming three lower swing lows
/// whose drop sizes diminish while RSI prints higher lows (bullish divergence).
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
            ? DetectBullish(swings, rsi)
            : DetectBearish(swings, rsi);
    }

    private static ExhaustionResult? DetectBullish(IReadOnlyList<SwingPoint> swings, IReadOnlyList<decimal?> rsi)
    {
        var lows = swings.Where(s => !s.IsHigh).TakeLast(3).ToList();
        if (lows.Count < 3)
        {
            return null;
        }

        // Price prints progressively lower lows (the correction extends down).
        if (!(lows[0].Price > lows[1].Price && lows[1].Price > lows[2].Price))
        {
            return null;
        }

        if (!TryGetRsi(rsi, lows, out var rsiValues))
        {
            return null;
        }

        // Bullish RSI divergence: momentum prints higher lows against falling price.
        if (!(rsiValues[0] < rsiValues[1] && rsiValues[1] < rsiValues[2]))
        {
            return null;
        }

        if (!PushesAreDiminishing(swings, lows, isHighPush: false))
        {
            return null;
        }

        return new ExhaustionResult(
            lows,
            $"Three diminishing down-pushes with bullish RSI divergence " +
            $"({rsiValues[0]:F1} → {rsiValues[1]:F1} → {rsiValues[2]:F1}).");
    }

    private static ExhaustionResult? DetectBearish(IReadOnlyList<SwingPoint> swings, IReadOnlyList<decimal?> rsi)
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

        if (!TryGetRsi(rsi, highs, out var rsiValues))
        {
            return null;
        }

        // Bearish RSI divergence: momentum prints lower highs against rising price.
        if (!(rsiValues[0] > rsiValues[1] && rsiValues[1] > rsiValues[2]))
        {
            return null;
        }

        if (!PushesAreDiminishing(swings, highs, isHighPush: true))
        {
            return null;
        }

        return new ExhaustionResult(
            highs,
            $"Three diminishing up-pushes with bearish RSI divergence " +
            $"({rsiValues[0]:F1} → {rsiValues[1]:F1} → {rsiValues[2]:F1}).");
    }

    private static bool TryGetRsi(
        IReadOnlyList<decimal?> rsi,
        IReadOnlyList<SwingPoint> points,
        out decimal[] values)
    {
        values = new decimal[points.Count];
        for (var i = 0; i < points.Count; i++)
        {
            var value = rsi[points[i].Index];
            if (value is null)
            {
                return false;
            }

            values[i] = value.Value;
        }

        return true;
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
