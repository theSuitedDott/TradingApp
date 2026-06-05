using TradingApp.TradingEngine.Models;
using TradingApp.TradingEngine.Setup;
using Xunit;

namespace TradingApp.TradingEngine.Tests.Setup;

public sealed class LiquiditySweepDetectorTests
{
    [Fact]
    public void Detect_WhenFinalLowSweepsAndReclaims_ReturnsResult()
    {
        IReadOnlyList<Candle> candles =
        [
            TestCandles.Of(0, 50m, 55m),
            TestCandles.Of(1, 52m, 60m),
            TestCandles.Of(2, 45m, 53m), // inducement swing low at 45
            TestCandles.Of(3, 48m, 58m),
            TestCandles.Of(4, 42m, 50m), // sweep low at 42 (< 45)
            TestCandles.Ohlc(5, 48m, 54m, 46m, 52m), // closes back above 45
            TestCandles.Of(6, 49m, 56m)
        ];
        var sut = new LiquiditySweepDetector(swingStrength: 1);

        var result = sut.Detect(candles, MarketBias.Bullish);

        Assert.NotNull(result);
        Assert.Equal(45m, result!.SweptLevel);
        Assert.Equal(42m, result.ExtremePrice);
    }

    [Fact]
    public void Detect_WhenNoSweep_ReturnsNull()
    {
        IReadOnlyList<Candle> candles =
        [
            TestCandles.Of(0, 50m, 55m),
            TestCandles.Of(1, 52m, 60m),
            TestCandles.Of(2, 45m, 53m),
            TestCandles.Of(3, 48m, 58m),
            TestCandles.Of(4, 47m, 52m), // higher low, no sweep
            TestCandles.Of(5, 49m, 56m),
            TestCandles.Of(6, 50m, 57m)
        ];
        var sut = new LiquiditySweepDetector(swingStrength: 1);

        var result = sut.Detect(candles, MarketBias.Bullish);

        Assert.Null(result);
    }
}
