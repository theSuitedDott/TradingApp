using TradingApp.TradingEngine.Models;
using TradingApp.TradingEngine.Setup;
using Xunit;

namespace TradingApp.TradingEngine.Tests.Setup;

public sealed class ThreePushExhaustionDetectorTests
{
    // Down-correction with three diminishing pushes:
    // pushes 50->40 (10), 46->38 (8), 44->37 (7); lower lows 40 > 38 > 37.
    private static IReadOnlyList<Candle> DownCorrection() =>
    [
        TestCandles.Of(0, 41m, 44m),
        TestCandles.Of(1, 45m, 50m),
        TestCandles.Of(2, 40m, 43m),
        TestCandles.Of(3, 41m, 46m),
        TestCandles.Of(4, 38m, 42m),
        TestCandles.Of(5, 39m, 44m),
        TestCandles.Of(6, 37m, 41m),
        TestCandles.Of(7, 39m, 40m)
    ];

    [Fact]
    public void Detect_WithDiminishingPushesAndBullishDivergence_ReturnsResult()
    {
        var candles = DownCorrection();
        // RSI prints higher lows (28 -> 33 -> 39) at the three swing lows (indices 2, 4, 6).
        decimal?[] rsi = [50m, 45m, 28m, 42m, 33m, 44m, 39m, 50m];
        var sut = new ThreePushExhaustionDetector(swingStrength: 1);

        var result = sut.Detect(candles, MarketBias.Bullish, rsi);

        Assert.NotNull(result);
        Assert.Equal(3, result!.Pushes.Count);
        Assert.Equal(6, result.LastPush.Index);
    }

    [Fact]
    public void Detect_WithoutRsiDivergence_ReturnsNull()
    {
        var candles = DownCorrection();
        // RSI falls together with price -> no divergence.
        decimal?[] rsi = [50m, 45m, 40m, 42m, 35m, 44m, 30m, 50m];
        var sut = new ThreePushExhaustionDetector(swingStrength: 1);

        var result = sut.Detect(candles, MarketBias.Bullish, rsi);

        Assert.Null(result);
    }

    [Fact]
    public void Detect_WithNeutralBias_ReturnsNull()
    {
        var candles = DownCorrection();
        decimal?[] rsi = [50m, 45m, 28m, 42m, 33m, 44m, 39m, 50m];
        var sut = new ThreePushExhaustionDetector(swingStrength: 1);

        var result = sut.Detect(candles, MarketBias.Neutral, rsi);

        Assert.Null(result);
    }
}
