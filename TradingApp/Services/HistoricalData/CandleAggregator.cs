using TradingApp.TradingEngine.Models;

namespace TradingApp.Services.HistoricalData;

public static class CandleAggregator
{
    public static IReadOnlyList<Candle> Aggregate(IReadOnlyList<Candle> source, TimeSpan targetInterval)
    {
        if (source.Count == 0) return Array.Empty<Candle>();

        var result = new List<Candle>();
        var currentPeriodStart = GetPeriodStart(source[0].OpenTime, targetInterval);
        
        decimal open = source[0].Open;
        decimal high = source[0].High;
        decimal low = source[0].Low;
        decimal close = source[0].Close;

        for (int i = 1; i < source.Count; i++)
        {
            var candle = source[i];
            var periodStart = GetPeriodStart(candle.OpenTime, targetInterval);

            if (periodStart > currentPeriodStart)
            {
                // Close current period
                result.Add(new Candle(currentPeriodStart, open, high, low, close));
                
                // Start new period
                currentPeriodStart = periodStart;
                open = candle.Open;
                high = candle.High;
                low = candle.Low;
                close = candle.Close;
            }
            else
            {
                // Update current period
                high = Math.Max(high, candle.High);
                low = Math.Min(low, candle.Low);
                close = candle.Close;
            }
        }

        // Add last period
        result.Add(new Candle(currentPeriodStart, open, high, low, close));

        return result;
    }

    private static DateTimeOffset GetPeriodStart(DateTimeOffset time, TimeSpan interval)
    {
        var ticks = time.Ticks / interval.Ticks * interval.Ticks;
        return new DateTimeOffset(ticks, time.Offset);
    }
}
