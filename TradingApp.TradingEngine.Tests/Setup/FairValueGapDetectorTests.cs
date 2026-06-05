using TradingApp.TradingEngine.Models;
using TradingApp.TradingEngine.Setup;
using Xunit;

namespace TradingApp.TradingEngine.Tests.Setup;

public sealed class FairValueGapDetectorTests
{
    [Fact]
    public void Detect_WithBullishImbalance_ReturnsZone()
    {
        IReadOnlyList<Candle> candles =
        [
            TestCandles.Ohlc(0, 99m, 100m, 98m, 99.5m),   // before: high 100
            TestCandles.Ohlc(1, 100m, 104.5m, 99.8m, 104m), // displacement
            TestCandles.Ohlc(2, 104m, 105m, 103m, 104.5m)  // after: low 103
        ];
        var sut = new FairValueGapDetector();

        var zone = sut.Detect(candles, MarketBias.Bullish, displacementIndex: 1);

        Assert.NotNull(zone);
        Assert.Equal(100m, zone!.Lower);
        Assert.Equal(103m, zone.Upper);
    }

    [Fact]
    public void Detect_WithoutImbalance_ReturnsNull()
    {
        IReadOnlyList<Candle> candles =
        [
            TestCandles.Ohlc(0, 99m, 103m, 98m, 102m),    // before high 103 overlaps
            TestCandles.Ohlc(1, 102m, 104.5m, 101m, 104m),
            TestCandles.Ohlc(2, 104m, 105m, 102m, 104.5m) // after low 102 < before high 103
        ];
        var sut = new FairValueGapDetector();

        var zone = sut.Detect(candles, MarketBias.Bullish, displacementIndex: 1);

        Assert.Null(zone);
    }
}
