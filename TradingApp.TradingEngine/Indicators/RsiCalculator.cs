using TradingApp.TradingEngine.Models;

namespace TradingApp.TradingEngine.Indicators;

/// <summary>
/// Wilder's RSI implementation using smoothed average gains and losses.
/// </summary>
public sealed class RsiCalculator : IRsiCalculator
{
    /// <inheritdoc />
    public IReadOnlyList<decimal?> Calculate(IReadOnlyList<Candle> candles, int period = 14)
    {
        ArgumentNullException.ThrowIfNull(candles);
        ArgumentOutOfRangeException.ThrowIfLessThan(period, 2);

        var result = new decimal?[candles.Count];
        if (candles.Count <= period)
        {
            return result;
        }

        decimal gainSum = 0m;
        decimal lossSum = 0m;
        for (var i = 1; i <= period; i++)
        {
            var change = candles[i].Close - candles[i - 1].Close;
            if (change >= 0)
            {
                gainSum += change;
            }
            else
            {
                lossSum -= change;
            }
        }

        var avgGain = gainSum / period;
        var avgLoss = lossSum / period;
        result[period] = ToRsi(avgGain, avgLoss);

        for (var i = period + 1; i < candles.Count; i++)
        {
            var change = candles[i].Close - candles[i - 1].Close;
            var gain = change > 0 ? change : 0m;
            var loss = change < 0 ? -change : 0m;

            avgGain = ((avgGain * (period - 1)) + gain) / period;
            avgLoss = ((avgLoss * (period - 1)) + loss) / period;
            result[i] = ToRsi(avgGain, avgLoss);
        }

        return result;
    }

    private static decimal ToRsi(decimal avgGain, decimal avgLoss)
    {
        if (avgLoss == 0m)
        {
            return 100m;
        }

        var rs = avgGain / avgLoss;
        return 100m - (100m / (1m + rs));
    }
}
